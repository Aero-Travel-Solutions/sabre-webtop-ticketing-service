using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class AccountDetails
    {
        [JsonPropertyName("credit_limit")]
        public Currency CreditLimit { get; set; }

        [JsonPropertyName("bank_account_number")]
        public string BankAccountNumber { get; set; }

        [JsonPropertyName("payment_terms")]
        public string PaymentTerms { get; set; }

        [JsonPropertyName("credit_terms")]
        public string CreditTerms { get; set; }

        [JsonPropertyName("ticketing_location")]
        public string TicketingLocation { get; set; }

        [JsonPropertyName("payment_options")]
        public PaymentOptions PaymentOptions { get; set; }
    }
}
