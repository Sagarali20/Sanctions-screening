using Nec.Web.Models;
using Nec.Web.Models.DTO;

namespace Nec.Web.Interfaces
{
    public interface IOfacService
    {
        int CreateOfacSanction(SdnEntry model);
        int UpdateOfacSanctionSDN(List<SdnEntry> model);
        int UpdateOfacSanctionNONSDN(List<SdnEntry> model);
        bool CreateOfacRefDetails(string query);
        Task<List<OfacResponse?>> GetSearchSanction(OfacFilter model);
        Task<SdnEntry> GetSanctionDetailsById(int id);

    }
}
