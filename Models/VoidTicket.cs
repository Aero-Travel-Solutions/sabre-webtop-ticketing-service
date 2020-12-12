using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class VoidTicket
    {
        public string DocumentNumber { get; set; }
        public string DocumentType { get; set; }
        public string IssuingPCC { get; set; }
        public List<LinkedDocument> LinkedDocuments { get; set; }
    }

    public class LinkedDocument
    {
        public string DocumentNumber { get; set; }
        public string DocumentType { get; set; }
    }
}