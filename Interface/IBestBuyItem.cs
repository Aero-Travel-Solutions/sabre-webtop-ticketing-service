using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Interface
{
    public interface IBestBuyQuote
    {
       public List<BestBuyItem> BestBuyItems { get; }
    }

    public interface IBestBuyItem
    {
        public string LastPurchaseDate { get; set; }
        public string PaxType { get; set; }
        public decimal BaseFare { get; set; }
        public string BaseFareCurrency { get; set; }
        public List<Tax> Taxes { get; set; }
        public string PriceHint { get; set; }
        public string FareCalculation { get; set; }
        public string ROE { get; set; }
        public string CCFeeData { get; set; }
        public List<string> Endorsements { get; set; }
        public List<SectorFBData> Farebasis { get; set; }
    }
}
