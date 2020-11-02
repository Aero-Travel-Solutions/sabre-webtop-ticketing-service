using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Consolidator
    {
        [JsonPropertyName("consolidator_id")]
        public string ConsolidatorId {get;set;}

        [JsonPropertyName("pool_id")]
        public string PoolId {get;set;}

        [JsonPropertyName("client_id")]
        public string ClientId {get;set;}

        [JsonPropertyName("name")]
        public string Name {get;set;}

        [JsonPropertyName("site_settings")]
        public string SiteSettings {get;set;}

        [JsonPropertyName("address")]
        public Address Address {get;set;}

        [JsonPropertyName("fees")]
        public Fees Fees {get;set;}

        [JsonPropertyName("ticket_printer_address")]
        public string TicketPrinterAddress { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("ticket_price_deviation_tolerance")]
        public decimal ticketpricedeviationtolerance { get; set; }
    }
}
