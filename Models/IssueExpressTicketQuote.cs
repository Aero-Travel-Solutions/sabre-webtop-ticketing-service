using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Interface;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Models
{ 
    public class IssueExpressTicketQuote: IIssueExpressTicketDocument
    {
        public int QuoteNo { get; set; }
        public PriceType PriceType { get; set; }
        public FareType FareType { get; set; }
        public decimal CreditCardFee { get; set; } = 0.00M;
        public decimal BaseFare { get; set; }
        public string BaseFareCurrency { get; set; }
        public decimal? EquivFare { get; set; }
        public string EquivFareCurrency { get; set; }
        public string ROE { get; set; }
        public List<Tax> Taxes { get; set; }
        public decimal TotalFare { get; set; }
        public decimal TotalTax { get; set; }
        public decimal? AgentCommissionRate { get; set; }
        public decimal? BSPCommissionRate { get; set; }
        public decimal Commission => AgentCommissions.IsNullOrEmpty() || !AgentCommissions.First().AgtComm.Amount.HasValue ?
                                    0.00M :
                                    AgentCommissions.First().AgtComm.Amount.Value;
        public List<AgentCommission> AgentCommissions { get; set; }
        public decimal Fee { get; set; }
        public decimal? FeeGST { get; set; }
        public bool FiledFare { get; set; }
        public bool PendingSfData { get; set; }
        public QuotePassenger QuotePassenger { get; set; }
        public string PassengerName 
        {
            get
            {
                return QuotePassenger.PassengerName;
            }
            set { }
        }

        public List<QuoteSector> QuoteSectors { get; set; }
        public bool PartialIssue { get; set; }
        public int SectorCount { get; set; }
        public string PlatingCarrier { get; set; }
        public string Route { get; set; }
        public string FareCalculation { get; set; }
        public decimal PriceIt { get; set; }
        public string PriceCode { get; set; }
        public string TourCode { get; set; }
        public bool ApplySupressITFlag { get; set; }
        public string TicketingPCC { get; set; }
        public string BCode { get; set; }
        public List<string> Endorsements { get; set; }
        public decimal MerchantFee { get; set; }
    }
}