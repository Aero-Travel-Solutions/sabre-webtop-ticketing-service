using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    //ABACUS
    //Example 01:
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

    //Example 02
    //    PSGR TYPE  ADT - 01
    //     CXR RES DATE FARE BASIS NVB   NVA BG
    // HKG
    // BKK CX Q   01SEP QAZZWTAE        01SEP 01SEP 30K
    //HKG CX Q   08SEP QAZZWTAE        08SEP 08SEP 30K
    //FARE  HKD      1670  
    //TAX HKD       120HK HKD        90G3 HKD       368XT
    //TOTAL HKD      2248
    //ADT-01  QAZZWTAE
    // HKG CX BKK107.35CX HKG107.35NUC214.70END ROE7.75325
    //XT HKD50I5 HKD182TS HKD8G8 HKD18E7 HKD110YR
    //ENDOS* SEG1/2* NONENDORSEABLE/FARE RESTRICTIONS APPLY
    //ATTN* PRIVATE FARE APPLIED - CHECK RULES FOR CORRECT TICKETING
    //ATTN* PRIVATE Â¤
    //ATTN* VALIDATING CARRIER SPECIFIED - CX
    //ATTN* CHANGE BOOKING CLASS -   2Q

    //Example 02 - Multi pax
    // PSGR TYPE CNN - 02
    //     CXR RES DATE FARE BASIS NVB   NVA BG
    // HKG
    // BKK CX Q   01SEP QAZZWTAE/CH25   01SEP 01SEP 30K
    //HKG CX Q   08SEP QAZZWTAE/CH25   08SEP 08SEP 30K
    //FARE  HKD      1250  
    //TAX HKD        90G3 HKD        50I5 HKD       318XT
    //TOTAL HKD      1708
    //CNN-01  QAZZWTAE/CH25
    // HKG CX BKK80.51CX HKG80.51NUC161.02END ROE7.75325
    //XT HKD182TS HKD8G8 HKD18E7 HKD110YR
    //ENDOS* SEG1/2* NONENDORSEABLE/FARE RESTRICTIONS APPLY
    //ATTN* EACH CNN REQUIRES ACCOMPANYING SAME RULE SAME CABIN ADT
    //ATTN* PRIVATE FARE APPLIED - CHECK RULES FOR CORRECT TICKETING
    //ATTN* PRIVATE Â¤
    //ATTN* VALIDATING CARRIER SPECIFIED - CX
    //ATTN* CHANGE BOOKING CLASS -   2Q

    // PSGR TYPE INF - 03
    //     CXR RES DATE FARE BASIS NVB   NVA BG
    // HKG
    // BKK CX Q   01SEP QAZZWTAE/IN90   01SEP 01SEP 10K
    //HKG CX Q   08SEP QAZZWTAE/IN90   08SEP 08SEP 10K
    //FARE  HKD       170  
    //TAX HKD        90G3 HKD        50I5 HKD       136XT
    //TOTAL HKD       446
    //INF-01  QAZZWTAE/IN90
    // HKG CX BKK10.73CX HKG10.73NUC21.46END ROE7.75325
    //XT HKD8G8 HKD18E7 HKD110YR
    //ENDOS* SEG1/2* NONENDORSEABLE/FARE RESTRICTIONS APPLY
    //ATTN* PRIVATE FARE APPLIED - CHECK RULES FOR CORRECT TICKETING
    //ATTN* EACH INF REQUIRES ACCOMPANYING ADT PASSENGER
    //ATTN* PRIVATE Â¤
    //ATTN* VALIDATING CARRIER SPECIFIED - CX
    //ATTN* CHANGE BOOKING CLASS -   2Q
    //            3090                      1312              4402TTL

    //FORM OF PAYMENT FEES PER TICKET MAY APPLY
    //ADT      DESCRIPTION FEE      TKT TOTAL
    // OBFCA - CC NBR BEGINS WITH 516470         0           2248
    // OBFCA - CC NBR BEGINS WITH 1611           0           2248
    // OBFCA - CC NBR BEGINS WITH 559867         0           2248
    // OBFCA - CC NBR BEGINS WITH 900024         0           2248
    // OBFCA - CC SVC FEE                        0           2248

    //CNN DESCRIPTION                     FEE TKT TOTAL
    // OBFCA - CC NBR BEGINS WITH 516470         0           1708
    // OBFCA - CC NBR BEGINS WITH 1611           0           1708
    // OBFCA - CC NBR BEGINS WITH 559867         0           1708
    // OBFCA - CC NBR BEGINS WITH 900024         0           1708
    // OBFCA - CC SVC FEE                        0           1708

    //INF DESCRIPTION                     FEE TKT TOTAL
    // OBFCA - CC NBR BEGINS WITH 516470         0            446
    // OBFCA - CC NBR BEGINS WITH 1611           0            446
    // OBFCA - CC NBR BEGINS WITH 559867         0            446
    // OBFCA - CC NBR BEGINS WITH 900024         0            446
    // OBFCA - CC SVC FEE                        0            446

    //ATTN* AIR EXTRAS AVAILABLE - SEE WP* AE
    //ATTN* BAGGAGE INFO AVAILABLE - SEE WP* BAG
    // .

    //Example 03 - Foreign Currency
    //PSGR TYPE  ADT - 01
    //CXR RES DATE FARE BASIS NVB   NVA BG
    //BKK
    //HKG CX S   01JUL SRZZTHAO        01JUL 01JUL 30K
    //FARE  THB      4390 EQUIV HKD      1140
    //TAX HKD       182TS HKD         4G8 HKD        64XT
    //TOTAL HKD      1390\nADT-01  SRZZTHAO
    //BKK CX HKG145.33NUC145.33END ROE30.2068
    //XT HKD9E7 HKD55YR
    //ENDOS* SEG1* NONENDORSEABLE/FARE RESTRICTIONS APPLY
    //  RATE USED 1THB-0.25935544HKD
    //ATTN* VALIDATING CARRIER SPECIFIED - CX

    // FORM OF PAYMENT FEES PER TICKET MAY APPLY
    // ADT      DESCRIPTION FEE      TKT TOTAL
    //OBFCA - CC NBR BEGINS WITH 516470         0           1390
    //OBFCA - CC NBR BEGINS WITH 1611           0           1390
    //OBFCA - CC NBR BEGINS WITH 559867         0           1390
    //OBFCA - CC NBR BEGINS WITH 900024         0           1390

    //ATTN* AIR EXTRAS AVAILABLE - SEE WP* AE
    //ATTN* BAGGAGE INFO AVAILABLE - SEE WP* BAG
    // .


    internal class AbacusBuyQuote : IBestBuyQuote
    {
        string fullgdsresponse = "";
        List<int> selectedsectors = null;
        List<PNRSector> pnrsecs = null;

        public AbacusBuyQuote(string res, List<int> selsec, List<PNRSector> pnrsec)
        {
            selectedsectors = selsec;
            pnrsecs = pnrsec;
            fullgdsresponse = res;
        }

        public List<BestBuyItem> BestBuyItems => GetBestBuyItems();

        private List<BestBuyItem> GetBestBuyItems()
        {
            List<BestBuyItem> bestBuyItems = new List<BestBuyItem>();
            var items = fullgdsresponse.SplitOnRegex(@"PSGR\s+TYPE\s+([ACI][DHN][TDFN]\s*-\s*\d+.*)");
            List<string> usedfbs = new List<string>();

            for (int i = 1; i < items.Skip(1).Count(); i += 2)
            {
                //PSGR TYPE  ADT - 01
                string paxtype = items[i].SplitOn("-").First().Trim();
                List<TaxInfo> taxitems = new List<TaxInfo>();

                List<string> lines = items[i + 1].
                                        SplitOn("\n").
                                        ToList();
                string taxline = lines.
                                    SkipWhile(s => !s.StartsWith("TAX")).
                                    Take(1).
                                    First();
                taxitems.Add(new TaxInfo(taxline.Replace("\n", "###")));
                taxitems.Add(new TaxInfo(lines.FirstOrDefault(f => f.StartsWith("XT"))));


                //single currency
                string fareline = lines.
                                        SkipWhile(s => !s.StartsWith("FARE")).
                                        Take(1).
                                        First();

                string basefare = fareline.Contains("EQUIV") ?
                                        fareline.SplitOnRegex(@"EQUIV\s+[A-Z]{3}\s*(\d+\.*\d*)s*")[1]:
                                        fareline.SplitOnRegex(@"FARE\s+[A-Z]{3}\s*(\d+\.*\d*)s*")[1];

                string basefarecurrency = fareline.Contains("EQUIV") ?
                        fareline.SplitOnRegex(@"EQUIV\s+([A-Z]{3})\s*\d+\.*\d*s*")[1] :
                        fareline.SplitOnRegex(@"FARE\s+([A-Z]{3})\s*\d+\.*\d*s*")[1];


                string[] farebasis = items[i+1].SplitOnRegex(@"[ACI][DHN][TDFN]-\d+(.*)")[1].SplitOnRegex(@"\s+").Where(w=> !string.IsNullOrEmpty(w)).ToArray();

                string pricehintline = lines.FirstOrDefault(w => w.StartsWith("ATTN*CHANGE BOOKING CLASS"));
                string pricehint = string.IsNullOrEmpty(pricehintline) ?
                                        "":
                                        pricehintline.SplitOn("ATTN*").Last();

                string[] farecalcitems = string.Join("", lines.
                                                            SkipWhile(w=> !w.StartsWith("TOTAL")).
                                                            Skip(2).
                                                            TakeWhile(t => !t.StartsWith("ENDOS"))).
                                                            SplitOnRegex(@"(ROE\d+\.\d+)\s*");

                List<string> endos = lines.
                                        First(f=> f.StartsWith("ENDOS")).
                                        SplitOnRegex(@"ENDOS\*SEG.*\*").
                                        Last().
                                        SplitOn("/").
                                        Where(w=> !string.IsNullOrEmpty(w)).
                                        Select(s=> s.Trim()).
                                        ToList();

                IEnumerable<string> changesecs = string.IsNullOrEmpty(pricehintline)?
                                                    null:
                                                    pricehintline.
                                                    SplitOnRegex(@"CHANGE\sBOOKING\sCLASS\s*-\s*").
                                                    Last().
                                                    SplitOnRegex(@"\s+").
                                                    Where(w => !string.IsNullOrEmpty(w)).
                                                    Distinct();

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

                List<string> seclines = lines.TakeWhile(t => !t.StartsWith("FARE")).Skip(1).ToList();
                List<SectorFBData> sectors = new List<SectorFBData>();
                for (int j = 1; j < seclines.Count(); j++)
                {
                    //BKK
                    //HKG CX S   01JUL SRZZTHAO        01JUL 01JUL 30K

                   PNRSector pnrsec = pnrsecs.
                                            First(p => 
                                                p.From == seclines[j-1].Trim().Substring(0,3) &&
                                                p.To == seclines[j].Trim().Substring(0, 3));
                    List<string> lineitems = seclines[j].SplitOnRegex(@"\s+").ToList();
                    sectors.
                        Add(new SectorFBData()
                        {
                            SectorNo = pnrsec.SectorNo,
                            Farebasis = seclines[j].LastMatch(@"\w{2}\s+[A-Z]\s+\d{2}[A-Z]{3}\s+(.*)\s+\d{2}[A-Z]{3}").SplitOnRegex(@"\s+").First(),
                            NVA = lineitems[lineitems.Count()-2],
                            NVB = lineitems[lineitems.Count() - 3],
                            Baggage = lineitems.Last()
                        });
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
                        FareCalculation = GetFareCalc(farecalcitems),
                        ROE = farecalcitems.Count() == 1? "1.0000": farecalcitems[1].Substring(3).Trim(),
                        Taxes = taxitems.
                                    SelectMany(s => s.Taxes).
                                    Select(s => new Tax()
                                    {
                                        Code = s.Code,
                                        Amount = s.Amount
                                    }).
                                    ToList(),
                        BaseFare = decimal.Parse(basefare),
                        BaseFareCurrency = basefarecurrency
                    });
            }

            return bestBuyItems;
        }

        private string GetFareCalc(string[] farecalcitems)
        {
            if (farecalcitems.Count() == 1)
            {
                return string.Join("", farecalcitems).SplitOn("END").First() + "END";
            }

            string farecalc = farecalcitems.First() + farecalcitems[1];
            if (farecalcitems[2].Contains("XF"))
            {
                //SYD QF TYO AA LAX AA HNL QF SYD2860.27NUC2860.27END ROE1.293231 XFHNL4.5
                //MEL LH X/HKG LH X/FRA LH LON193.31/-ROM LH X/MUC LH NYC//LAX QF MEL386.62NUC579.93END ROE1.293231 XFLAX4.5
                farecalc += " " + farecalcitems[2].SplitOnRegex(@"(XF[A-Z]{3}\d+\.*\d+)\s*")[1];
            }

            if (farecalcitems[2].Contains("ZP"))
            {
                farecalc += " " + farecalcitems[2].SplitOnRegex(@"(ZP[A-Z]{3}\d+\.*\d+)\s*")[1];
            }

            return farecalc;
        }
    }
}
