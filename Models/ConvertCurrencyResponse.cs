using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class ConvertCurrencyResponse
    {
        public string CurrencyCode { get; set; }
        public int Amount { get; set; }
        public WebtopError Error { get; set; }
    }
}
