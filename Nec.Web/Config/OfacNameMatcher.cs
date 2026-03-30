using System.Text.RegularExpressions;

namespace Nec.Web.Config
{
    public static class OfacNameMatcher
    {
        private static readonly HashSet<string> StopWords = new()
    {
        "THE","AL","OF","EL","LA","LE","DE","DA","AND","INC","LTD","LLC","COMPANY"
    };

        // -----------------------
        // Normalize text
        // -----------------------
        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            value = value.ToUpperInvariant();
            value = Regex.Replace(value, @"[^A-Z0-9\s]", " ");
            value = Regex.Replace(value, @"\s+", " ").Trim();

            var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                             .Where(w => !StopWords.Contains(w));

            return string.Join(" ", words);
        }

        // -----------------------
        // Compute OFAC-like score
        // -----------------------
        public static int ComputeScore(string input, string candidate)
        {
            string normInput = Normalize(input);
            string normCandidate = Normalize(candidate);

            if (string.IsNullOrEmpty(normInput) || string.IsNullOrEmpty(normCandidate))
                return 0;

            var inputWords = normInput.Split(' ');
            var candidateWords = normCandidate.Split(' ');

            double totalScore = 0;
            int strongMatches = 0;

            foreach (var w1 in inputWords)
            {
                double bestWordScore = 0;

                foreach (var w2 in candidateWords)
                {
                    double score = ScoreWord(w1, w2);
                    if (score > bestWordScore)
                        bestWordScore = score;
                }

                totalScore += bestWordScore;
                if (bestWordScore >= 0.85) strongMatches++;
            }

            double avgScore = totalScore / inputWords.Length;

            // Phrase-level boost: capped to 90%
            if (strongMatches >= inputWords.Length - 1)
                avgScore = Math.Max(avgScore, 0.90);

            // Extra word penalty
            int extraWords = Math.Abs(inputWords.Length - candidateWords.Length);
            avgScore -= extraWords * 0.03;

            int finalScore = (int)Math.Round(Math.Min(Math.Max(avgScore * 100, 0), 100));
            return finalScore;
        }

        // -----------------------
        // Score a single word
        // -----------------------
        private static double ScoreWord(string a, string b)
        {
            a = a.ToUpperInvariant();
            b = b.ToUpperInvariant();

            double score = 0;

            if (a == b) score += 0.40;                       // exact match
            if (a.Contains(b) || b.Contains(a)) score += 0.20; // partial match
            if (Soundex(a) == Soundex(b)) score += 0.15;     // phonetic
            score += JaroWinkler(a, b) * 0.25;              // fuzzy similarity

            if (a.Length > 3 && b.Length > 3 && a.Substring(0, 4) == b.Substring(0, 4))
                score = Math.Max(score, 0.85);              // root-word boost

            return Math.Min(score, 1.0);
        }

        // -----------------------
        // Soundex
        // -----------------------
        public static string Soundex(string word)
        {
            if (string.IsNullOrEmpty(word)) return "";

            word = word.ToUpperInvariant();
            char first = word[0];

            string tail = Regex.Replace(word.Substring(1), "[AEIOUYHW]", "0");
            tail = Regex.Replace(tail, "B|F|P|V", "1");
            tail = Regex.Replace(tail, "C|G|J|K|Q|S|X|Z", "2");
            tail = Regex.Replace(tail, "D|T", "3");
            tail = Regex.Replace(tail, "L", "4");
            tail = Regex.Replace(tail, "M|N", "5");
            tail = Regex.Replace(tail, "R", "6");

            tail = Regex.Replace(tail, @"(\d)\1+", "$1");
            tail = tail.Replace("0", "");

            return (first + tail + "0000").Substring(0, 4);
        }

        // -----------------------
        // Jaro-Winkler similarity
        // -----------------------
        public static double JaroWinkler(string s1, string s2)
        {
            if (s1 == s2) return 1.0;
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;

            int matchDistance = Math.Max(s1.Length, s2.Length) / 2 - 1;
            bool[] s1Matches = new bool[s1.Length];
            bool[] s2Matches = new bool[s2.Length];

            int matches = 0;
            int transpositions = 0;

            for (int i = 0; i < s1.Length; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, s2.Length);
                for (int j = start; j < end; j++)
                {
                    if (!s2Matches[j] && s1[i] == s2[j])
                    {
                        s1Matches[i] = s2Matches[j] = true;
                        matches++;
                        break;
                    }
                }
            }

            if (matches == 0) return 0;

            int k = 0;
            for (int i = 0; i < s1.Length; i++)
            {
                if (!s1Matches[i]) continue;
                while (!s2Matches[k]) k++;
                if (s1[i] != s2[k]) transpositions++;
                k++;
            }

            transpositions /= 2;

            double jaro = ((matches / (double)s1.Length) +
                           (matches / (double)s2.Length) +
                           ((matches - transpositions) / (double)matches)) / 3.0;

            int prefix = 0;
            for (int i = 0; i < Math.Min(4, Math.Min(s1.Length, s2.Length)); i++)
            {
                if (s1[i] == s2[i]) prefix++;
                else break;
            }

            return jaro + prefix * 0.1 * (1 - jaro);
        }
    }
}




