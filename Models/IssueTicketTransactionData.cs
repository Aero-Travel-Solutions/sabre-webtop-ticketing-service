
namespace SabreWebtopTicketingService.Models
{
    public class IssueTicketTransactionData
    {
        public string SessionId { get; set; }

        public User User { get; set; }

        public Pcc Pcc { get; set; }

        public PNR Pnr { get; set; }

        public IssueExpressTicketRS TicketingResult { get; set; }
    }
}
