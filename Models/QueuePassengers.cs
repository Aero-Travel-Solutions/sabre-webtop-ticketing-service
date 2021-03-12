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
        public QueuePassengersFOP FormOfPayment { get; set; }

        [JsonPropertyName("is_queued")]
        public bool IsQueued { get; set; }
    }

    public class QueuePassengersFOP
    {
        [JsonPropertyName("payment_type")]
        public PaymentType PaymentType { get; set; }

        [JsonPropertyName("bcode")]
        public string BCode { get; set; }

        [JsonPropertyName("card_number")]
        public string CardNumber { get; set; }

        [JsonPropertyName("expiry_date")]
        public string ExpiryDate { get; set; }

        [JsonPropertyName("approval_code")]
        public string ApprovalCode { get; set; }

        [JsonPropertyName("credit_amount")]
        public decimal CreditAmount { get; set; }

        [JsonPropertyName("masked_card_number")]
        public string MaskedCardNumber { get; set; }

        [JsonPropertyName("card_type")]
        public string CardType { get; set; }
    }
}
