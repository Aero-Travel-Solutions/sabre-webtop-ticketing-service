using System;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Common
{
    public class SabreSession
    {
        [JsonPropertyName("sabre_session_id")]
        public string SabreSessionID { get; set; }
        public bool Stored { get; set; }
    }
}
