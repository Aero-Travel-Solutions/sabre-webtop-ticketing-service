using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class FinanceDetails
    {
        [JsonPropertyName("travel_agent_license")]
        public string TravelAgentLicense { get; set; }

        [JsonPropertyName("iata_number")]
        public string IataNumber { get; set; }

        [JsonPropertyName("dapa_number")]
        public string DapaNumber { get; set; }

        [JsonPropertyName("agency_backoffice")]
        public string AgencyBackoffice { get; set; }

        [JsonPropertyName("abn_number")]
        public string AbnNumber { get; set; }

        [JsonPropertyName("acn_number")]
        public string AcnNumber { get; set; }

        [JsonPropertyName("atas_number")]
        public string AtasNumber { get; set; }

        [JsonPropertyName("afta_number")]
        public string AftaNumber { get; set; }

        [JsonPropertyName("comments")]
        public Comment[] Comments { get; set; }
    }
}
