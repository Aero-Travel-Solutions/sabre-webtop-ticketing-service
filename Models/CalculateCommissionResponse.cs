using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class CalculateCommissionResponse
    {
        [JsonPropertyName("agent_commissions")]
        public AgentCommission[] AgentCommissions { get; set; }

		[JsonPropertyName("agent_fees")]
		public CalcAgentFee[] AgentFees { get; set; }

        [JsonPropertyName("plating_carrier_bsp_rate")]
        public decimal? PlatingCarrierBspRate { get; set; }

        [JsonPropertyName("plating_carrier_tour_code")]
        public string PlatingCarrierTourCode { get; set; }

        [JsonPropertyName("plating_carrier_agent_fee")]
        public Currency PlatingCarrierAgentFee { get; set; }
    }

    public partial class AgentCommission
    {
        [JsonPropertyName("quote_number")]
        public string QuoteNumber { get; set; }

        [JsonPropertyName("passenger_number")]
        public string PassengerNumber { get; set; }

        [JsonPropertyName("fuel_surcharge")]
        public decimal FuelSurcharge { get; set; }

        [JsonPropertyName("pax_type")]
        public string PaxType { get; set; }

        [JsonPropertyName("agt_comm")]
        public Currency AgtComm { get; set; }

        [JsonPropertyName("agt_comm_rate")]
        public decimal? AgtCommRate { get; set; }

        [JsonPropertyName("agent_fuel_comm")]
        public Currency AgtFuelComm { get; set; }

        [JsonPropertyName("agent_fuel_comm_rate")]
        public decimal? AgentFueldCommRate { get; set; }
    }

	public class CalcAgentFee
	{
		[JsonPropertyName("quote_number")]
		public string QuoteNumber { get; set; }

		[JsonPropertyName("passenger_number")]
		public string PassengerNumber { get; set; }

		[JsonPropertyName("agt_fee")]
		public Currency AgentFee { get; set; }

		[JsonPropertyName("gst")]
		public Currency Gst { get; set; }
	}

    //public partial class Currency
    //{
    //    [JsonPropertyName("amount")]
    //    public decimal? Amount { get; set; }

    //    [JsonPropertyName("currency_code")]
    //    public string CurrencyCode { get; set; }
    //}
}
