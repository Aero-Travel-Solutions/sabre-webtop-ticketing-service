using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class SessionUser
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }
    }
}
