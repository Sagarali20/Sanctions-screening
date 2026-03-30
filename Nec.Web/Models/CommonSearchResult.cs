using Nec.Web.Models.Model;

namespace Nec.Web.Models
{
    public class CommonSearchResult
    {
        public int Id { get; set; }
        public string? Guid { get; set; }
        public string? FirstName { get; set; }
        public string? SecondName { get; set; }
        public string? ThirdName { get; set; }
        public string? FourthName { get; set; }
        public string? SourceType { get; set; }
        public string? EntityType { get; set; }
        public string? DateOfBirth { get; set; }
        public List<IndividualDateOfBirthModel> IndividualDateOfBirth { get; set; }
        public string? Address { get; set; }
        public List<Address>? Address2 { get; set; }
        public string? Aliases { get; set; }
        public string? Country { get; set; }
        public string? DataSource { get; set; }
        public int? Score { get; set; }
        public string? Type { get; set; }

    }
}
