using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class IssueExpressTicketRQ
    {
        public bool CheckCommission { get; set; }
        public string SessionID { get; set; }
        public string AgentID { get; set; }
        public ContactDetails ContactDetails { get; set; }
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public string PriceCode { get; set; }
        public List<string> IssueTicketQuoteKeys { get; set; }
        public List<string> IssueTicketEMDKeys { get; set; }
        public List<IssueExpressTicketQuote> Quotes { get; set; }
        public List<IssueExpressTicketEMD> EMDs { get; set; }
        public decimal? GrandPriceItAmount { get; set; }
        public MerchantData MerchantData { get; set; }
    }

    public class ContactDetails
    {
        public string ContactName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string AgentUserName { get; set; }
        public string AgentUserFullName { get; set; }
    }

    public class MerchantData
    {
        public string PaymentSessionID { get; set; }
        public string OrderID { get; set; }
        public List<MerchantFeeData> MerchantFeeData { get; set; }
        public int MerchantFeeRate { get; set; }

    }

    public class MerchantFeeData
    {
        public int DocumentNumber { get; set; }
        public DocumentType DocumentType { get; set; }
        public decimal MerchantFee { get; set; }
    }

    public enum DocumentType
    {
        Unknown,
        Quote,
        EMD,    
        ReissueQuote,
        ChangeFeeEMD,
        ResidualEMD
    }
}
