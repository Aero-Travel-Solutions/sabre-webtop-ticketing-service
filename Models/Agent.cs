using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Agent
    {
        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("logo")]
        public string Logo { get; set; }

        [JsonPropertyName("consolidator_id")]
        public string ConsolidatorId { get; set; }

        [JsonPropertyName("consolidator")]
        public Consolidator Consolidator { get; set; }

        [JsonPropertyName("contacts")]
        public Contact[] Contacts { get; set; }

        [JsonPropertyName("permissions")]
        public Permission Permission { get; set; }

        [JsonPropertyName("account_details")]
        public AccountDetails AccounDetails { get; set; }

        [JsonPropertyName("finance_details")]
        public FinanceDetails FinanceDetails { get; set; }

        [JsonPropertyName("ticketing_queue")]
        public string TicketingQueue { get; set; }

        [JsonPropertyName("address")]
        public Address Address { get; set; }

        [JsonPropertyName("customer_no")]
        public string CustomerNo { get; set; }
    }
}
