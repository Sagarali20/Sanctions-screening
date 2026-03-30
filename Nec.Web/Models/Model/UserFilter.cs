namespace Nec.Web.Models.Model
{
    public class UserFilter
    {
        public USHeader? Header { get; set; }
        public USPayload? Payload { get; set; }

    }
    public class USHeader
    {
        public string? ActionName { get; set; }
        public string? ServiceName { get; set; }
    }

    public class USPayload
    {
        public string? ActionName { get; set; }
        public int UserKey { get; set; }
        public DateTime? DtFrom { get; set; }
        public DateTime? DtTo { get; set; }
    }
}
