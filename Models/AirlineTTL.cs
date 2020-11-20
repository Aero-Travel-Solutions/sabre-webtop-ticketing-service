using System;

namespace SabreWebtopTicketingService.Models
{
    public class AirlineTTL
    {
        public string Airline { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime TTL { get; set; }
        public string TTLDate { get; set; }
        public string TTLTime { get; set; }
        public string TimeZone { get; set; }
        public string DisplayText => $"{TTLDate} {TTLTime} {TimeZone}".Trim();
        public bool MostRestrictive { get; set; }
    }
}
