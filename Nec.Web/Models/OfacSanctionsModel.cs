using System.Xml.Serialization;

namespace Nec.Web.Models
{
    public class OfacSanctionsModel
    {

    }
    [XmlRoot(ElementName = "sdnList", Namespace = "https://sanctionslistservice.ofac.treas.gov/api/PublicationPreview/exports/XML")]
    public class SdnList
    {
        [XmlElement(ElementName = "publshInformation")]
        public PublishInformation PublshInformation { get; set; }

        [XmlElement(ElementName = "sdnEntry")]
        public List<SdnEntry> SdnEntries { get; set; }
    }

    public class PublishInformation
    {
        [XmlElement(ElementName = "Publish_Date")]
        public string PublishDate { get; set; }

        [XmlElement(ElementName = "Record_Count")]
        public int RecordCount { get; set; }
    }

    public class SdnEntry
    {
        [XmlElement(ElementName = "uid")]
        public int Uid { get; set; }

        [XmlElement(ElementName = "firstName")]
        public string FirstName { get; set; }

        [XmlElement(ElementName = "lastName")]
        public string LastName { get; set; }

        [XmlElement(ElementName = "title")]
        public string Title { get; set; }

        [XmlElement(ElementName = "sdnType")]
        public string SdnType { get; set; }

        [XmlElement(ElementName = "entityType")]
        public string EntityType { get; set; }

        [XmlElement(ElementName = "remarks")]
        public string Remarks { get; set; }

        [XmlArray(ElementName = "programList")]
        [XmlArrayItem(ElementName = "program")]
        public List<string> ProgramList { get; set; }

        [XmlArray(ElementName = "akaList")]
        [XmlArrayItem(ElementName = "aka")]
        public List<Aka> AkaList { get; set; }

        [XmlArray(ElementName = "addressList")]
        [XmlArrayItem(ElementName = "address")]
        public List<Address> AddressList { get; set; }

        [XmlArray(ElementName = "idList")]
        [XmlArrayItem(ElementName = "id")]
        public List<Id> IdList { get; set; }

        [XmlArray(ElementName = "dateOfBirthList")]
        [XmlArrayItem(ElementName = "dateOfBirthItem")]
        public List<DateOfBirthItem> DateOfBirthList { get; set; }

        [XmlArray(ElementName = "placeOfBirthList")]
        [XmlArrayItem(ElementName = "placeOfBirthItem")]
        public List<PlaceOfBirthItem> PlaceOfBirthList { get; set; }

        [XmlArray(ElementName = "nationalityList")]
        [XmlArrayItem(ElementName = "nationality")]
        public List<Nationality> NationalityList { get; set; }

        [XmlElement("vesselInfo")]
        public VesselInfo? VesselInfo { get; set; }

        public string? DataInfoType { get; set; }

    }

    public class Aka
    {
        [XmlElement(ElementName = "uid")]
        public int Uid { get; set; }

        [XmlElement(ElementName = "type")]
        public string Type { get; set; }

        [XmlElement(ElementName = "category")]
        public string Category { get; set; }

        [XmlElement(ElementName = "firstName")]
        public string FirstName { get; set; }

        [XmlElement(ElementName = "lastName")]
        public string LastName { get; set; }
    }

    public class Address
    {
        [XmlElement(ElementName = "uid")]
        public int? Uid { get; set; }

        [XmlElement(ElementName = "address1")]
        public string? Address1 { get; set; }

        [XmlElement(ElementName = "city")]
        public string? City { get; set; }

        [XmlElement(ElementName = "postalCode")]
        public string? PostalCode { get; set; }

        [XmlElement(ElementName = "country")]
        public string? Country { get; set; }
    }

    public class Id
    {
        [XmlElement(ElementName = "uid")]
        public int Uid { get; set; }

        [XmlElement(ElementName = "idType")]
        public string IdType { get; set; }

        [XmlElement(ElementName = "idNumber")]
        public string IdNumber { get; set; }

        [XmlElement(ElementName = "idCountry")]
        public string IdCountry { get; set; }
    }

    public class DateOfBirthItem
    {
        [XmlElement(ElementName = "uid")]
        public int Uid { get; set; }

        [XmlElement(ElementName = "dateOfBirth")]
        public string DateOfBirth { get; set; }

        [XmlElement(ElementName = "mainEntry")]
        public bool MainEntry { get; set; }
    }

    public class PlaceOfBirthItem
    {
        [XmlElement(ElementName = "uid")]
        public int Uid { get; set; }

        [XmlElement(ElementName = "placeOfBirth")]
        public string PlaceOfBirth { get; set; }

        [XmlElement(ElementName = "mainEntry")]
        public bool MainEntry { get; set; }
    }

    public class Nationality
    {
        [XmlElement(ElementName = "uid")]
        public int Uid { get; set; }

        [XmlElement(ElementName = "country")]
        public string Country { get; set; }

        [XmlElement(ElementName = "mainEntry")]
        public bool MainEntry { get; set; }
    }

    [XmlType("vesselInfo")]
    public class VesselInfo
    {
        [XmlElement("callSign")]
        public string CallSign { get; set; }

        [XmlElement("vesselType")]
        public string VesselType { get; set; }

        [XmlElement("vesselFlag")]
        public string VesselFlag { get; set; }

        // numeric values may be missing in some XMLs, so use nullable long
        [XmlElement("tonnage")]
        public long? Tonnage { get; set; }

        [XmlElement("grossRegisteredTonnage")]
        public long? GrossRegisteredTonnage { get; set; }
    }

}
