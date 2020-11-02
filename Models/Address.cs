using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Address
    {
        [JsonPropertyName("line_1")]
        public string Line1 { get; set; }

        [JsonPropertyName("line_2")]
        public string Line2 { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("postal_code")]
        public string PostalCode { get; set; }

        [JsonPropertyName("country_code")]
        public string Country { get; set; }
    }
}
