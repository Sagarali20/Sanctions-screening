namespace Nec.Web.Models
{
    public class User
    {
        public Header3? Header { get; set; }
        public Payload3? Payload { get; set; }
    }
    public class Header3
    {
        public int? UserId { get; set; }
        public string? ApiKey { get; set; }
        public string? ActionName { get; set; }
        public string? ServiceName { get; set; }
    }

    public class Payload3
    {
        public int? UserModifiedId { get; set; }
        public string? ActionName { get; set; }
        public int? UserId { get; set; }
        public int? IsAllowLogin { get; set; }

        public string? LoginName { get; set; }
        public string? Password { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public List<Role3>? Roles { get; set; }
        public UserInfo3? UserInfo { get; set; }
    }

    public class Role3
    {
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
    }

    public class UserInfo3
    {
        public string? Country { get; set; }
        public string? Address1 { get; set; }
        public string? CompanyName { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? ApiKey { get; set; }
    }
}
