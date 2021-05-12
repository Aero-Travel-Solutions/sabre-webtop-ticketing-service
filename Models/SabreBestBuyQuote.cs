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

    //Example 05
    //1-   AUD1320.00                    229.10XT AUD1549.10ADT
    //XT     60.00AU       4.00WG      42.00WY      77.50YQ 
    //            5.60AX       1.20C4      32.30KX       6.50L5 
    //    1-    AUD990.00                    150.20XT AUD1140.20CNN
    //XT      4.00WG      42.00WY      77.50YQ       2.80AX 
    //            0.60C4      16.80KX       6.50L5 
    //    1-    AUD132.00                     15.60YQ AUD147.60INF
    //            2442.00                    394.90           2836.90TTL
    //ADT-01  LH1YAUF LL1YAUF
    //    MEL VN X/SGN VN HAN606.56/-PNH VN X/SGN VN MEL400.56NUC1007.12
    //    END ROE1.310655
    //RESTRICTIONS MAY APPLY./NON-END.
    //PRIVATE FARE APPLIED - CHECK RULES FOR CORRECT TICKETING
    //PRIVATE Â¤
    //VALIDATING CARRIER - VN
    //CHANGE BOOKING CLASS -   5L
    //CNN-01  LH1YAUF/CH25 LL1YAUF/CH25
    //    MEL VN X/SGN VN HAN454.92/-PNH VN X/SGN VN MEL300.42NUC755.34
    //    END ROE1.310655
    //RESTRICTIONS MAY APPLY./NON-END.
    //EACH CNN REQUIRES ACCOMPANYING SAME CABIN ADT
    //PRIVATE FARE APPLIED - CHECK RULES FOR CORRECT TICKETING
    //PRIVATE Â¤
    //VALIDATING CARRIER - VN
    //CHANGE BOOKING CLASS -   5L
    //INF-01  LH1YAUF/IN90 LL1YAUF/IN90
    //    MEL VN X/SGN VN HAN60.65/-PNH VN X/SGN VN MEL40.05NUC100.70
    //    END ROE1.310655
    //RESTRICTIONS MAY APPLY./NON-END.
    //PRIVATE FARE APPLIED - CHECK RULES FOR CORRECT TICKETING
    //EACH INF REQUIRES ACCOMPANYING ADT PASSENGER
    //PRIVATE Â¤
    //VALIDATING CARRIER - VN
    //CHANGE BOOKING CLASS -   5L
                                                               
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
            List<SectorFBData> sectors = null;
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
                if (paxtypeindex != -1)
                {
                    if (taxlines[paxtypeindex + 1].Trim().StartsWith("XT"))
                    {
                        taxitems.Add(new TaxInfo(string.Join("###", taxlines[paxtypeindex + 1].SplitOn("\n").Where(w => !w.Contains("TTL")))));
                    }
                    else
                    {
                        var totalitems = taxlines[paxtypeindex].SplitOnRegex(@"\s+");
                        if (totalitems[totalitems.Count() - 2].IsMatch(@"\s+\d+\.*\d*\w{2}\s+"))
                        {
                            taxitems.Add(new TaxInfo(totalitems[totalitems.Count() - 2]));
                        }
                    }
                }

                //base fare
                string basefare = taxlines[paxtypeindex].
                                        SplitOnRegex(@"\d+\s*-\s+[A-Z]{3}(\d+\.{0,1}\d*)")[1];

                string basefarecurrency = taxlines[paxtypeindex].
                                        SplitOnRegex(@"\d+\s*-\s+([A-Z]{3})\d+\.{0,1}\d*")[1];


                //equiv fare
                string equivfare = items[0].Contains("EQUIV AMT") ?
                    taxlines[paxtypeindex].
                        SplitOnRegex(@"\d+\s*-\s+[A-Z]{3}\d+\.{0,1}\d*\s+[A-Z]{3}(\d+\.{0,1}\d*)\s+")[1] :
                    taxlines[paxtypeindex].
                                        SplitOnRegex(@"\d+\s*-\s+[A-Z]{3}(\d+\.{0,1}\d*)")[1];

                string equivfarecurrency = items[0].Contains("EQUIV AMT") ?
                                    taxlines[paxtypeindex].
                                        SplitOnRegex(@"\d+\s*-\s+[A-Z]{3}\d+\.{0,1}\d*\s+([A-Z]{3})\d+\.{0,1}\d*\s+")[1] :
                                    taxlines[paxtypeindex].
                                        SplitOnRegex(@"\d+\s*-\s+([A-Z]{3})\d+\.{0,1}\d*")[1];

                string[] farebasis = items[i].SplitOnRegex(@"[ACI][DHN][TDFN]-\d+(.*)")[1].SplitOnRegex(@"\s+").Where(w=> !string.IsNullOrEmpty(w)).ToArray();
                string pricehint = items[i + 1].Contains("CHANGE BOOKING CLASS") ?
                                        items[i + 1].SplitOn("\n").First(f => f.StartsWith("CHANGE BOOKING CLASS")) :
                                        "";

                List<string> farecalcitems = new List<string>();
                if (items[i + 1].Contains("ROE"))
                {
                    farecalcitems = string.Join(" ", items[i + 1].
                                                    SplitOn("\n").
                                                    TakeWhile(t => !t.StartsWith("VALIDATING CARRIER"))).
                                                    SplitOnRegex(@"(ROE\d+\.\d+)\s*").
                                                    ToList();
                }

                string farecalclines = string.Join(" ", items[i + 1].
                                                    SplitOn("\n").
                                                    TakeWhile(t => !t.StartsWith("VALIDATING CARRIER")));
                int splitindex = farecalclines.IndexOf("END");
                if (splitindex > 0)
                {
                    farecalcitems.Add(farecalclines.Substring(0, splitindex).Trim());
                    farecalcitems.Add("END");
                    farecalcitems.Add(farecalclines.Substring(splitindex + 3));
                }

                List<string> endos = items[i + 1].Contains("ROE") ?
                                        items[i + 1].
                                            SplitOn("\n").
                                            SkipWhile(s => !s.Contains("ROE")).
                                            Skip(1).
                                            TakeWhile(t => !t.StartsWith("VALIDATING CARRIER")).
                                            ToList() :
                                        items[i + 1].
                                            SplitOn("\n").
                                            SkipWhile(s => !s.Contains("END")).
                                            Skip(1).
                                            TakeWhile(t => !t.StartsWith("VALIDATING CARRIER")).
                                            ToList();

                endos = endos.
                            Where(w => 
                                    !w.StartsWith("PRIVATE FARE APPLIED") && 
                                    !w.StartsWith("RATE USED") &&
                                    !w.StartsWith("PRIVATE Â¤") &&
                                    !w.IsMatch(@"EACH \w{3} REQUIRES ACCOMPANYING ADT PASSENGER") &&
                                    !w.IsMatch(@"EACH \w{3} REQUIRES ACCOMPANYING ADT") &&
                                    !w.IsMatch(@"EACH \w{3} REQUIRES ACCOMPANYING SAME CABIN ADT")).
                            Distinct().
                            ToList();

                //remove XF and ZP from endorsements
                if (!endos.IsNullOrEmpty())
                {
                    endos[0] = endos[0].SplitOnRegex(@"(XF[A-Z]{3}\d+\.*\d*)").Last();
                    endos[0] = endos[0].SplitOnRegex(@"(ZP[A-Z]{3})").Last();
                }

                IEnumerable<string> changesecs = items[i + 1].Contains("CHANGE BOOKING CLASS") ?
                                                    items[i+1].
                                                    SplitOn("\n").
                                                    FirstOrDefault(w => w.IsMatch(@"CHANGE\sBOOKING\sCLASS\s*-"))?.
                                                    SplitOnRegex(@"CHANGE\sBOOKING\sCLASS\s*-").
                                                    Last().
                                                    SplitOnRegex(@"\s+").
                                                    Where(w => !string.IsNullOrEmpty(w)).
                                                    Distinct() :
                                                    null;
                string platingcarrier = items[i + 1].
                                            SplitOn("\n").
                                            First(f => f.StartsWith("VALIDATING CARRIER")).
                                            Trim().
                                            Last(2);

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
                sectors = new List<SectorFBData>();
                string previousbagallowance = "";
                for (int j = 0; j < selectedsectors.Count; j++)
                {
                    int sectorno = selectedsectors[j];
                    PNRSector pnrsec = pnrsecs.First(p => p.SectorNo == sectorno);
                    if (pnrsec.From == "ARUNK") { continue; }
                    string changesec = changesecs.IsNullOrEmpty() ? "" : changesecs.FirstOrDefault(f => f.LastMatch(@"(\d+)[A-Z]") == sectorno.ToString());
                    string selectedfarebasis = string.IsNullOrEmpty(changesec) ?
                                                    usedfbs.IsNullOrEmpty() ?
                                                        string.IsNullOrEmpty(farebasis.FirstOrDefault(f => pnrsec.Class == f.Substring(0, 1))) ?
                                                            farebasis .First():
                                                            farebasis.First(f => pnrsec.Class == f.Substring(0, 1)):
                                                        string.IsNullOrEmpty(farebasis.FirstOrDefault(f => !usedfbs.Contains(f) && pnrsec.Class == f.Substring(0, 1))) ?
                                                            farebasis.FirstOrDefault(f => pnrsec.Class == f.Substring(0, 1)) :
                                                            farebasis.FirstOrDefault(f => !usedfbs.Contains(f) && pnrsec.Class == f.Substring(0, 1)) :
                                                    farebasis.FirstOrDefault(f => f.Substring(0, 1) == changesec.LastMatch(@"\d+([A-Z])")) ?? "";
                    
                    string baggage = GetBaggage(baggageinfo, paxtype, ref previousbagallowance, pnrsec);

                    sectors.
                        Add(new SectorFBData()
                        {
                            SectorNo = sectorno,
                            Farebasis = selectedfarebasis,
                            Baggage = baggage
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
                        Endorsements = endos.Where(w=> !string.IsNullOrEmpty(w)).ToList(),
                        FareCalculation = GetFareCalc(farecalcitems),
                        ROE = farecalclines.LastMatch(@"ROE(\d+\.\d+)\s*", "1.0000"),
                        Taxes = taxitems.
                                    SelectMany(s => s.Taxes).
                                    Select(s => new Tax()
                                    {
                                        Code = s.Code,
                                        Amount = s.Amount
                                    }).
                                    ToList(),
                        BaseFare = decimal.Parse(basefare),
                        BaseFareCurrency = basefarecurrency,
                        EquivFare = string.IsNullOrEmpty(equivfare) ? default : decimal.Parse(equivfare),
                        EquivFareCurrency = string.IsNullOrEmpty(equivfarecurrency) ? basefarecurrency : equivfarecurrency,
                        PlatingCarrier = platingcarrier
                    });
            }

            return bestBuyItems;
        }

        private static string GetBaggage(List<BaggageInfo> baggageinfo, string paxtype, ref string previousbagallowance, PNRSector pnrsec)
        {
            var bagdata = baggageinfo.IsNullOrEmpty() ?
                            null :
                            baggageinfo.
                                First(w =>
                                    w.PaxType == paxtype)?.
                                SectorBags.
                                FirstOrDefault(f => (f.From == pnrsec.From &&
                                           f.To == pnrsec.To) || f.From == pnrsec.From);
            string baggage = bagdata == null ? string.IsNullOrEmpty(previousbagallowance) ? "" : previousbagallowance : bagdata.BaggageAllowance;
            previousbagallowance = bagdata == null ? previousbagallowance : bagdata.BaggageAllowance;
            return baggage;
        }

        private string GetFareCalc(List<string> farecalcitems)
        {
            if (farecalcitems.Count() == 1)
            {
                return string.Join("", farecalcitems).SplitOn("END").First() + "END";
            }

            string farecalc = farecalcitems.First();

            if (farecalcitems.Count() > 2)
            {
                farecalc += farecalcitems[1];
            }

            //SYD QF TYO AA LAX AA HNL QF SYD2860.27NUC2860.27END ROE1.293231 XFHNL4.5
            //MEL LH X/HKG LH X/FRA LH LON193.31/-ROM LH X/MUC LH NYC//LAX QF MEL386.62NUC579.93END ROE1.293231 XFLAX4.5
            //LAX AA HNL134.96USD134.96END ZPLAX XFLAX4.5

            if (farecalcitems.Last().Contains("ZP"))
            {
                farecalc += " " + farecalcitems.Last().SplitOnRegex(@"(ZP\s*[A-Z]{3})\s*")[1];
            }

            if (farecalcitems.Last().Contains("XF"))
            {
                farecalc += " " + farecalcitems.Last().SplitOnRegex(@"(XF\s*[A-Z]{3}\d+\.*\d*)\s*")[1];
            }

            return farecalc;
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
