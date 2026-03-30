namespace Nec.Web.Models
{
    public class PersonRow
    {
        public string Guid { get; set; } = "";
        public string RecordType { get; set; } = "";
        public string FullName { get; set; } = "";
        public string AddressLine1 { get; set; } = "";
        public string AddressLine2 { get; set; } = "";
        public string AddressLine3 { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "";
        public string PostCode { get; set; } = "";
        public string DateOfBirth { get; set; } = ""; // Keep as string if unsure about date format
    }
}
