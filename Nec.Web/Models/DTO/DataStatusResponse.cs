namespace Nec.Web.Models.DTO
{
    public class DataStatusResponse
    {
        public int? Id { get; set; }
        public DateTime? DateAdded { get; set; }
        public int? TotalData { get; set; }
        public int? TotalNewlyAdded { get; set; }
        public int? TotalUpdated { get; set; }
        public int? TotalDataRemove { get; set; }
        public int? PrevDataTotal { get; set; }
        public string? SourceId { get; set; }
        public string? SourceCountry { get; set; }
        public string? SourceName { get; set; }
        public string? SourceUrl { get; set; }
    }
}

