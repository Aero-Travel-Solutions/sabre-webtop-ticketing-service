using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class ConsolidatorPccList
    {
        [JsonPropertyName("pcc_list")]
        public Pcc[] PccList { get; set; }
    }
}
