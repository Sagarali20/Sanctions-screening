namespace Nec.Web.Models
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool IsAllow { get; set; }
        public DateTime? CreatedDate { get; set; }

        public string? Phone { get; set; }
        public string? Country { get; set; }
        public string? ZipCode { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? Role { get; set; }
        public int? CreatedBy { get; set; }
    }
}
