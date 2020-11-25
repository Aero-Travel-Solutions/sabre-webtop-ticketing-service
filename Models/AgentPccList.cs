using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class AgentPccList
    {
        [JsonPropertyName("pcc_list")]
        public AgentPcc[] PccList { get; set; }
    }
}
