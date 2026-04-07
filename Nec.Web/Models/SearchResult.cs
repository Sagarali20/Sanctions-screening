namespace Nec.Web.Models
{
    public class SearchResult
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? EntityType { get; set; }
        public string SourceType { get; set; }
        public string SourceId { get; set; }
        public string? Source { get; set; }
        public string? DateOfBirth { get; set; }
        public string? AliasName { get; set; }
        public int Distance { get; set; }
        public double score { get; set; }

    }
}
