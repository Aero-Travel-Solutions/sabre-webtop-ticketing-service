using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    //Example 01
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

    //Example 02
    //01NOV DEPARTURE DATE-----LAST DAY TO PURCHASE 11FEB/23:59
    //BASE FARE        TAXES/FEES/CHARGES   
    //2-    AUD844.00                AUD133.63    XT AUD977.63    ADT
    //XT    60.00    AU    6.50    WG    32.08    WY    14.15    QR
    //11.70    G3    9.20    I5
    //1688.00        267.26
    //TOTAL:AUD1955.26
    //FOP FEES PER TICKET MAY APPLY
    //ADT-2 QIQW SSVNOV
    //MEL QF SYD119.22QF X/HKG QF SGN507.24NUC626.46END ROE1.346491
    //CARRIER RESTRICTION APPLY/FEES APPLY
    //VALID ON QF SERVICES ONLY
    //VALIDATING CARRIER - QF
    //CAT 15 SALES RESTRICTIONS FREE TEXT FOUND - VERIFY RULES
    //CHANGE BOOKING CLASS - 1Q 2S 3S
    //FORM OF PAYMENT FEES PER TICKET MAY APPLY
    //ADT    DESCRIPTION FEE
    //.
    //
    //Example 03
    //01NOV DEPARTURE DATE-----LAST DAY TO PURCHASE 11FEB/2359
    //       BASE FARE                 TAXES/FEES/CHARGES TOTAL
    // 2-   AUD2206.00                    217.14XT AUD2423.14ADT
    //XT     60.00AU       6.50WG      64.16WY      32.78QR 
    //           11.80G3       9.20I5      30.40TS       0.70G8 
    //            1.60E7 
    //         4412.00                    434.28           4846.28TTL
    //ADT-02  QIQW SSVNV KSVNV KIQW
    // MEL QF SYD119.22QF X/HKG QF SGN389.90//BKK QF SYD616.41QF PER
    // 512.48NUC1638.01END ROE1.346491
    //CARRIER RESTRICTION APPLY/FEES APPLY
    //VALID ON QF SERVICES ONLY
    //VALIDATING CARRIER SPECIFIED - QF
    //CAT 15 SALES RESTRICTIONS FREE TEXT FOUND - VERIFY RULES
    //CHANGE BOOKING CLASS -   1Q 2S 3S 6K

    //FORM OF PAYMENT FEES PER TICKET MAY APPLY
    //ADT      DESCRIPTION FEE      TKT TOTAL
    // OBFCA - CC NBR BEGINS WITH 1081        0.00        2423.14
    // OBFCA - CC NBR BEGINS WITH 1611        0.00        2423.14
    // OBFCA - CARD FEE                      25.50        2448.64
    // OBFDA - CC NBR BEGINS WITH 1081        0.00        2423.14
    // OBFDA - CC NBR BEGINS WITH 1611        0.00        2423.14
    // OBFDA - CC NBR BEGINS WITH 3          10.90        2434.04
    // OBFDA - CC NBR BEGINS WITH 4          10.90        2434.04
    // OBFDA - CC NBR BEGINS WITH 5           8.30        2431.44
    // OBFDA - CC NBR BEGINS WITH 6          10.90        2434.04
    // OBFDA - CC NBR BEGINS WITH 2           8.30        2431.44
    // OBFDA - CARD FEE                       8.30        2431.44
                                                               
    //AIR EXTRAS AVAILABLE - SEE WP* AE
    //BAGGAGE INFO AVAILABLE - SEE WP* BAG
    //.


    internal class SabreBestBuyQuote
    {
        string gdsresponse = "";
        string fullgdsresponse = "";
        public SabreBestBuyQuote(string res)
        {
            if (!res.Contains("TAXES/FEES/CHARGES")) { throw new AeronologyException("INVALID_GDS_RESPONSE", res); }
            fullgdsresponse = res;
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

            string[] farecalcitems = string.
                                        Join(",", 
                                                gdsresponse.
                                                SplitOnRegex(@"([ACI][DHN][TDF]-\d+.*)").
                                                Last().
                                                SplitOn("\n").
                                                TakeWhile(t => !t.StartsWith("VALIDATING CARRIER SPECIFIED"))).
                                                SplitOnRegex(@"(ROE\d+\.\d+)\s*,");

            string basefare = gdsresponse.
                                        SplitOn("\n").
                                        First().
                                        SplitOnRegex(@"([A-Z]{3}\d+\.\d+)")[1];

            List<string> farebasis = gdsresponse.
                                        SplitOnRegex(@"[ACI][DHN][TDF]-\d+(.*)")[1].
                                        SplitOnRegex(@"\s+").
                                        Where(w=> !string.IsNullOrEmpty(w)).
                                        Distinct().
                                        ToList();

            List<FBData> fBData = gdsresponse.
                                        SplitOnRegex(@"CHANGE\sBOOKING\sCLASS\s*-").
                                        Last().
                                        SplitOn("FORM OF PAYMENT FEES PER TICKET MAY APPLY").
                                        First().
                                        SplitOnRegex(@"\s+").
                                        Where(w=> !string.IsNullOrEmpty(w)).
                                        Distinct().
                                        Select(s => new FBData()
                                        {
                                            SectorNo = int.Parse(s.LastMatch(@"(\d+)[A-Z]")),
                                            BookingClass = s.LastMatch(@"\d+([A-Z])"),
                                            Farebasis = farebasis.First(f => f.Substring(0, 1) == s.LastMatch(@"\d+([A-Z])"))
                                        }).
                                        ToList();


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
                                PriceHint = s.bestbuypricehint.PriceHint,
                                FareCalculation = farecalcitems[0].Trim() + farecalcitems[1].Trim(),
                                ROE = farecalcitems[1].SplitOn("ROE").Last(),
                                CCFeeData = fullgdsresponse.
                                                SplitOn("FORM OF PAYMENT FEES PER TICKET MAY APPLY").
                                                Last().
                                                Trim(),
                                Endorsements = gdsresponse.
                                                        SplitOnRegex(@"(ROE\d+\.\d+)\s*").
                                                        Skip(1).
                                                        TakeWhile(t=> t.StartsWith("VALIDATING CARRIER")).
                                                        ToList(),
                                BaseFare = decimal.Parse(basefare.Substring(3)),
                                Farebasis = fBData,
                                LastPurchaseDate = fullgdsresponse.
                                                        SplitOn("\n").
                                                        First().
                                                        SplitOn("LAST DAY TO PURCHASE").
                                                        Last().
                                                        Trim()
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
        public string LastPurchaseDate { get; set; }
        public string PaxType { get; set; }
        public decimal BaseFare { get; set; }
        public List<Tax> Taxes { get; set; }
        public string PriceHint { get; set; }
        public string FareCalculation { get; set; }
        public string ROE { get; set; }
        public string CCFeeData { get; set; }
        public List<string> Endorsements { get; set; }
        public List<FBData> Farebasis { get; set; }
    }

    public class FBData
    {
        public int SectorNo { get; set; }
        public string BookingClass { get; set; }
        public string Farebasis { get; set; }
    }
}
