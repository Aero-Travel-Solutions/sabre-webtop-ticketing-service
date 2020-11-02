using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class TicketingPccList
    {
        [JsonPropertyName("pcc_list")]
        public TicketingPcc[] PccList { get; set; }
    }
}
