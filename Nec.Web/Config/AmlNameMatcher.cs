using System.Text.RegularExpressions;

namespace Nec.Web.Config
{
    public static class AmlNameMatcher
    {

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "THE","OF","EL","LA","LE","DE","DA",
        "AND","INC","LTD","LLC","COMPANY"
    };

        // ======================
        // MAIN ENTRY POINT
        // ======================
        public static int SearchScore(string input, string candidate)
        {
            var inWords = Normalize(input);
            var caWords = Normalize(candidate);

            if (inWords.Length == 0 || caWords.Length == 0)
                return 0;

            double total = 0;
            int strong = 0;

            foreach (var iw in inWords)
            {
                double best = 0;

                foreach (var cw in caWords)
                {
                    double s = WordScore(iw, cw);
                    if (s > best) best = s;
                }

                total += best;
                if (best >= 0.80) strong++;
            }

            double avg = total / inWords.Length;

            // Phrase confidence
            if (strong >= inWords.Length - 1)
                avg += 0.05;

            // Extra word penalty
            avg -= Math.Abs(inWords.Length - caWords.Length) * 0.03;

            avg = Math.Clamp(avg, 0, 1);

            return (int)Math.Round(avg * 100);
        }

        // ======================
        // NORMALIZATION
        // ======================
        private static string[] Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Array.Empty<string>();

            name = name.ToUpperInvariant();
            name = Regex.Replace(name, @"[^A-Z0-9\s]", " ");
            name = Regex.Replace(name, @"\s+", " ").Trim();

            return name
                .Split(' ')
                .Where(w => !StopWords.Contains(w))
                .ToArray();
        }

        // ======================
        // WORD LEVEL SCORE
        // ======================
        private static double WordScore(string a, string b)
        {
            if (a == b)
                return 1.0; // exact match

            double score = 0;

            // Root match
            if (a.Length >= 4 && b.Length >= 4 && a[..4] == b[..4])
                score = Math.Max(score, 0.80);

            // Phonetic
            if (Soundex(a) == Soundex(b))
                score = Math.Max(score, 0.70);

            // Fuzzy typo tolerance
            double jw = JaroWinkler(a, b);
            if (jw >= 0.88)
                score = Math.Max(score, jw * 0.90);

            return Math.Min(score, 1.0);
        }

        // ======================
        // SOUNDEX
        // ======================
        private static string Soundex(string word)
        {
            if (string.IsNullOrEmpty(word)) return "";

            word = word.ToUpperInvariant();
            char first = word[0];

            string tail = Regex.Replace(word[1..], "[AEIOUYHW]", "0");
            tail = Regex.Replace(tail, "[BFPV]", "1");
            tail = Regex.Replace(tail, "[CGJKQSXZ]", "2");
            tail = Regex.Replace(tail, "[DT]", "3");
            tail = Regex.Replace(tail, "L", "4");
            tail = Regex.Replace(tail, "[MN]", "5");
            tail = Regex.Replace(tail, "R", "6");

            tail = Regex.Replace(tail, @"(\d)\1+", "$1");
            tail = tail.Replace("0", "");

            return (first + tail + "000").Substring(0, 4);
        }

        // ======================
        // JARO-WINKLER
        // ======================
        private static double JaroWinkler(string s1, string s2)
        {
            if (s1 == s2) return 1.0;

            int matchDistance = Math.Max(s1.Length, s2.Length) / 2 - 1;
            bool[] s1Matches = new bool[s1.Length];
            bool[] s2Matches = new bool[s2.Length];

            int matches = 0;

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

            int t = 0, k = 0;
            for (int i = 0; i < s1.Length; i++)
            {
                if (!s1Matches[i]) continue;
                while (!s2Matches[k]) k++;
                if (s1[i] != s2[k]) t++;
                k++;
            }

            double transpositions = t / 2.0;

            double jaro =
                (matches / (double)s1.Length +
                 matches / (double)s2.Length +
                 (matches - transpositions) / matches) / 3.0;

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
