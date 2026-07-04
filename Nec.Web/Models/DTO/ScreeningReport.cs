namespace Nec.Web.Models.DTO
{
    public class ScreeningReport
    {
        public string? RemittanceNumber { get; set; }
        public string? Remitter { get; set; }
        public string? Beneficiary { get; set; }
        public int? HitCountRM { get; set; }
        public int? HitCountBnF { get; set; }
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedDate { get; set; }
    }
}
