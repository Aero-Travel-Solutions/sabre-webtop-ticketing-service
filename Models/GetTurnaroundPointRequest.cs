using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class GetTurnaroundPointRequest
    {
        public string FareCalculation { get; set; }
        public List<TPSector> Sectors { get; set; }
    }

    public class TPSector
    {
        public string From { get; set; }
        public string To { get; set; }
    }
}
