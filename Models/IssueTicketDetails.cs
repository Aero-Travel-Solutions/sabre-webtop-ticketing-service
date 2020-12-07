using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class IssueTicketDetails
    {
        public string DocumentNumber { get; set; }
        public bool Conjunction => !string.IsNullOrEmpty(ConjunctionPostfix);
        public string ConjunctionPostfix { get; set; }
        public string DocumentType { get; set; }
        public string LocalIssueDateTime { get; set; }
        public string IssuingPCC { get; set; }
        public string PassengerName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AgentPrice { get; set; }
        public string CurrencyCode { get; set; }
        public decimal GrossPrice { get; set; }
        public int QuoteRefNo { get; set; }
        public List<int> EMDNumber { get; set; }
        public string Route { get; set; }
        public decimal PriceIt { get; set; }
        public FOP FormOfPayment { get; set; }
        public decimal CashAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public Warning Warning { get; set; }
        public MerchantFOP MerchantFOP { get; set; }
    }
}