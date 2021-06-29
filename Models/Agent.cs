using SabreWebtopTicketingService.Models;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Agent : User
    {
        [JsonPropertyName("pcc_list")]
        public AgentPcc[] PccList { get; set; }

        [JsonPropertyName("ticketing_queue")]
        public string TicketingQueue { get; set; }
        public string TicketingPcc { get; set; }
        public string AgentPCC { get; set; }
        public Currency CreditLimit { get; set; }
        public string CustomerNo { get; set; }
        public Address Address { get; set; }
        public string Logo { get; set; }
        public string PhoneNumber { get; set; }
    }
}
