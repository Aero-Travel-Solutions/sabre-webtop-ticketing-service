using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class ConvertCurrencyResponse
    {
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public WebtopError Error { get; set; }
    }
}
