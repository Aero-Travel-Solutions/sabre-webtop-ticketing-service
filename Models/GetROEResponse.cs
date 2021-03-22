using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class GetROEResponse
    {
        public decimal ROE { get; set; }
        public WebtopError Error { get; set; }
    }
}
