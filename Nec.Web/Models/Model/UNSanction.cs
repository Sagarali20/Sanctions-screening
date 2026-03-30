using System.Xml.Serialization;

namespace Nec.Web.Models.Model
{
    public class IndividualModel
    {
        public string? DataId { get; set; }
        public string? VersionNum { get; set; }
        public string? FirstName { get; set; }
        public string? SecondName { get; set; }
        public string? ThirdName { get; set; }
        public string? FourthName { get; set; }
        public string? UnListType { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ListedOn { get; set; }
        public string? NameOriginalScript { get; set; }
        public string? Gender { get; set; }
        public string? Comments { get; set; }
        public string? ListType { get; set; }
        public string? DateOfBirthYear { get; set; }
        public NationalityModel? Nationality { get; set; }
        public LastDayUpdatedModel? LastDayUpdated { get; set; }
        public DesignationModel? Designation { get; set; }
        public TitleModel? Title { get; set; }
        public List<AddressModel>? Address { get; set; } = new();
        public List<AliasModel>? Aliases { get; set; } = new();
        public List<IndividualDateOfBirthModel>? IndividualDateOfBirth { get; set; } = new();
        public List<IndividualPlaceOfBirthModel>? IndividualPlaceOfBirth { get; set; } = new();
        public List<IndividualDocument>? IndividualDocument { get; set; } = new();

    }

    public class AliasModel
    {
        public string? Quality { get; set; }
        public string? AliasName { get; set; }
        public string? CityOfBirth { get; set; }
        public string? CountryOfBirth { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Note { get; set; }
    }
    public class AddressModel
    {
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Note { get; set; }
        public string? State_Province { get; set; }
        public string? Street { get; set; }
    }
    public class LastDayUpdatedModel
    {
        public List<string>? Value { get; set; }
    }
    public class DesignationModel
    {
        public List<string>? Value { get; set; }
    }
    public class NationalityModel
    {
        public List<string>? Value { get; set; }
    }
    public class TitleModel
    {
        public List<string>? Value { get; set; }
    }
    public class IndividualDateOfBirthModel
    {
        public string? TypeOfDate { get; set; }
        public string? Date { get; set; }
        public string? Year { get; set; }
    }
    public class IndividualPlaceOfBirthModel
    {
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? State_Province { get; set; }
    }
    public class IndividualDocument
    {
        public string? TypeOfDocument { get; set; }
        public string? TypeOfDocument2 { get; set; }
        public string? Number { get; set; }
        public string? IssuingCountry { get; set; }
        public string? DateOfIssue { get; set; }
        public string? CityOfIssue { get; set; }
        public string? Note { get; set; }
    }

}
