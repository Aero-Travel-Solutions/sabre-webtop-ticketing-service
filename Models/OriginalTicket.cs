using SabreWebtopTicketingService.Interface;
using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class OriginalTicket
    {
        public string DocumentNumber { get; set; }
        public string TicketingPCC { get; set; }
        public string TicketingIATA { get; set; }
        public string Locator { get; set; }
        public TicketPassenger TicketPassenger { get; set; }
        public string CurrencyCode { get; set; }
        public decimal BaseFare { get; set; }
        public decimal TotalFare { get; set; }
        public List<Tax> Taxes { get; set; }
        public string ValidatingCarrier { get; set; }
        public string IssueDateTime { get; set; }
        public List<Coupon> TicketCoupons { get; set; }
        public string FareCalculation { get; set; }
        public string TourCode { get; set; }
        public List<string> Endosements { get; set; }
        public List<WebtopError> ValidityErrors { get; set; }
        public List<FormOfPayment> Payment { get; internal set; }
        public bool Exchanged { get; internal set; }
        public decimal? CommissionAmount { get; internal set; }
        public decimal? CommissionPercentage { get; internal set; }
        public string Route { get; internal set; }
    }

    public class TicketPassenger
    {
        public string ExternalNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PassengerName => $"{LastName}/{FirstName}";
        public string PaxType { get; set; }
        public FrequentFlyer FrequentFlyer { get; set; }
    }

    public class Coupon: ICoupon
    {
        public string CouponNumber { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string DepartureDate { get; set; }
        public string DepartureTime { get; set; }
        public string ArivalDate { get; set; }
        public string MarketingCarrier { get; set; }
        public string FlightNumber { get; set; }
        public string OperatingCarrier { get; set; }
        public string BookingClass { get; set; }
        public string FareBasis { get; set; }
        public string TicketDesignator { get; set; }
        public string NotValidBeforeDate { get; set; }
        public string NotValidAfterDate { get; set; }
        public BaggageAllowance BagAllowance { get; set; }
        public string BookedStatus { get; set; }
        public string CurrentStatus { get; set; }
    }

    public class BaggageAllowance
    {
        public string Code { get; set; }
        public int? Amount { get; set; }
    }

    public class SectorInfo
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string BookingClass { get; set; }
        public string MarketingCarrier { get; set; }
        public string OperatingCarrier { get; set; }
    }
}