using System;

namespace SabreWebtopTicketingService.Models
{
    public class SelectedPassengerData
    {
        public string PassengerKey { get; set; }
        public string PaxType { get; set; }
        public DateTime? DOB { get; set; }
        public FOP FormOfPayment { get; set; }
        public decimal PriceIt { get; set; }
        public bool DOBChanged { get; set; }
    }
}