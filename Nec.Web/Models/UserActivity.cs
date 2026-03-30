namespace Nec.Web.Models
{
    public class UserActivity
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public string? SearchedText { get; set; } = null!;
        public int? TotalHitCount { get; set; }
        public string? IpAddress { get; set; }
        public string? DateAdded { get; set; }
    }
}
