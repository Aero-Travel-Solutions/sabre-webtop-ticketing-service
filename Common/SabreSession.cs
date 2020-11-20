using System;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Common
{
    public class SabreSession
    {
        [JsonPropertyName("sabre_session_id")]
        public string SessionID { get; set; }
        public bool Stored { get; set; }
        public bool IsLimitReached { get; set; }
    }
}
