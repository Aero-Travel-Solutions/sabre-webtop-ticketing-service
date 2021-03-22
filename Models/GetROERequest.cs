using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class GetROERequest
    {
        public string warmer { get; set; }
        public string SessionID { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
    }
}
