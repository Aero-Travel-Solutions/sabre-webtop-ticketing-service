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



    internal class AbacusBuyQuote : IBestBuyQuote
    {
        string gdsresponse = "";
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
            var items = gdsresponse.SplitOnRegex(@"([ACI][DHN][TDFN]-\d+.*)");
            List<string> usedfbs = new List<string>();
            //List<FBData> fBData = new List<FBData>();
            List<SectorFBData> sectors = new List<SectorFBData>();

            for (int i = 1; i < items.Skip(1).Count(); i += 2)
            {
                //PSGR TYPE  ADT - 01
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




}
