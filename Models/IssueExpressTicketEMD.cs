using SabreWebtopTicketingService.Interface;

namespace SabreWebtopTicketingService.Models
{
    public class IssueExpressTicketEMD: IIssueExpressTicketDocument
    {
        public int EMDNo { get; set; }
        public decimal Total { get; set; }
        public decimal TotalTax { get; set; }
        public decimal Commission { get; set; }
        public decimal Fee { get; set; }
        public decimal? FeeGST { get; set; }
        public decimal PriceIt { get; set; }
        public FOP FormOfPayment { get; set; }
        public string PassengerName { get; set; }
        public bool Ticketed { get; set; }
        public string PlatingCarrier { get; set; }
        public string Route { get; set; }
        public string RFISC { get; set; }
        public int SectorCount { get; set; }
    }
}