using Nec.Web.Models.DTO;

namespace Nec.Web.Models
{
    public class UserRole
    {
        public int? Id { get; set; }
        public int? RoleId { get; set; }
        public string? Description { get; set; }
        public string? RoleVer { get; set; }
        public string? RoleName { get; set; }

    }
    public class RolePayload
    {
        public List<UserRole> Roles { get; set; }
    }
    public class ApiResponse<T>
    {
        public HeaderR Header { get; set; }
        public List<T> Payload { get; set; }
    }
    public class HeaderR
    {
        public string UserId { get; set; }
        public string ApiKey { get; set; }
        public string ActionName { get; set; }
        public string ServiceName { get; set; }
    }
}
