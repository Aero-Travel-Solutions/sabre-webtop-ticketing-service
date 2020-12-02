using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    internal class SabreBestBuyQuote
    {
        string gdsresponse = "";
        List<string> lines = new List<string>();
        public SabreBestBuyQuote(string res)
        {
            if (!Sucess) { throw new AeronologyException("INVALID_GDS_RESPONSE", res); }
            gdsresponse = res.SplitOn("FORM OF PAYMENT FEES PER TICKET MAY APPLY")[0];
        }

        public bool Sucess => gdsresponse.Contains("BASE FARE                 TAXES/FEES/CHARGES");

        public List<string> PricingHint => lines.
                                              Where(w => w.StartsWith("CHANGE BOOKING CLASS")).
                                              Distinct().
                                              ToList();

        public List<BestBuyItem> BestBuyItems => GetBestBuyItems(gdsresponse);

        private List<BestBuyItem> GetBestBuyItems(string gdsresponse)
        {
            throw new NotImplementedException();
        }
    }

    internal class BestBuyItem
    {
        string gdsresponse = "";
        public BestBuyItem(string res)
        {
            gdsresponse = res;
        }

        public string PaxType { get; set; }

        public List<Tax> Taxes { get; set; }


    }
}
