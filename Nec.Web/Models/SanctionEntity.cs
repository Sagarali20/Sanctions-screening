using System.Text.Json.Serialization;

namespace Nec.Web.Models
{
    public class SanctionEntity
    {
        public string? Guid { get; set; }
        public string? entity_type { get; set; }
        public string? name { get; set; }
        public string source_id { get; set; }
        public string source_type { get; set; }
        public string? gender { get; set; }
        public string? pep_type { get; set; }
        public string? AmlId { get; set; }
        public string? id { get; set; }

        public string? tl_name { get; set; }
        public List<string>? alias_names { get; set; }
        public List<string>? last_names { get; set; }
        public List<string>? given_names { get; set; }
        public List<string>? alias_given_names { get; set; }
        public List<string>? spouse { get; set; }
        public List<string>? parents { get; set; }
        public List<string>? children { get; set; }
        public List<string>? siblings { get; set; }
        public List<string>? date_of_birth { get; set; }
        public List<string>? date_of_birth_remarks { get; set; }
        public List<string>? place_of_birth { get; set; }
        public List<string>? place_of_birth_remarks { get; set; }
        public List<string>? address { get; set; }
        public List<string>? address_remarks { get; set; }
        public List<string>? sanction_details { get; set; }
        public List<string>? description { get; set; }
        public List<string>? occupations { get; set; }
        public List<string>? positions { get; set; }
        public List<string>? political_parties { get; set; }
        public List<string>? links { get; set; }
        public List<string>? titles { get; set; }
        public DateTime? list_date { get; set; }
        public List<string>? functions { get; set; }
        public List<string>? citizenship { get; set; }
        public List<string>? citizenship_remarks { get; set; }
        public List<string>? other_information { get; set; }
        public List<string>? company_number { get; set; }
        public List<string>? name_remarks { get; set; }
        public List<string>? jurisdiction { get; set; }
        public string? source_country { get; set; }
        public int? VersionId { get; set; }
        public string? Type { get; set; }
        public int? Score { get; set; }


    }
    public class ConsolidatedDelta
    {
        public string? type { get; set; }
        public SanctionEntity? record { get; set; }
    }

    public class Sanction
    {
        public string? Guid { get; set; } = "";
        public string SourceId { get; set; }
        public string SourceType { get; set; }
        public string? PepType { get; set; }
        public string? AmlId { get; set; }
        public string? Id { get; set; }
        public string? EntityType { get; set; }
        public string? Gender { get; set; }
        public string? Name { get; set; }
        public string? TlName { get; set; }
        public List<string?> AliasNames { get; set; }
        public List<string?> LastNames { get; set; }
        public List<string?> GivenNames { get; set; }
        public List<string?> AliasGivenNames { get; set; }
        public List<string?> Spouse { get; set; }
        public List<string?> Parents { get; set; }
        public List<string?> Children { get; set; }
        public List<string?> Siblings { get; set; }
        public List<string?> DateOfBirth { get; set; }
        public List<string?> DateOfBirthRemarks { get; set; }
        public List<string?> PlaceOfBirth { get; set; }
        public List<string?> PlaceOfBirthRemarks { get; set; }
        public List<string?> Address { get; set; }
        public List<string?> AddressRemarks { get; set; }
        public List<string?> SanctionDetails { get; set; }
        public List<string?> Description { get; set; }
        public List<string?> Occupations { get; set; }
        public List<string?> Positions { get; set; }
        public List<string?> PoliticalParties { get; set; }
        public List<string?> Links { get; set; }
        public List<string?> Titles { get; set; }
        public DateTime? ListDate { get; set; }
        public List<string?> Functions { get; set; }
        public List<string?> Citizenship { get; set; }
        public List<string?> CitizenshipRemarks { get; set; }
        public List<string?> OtherInformation { get; set; }
        public string? SourceCountry { get; set; }
    }

    public class Source
    {
        public string? source { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? link { get; set; }
        public string? country_name { get; set; }
        public string? source_name { get; set; }
        public long? last_seen { get; set; }
    }

    public class Root
    {
        public List<Source> sources { get; set; }
    }
}
