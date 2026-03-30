using NPOI.HSSF.Record;

namespace Nec.Web.Models
{
    public class DownloadModel
    {
        public Header? Header { get; set; }
        public Payload? Payload { get; set; }
    }
    public class Header
    {
        public string? AuthToken { get; set; }
        public string? ActionName { get; set; }
        public string? CopyRight { get; set; }
        public string? RequestToken { get; set; }
        public string? ServiceName { get; set; }
        public string? Status { get; set; }
        public int? Version { get; set; }
        public string? ApiKey { get; set; }
        public string? UserModifidId { get; set; }
    }
    public class Payload
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? Dob { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public int? MatchParcentage { get; set; }
        public int? Max_Match_Parcentage { get; set; }
        public int? Total_Hits { get; set; }
        public List<string>? Includes { get; set; }
        public string? Message { get; set; }
        public List<FoundRecord?> Found_Records { get; set; }
    }
    public class FoundRecord
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public int? MatchParcentage { get; set; }
        public int? Score { get; set; }
        public string? Source_Id { get; set; }
        public string? Source_Type { get; set; }
        public List<string>? Nationality { get; set; }
        public List<string>? Address { get; set; }
        public string? Entity_Type { get; set; }
        public string? Designations { get; set; }
        public List<string>? Other_Information { get; set; }
        public List<string>? Date_Of_Birth { get; set; }
        public List<string>? Place_Of_Birth { get; set; }
        public List<string>? Alias_Names { get; set; }
        public List<string>? Last_Names { get; set; }
        public List<string>? Titles { get; set; }
    }

    public class OtherInformation
    {
        public string? Dt_Listed_On { get; set; }
        public List<string>? List_Type { get; set; }
        public List<string>? Last_Day_Updated { get; set; }
        public string? Comment { get; set; }
        public string? Tx_Reference_No { get; set; }
    }

}
