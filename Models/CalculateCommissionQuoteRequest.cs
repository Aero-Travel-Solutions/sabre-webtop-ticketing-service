using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class CalculateCommissionQuoteRequest
    {
        [JsonPropertyName("quote_number")]
        public string QuoteNumber { get; set; }

        [JsonPropertyName("quoted_sectors")]
        public string[] QuotedSectors { get; set; }

        [JsonPropertyName("passenger_number")]
        public string PassengerNumber { get; set; }

        [JsonPropertyName("base_fare_amount")]
        public decimal BaseFareAmount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("fuel_surcharge")]
        public decimal FuelSurcharge { get; set; }

        [JsonPropertyName("agt_comm")]
        public decimal AgentCommissionRate { get; set; }
    }
}
