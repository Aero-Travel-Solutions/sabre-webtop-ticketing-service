using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Currency
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; }
    }
}
