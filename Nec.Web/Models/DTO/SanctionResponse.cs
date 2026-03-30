namespace Nec.Web.Models.DTO
{
    using System.Text.Json.Serialization;

    public class SanctionResponse
    {
        [JsonPropertyOrder(1)]
        public string entity_type { get; set; }

        [JsonPropertyOrder(2)]
        public string name { get; set; }

        [JsonPropertyOrder(3)]
        public string source_type { get; set; }

        [JsonPropertyOrder(4)]
        public DateTime list_date { get; set; }

        [JsonPropertyOrder(5)]
        public string gender { get; set; }

        [JsonPropertyOrder(6)]
        public string source_id { get; set; }

        [JsonPropertyOrder(7)]
        public List<string> alias_names { get; set; }

        [JsonPropertyOrder(8)]
        public List<string> date_of_birth { get; set; }

        [JsonPropertyOrder(9)]
        public List<string> place_of_birth { get; set; }

        [JsonPropertyOrder(10)]
        public List<string> citizenship { get; set; }

        [JsonPropertyOrder(11)]
        public List<string> links { get; set; }

        [JsonPropertyOrder(12)]
        public List<string> other_information { get; set; }
    }

}
