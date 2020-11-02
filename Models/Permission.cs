using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Permission
    {
        [JsonPropertyName("allow_ticketing")]
        public bool AllowTicketing { get; set; }

        [JsonPropertyName("allow_shopping")]
        public bool AllowShopping { get; set; }

        [JsonPropertyName("allow_booking")]
        public bool AllowBooking { get; set; }

        [JsonPropertyName("call_access_level")]
        public bool CallAccessLevel { get; set; }
    }
}
