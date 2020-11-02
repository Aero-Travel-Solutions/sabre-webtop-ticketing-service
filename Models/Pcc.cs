using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Pcc
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("gds_code")]
        public string GdsCode { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("pcc_code")]
        public string PccCode { get; set; }

        [JsonPropertyName("ticket_printer_address")]
        public string TicketPrinterAddress { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("consolidator_id")]
        public string ConsolidatorId { get; set; }
    }
}
