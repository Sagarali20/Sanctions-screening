using Nec.Web.Models;
using Raffinert.FuzzySharp;

namespace Nec.Web.Config
{
    public static class FuzzySearch
    {

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "THE","OF","EL","LA","LE","DE","DA","AND","INC","LTD","LLC","COMPANY"
        };

        private static string[] TokenizeWithoutStopWords(string text)
        {
            return Normalizer.Normalize(text)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !StopWords.Contains(t))
                .ToArray();
        }
        // Levenshtein Distance Implementation
        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] dp = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) dp[i, 0] = i;
            for (int j = 0; j <= m; j++) dp[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost);
                }
            }

            return dp[n, m];
        }
        public static int Levenshtein(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length, m = t.Length;
            int[] prev = new int[m + 1], curr = new int[m + 1];

            for (int j = 0; j <= m; j++) prev[j] = j;

            for (int i = 0; i < n; i++)
            {
                curr[0] = i + 1;
                for (int j = 0; j < m; j++)
                {
                    int cost = s[i] == t[j] ? 0 : 1;
                    curr[j + 1] = Math.Min(
                                    Math.Min(curr[j] + 1, prev[j + 1] + 1),
                                    prev[j] + cost);
                }
                (prev, curr) = (curr, prev);
            }

            return prev[m];
        }

        // Convert Levenshtein distance to similarity score 0-100
        public static int GetSimilarity(string s1, string s2)
        {
            int distance = Levenshtein(s1, s2);
            int maxLen = Math.Max(s1.Length, s2.Length);
            if (maxLen == 0) return 100;
            return (int)((1.0 - (double)distance / maxLen) * 100);
        }
        public static double Similarity(string s1, string s2)
        {
            if (s1 == s2) return 1.0;

            int l1 = s1.Length, l2 = s2.Length;
            if (l1 == 0 || l2 == 0) return 0.0;

            int matchDistance = Math.Max(l1, l2) / 2 - 1;

            bool[] s1Matches = new bool[l1];
            bool[] s2Matches = new bool[l2];

            int matches = 0;

            for (int i = 0; i < l1; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, l2);

                for (int j = start; j < end; j++)
                {
                    if (s2Matches[j]) continue;
                    if (s1[i] != s2[j]) continue;

                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0) return 0.0;

            double t = 0;
            int k = 0;
            for (int i = 0; i < l1; i++)
            {
                if (!s1Matches[i]) continue;
                while (!s2Matches[k]) k++;
                if (s1[i] != s2[k]) t++;
                k++;
            }
            t /= 2.0;

            double jaro = ((matches / (double)l1) + (matches / (double)l2) + ((matches - t) / matches)) / 3.0;

            // Jaro-Winkler adjustment
            int prefix = 0;
            for (int i = 0; i < Math.Min(4, Math.Min(l1, l2)); i++)
            {
                if (s1[i] == s2[i]) prefix++;
                else break;
            }

            return jaro + 0.1 * prefix * (1 - jaro);
        }

        public static int SimilarityScore(string s1, string s2)
        {
            return (int)(Similarity(s1, s2) * 100);
        }

        public static int CalculateScore(string inputName, string recordName)
        {
            var inputTokens = TokenizeWithoutStopWords(inputName);
            var recordTokens = TokenizeWithoutStopWords(recordName);

            if (inputTokens.Length == 0 || recordTokens.Length == 0)
                return 0;

            int totalScore = 0;
            int matchedTokens = 0;

            foreach (var inputToken in inputTokens)
            {
                int bestScore = 0;

                foreach (var recToken in recordTokens)
                {
                    int lev = GetSimilarity(inputToken, recToken);
                    int jw = SimilarityScore(inputToken, recToken);

                    int score = Math.Max(lev, jw);
                    bestScore = Math.Max(bestScore, score);
                }

                if (bestScore > 0)
                {
                    totalScore += bestScore;
                    matchedTokens++;
                }
            }

            return matchedTokens == 0 ? 0 : totalScore / matchedTokens;
        }


        // Search with fuzzy level
        public static List<SanctionEntity> Search(List<SanctionEntity> dataset, string query, int? fuzzyLevel)
        {
            query = query.ToUpper();
            double threshold = fuzzyLevel switch
            {
                0 => 1.0,   // Exact match only
                1 => 0.90,  // Loose match (e.g. 75%+ similarity)
                2 => 0.70,  // Aggressive fuzzy (60%+ similarity)
                _ => 1.0
            };

            // Parallel search for performance, calculating similarity
            var results = dataset
                .AsParallel()
                .Select(p =>
                {
                    string name = p.name.ToUpper();
                    double sim = fuzzyLevel == 0 ? (name == query ? 1.0 : 0.0) : Similarity(name, query);
                    return new { Entity = p, Similarity = sim };
                })
                .Where(x => x.Similarity >= threshold)
                .OrderByDescending(x => x.Similarity)  // Descending order of similarity
                .ThenByDescending(x => x.Entity.source_id)  // Descending by source_id
                .Select(x => x.Entity)
                .ToList();

            return results;
        }

    }
}
