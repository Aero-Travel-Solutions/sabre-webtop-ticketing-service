using System;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class CalculateCommissionSectorRequest
    {
        [JsonPropertyName("sector_number")]
        public string SectorNumber { get; set; }

        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonPropertyName("departure_date")]
        public DateTime DepartureDate { get; set; }

        [JsonPropertyName("arrival_date")]
        public DateTime ArrivalDate { get; set; }

        [JsonPropertyName("marketing_carrier")]
        public string MarketingCarrier { get; set; }

        [JsonPropertyName("marketing_flight_number")]
        public string MarketingFlightNumber { get; set; }

        [JsonPropertyName("mileage")]
        public int? Mileage { get; set; }

        [JsonPropertyName("operating_carrier")]
        public string OperatingCarrier { get; set; }

        [JsonPropertyName("operating_flight_number")]
        public string OperatingFlightNumber { get; set; }

        [JsonPropertyName("is_codeshare")]
        public bool? IsCodeshare { get; set; }

        [JsonPropertyName("booking_class")]
        public string BookingClass { get; set; }

        [JsonPropertyName("cabin")]
        public string Cabin { get; set; }

        [JsonPropertyName("fare_basis")]
        public string FareBasis { get; set; }
    }
}
