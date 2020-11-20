using System;

namespace SabreWebtopTicketingService.Models
{
    public class QuotePassenger
    {
        public string PassengerName { get; set; }
        public string NameNumber { get; set; }
        public string PaxType { get; set; }
        public DateTime? DOB { get; set; } 
        public FOP FormOfPayment { get; set; }
        public Passport Passport { get; set; }
        public decimal PriceIt { get; set; }
        public bool DOBChanged { get; set; }
    }
}