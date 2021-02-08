﻿using SabreWebtopTicketingService.Common;
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

    //Example 04

    //BASE FARE      EQUIV AMT  TAXES/FEES/CHARGES TOTAL
    // 1-     THB12165 AUD534.00      97.40XT AUD631.40ADT
    //XT     21.00WY      43.40YQ      30.70TS       0.70G8 
    //            1.60E7 
    // 1-      THB9125 AUD401.00      97.40XT AUD498.40CNN
    //XT     21.00WY      43.40YQ      30.70TS       0.70G8 
    //            1.60E7 
    //           21290         935.00     194.80           1129.80TTL
    //ADT-01  K1LEOTG
    // BKK TG MEL402.72NUC402.72END ROE30.2068
    //NON ENDORSE/CANCELLATION/CHANGE FEE APPLIED/RFND/NOT LATER THAN
    // 90 DAYS/AFTER TKT EXPIRY
    //VALIDATING CARRIER SPECIFIED - TG
    //CAT 15 SALES RESTRICTIONS FREE TEXT FOUND - VERIFY RULES
    //CHANGE BOOKING CLASS -   2K
    //CNN-01  K1LEOTG/CH
    // BKK TG MEL302.04NUC302.04END ROE30.2068
    //NON ENDORSE/CANCELLATION/CHANGE FEE APPLIED/RFND/NOT LATER THAN
    // 90 DAYS/AFTER TKT EXPIRY
    //VALIDATING CARRIER SPECIFIED - TG
    //CAT 15 SALES RESTRICTIONS FREE TEXT FOUND - VERIFY RULES
    //CHANGE BOOKING CLASS -   2K

    //FORM OF PAYMENT FEES PER TICKET MAY APPLY
    //ADT      DESCRIPTION FEE      TKT TOTAL
    // OBFCA - CC NBR BEGINS WITH 36         20.90         652.30
    // OBFCA - CC NBR BEGINS WITH 37         19.20         650.60
    // OBFCA - CC NBR BEGINS WITH 4           6.40         637.80
    // OBFCA - CC NBR BEGINS WITH 5           6.40         637.80

    //CNN DESCRIPTION                     FEE TKT TOTAL
    // OBFCA - CC NBR BEGINS WITH 36         16.50         514.90
    // OBFCA - CC NBR BEGINS WITH 37         15.10         513.50
    // OBFCA - CC NBR BEGINS WITH 4           5.00         503.40
    // OBFCA - CC NBR BEGINS WITH 5           5.00         503.40

    //AIR EXTRAS AVAILABLE - SEE WP* AE
    //BAGGAGE INFO AVAILABLE - SEE WP* BAG
    //.


    //ABACUS
    //PSGR TYPE  ADT - 01
    // CXR RES DATE FARE BASIS NVB   NVA BG
    // HKG
    // TPE HX N   05AUG NAVT1HS         05AUG 05AUG 30K
    //FARE  HKD       750  
    //TAX HKD       120HK HKD        90G3 HKD       105XT
    //TOTAL HKD      1065
    //ADT-01  NAVT1HS
    // HKG HX TPE96.42NUC96.42END ROE7.75325
    //XT HKD50I5 HKD55YR
    //ENDOS* SEG1* Q/NONEND RFD HKD700/NOSH HKD1000
    //ATTN* PRIVATE FARE APPLIED - CHECK RULES FOR CORRECT TICKETING
    //ATTN* PRIVATE Â¤
    //ATTN* VALIDATING CARRIER SPECIFIED - HX
    //ATTN* CHANGE BOOKING CLASS -   1N
    //ATTN* BAGGAGE INFO AVAILABLE - SEE WP* BAG
    // .



    internal class SabreBestBuyQuote
    {
        string gdsresponse = "";
        string fullgdsresponse = "";
        List<int> selectedsectors = null;
        List<PNRSector> pnrsecs = null;

        public SabreBestBuyQuote(string res, List<int> selsec, List<PNRSector> pnrsec)
        {
            if (!res.Contains("TAXES/FEES/CHARGES")) { throw new AeronologyException("INVALID_GDS_RESPONSE", res); }
            selectedsectors = selsec;
            pnrsecs = pnrsec;
            fullgdsresponse = res;
            gdsresponse = res.SplitOnRegex(@"BASE\s+FARE\s+TAXES/FEES/CHARGES\s+TOTAL").Last().SplitOn("FORM OF PAYMENT FEES PER TICKET MAY APPLY").First().Trim();

        }

        public List<BestBuyItem> BestBuyItems => GetBestBuyItems();

        private List<BestBuyItem> GetBestBuyItems()
        {
            List<BestBuyItem> bestBuyItems = new List<BestBuyItem>();
            var items = gdsresponse.SplitOnRegex(@"([ACI][DHN][TDFN]-\d+.*)");
            List<string> usedfbs = new List<string>();
            //List<FBData> fBData = new List<FBData>();
            List<SectorFBData> sectors = new List<SectorFBData>();

            for (int i = 1; i < items.Skip(1).Count(); i += 2)
            {
                string paxtype = items[i].SplitOn("-").First();
                List<TaxInfo> taxitems = new List<TaxInfo>();

                var taxlines = items[0].
                                    SplitOnRegex(@"(\d+\s*-.*)").
                                    Where(w => !string.IsNullOrEmpty(w)).
                                    ToList();

                int paxtypeindex = taxlines.FindLastIndex(f => f.IsMatch(@"\d+\s*-.*" + paxtype));
                if (paxtypeindex != -1)
                {
                    taxitems.Add(new TaxInfo(taxlines[paxtypeindex].Replace("\n", "###") + taxlines[paxtypeindex + 1].Replace("\n", "###")));
                }

                //single currency
                string basefare = items[0].Contains("EQUIV AMT") ?
                                    taxlines[paxtypeindex].
                                        SplitOnRegex(@"\d+\s*-\s+[A-Z]{3}\d+\.{0,1}\d*\s+[A-Z]{3}(\d+\.{0,1}\d*)\s+")[1] :
                                    taxlines[paxtypeindex].
                                        SplitOnRegex(@"\d+\s*-\s+[A-Z]{3}(\d+\.{0,1}\d*)")[1];


                string[] farebasis = items[i].SplitOnRegex(@"[ACI][DHN][TDFN]-\d+(.*)")[1].SplitOnRegex(@"\s+").Where(w=> !string.IsNullOrEmpty(w)).ToArray();
                string pricehint = items[i + 1].Contains("CHANGE BOOKING CLASS") ?
                                        items[i + 1].SplitOn("\n").First(f => f.StartsWith("CHANGE BOOKING CLASS")) :
                                        "";

                string[] farecalcitems = string.Join("", items[i + 1].
                                                    SplitOn("\n").
                                                    TakeWhile(t => !t.StartsWith("VALIDATING CARRIER SPECIFIED"))).
                                                    SplitOnRegex(@"(ROE\d+\.\d+)\s*");

                List<string> endos = items[i + 1].Contains("ROE") ?
                                        items[i + 1].
                                            SplitOnRegex(@"(ROE\d+\.\d+)\s*").
                                            Last().
                                            SplitOn("\n").
                                            TakeWhile(t => !t.StartsWith("VALIDATING CARRIER SPECIFIED")).
                                            ToList() :
                                        items[i + 1].
                                            SplitOnRegex(@"(END)").
                                            Last().
                                            SplitOn("\n").
                                            TakeWhile(t => !t.StartsWith("VALIDATING CARRIER SPECIFIED")).
                                            ToList();

                IEnumerable<string> changesecs = items[i + 1].Contains("CHANGE BOOKING CLASS") ?
                                                    items[i+1].
                                                    SplitOnRegex(@"CHANGE\sBOOKING\sCLASS\s*-").
                                                    Last().
                                                    SplitOn("FORM OF PAYMENT FEES PER TICKET MAY APPLY").
                                                    First().
                                                    SplitOnRegex(@"\s+").
                                                    Where(w => !string.IsNullOrEmpty(w)).
                                                    Distinct() :
                                                    null;

                List<string> ccfeedataarray = fullgdsresponse.
                                        SplitOn("FORM OF PAYMENT FEES PER TICKET MAY APPLY").
                                        Last().
                                        SplitOnRegex(@"([ACI][DHN][TDFN]\s+DESCRIPTION\.*)").
                                        ToList();

                string ccfeedata = "";
                int ccfeedataindex = ccfeedataarray.FindIndex(f => f.IsMatch(@$"{paxtype}\s+DESCRIPTION"));
                if(ccfeedataindex != -1)
                {
                    ccfeedata = ccfeedataarray[ccfeedataindex] + ccfeedataarray[ccfeedataindex + 1].SplitOn("AIR EXTRAS AVAILABLE").First().Trim();
                }


                for (int j = 0; j < selectedsectors.Count; j++)
                {
                    int sectorno = selectedsectors[j];
                    PNRSector pnrsec = pnrsecs.First(p => p.SectorNo == sectorno);
                    if (pnrsec.From == "ARUNK") { continue; }
                    string changesec = changesecs.IsNullOrEmpty()? "" :  changesecs.FirstOrDefault(f => int.Parse(f.LastMatch(@"(\d+)[A-Z]")) == sectorno);
                    string selectedfarebasis = string.IsNullOrEmpty(changesec) ?
                                                    usedfbs.IsNullOrEmpty() ?
                                                        farebasis.
                                                            First(f => pnrsec.Class == f.Substring(0, 1)) :
                                                        string.IsNullOrEmpty(farebasis.FirstOrDefault(f => !usedfbs.Contains(f) && pnrsec.Class == f.Substring(0, 1))) ?
                                                            farebasis.First(f => pnrsec.Class == f.Substring(0, 1)) :
                                                            farebasis.First(f => !usedfbs.Contains(f) && pnrsec.Class == f.Substring(0, 1)) :
                                                        farebasis.First(f => f.Substring(0, 1) == changesec.LastMatch(@"\d+([A-Z])"));

                    sectors.
                        Add(new SectorFBData()
                        {
                            SectorNo = sectorno,
                            Farebasis = selectedfarebasis
                        });

                    usedfbs.Add(selectedfarebasis);
                }

                bestBuyItems.
                    Add(new BestBuyItem()
                    {
                        PaxType = paxtype,
                        Farebasis = sectors,
                        PriceHint = pricehint,
                        CCFeeData = ccfeedata,
                        LastPurchaseDate = fullgdsresponse.
                                                SplitOn("\n").
                                                First().
                                                SplitOn("LAST DAY TO PURCHASE").
                                                Last().
                                                Trim(),
                        Endorsements = endos,
                        FareCalculation = farecalcitems.Count() == 1? farecalcitems.First().SplitOn("END").First(): farecalcitems.First(),
                        ROE = farecalcitems.Count() == 1? "1.0000": farecalcitems[1].Substring(3).Trim(),
                        Taxes = taxitems.
                                    SelectMany(s => s.Taxes).
                                    Select(s => new Tax()
                                    {
                                        Code = s.Code,
                                        Amount = s.Amount
                                    }).
                                    ToList(),
                        BaseFare = decimal.Parse(basefare)
                    });
            }

            return bestBuyItems;
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
                                    Where(w => w.Trim().Last(2) != "XT").
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
        public List<SectorFBData> Farebasis { get; set; }
    }

    public class SectorFBData
    {
        public int SectorNo { get; set; }
        public string Farebasis { get; set; }
    }
}
