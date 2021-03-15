using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class CalculateCommissionRequest
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("document_type")]
        public string DocumnentType { get; set; }
        [JsonPropertyName("channel")]
        public Channel Channel { get; set; }

        [JsonPropertyName("issue_date")]
        public DateTime IssueDate { get; set; }

        [JsonPropertyName("consolidator_id")]
        public string ConsolidatorId { get; set; }

        [JsonPropertyName("agent_number")]
        public string AgentNumber { get; set; }

        [JsonPropertyName("ticketing_pcc")]
        public string TicketingPcc { get; set; }

        [JsonPropertyName("gds_code")]
        public string GdsCode { get; set; }

        [JsonPropertyName("agent_username")]
        public string AgentUsername { get; set; }

        [JsonPropertyName("agent_iata")]
        public string AgentIata { get; set; }

        [JsonPropertyName("country_of_sale")]
        public string CountryOfSale { get; set; }

        [JsonPropertyName("plating_carrier")]
        public string PlatingCarrier { get; set; }

        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonPropertyName("tour_code")]
        public string TourCode { get; set; }

        [JsonPropertyName("bsp_commission")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public decimal? BspCommission { get; set; }

        [JsonPropertyName("form_of_payment")]
        public string FormOfPayment { get; set; }

        [JsonPropertyName("sectors")]
        public CalculateCommissionSectorRequest[] Sectors { get; set; }

        [JsonPropertyName("passengers")]
        public CalculateCommissionPassengerRequest[] Passengers { get; set; }

        [JsonPropertyName("quotes")]
        public CalculateCommissionQuoteRequest[] Quotes { get; set; }

    }

    public enum Channel
    {
        ONLINE,
        WEBTOP
    }
}
