using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Contact
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("job_title")]
        public string JobTitle { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }
    }
}
