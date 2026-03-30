using Nec.Web.Models;
using Nec.Web.Models.Model;
using System.Threading.Tasks;

namespace Nec.Web.Interfaces
{
    public interface IUserService
    {
        bool CreateUser(User model);
        bool UpdateUser(User model);
        Task<List<Payload3>> GetAllUser();
        Task<Payload3> GetUserByUserName(string username);
        Task<UserInfo> ValidationUser(string username);
        Task<List<UserActivity>> GetAllUserActivity(UserFilter userFilter);
        Task<List<UserRole>> GetAlLRole();

    }

}
