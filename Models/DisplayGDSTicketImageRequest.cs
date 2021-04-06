namespace SabreWebtopTicketingService.Models
{
    public class DisplayGDSTicketImageRequest
    {
        public string SessionID { get; set; }
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public string TicketingPcc { get; set; }
        public string warmer { get; set; }
    }
}
