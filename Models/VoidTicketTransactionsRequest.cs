using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class VoidTicketTransactionsRequest
    {
        [JsonPropertyName("locator")]
        public string Locator { get; set; }

        [JsonPropertyName("ticket_number_list")]
        public List<string> TicketNumberList { get; set; }
    }
}
