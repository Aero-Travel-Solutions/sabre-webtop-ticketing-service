
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
        public string AgentUserName { get; set; }
        public string AgentUserFullName { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ConsolidatorUserName { get; set; }
        public string ConsolidatorUserFullName { get; set; }
        public string QueueID { get; set; }
    }
}
