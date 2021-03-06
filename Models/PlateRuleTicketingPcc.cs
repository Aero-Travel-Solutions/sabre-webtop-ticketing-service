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
        public SectorData[] Sectors { get; set; }
    }

    public class SectorData
    {
        [JsonPropertyName("carrier")]
        public string Carrier { get; set; }
        [JsonPropertyName("cabin")]
        public string Cabin { get; set; }
        [JsonPropertyName("booking_class")]
        public string BookingClass { get; set; }
        [JsonPropertyName("fare_basis")]
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
        [JsonPropertyName("ticket_printer_address")]
        public string TicketinPrinterAddress { get; set; }
        [JsonPropertyName("printer_country_code")]
        public string PrinterCountryCode { get; set; }
        [JsonPropertyName("target_branch")]
        public string TargetBranch { get; set; }
        [JsonPropertyName("iata_number")]
        public string IataNumber { get; set; }
    }
}