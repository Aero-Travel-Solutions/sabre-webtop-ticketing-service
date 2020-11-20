using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class SabreOpenSSR
    {
        public string SSRCode { get; set; }
        public string Carrier { get; internal set; }
        public string Status { get; internal set; }
        public List<OpenSSRSector> Sectors { get; set; }
        public List<OpenSSRPassenger> Passengers { get; set; }
        public string FreeText { get; internal set; }
    }

    public class OpenSSRPassenger
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string NameNumber { get; set; }
        public string PassengerName => $"{LastName}/{FirstName}";
    }

    public class OpenSSRSector
    {
        public string From { get; internal set; }
        public string To { get; internal set; }
        public string Carrier { get; set; }
        public string FlightNumber { get; internal set; }
        public string BookingClass { get; internal set; }
        public string DepartureDate { get; internal set; }
    }
}
