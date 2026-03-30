namespace Nec.Web.Models
{
    public class AMLFilter
    {
        public string? Name { get; set; }    
        public string? EntityType { get; set; }
        public string? SourceType { get; set; }
        public string? Type { get; set; }
        public string? SourceId { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? StateProvince { get; set; }
        public int? TopMatch{ get; set; }
        public int? MatchParcentage { get; set; }
        public List<string>? Includes { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public int? ScreeningId { get; set; }
        public string? ScreeningType { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostCode { get; set; }
        public string? Guid { get; set; }
        public string? Ids { get; set; }
        public string? DocumentType { get; set; }
        public Boolean IsFuzzy { get; set; }
        public string? UserId { get; set; }
        public string? TotalHitCount { get; set; }
        public string? IpAddress { get; set; }

    }
}
