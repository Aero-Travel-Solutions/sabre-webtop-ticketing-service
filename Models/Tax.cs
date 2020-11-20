using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class Tax
    {
        public string Code { get; set; }
        public decimal Amount { get; set; }
        public bool Paid { get; set; } = false;
        public bool Fuel { get; set; }
    }
}
