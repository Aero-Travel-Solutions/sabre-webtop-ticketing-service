using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class ConvertCurrencyRequest
    {
        public string SessionID { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency  { get;set;}
        public int Amount { get; set; }
    }
}
