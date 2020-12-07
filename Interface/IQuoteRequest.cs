using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Interface
{
    public interface IQuoteRequest
    {
        string SessionID { get; set; }
        string AgentID { get; set; }
        string GDSCode { get; set; }
        string Locator { get; set; }
        string AccessKey { get; set; }
        List<QuotePassenger> SelectedPassengers { get; set; }
        List<IQuoteSector> SelectedSectors{ get; set; }
    }
}
