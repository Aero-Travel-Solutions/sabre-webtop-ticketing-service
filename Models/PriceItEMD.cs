using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class PriceItEMD
    {
        public string DocumentNumber { get; set; }
        public bool IsConjunction { get; set; }
        public string ConjunctionPostfix { get; set; }
        public string TicketingPCC { get; set; }
        public string TicketingIATA { get; set; }
        public PriceItPassenger Passenger { get; set; }
        public string IssueDateTime { get; set; }
        public List<EMDCoupon> EMDCoupons { get; set; }
        public decimal TotalAmount { get; set; }
        public string PlatingCarrier { get; set; }
        public  FOP Payment { get; set; }
    }

    public class EMDCoupon : ICoupon
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Carrier { get; set; }
        public string ICW { get; set; }
        public string PresentTo { get; set; }
        public string PresentAt { get; set; }
        public string GroupCode { get; set; }
        public string GroupDescription { get; set; }
        public string Reason { get; set; }
        public string Consumed { get; set; }
        public bool TaxExcempt { get; set; }
        public bool FeeOverride { get; set; }
        public string SSR { get; set; }
        public string JourneyType { get; set; }
        public string CurrencyCode { get; set; }
        public decimal TotalAmount { get; set; }
        public List<Tax> Taxes { get; set; }
    }
}