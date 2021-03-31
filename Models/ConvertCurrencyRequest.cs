using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class ConvertCurrencyRequest
    {
        public string warmer { get; set; }
        public string SessionID { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency  { get;set;}
        public decimal Amount { get; set; }
    }
}
