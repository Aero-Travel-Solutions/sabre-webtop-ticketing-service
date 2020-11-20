using System;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Commission
    {
        [JsonPropertyName("hash_key")]
        public string HashKey { get; set; }

        [JsonPropertyName("sort_key")]
        public string SortKey { get; set; }

        [JsonPropertyName("consolidator_id")]
        public string ConsolidatorId { get; set; }

        [JsonPropertyName("country_of_sale")]
        public string CountryOfSale { get; set; }

        [JsonPropertyName("plating_carrier")]
        public string PlatingCarrier { get; set; }

        [JsonPropertyName("origin")]
        public string[] Origin { get; set; }

        [JsonPropertyName("destination")]
        public string[] Destination { get; set; }

        [JsonPropertyName("document_type")]
        public string DocumentType { get; set; }

        [JsonPropertyName("effective_date")]
        public DateTime EffectiveDate { get; set; }

        [JsonPropertyName("expiry_date")]
        public DateTime ExpiryDate { get; set; }

        [JsonPropertyName("bsp_commission_rate")]
        public decimal BspCommissionRate { get; set; }

        [JsonPropertyName("agent_commission_rate")]
        public decimal AgentCommissionRate { get; set; }

        [JsonPropertyName("tour_code")]
        public string TourCode { get; set; }

        [JsonPropertyName("minimum_content")]
        public decimal MinimumContent { get; set; }

        [JsonPropertyName("codeshare_applicable")]
        public bool CodeshareApplicable { get; set; }

        [JsonPropertyName("agents")]
        public string[] Agents { get; set; }

        [JsonPropertyName("apply_on_fuel")]
        public bool ApplyOnFuel { get; set; }

        [JsonPropertyName("carriers_in")]
        public string[] CarriersIn { get; set; }

        [JsonPropertyName("carriers_ex")]
        public string[] CarriersEx { get; set; }

        [JsonPropertyName("fare_basis_in")]
        public string[] FareBasisIn { get; set; }

        [JsonPropertyName("fare_basis_ex")]
        public string[] FareBasisEx { get; set; }

        [JsonPropertyName("booking_class_in")]
        public string[] BookingClassIn { get; set; }

        [JsonPropertyName("booking_class_ex")]
        public string[] BookingClassEx { get; set; }

        [JsonPropertyName("flight_range_in")]
        public string[] FlightRangeIn { get; set; }

        [JsonPropertyName("iata_numbers_in")]
        public string[] IataNumbersIn { get; set; }

        [JsonPropertyName("ptcs_in")]
        public string[] PtcsIn { get; set; }

        [JsonPropertyName("cabin_in")]
        public string[] CabinIn { get; set; }

        [JsonPropertyName("travel_start_date")]
        public DateTime TravelStartDate { get; set; }

        [JsonPropertyName("travel_end_date")]
        public DateTime TravelEndDate { get; set; }
    }
}
