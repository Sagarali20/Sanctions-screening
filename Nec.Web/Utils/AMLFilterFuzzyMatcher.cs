using Nec.Web.Models;

namespace Nec.Web.Utils
{
    public static class AMLFilterFuzzyMatcher
    {
        public  class NameMatchResult
        {
            public string Name { get; set; }
            public int Distance { get; set; }
            public double MatchPercentage { get; set; }
        }
        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int[,] d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= t.Length; j++) d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }
            return d[s.Length, t.Length];
        }

        public static double GetMatchPercentage(string source, string target)
        {
            source = source.ToLower().Trim();
            target = target.ToLower().Trim();

            int distance = LevenshteinDistance(source, target);
            int maxLen = Math.Max(source.Length, target.Length);

            if (maxLen == 0) return 100;

            return (1.0 - (double)distance / maxLen) * 100;
        }
        public static NameMatchResult MatchSingleName(string name, string query)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(query))
                return null;

            int distance = LevenshteinDistance(name.ToLower(), query.ToLower());
            double percentage = GetMatchPercentage(name, query);

            return new NameMatchResult
            {
                Name = name,
                Distance = distance,
                MatchPercentage = percentage
            };
        }

        //public static List<NameMatchResult> SearchByName(List<string> names, string query, int maxDistance = 2, double minMatch = 60)
        //{
        //    query = query.ToLower().Trim();

        //    return names
        //        .Select(name =>
        //        {
        //            int distance = LevenshteinDistance(name.ToLower(), query);
        //            double percentage = GetMatchPercentage(name, query);

        //            return new NameMatchResult
        //            {
        //                Name = name,
        //                Distance = distance,
        //                MatchPercentage = percentage
        //            };
        //        })
        //        .Where(x => x.Distance <= maxDistance || x.MatchPercentage >= minMatch)
        //        .OrderBy(x => x.Distance)
        //        .ThenByDescending(x => x.MatchPercentage)
        //        .ToList();
        //}

        public static List<SearchResult> SearchByName(
            List<SearchResult> data,
            string query,
            int maxDistance = 2,
            double minMatch = 60)
        {
            query = query.ToLower().Trim();

            return data
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .Select(item =>
                {
                    item.Distance = LevenshteinDistance(item.Name!.ToLower(), query);
                    item.score = GetMatchPercentage(item.Name, query);

                    return item;
                })
                .Where(x => x.Distance <= maxDistance || x.score >= minMatch)
                .OrderBy(x => x.Distance)
                .ThenByDescending(x => x.score)
                .ToList();
        }


    }
}
