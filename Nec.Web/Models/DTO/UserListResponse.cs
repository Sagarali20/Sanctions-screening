namespace Nec.Web.Models.DTO
{
    public class UserListResponse
    {
        public int? EnvId { get; set; }
        public int? LegalEntityId { get; set; }
        public int? UserModifiedId { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? UserId { get; set; }
        public int? UserVer { get; set; }
        public int? IsAllowLogin { get; set; }

        public string? LoginName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public List<Role>? Roles { get; set; }
        public UserInfo? UserInfo { get; set; }
    }
    public class Role
    {
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
    }

    public class UserInfo
    {
        public string? Country { get; set; }
        public string? Address1 { get; set; }
        public string? CompanyName { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? ApiKey { get; set; }
    }
}
