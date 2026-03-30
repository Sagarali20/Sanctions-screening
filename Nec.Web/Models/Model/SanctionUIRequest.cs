namespace Nec.Web.Models.Model
{

    public class SanctionUIRequest
    {
        public Header2? Header { get; set; }
        public Payload2? Payload { get; set; }
    }

    public  class Header2
    {
        public string? AuthToken { get; set; }
        public string? ActionName { get; set; }
        public string? CopyRight { get; set; }
        public string? RequestToken { get; set; }
        public string? ServiceName { get; set; }
        public string? Status { get; set; }
        public int? Version { get; set; }
        public string? ApiKey { get; set; }
        public string? UserModifidId { get; set; }
    }

    public class Payload2
    {
        public string? ActionName { get; set; }
        public string? IpAddress { get; set; }
        public string? Type { get; set; }
        public string? SourceType { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? StateProvince { get; set; }
        public string? Nationality { get; set; }
        public string? Country { get; set; }
        public string? DateOfBirth { get; set; }
        public int? MatchParcentage { get; set; }
        public List<string>? Includes { get; set; }
    }

}
