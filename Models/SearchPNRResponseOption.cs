using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class SearchPNRResponseOption
    {
        public List<string> PassengerNames { get; set; }
        public string DepartureDate { get; set; }
        public string ArrivalDate { get; set; }
        public string Locator { get; set; }
    }
}
