using System.Collections.Generic;
using System.Linq;
using Nec.Web.Models; // ✅ this is the important using!
using Raffinert.FuzzySharp;


namespace Nec.Web.Config
{

    public class FuzzyNameMatcher
    {
        public (string BestMatch, int Score) GetBestMatch(string input, IEnumerable<string> candidates)
        {
            var result = Process.ExtractOne(input, candidates);
            return (result.Value, result.Score);
        }
        public int GetBestMatchPercentage(string input, IEnumerable<string> candidates)
        {
            var result = Process.ExtractOne(input, candidates);
            return result.Score;
        }

        public List<(string Name, int Score)> GetTopMatches(string input, IEnumerable<string> candidates, int top = 200)
        {
            var results = Process.ExtractTop(input, candidates, limit: top);
            return results.Select(r => (r.Value, r.Score)).ToList();
        }
    }
    public static class FuzzySearchExtensions
    {
        public static IEnumerable<(T Value, int Score)> FuzzySearch<T>(
            this IEnumerable<T> source,
            string query,
            Func<T, string> selector,
            int top = 5)
        {
            return source
                .Select(item => (Value: item, Score: Fuzz.Ratio(query.ToLower(), selector(item).ToLower())))
                .OrderByDescending(x => x.Score)
                .Take(top);
        }
        public static IEnumerable<(T Value, int Score)> FuzzySearch<T>(
        this IEnumerable<T> source,
        string query,
        Func<T, string> selector)
            {
                return source
                    .Select(item => (Value: item, Score: Fuzz.Ratio(query.ToLower(), selector(item).ToLower())))
                    .OrderByDescending(x => x.Score);
            }
        }


}


