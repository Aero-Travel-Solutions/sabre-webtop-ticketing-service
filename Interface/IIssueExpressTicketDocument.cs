using System.Collections.Generic;

namespace SabreWebtopTicketingService.Interface
{
    public interface IIssueExpressTicketDocument
    {
        public string PassengerName { get; set; }
        public decimal Fee { get; set; }
        public decimal? FeeGST { get; set; }
        public int SectorCount { get; set; }
        public string PlatingCarrier { get; set; }
        public string Route { get; set; }
        public decimal PriceIt { get; set; }
        public decimal BaseFare { get; set; }
        public string BaseFareCurrency { get; set; }
        public decimal EquivFare { get; set; }
        public string EquivFareCurrencyCode { get; set; }
    }
}