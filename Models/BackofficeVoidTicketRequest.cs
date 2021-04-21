using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class BackofficeVoidTicketRequest
    {
        public string SessionId { get; set; }
        public string ConsolidatorId { get; set; }
        public string Locator { get; set; }
        public string HostUserId { get; set; }
        public List<string> Tickets { get; set; }
        public string VoidFeeCurrencyCode { get; set; }
        public Dictionary<string, decimal> VoidFees { get; set; }
    }
}
