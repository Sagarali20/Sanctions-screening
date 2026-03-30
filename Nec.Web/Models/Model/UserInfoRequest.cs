namespace Nec.Web.Models.Model
{
    public class UserInfoRequest
    {
        public HeaderInfo Header { get; set; }
        public PayloadInfo Payload { get; set; }
    }
    public class HeaderInfo
    {
        public string? AuthToken { get; set; }
        public string? ActionName { get; set; }
        public string? CopyRight { get; set; }
        public string? RequestToken { get; set; }
        public string? ServiceName { get; set; }
        public string? Status { get; set; }
        public int? Version { get; set; }
        public string? ApiKey { get; set; }
        public int? UserModifidId { get; set; }
    }
    public class PayloadInfo
    {
        public string? LoginName { get; set; }
        public string? ActionName { get; set; }
        public string? Password { get; set; }

    }
}
