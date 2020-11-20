using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class CalculateCommissionDebugResponse
    {
        [JsonPropertyName("rule_set")]
        public Commission[] RuleSet { get; set; }

        [JsonPropertyName("matched_rules")]
        public Commission[] MatchedRules { get; set; }

        [JsonPropertyName("log")]
        public string[] Log { get; set; }
    }
}
