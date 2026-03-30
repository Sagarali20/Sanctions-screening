namespace Nec.Web.Models
{
    public class AMLSourceLog
    {
        public int? Id { get; set;}
        public string? FileName { get; set;}
        public string? FileVersion { get; set;}
        public string? SourceName { get; set;}
        public string? SourceLink { get; set;}
        public string? SourceCountry { get; set;}
        public int? Total { get; set;}
        public int? TotalNew { get; set;}
        public int? TotalNewlyAdded { get; set;}
        public int? TotalUpdate { get; set;}
        public int? TotalUpdated { get; set;}
        public int? TotalDelete { get; set;}
        public int? TotalDataRemove { get; set;}
        public int? TotalPrivious { get; set;}
        public int? TotalData { get; set;}
        public int? PrevDataTotal { get; set;}
        public DateTime? CreateDate { get; set;}
        public string? DateAdded { get; set;}
        public int? TotalRecord { get; set;}
        public bool? IsDownloaded { get; set;}
        public bool? IsProcessed { get; set;}
        public bool? IsExceptionOccured { get; set;}
        public string? SourceId { get; set; }
        public string? SourceUrl { get; set; }

    }
}
