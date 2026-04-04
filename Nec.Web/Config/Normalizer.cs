namespace Nec.Web.Config
{
    public static class Normalizer
    {
        private static readonly HashSet<string> StopWords = new()
    {
        "THE","OF","EL","LA","LE","DE","DA",
        "AND","INC","LTD","LLC","COMPANY"
    };

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            return string.Join(" ",
                text.ToUpperInvariant()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => !StopWords.Contains(t))
            );
        }
    }

}
