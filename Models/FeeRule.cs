using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class FeeRule
    {
        [JsonPropertyName("min_charge")]
        public Currency MinCharge { get; set; }

        [JsonPropertyName("max_charge")]
        public Currency MaxCharge { get; set; }

        [JsonPropertyName("fee_amount")]
        public Currency FeeAmount { get; set; }

        [JsonPropertyName("fee_rate")]
        public decimal FeeRate { get; set; }

        [JsonPropertyName("charge_type")]
        public string ChargeType { get; set; }
    }
}
