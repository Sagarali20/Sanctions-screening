using System.Xml.Serialization;

namespace Nec.Web.Models
{
    [XmlRoot("Designations")]
    public class Designations
    {
        public string DateGenerated { get; set; }

        [XmlElement("Designation")]
        public List<Designation> DesignationList { get; set; } = new List<Designation>();
    }

    public class Designation
    {
        public string LastUpdated { get; set; }
        public string DateDesignated { get; set; }
        public string UniqueID { get; set; }
        public string OFSIGroupID { get; set; }
        public string UNReferenceNumber { get; set; }
        public Names Names { get; set; }
        public NonLatinNames NonLatinNames { get; set; }
        public Titles Titles { get; set; }
        public string RegimeName { get; set; }
        public string IndividualEntityShip { get; set; }
        public string DesignationSource { get; set; }
        public string SanctionsImposed { get; set; }
        public SanctionsImposedIndicators SanctionsImposedIndicators { get; set; }
        public string OtherInformation { get; set; }
        public string UKStatementofReasons { get; set; }
        public IndividualDetails IndividualDetails { get; set; }
    }

    public class Names
    {
        [XmlElement("Name")]
        public List<Name> NameList { get; set; } = new List<Name>();
    }

    public class Name
    {
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public string Name6 { get; set; }
        public string NameType { get; set; }
    }

    public class NonLatinNames
    {
        [XmlElement("NonLatinName")]
        public List<NonLatinName> NonLatinNameList { get; set; } = new List<NonLatinName>();
    }

    public class NonLatinName
    {
        public string NameNonLatinScript { get; set; }
    }

    public class Titles
    {
        [XmlElement("Title")]
        public List<string> TitleList { get; set; } = new List<string>();
    }

    public class SanctionsImposedIndicators
    {
        public bool AssetFreeze { get; set; }
        public bool ArmsEmbargo { get; set; }
        public bool TargetedArmsEmbargo { get; set; }
        public bool CharteringOfShips { get; set; }
        public bool ClosureOfRepresentativeOffices { get; set; }
        public bool CrewServicingOfShipsAndAircraft { get; set; }
        public bool Deflag { get; set; }
        public bool PreventionOfBusinessArrangements { get; set; }
        public bool ProhibitionOfPortEntry { get; set; }
        public bool TravelBan { get; set; }
        public bool PreventionOfCharteringOfShips { get; set; }
        public bool PreventionOfCharteringOfShipsAndAircraft { get; set; }
        public bool TechnicalAssistanceRelatedToAircraft { get; set; }
        public bool TrustServicesSanctions { get; set; }
        public bool DirectorDisqualificationSanction { get; set; }
    }

    public class IndividualDetails
    {
        [XmlElement("Individual")]
        public List<Individual> IndividualList { get; set; }
    }

    public class Individual
    {
        public DOBs DOBs { get; set; }
        public Nationalities Nationalities { get; set; }
        public Positions Positions { get; set; }
        public BirthDetails BirthDetails { get; set; }
    }

    public class DOBs
    {
        [XmlElement("DOB")]
        public List<string>? DOBList { get; set; } = new List<string>();
    }

    public class Nationalities
    {
        [XmlElement("Nationality")]
        public List<string> NationalityList { get; set; } = new List<string>();
    }

    public class Positions
    {
        [XmlElement("Position")]
        public List<string> PositionList { get; set; } = new List<string>();
    }

    public class BirthDetails
    {
        [XmlElement("Location")]
        public List<Location> LocationList { get; set; } = new List<Location>();
    }

    public class Location
    {
        public string TownOfBirth { get; set; }
        public string CountryOfBirth { get; set; }
    }

}
