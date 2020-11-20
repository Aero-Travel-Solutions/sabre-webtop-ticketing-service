using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{ 
    public class PlateRuleTicketingPccRequest
    {
        [JsonPropertyName("consolidator_id")]
        public string ConsolidatorId { get; set; }

        [JsonPropertyName("gds_code")]
        public string GdsCode { get; set; }

        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }

        [JsonPropertyName("booking_pcc")]
        public string BookingPcc { get; set; }

        [JsonPropertyName("plating_carrier")]
        public string PlatingCarrier { get; set; }

        [JsonPropertyName("Cabin")]
        public string Cabin { get; set; }

        [JsonPropertyName("booking_class")]
        public string[] BookingClass { get; set; }
    }

    public class PlateRuleTicketingPccResponse
    {
        [JsonPropertyName("consolidator_id")]
        public string ConsolidatorId { get; set; }

        [JsonPropertyName("gds_code")]
        public string GdsCode { get; set; }

        [JsonPropertyName("ticketing_pcc_code")]
        public string TicketingPccCode { get; set; }
    }
}