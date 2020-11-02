using System;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Comment
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; }
    }
}
