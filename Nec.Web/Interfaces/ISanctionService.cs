using Nec.Web.Models;
using System.Collections.Generic;

namespace Nec.Web.Interfaces
{
    public interface ISanctionService
    {
        bool CreateSanction(SanctionEntity model);
        bool CreateSanctionNew(SanctionEntity model);
        bool UpdateSanction(SanctionEntity model);
        bool DeleteSanction(string id);
        Task<List<SanctionEntity>> GetExcelSanctionDetailsBySearch(string name, string entitytype, string address,string city,string state,string country,string dateofbirth, string guid);
        Task<List<SearchResult?>> GetSearchSanction(AMLFilter model);
        Task<List<SanctionEntity?>> GetSearchSanctionIndividual(AMLFilter model);
        Task<List<SanctionEntity?>> GetSearchSanctionIndividualForUI(AMLFilter model);
        Task<List<SanctionEntity?>> GetSearchingResultDownload(string ids);

        Task<List<SanctionEntity?>> GetSearchSanctionCheckEntity(AMLFilter model);
        Task<Sanction> GetSanctionDetailsById(int id);
        Task<string> GetAllSourceId();
        int CreateAMLLog(AMLSourceLog model);
        int CreateAMLDataStatusLog(AMLSourceLog model);
        Task<List<AMLSourceLog>> GetAllSourceLog(string from,string to);
        Task<List<Source>> GetAllSourceList();
        Task<List<AMLSourceLog>> GetAllDataStatusLog(string from,string to);
        Task<string?> GetFileVersion();
        Task<int?> TotalDataCount();
        bool CreateSource(Source model);

    }
}
