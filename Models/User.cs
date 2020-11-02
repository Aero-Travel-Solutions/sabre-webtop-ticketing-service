using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class User
    {
        [JsonPropertyName("consolidator_id")]
        public string ConsolidatorId { get; set; }

        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("agent")]
        public Agent Agent { get; set; }

        [JsonPropertyName("consolidator")]
        public Consolidator Consolidator { get; set; }

        [JsonPropertyName("permissions")]
        public Permission Permissions { get; set; }

        [JsonPropertyName("roles")]
        public string[] Roles { get; set; }
    }
}
