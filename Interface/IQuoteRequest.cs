using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Interface
{
    interface IQuoteRequest
    {
        public string SessionID { get; set; }
        public string AgentID { get; set; }
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public string AccessKey { get; set; }
        public List<QuotePassenger> SelectedPassengers { get; set; }
        public List<IQuoteSector> SelectedSectors{ get; set; }
    }
}
