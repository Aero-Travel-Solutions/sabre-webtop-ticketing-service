using SabreWebtopTicketingService.Interface;
using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class PriceItTicket
    {
        public string DocumentNumber { get; set; }
        public bool IsConjunction { get; set; }
        public string ConjunctionPostfix { get; set; }
        public string TicketingPCC { get; set; }
        public string TicketingIATA { get; set; }
        public PriceItPassenger Passenger { get; set; }
        public string IssueDateTime { get; set; }
        public List<Coupon> Coupons { get; set; }
        public string FareCalculation { get; set; }
        public string PlatingCarrier { get; set; }
        public decimal TotalFare { get; set; }
        public FOP Payment { get; set; }
    }

    public class PriceItPassenger
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PassengerName { get; set; }
        public string FrequentFlyerNumber { get; set; }
        public string FrequentFlyerProvider { get; set; }
    }
}