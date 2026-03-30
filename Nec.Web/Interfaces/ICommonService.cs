using Nec.Web.Models;

namespace Nec.Web.Interfaces
{
    public interface ICommonService
    {
        Task<List<CommonSearchResult?>> GetCommonSearch(AMLFilter model);
        Task<List<CommonSearchResult?>> GetCommonSearchForExcel(AMLFilter model);

    }
}
