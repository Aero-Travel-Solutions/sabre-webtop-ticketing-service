using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class QueuePassengers
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("name_number")]
        public string NameNumber { get; set; }

        [JsonPropertyName("expected_fare")]
        public decimal ExpectedFare { get; set; }

        [JsonPropertyName("expected_tax")]
        public decimal ExpectedTax { get; set; }

        [JsonPropertyName("original_ticket_number")]
        public string OriginalTicketNumber { get; set; }

        [JsonPropertyName("form_of_payment")]
        public FOP FormOfPayment { get; set; }

        [JsonPropertyName("is_queued")]
        public bool IsQueued { get; set; }
    }
}
