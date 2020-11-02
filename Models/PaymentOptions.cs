using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class PaymentOptions
    {
        [JsonPropertyName("direct_debit")]
        public bool DirectDebit { get; set; }

        [JsonPropertyName("cheque")]
        public bool Cheque { get; set; }

        [JsonPropertyName("credit_card")]
        public bool CreditCard { get; set; }

        [JsonPropertyName("enett")]
        public bool Enett { get; set; }

        [JsonPropertyName("prepaid")]
        public bool Prepaid { get; set; }

        [JsonPropertyName("tias_tips")]
        public bool TiasTips { get; set; }

        [JsonPropertyName("money_direct")]
        public bool MoneyDirect { get; set; }

        [JsonPropertyName("smartmoney")]
        public bool Smartmoney { get; set; }
    }
}
