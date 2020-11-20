using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class ManageFlightItem
    {
        public string Locator { get; set; }
        public string DateCreated { get; set; }
        public string BookingTTL { get; set; }
        public string BookingPCC { get; set; }
        public List<string> PassengerNames { get; set; }
        public List<ManageFlightSector> Sectors{ get; set; }
    }

    public class ManageFlightTTL
    {
        public string TTL { get; set; }
        public string TimeZone { get; set; }
    }

    public class ManageFlightSector
    {
        public int SectorNo { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Carrier { get; set; }
        public string FlightNo { get; set; }
        public string BookingClass { get; set; }
        public string DepartureDate { get; set; }
        public string Status { get; set; }
        public string MarriageGroup { get; set; }
        public bool Ticketed { get; set; }
    }
}
