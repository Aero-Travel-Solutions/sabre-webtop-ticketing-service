using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class CalculateCommissionPassengerRequest
    {
        [JsonPropertyName("passenger_number")]
        public string PassengerNumber { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("pax_type")]
        public string PaxType { get; set; }
    }
}
