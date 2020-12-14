using System.Collections.Generic;
using System.ComponentModel;

namespace SabreWebtopTicketingService.Models
{
    public class BookAncillaryRequest
    {
        [DefaultValue("1W")]
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public List<BookAncillaryRequestItem> Ancillaries { get; set; }
    }

    public class BookAncillaryRequestItem
    {
        public string OfferKey { get; set; }
        public int AncillaryQuantity { get; set; }
        public string NameNumber { get; set; }
        public string PassengerName { get; set; }
        public string PassengerType { get; set; }
    }
}
