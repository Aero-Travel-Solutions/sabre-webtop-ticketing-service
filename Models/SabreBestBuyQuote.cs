using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
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

    internal class SabreBestBuyQuote : IBestBuyQuote
    {
        string gdsresponse = "";
        string fullgdsresponse = "";
        List<int> selectedsectors = null;
        List<PNRSector> pnrsecs = null;
        string wpbag = "";

        public SabreBestBuyQuote(string res, List<int> selsec, List<PNRSector> pnrsec, string wpbagres = "")
        {
            wpbag = wpbagres;
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
            List<BaggageInfo> baggageinfo = GetBestBuyBaggageAllowance(wpbag);

            for (int i = 1; i < items.Skip(1).Count(); i += 2)
            {
                string paxtype = items[i].SplitOn("-").First();
                List<TaxInfo> taxitems = new List<TaxInfo>();

                var taxlines = items[0].
                                    SplitOnRegex(@"(\d+\s*-.*)").
                                    Where(w => !string.IsNullOrEmpty(w)).
                                    ToList();

                int paxtypeindex = taxlines.FindLastIndex(f => f.IsMatch(@"\d+\s*-.*" + paxtype));
                if (paxtypeindex != -1 && taxlines[paxtypeindex + 1].Trim().StartsWith("XT"))
                {
                    taxitems.Add(new TaxInfo(taxlines[paxtypeindex + 1].Replace("\n", "###")));
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
                            Farebasis = selectedfarebasis,
                            Baggage = baggageinfo.
                                        First(w=> 
                                            w.PaxType == paxtype).
                                        SectorBags.
                                        First(f => f.From == pnrsec.From &&
                                                   f.To == pnrsec.To).
                                        BaggageAllowance
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

        private List<BaggageInfo> GetBestBuyBaggageAllowance(string wpbag)
        {
            //ADT - 01
            //BAG ALLOWANCE     -SYDWLG - 30KG / QF
            //BAG ALLOWANCE     -AKLMEL - 30KG / QF
            //CARRY ON ALLOWANCE
            //SYDWLG AKLMEL - 01P / QF
            //01 / UP TO 15 POUNDS / 7 KILOGRAMS AND UP TO 45 LINEAR INCHES/ 115 L
            //     INEAR CENTIMETERS
            //     CARRY ON CHARGES
            //SYDWLG AKLMEL-QF - CARRY ON FEES UNKNOWN - CONTACT CARRIER
            //    ADDITIONAL ALLOWANCES AND/ OR DISCOUNTS MAY APPLY
            //CNN - 01
            //BAG ALLOWANCE     -SYDWLG - 30KG / QF
            //BAG ALLOWANCE     -AKLMEL - 30KG / QF
            //CARRY ON ALLOWANCE
            //SYDWLG AKLMEL - 01P / QF
            //01 / UP TO 15 POUNDS / 7 KILOGRAMS AND UP TO 45 LINEAR INCHES/ 115 L
            //     INEAR CENTIMETERS
            //     CARRY ON CHARGES
            //SYDWLG AKLMEL-QF - CARRY ON FEES UNKNOWN - CONTACT CARRIER
            //    ADDITIONAL ALLOWANCES AND/ OR DISCOUNTS MAY APPLY


            //EMBARGOES - APPLY TO EACH PASSENGER
            //  SYDWLG AKLMEL - QF
            //SPORTING EQUIPMENT/ SURFBOARD UP TO 109 INCHES / 277 CENTIMETERS N
            //OT PERMITTED
            //OVER 70 POUNDS / 32 KILOGRAMS NOT PERMITTED.

            List<BaggageInfo> baginfo = new List<BaggageInfo>();
            string[] items = wpbag.SplitOnRegex(@"([ACI][DHN][TDFN]-\d+)");

            for (int i = 1; i < items.Skip(1).Count(); i+=2)
            {
                baginfo.
                    Add(new BaggageInfo()
                    {
                        PaxType = items[i].SplitOn("-").First().Trim(),
                        SectorBags = items[i+1].
                                        SplitOn("\n").
                                        Where(w=> w.IsMatch(@"BAG\sALLOWANCE\s+-(.*)")).
                                        Select(s => s.LastMatch(@"BAG\sALLOWANCE\s+-(.*)")).
                                        Select(s=> s.SplitOn("/").First().Trim()).
                                        Select(s=> new SectorBag()
                                        {
                                            From = s.Substring(0,3),
                                            To = s.Substring(3,3),
                                            BaggageAllowance = s.SplitOn("-").Last().Trim()
                                        }).
                                        ToList()
                    });
            }
            return baginfo;
        }
    }

    internal class BaggageInfo
    {
        public string PaxType { get; set; }
        public List<SectorBag> SectorBags { get; set; }
    }

    public class SectorBag
    {
        public string From { get; set; }
        public string To { get; set; }
        public string BaggageAllowance { get; set; }
    }
}
