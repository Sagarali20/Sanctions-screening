using System.Text.RegularExpressions;

namespace Nec.Web.Utils
{
    public static class FullTextHelper
    {
        public static string ToFullTextQuery(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var tokens = Regex.Split(input.ToUpper(), @"\s+")
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Where(t => t.Length > 1) 
                .Distinct()
                .Select(t => $"\"{t}\"");

            return string.Join(" AND ", tokens);
        }
    }
}
