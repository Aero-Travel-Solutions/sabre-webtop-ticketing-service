using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class IssueExpressTicketRS
    {
        public string OrderId { get; set; }
        public string ThirdPartyInvoiceNumber { get; set; }
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal? GrandPriceItAmount { get; set; }
        public List<IssueTicketError> Errors { get; set; }
        public List<IssueTicketDetails> Tickets { get; set; }
        public decimal MerchantFee { get; set; }
        public string ApprovalCode { get; set; }
    }


    public class MerchantFOP
    {
        public string PaymentSessionID { get; set; }
        public string OrderID { get; set; }
        public string CardNumber { get; set; }
        public string CardType { get; set; }
        public string ExpiryDate { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public decimal CashAmount { get; set; }
        public decimal MerchantFee { get; set; }
    }

    public class IssueTicketError
    {
        public WebtopError Error { get; set; }
        public DocumentType DocumentType{get;set;}
        public List<int> DocumentNumber { get; set; }
    }
}
