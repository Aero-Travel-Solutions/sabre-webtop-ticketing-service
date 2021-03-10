using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Itinerary
    {
        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonPropertyName("flight_number")]
        public string FlightNumber { get; set; }

        [JsonPropertyName("booking_class")]
        public string BookingClass { get; set; }

        [JsonPropertyName("departure_date")]
        public string DepartureDate { get; set; }

        [JsonPropertyName("departure_time")]
        public string DepartureTime { get; set; }

        [JsonPropertyName("marketing_carrier")]
        public string MarketingCarrier { get; set; }

        [JsonPropertyName("operating_carrier")]
        public string OperatingCarrier { get; set; }

        [JsonPropertyName("is_queued")]
        public bool? IsQueued { get; set; }

        [JsonPropertyName("sector_no")]
        public int? SectorNo { get; set; }
    }
}
