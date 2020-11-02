using System.ComponentModel;

namespace SabreWebtopTicketingService.Models
{
    public class FormOfPayment
    {
        public PaymentType PaymentType { get; set; }
        public string BCode { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string ApprovalCode { get; set; }
        public decimal CreditAmount { get; set; }
        public string CardType { get; set; }
    }

    public enum PaymentType
    {
        [Description("CASH")]
        CA,
        [Description("CREDIT")]
        CC,
        [Description("MERCHANT")]
        MA,
        [Description("QFALLOCATION")]
        QA
    }
}