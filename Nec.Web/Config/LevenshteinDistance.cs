using Nec.Web.Models;

namespace Nec.Web.Config
{
    public class Levenshtein
    {
        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] dp = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) dp[i, 0] = i;
            for (int j = 0; j <= m; j++) dp[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;

                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1,   // delete
                                 dp[i, j - 1] + 1),  // insert
                        dp[i - 1, j - 1] + cost);    // substitute
                }
            }

            return dp[n, m];
        }
        public static List<SanctionEntity> FindClosestPersons(List<SanctionEntity> people, string searchName, int maxDistance = 3)
        {
            return people
                .Select(p => new
                {
                    Person = p,
                    Distance = LevenshteinDistance(p.name.ToLower(), searchName.ToLower())
                })
                .Where(x => x.Distance <= maxDistance)
                .OrderBy(x => x.Distance)
                .Select(x => x.Person)
                .ToList();
        }

        public static int Ltest()
        {
            int res = LevenshteinDistance("Alex Alonso Contreras Miranda", "Alex Contreras");
            return 1;
        }

    }

}
