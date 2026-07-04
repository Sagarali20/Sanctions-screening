using Nec.Web.Models;
using Nec.Web.Models.Model;

namespace Nec.Web.Interfaces
{
    public interface IUKService
    {
        bool CreateUKSanction(Designation model);
        int CreateUKSanctionBulk(Designations model);
        Task<Designation> GetSanctionDetailsById(int id);
    }
}
