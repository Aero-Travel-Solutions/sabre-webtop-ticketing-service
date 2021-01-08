using System.Collections.Generic;

namespace SabreWebtopTicketingService.Models
{
    public class GetQuoteTextResponse
    {
        public List<QuoteData> QuoteData { get; set; }
        public List<EMDData> EMDData { get; set; }
        public string EMDText { get; set; }
        public string QuoteError { get; set; }
        public string EMDError { get; set; }
    }

    public class QuoteData
    {
        public int QuoteNo { get; set; }
        public bool Expired { get; set; }
        public string QuoteText { get; set; }
    }

    public class EMDData
    {
        public int EMDNo { get; set; }
        public bool Expired { get; set; }
        public string EMDText { get; set; }
    }
}
