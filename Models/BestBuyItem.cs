using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class BestBuyItem : IBestBuyItem
    {
        public string LastPurchaseDate { get; set; }
        public string PaxType { get; set; }
        public decimal BaseFare { get; set; }
        public string BaseFareCurrency { get; set; }
        public decimal? EquivFare { get; set; }
        public string EquivFareCurrency { get; set; }
        public List<Tax> Taxes { get; set; }
        public string PriceHint { get; set; }
        public string FareCalculation { get; set; }
        public string ROE { get; set; }
        public string CCFeeData { get; set; }
        public List<string> Endorsements { get; set; }
        public List<SectorFBData> Farebasis { get; set; }
        public string PlatingCarrier { get; set; }
    }

    public class SectorFBData
    {
        public int SectorNo { get; set; }
        public string Farebasis { get; set; }
        public string NVB { get; set; }
        public string NVA { get; set; }
        public string Baggage { get; set; }
    }


    public class PriceHintInfo
    {
        private string s;

        public PriceHintInfo(string s)
        {
            this.s = s;
        }

        public string PaxType => s.Trim().Substring(0, 3);
        public string PriceHint => s.SplitOn("###").Skip(1).Where(w => !string.IsNullOrEmpty(w)).First(w => w.Trim().StartsWith("CHANGE BOOKING CLASS"));
    }

    public class TaxInfo
    {
        private string gdsresponse;

        public TaxInfo(string s)
        {
            this.gdsresponse = s.Replace("\n", "###");
        }

        public string PaxType => gdsresponse.SplitOn("###").First().Last(3);
        public List<Tax> Taxes => gdsresponse.
                                    AllMatches(@"(\d+\.*\d*\w{2})\s+").
                                    Where(w => w.Trim().Last(2) != "XT").
                                    Select(tax => new Tax()
                                    {
                                        Code = tax.Trim().Last(2),
                                        Amount = decimal.Parse(tax.Trim().Substring(0, tax.Trim().Length - 2))
                                    }).
                                    ToList();
    }

}
