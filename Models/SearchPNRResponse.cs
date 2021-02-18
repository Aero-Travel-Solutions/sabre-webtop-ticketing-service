using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class SearchPNRResponse
    {
        public PNR PNR { get; set; }
        public GST GST { get; set; }
        public List<WebtopError> Errors { get; set; }
    }

    public class GST
    {
        public string CountryOfSale { get; set; }
        public string TaxCode { get; set; }
        public string GSTRate { get; set; }
    }
}
