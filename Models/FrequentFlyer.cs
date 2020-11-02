using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class FrequentFlyer
    {
        public string FrequentFlyerNo { get; set; }
        public string CarrierCode { get; set; }
        public List<string> PartnerCarrierCodes { get; set; }
    }
}
