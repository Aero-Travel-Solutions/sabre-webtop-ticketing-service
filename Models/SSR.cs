using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class SSR
    {
        public string SSRCode { get; set; }
        public string FlightSummary { get; set; }
        public string Description { get; set; }
        public string Carrier { get; set; }
        public string Status { get; set; }
        public string NameNumber { get; set; }
        public string PassengerName { get; set; }
        public int? SectorNo { get; set; }
        public bool AllSectors { get; set; }
        public string FreeText { get; set; }
        public List<string> Warnings { get; set; }
    }
}