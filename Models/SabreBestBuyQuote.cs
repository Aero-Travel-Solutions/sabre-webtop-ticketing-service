using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    //BASE FARE                 TAXES/FEES/CHARGES TOTAL
    // 1-    AUD810.00                    208.06XT AUD1018.06ADT
    //XT     60.00AU      64.16WY      28.30QR      38.40SG 
    //           11.00L7       6.20OP 
    //          810.00                    208.06           1018.06TTL
    //ADT-01  QSA
    // MEL QF X/SYD QF SIN277.32QF X/SYD QF MEL Q28.81 277.32NUC
    // 583.45END ROE1.388254
    //CARRIER RESTRICTIONS APPLY/FEES APPLY
    //VALIDATING CARRIER SPECIFIED - QF
    //CAT 15 SALES RESTRICTIONS FREE TEXT FOUND - VERIFY RULES
    //CHANGE BOOKING CLASS -   1Q

    //FORM OF PAYMENT FEES PER TICKET MAY APPLY
    //ADT      DESCRIPTION FEE      TKT TOTAL
    // OBFCA - CC NBR BEGINS WITH 1081        0.00        1018.06
    // OBFCA - CC NBR BEGINS WITH 1611        0.00        1018.06
    // OBFCA - CARD FEE                      10.70        1028.76
    // OBFDA - CC NBR BEGINS WITH 1081        0.00        1018.06
    // OBFDA - CC NBR BEGINS WITH 1611        0.00        1018.06
    // OBFDA - CC NBR BEGINS WITH 3           4.60        1022.66
    // OBFDA - CC NBR BEGINS WITH 4           4.60        1022.66
    // OBFDA - CC NBR BEGINS WITH 5           3.50        1021.56
    // OBFDA - CC NBR BEGINS WITH 6           4.60        1022.66
    // OBFDA - CC NBR BEGINS WITH 2           3.50        1021.56
    // OBFDA - CARD FEE                       3.50        1021.56
                                                               
    //AIR EXTRAS AVAILABLE - SEE WP* AE
    //BAGGAGE INFO AVAILABLE - SEE WP* BAG
    //.
    internal class SabreBestBuyQuote
    {
        string gdsresponse = "";
        List<string> lines = new List<string>();
        public SabreBestBuyQuote(string res)
        {
            if (!res.Contains("TAXES/FEES/CHARGES")) { throw new AeronologyException("INVALID_GDS_RESPONSE", res); }
            gdsresponse = res.SplitOnRegex(@"BASE\s+FARE\s+TAXES/FEES/CHARGES\s+TOTAL").Last().SplitOn("FORM OF PAYMENT FEES PER TICKET MAY APPLY").First().Trim();

        }

        public List<BestBuyItem> BestBuyItems => GetBestBuyItems(gdsresponse);

        private List<BestBuyItem> GetBestBuyItems(string gdsresponse)
        {
            List<TaxInfo> taxitems = new List<TaxInfo>();
            var taxlines = gdsresponse.
                                SplitOnRegex(@"[ACI][DHN][TDF]-\d+").
                                First().
                                SplitOnRegex(@"(\d+\s*-.*)").
                                Where(w => !string.IsNullOrEmpty(w)).
                                ToList();

            for (int i = 0; i < taxlines.Count(); i+=2)
            {
                taxitems.Add(new TaxInfo(taxlines[i].Replace("\n", "###") + taxlines[i + 1].Replace("\n", "###")));
            }

            List<PriceHintInfo> pricehintitems = new List<PriceHintInfo>();
            var pricehintlines = gdsresponse.
                                    SplitOnRegex(@"([ACI][DHN][TDF]-\d+.*)").
                                    Skip(1).
                                    ToList();

            for (int i = 0; i < taxlines.Count(); i += 2)
            {
                pricehintitems.Add(new PriceHintInfo(pricehintlines[i].Replace("\n", "###") + pricehintlines[i + 1].Replace("\n", "###")));
            }

            var items = taxitems.
                            Select(bestbuy => new
                            {
                                bestbuytax = bestbuy,
                                bestbuypricehint = pricehintitems.
                                        FirstOrDefault(f =>
                                            f.PaxType == bestbuy.PaxType)
                            }).
                            Select(s => new BestBuyItem()
                            {
                                PaxType = s.bestbuytax.PaxType,
                                Taxes = s.bestbuytax.Taxes,
                                PriceHint = s.bestbuypricehint.PriceHint
                            }).
                            ToList();

            return items;
        }
    }

    internal class PriceHintInfo
    {
        private string s;

        public PriceHintInfo(string s)
        {
            this.s = s;
        }

        public string PaxType => s.Trim().Substring(0, 3);
        public string PriceHint => s.SplitOn("###").Skip(1).Where(w=> !string.IsNullOrEmpty(w)).First(w => w.Trim().StartsWith("CHANGE BOOKING CLASS"));
    }

    internal class TaxInfo
    {
        private string gdsresponse;

        public TaxInfo(string s)
        {
            this.gdsresponse = s.Replace("\n", "###");
        }

        public string PaxType => gdsresponse.SplitOn("###").First().Last(3);
        public List<Tax> Taxes => gdsresponse.
                                    AllMatches(@"(\d+\.\d{2}\w{2})\s+").
                                    Skip(1).
                                    Select(tax => new Tax() 
                                    {
                                        Code = tax.Trim().Last(2),
                                        Amount = decimal.Parse(tax.Trim().Substring(0, tax.Trim().Length - 2))
                                    }).
                                    ToList();
    }

    internal class BestBuyItem
    {
        public string PaxType { get; set; }
        public List<Tax> Taxes { get; set; }
        public string PriceHint { get; set; }
    }
}
