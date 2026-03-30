using Nec.Web.Models;
using Nec.Web.Models.DTO;
using Nec.Web.Models.Model;

namespace Nec.Web.Interfaces
{
    public interface IUNService
    {
        bool CreateUNSanction(IndividualModel model);
        bool CreateUNRefDetails(string query);
        //Task<List<OfacResponse?>> GetSearchSanction(OfacFilter model);
        Task<IndividualModel> GetSanctionDetailsById(int id);

    }
}
