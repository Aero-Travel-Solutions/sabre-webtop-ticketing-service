using System.Collections.Generic;
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

        [JsonPropertyName("sectors")]
        public List<SectorData> Sectors { get; set; }
    }

    public class SectorData
    {
        public string Carrier { get; set; }
        public string Cabin { get; set; }
        public string BookingClass { get; set; }
        public string Farebasis { get; set; }
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