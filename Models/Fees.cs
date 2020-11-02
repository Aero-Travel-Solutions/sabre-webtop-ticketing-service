using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class Fees
    {
        [JsonPropertyName("refund_online")]
        public Currency RefundOnline { get; set; }

        [JsonPropertyName("refund_offline")]
        public Currency RefundOffline { get; set; }

        [JsonPropertyName("exchange_online")]
        public Currency ExchangeOnline { get; set; }

        [JsonPropertyName("exchange_offline")]
        public Currency ExchangeOffline { get; set; }

        [JsonPropertyName("revalidation_online")]
        public Currency RevalidationOnline { get; set; }

        [JsonPropertyName("revalidation_offline")]
        public Currency RevalidationOffline { get; set; }

        [JsonPropertyName("cancellation_online")]
        public Currency CancellationOnline { get; set; }

        [JsonPropertyName("cancellation_offline")]
        public Currency CancellationOffline { get; set; }

        [JsonPropertyName("emds_online")]
        public Currency EmdsOnline { get; set; }

        [JsonPropertyName("emds_offline")]
        public Currency EmdsOffline { get; set; }

        [JsonPropertyName("zero_bps")]
        public FeeRule[] ZeroBsp { get; set; }
    }
}
