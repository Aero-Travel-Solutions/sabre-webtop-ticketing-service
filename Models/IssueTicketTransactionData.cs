
namespace SabreWebtopTicketingService.Models
{
    public class IssueTicketTransactionData
    {
        public string SessionId { get; set; }
        public User User { get; set; }
        public Pcc Pcc { get; set; }
        public AgentQueuedByDetails AgentQueuedByDetails { get; set; }
        public PNR Pnr { get; set; }
        public IssueExpressTicketRS TicketingResult { get; set; }
    }

    public class AgentQueuedByDetails
    {
        public string AgentID { get; set; }
        public string AgentName { get; set; }
        public string AgentLogo { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
    }
}
