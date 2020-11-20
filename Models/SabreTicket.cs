namespace SabreWebtopTicketingService.Models
{
    public class SabreTicket
    {
        public string DocumentNumber { get; set; }
        public string DocumentType { get; set; }
        public string TicketingPCC { get; set; }
        public int RPH { get; set; }
        public bool Voided { get; set; }
        public string PassengerName { get; set; }
        public string IssueDate { get; set; }
        public string IssueTime { get; set; }
    }
}