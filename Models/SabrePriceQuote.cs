using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SabreWebtopTicketingService.Models
{
    internal class SabrePriceQuote
    {
        XElement pq;
        public SabrePriceQuote(XElement _detail)
        {
            pq = _detail;
        }

        public string QuoteText => pq.ToString();
        public int PQNo => int.Parse(pq.GetAttribute("number").Value);
        public string PaxType => pq.GetAttribute("passengerType").Value;
        public string QuoteType => pq.GetAttribute("type").Value;
        public string PricingType => pq.GetAttribute("pricingType").Value;
        public string Status => pq.GetAttribute("status").Value;
        public bool Guranteed => !"MA".Contains(PricingType);
        public string AgentSine => pq.GetFirstElement("AgentInfo").GetAttribute("sine").Value;
        public string BookedPCC => pq.GetFirstElement("AgentInfo").GetFirstElement("WorkLocation").Value;
        public string CreateDateTime => pq.GetFirstElement("TransactionInfo").GetFirstElement("LocalCreateDateTime").Value;
        public string LastDateToPurchase => pq.GetFirstElement("TransactionInfo").GetFirstElement("LastDateToPurchase") == null ?
                                                "" :
                                                pq.GetFirstElement("TransactionInfo").GetFirstElement("LastDateToPurchase").Value;
        public string PricingCommand => pq.GetFirstElement("TransactionInfo").GetFirstElement("InputEntry").Value;

        public bool Expired => !Guranteed;

        public string QuotePassengerName
        {
            get
            {

                var pax = pq.
                        GetElements("NameAssociationInfo").
                        First();

                return string.Format("{0}/{1}", pax.GetAttribute("lastName").Value, pax.GetAttribute("firstName").Value);
            }

            private set { }
        }
        public List<PQSector> PQSectors => pq.
                                        GetElements("SegmentInfo").
                                        Select(na => new PQSector(na)).
                                        ToList();

        public decimal BaseFare => pq.GetFirstElement("FareInfo").GetFirstElement("EquivalentFare") == null ?
                                    decimal.Parse(pq.
                                        GetFirstElement("FareInfo").
                                        GetFirstElement("BaseFare").
                                        Value):
                                   decimal.Parse(pq.
                                        GetFirstElement("FareInfo").
                                        GetFirstElement("EquivalentFare").
                                        Value);

        public string CurrencyCode => pq.GetFirstElement("FareInfo").GetFirstElement("EquivalentFare") == null ?
                                pq.
                                GetFirstElement("FareInfo").
                                GetFirstElement("BaseFare").
                                GetAttribute("currencyCode").
                                Value :
                                pq.
                                GetFirstElement("FareInfo").
                                GetFirstElement("EquivalentFare").
                                GetAttribute("currencyCode").
                                Value;

        public decimal TotalTax => decimal.Parse(pq.
                            GetFirstElement("FareInfo").
                            GetFirstElement("TotalTax").
                            Value);

        public decimal TotalFare => decimal.Parse(pq.
                            GetFirstElement("FareInfo").
                            GetFirstElement("TotalFare").
                            Value);

        public string FareCalculation => pq.
                            GetFirstElement("FareInfo").
                            GetFirstElement("FareCalculation").
                            Value;


        public decimal CreditCardFee
        {
            get
            {
                var temp = pq.
                              GetFirstElement("FeeInfo")?.
                              GetElements("OBFee")?.
                              FirstOrDefault(w => w.GetAttribute("code").Value == "FCA" &&
                                                  w.GetAttribute("type").Value == "OB" &&
                                                  decimal.Parse(w.GetFirstElement("Amount").Value) != 0M);

                if (temp == null) 
                {
                    temp = pq.
                            GetFirstElement("FeeInfo")?.
                            GetElements("OBFee")?.
                            FirstOrDefault(w => w.GetAttribute("code").Value == "FDA" &&
                                                w.GetAttribute("type").Value == "OB" &&
                                                decimal.Parse(w.GetFirstElement("Amount").Value) != 0M);
                }

                return temp == null ? 0.00M : decimal.Parse(temp.GetFirstElement("Amount").Value);
            }
        }

        public PQSecData PqSectorData => GetRoute(pq.GetFirstElement("FareInfo").GetElements("FareComponent").ToList(),
                                                  pq.GetElements("SegmentInfo").Select(na => new PQSector(na)).ToList());

        private PQSecData GetRoute(List<XElement> fareComponent, List<PQSector> pQSectors)
        {
            int seccount = 0;
            string route = "";
            for (int i = 0; i < fareComponent.Count(); i++)
            {
                int localflightsegcount = fareComponent[i].GetFirstElement("FlightSegmentNumbers").GetElements("SegmentNumber").Count();
                if (i == 0)
                {
                    if(localflightsegcount > 1)
                    {
                        GetLocalSegmentBaseRoute(fareComponent, pQSectors, ref seccount, ref route, i, localflightsegcount);
                        continue;
                    }

                    route += fareComponent[i].GetFirstElement("Departure").GetFirstElement("CityCode").Value;
                    route += "-" + fareComponent[i].GetFirstElement("Arrival").GetFirstElement("CityCode").Value;
                    seccount++;
                    continue;
                }

                if (i < fareComponent.Count()
                    && fareComponent[i - 1].GetFirstElement("Arrival").GetFirstElement("CityCode").Value != 
                    fareComponent[i].GetFirstElement("Departure").GetFirstElement("CityCode").Value)
                {
                    if (localflightsegcount > 1)
                    {
                        GetLocalSegmentBaseRoute(fareComponent, pQSectors, ref seccount, ref route, i, localflightsegcount);
                        continue;
                    }
                    route += "//";
                    route += fareComponent[i].GetFirstElement("Departure").GetFirstElement("CityCode").Value;
                    route += "-" + fareComponent[i].GetFirstElement("Arrival").GetFirstElement("CityCode").Value;
                    seccount+=2;
                    continue;
                }

                if (localflightsegcount > 1)
                {
                    GetLocalSegmentBaseRoute(fareComponent, pQSectors, ref seccount, ref route, i, localflightsegcount);
                    continue;
                }

                route += "-" + fareComponent[i].GetFirstElement("Arrival").GetFirstElement("CityCode").Value;
                seccount ++;
            }

            return new PQSecData()
            {
                Route = route,
                SectorCount = seccount
            };
        }

        private static void GetLocalSegmentBaseRoute(List<XElement> fareComponent, List<PQSector> pQSectors, ref int seccount, ref string route, int i, int localflightsegcount)
        {
            string prevArrival = route.Length > 0 ? route.Substring(route.Length-3, 3): "";
            foreach (var segno in fareComponent[i].GetFirstElement("FlightSegmentNumbers").GetElements("SegmentNumber"))
            {
                PQSector sec = pQSectors.First(f => f.SectorNo == int.Parse(segno.Value));
                if (string.IsNullOrEmpty(prevArrival))
                {
                    route += sec.DepartureCityCode + "-" + sec.ArrivalCityCode;
                }
                else if (prevArrival == sec.DepartureCityCode)
                {
                    route += "-" + sec.ArrivalCityCode;
                }
                else
                {
                    route += "//" + sec.DepartureCityCode + "-" + sec.ArrivalCityCode;
                    seccount += 1;
                }
                prevArrival = sec.ArrivalCityCode;
            }

            seccount += localflightsegcount;
        }

        public string ValidatingCarrier => pq.
                            GetFirstElement("MiscellaneousInfo").
                            GetFirstElement("ValidatingCarrier").
                            Value;

        public List<string> Endosements => pq.
                            GetFirstElement("MessageInfo").
                            GetElements("Remarks").
                            Where(w => w.GetAttribute("type").Value == "ENS").
                            SelectMany(end => end.Value.SplitOn("/")).
                            ToList();

        public List<PQTax> PQTaxes
        {
            get
            {
                List<PQTax> taxes = new List<PQTax>();

                if (TotalTax == 0.00M) { return taxes; }

                taxes.AddRange(pq.
                                GetFirstElement("FareInfo").
                                GetFirstElement("TaxInfo").
                                GetElements("Tax").
                                Select(t => new PQTax(t)).
                                ToList());
                return taxes;
            }

            private set { }
        }

        public decimal? Commission => pq.GetFirstElement("FareInfo").GetFirstElement("Commission") == null ||
                                      pq.GetFirstElement("FareInfo").GetFirstElement("Commission").GetFirstElement("Percentage") == null ?
                                        default(decimal?):
                                        decimal.
                                        Parse(
                                            pq.
                                                GetFirstElement("FareInfo").
                                                GetFirstElement("Commission").
                                                GetFirstElement("Percentage").
                                                Value);

        public string TourCode => pq.GetFirstElement("MiscellaneousInfo").GetFirstElement("TourNumber") == null ?
                                    "" :
                                    pq.GetFirstElement("MiscellaneousInfo").GetFirstElement("TourNumber").Value;

        //public bool ITFare => pq.GetFirstElement("MiscellaneousInfo").GetFirstElement("TourNumber") == null ?
        //                            false :
        //                            pq.GetFirstElement("MiscellaneousInfo").GetFirstElement("TourNumber").GetAttribute("code").Value == "IT";

        //public bool BTFare => pq.GetFirstElement("MiscellaneousInfo").GetFirstElement("TourNumber") == null ?
        //                    false :
        //                    pq.GetFirstElement("MiscellaneousInfo").GetFirstElement("TourNumber").GetAttribute("code").Value == "BT";

        public string PriceCode => pq.GetFirstElement("FareInfo").GetFirstElement("FareComponent").GetFirstElement("CorpIdOrAcctCd") == null ?
                                        "" :
                                        pq.GetFirstElement("FareInfo").GetFirstElement("FareComponent").GetFirstElement("CorpIdOrAcctCd").Value;
    }

    internal class PQTax
    {
        internal readonly XElement tax;

        public PQTax(XElement _tax)
        {
            tax = _tax;
        }

        public string Code => tax.
                                GetAttribute("code").
                                Value;
        public decimal Amount => decimal.Parse(tax.
                        GetFirstElement("Amount").
                        Value);
    }

    internal class PQPassenger
    {
        XElement na;
        public PQPassenger(XElement naxelement)
        {
            na = naxelement;
        }

        public string FisrtName => na.GetAttribute("firstName").Value;
        public string LastName => na.GetAttribute("lastName").Value.LastMatch(@"I\/\d+(.*)", na.GetAttribute("lastName").Value);
        public string NameId => na.GetAttribute("nameId").Value;
        public string NameNumber => na.GetAttribute("nameNumber").Value;
    }

    internal class PQSector
    {
        /*
        <SegmentInfo number="2" type="A" xmlns="http://www.sabre.com/ns/Ticketing/pqs/1.0">
          <Flight>
            <Departure>
              <CityCode name="SINGAPORE">SIN</CityCode>
            </Departure>
            <Arrival>
              <CityCode name="HONG KONG">HKG</CityCode>
            </Arrival>
          </Flight>
          <FareBasis>BFHK</FareBasis>
          <NotValidAfter>2021-05-02</NotValidAfter>
        </SegmentInfo>

        <SegmentInfo number="1" segmentStatus="OK" xmlns="http://www.sabre.com/ns/Ticketing/pqs/1.0">
          <Flight connectionIndicator="O">
            <MarketingFlight number="434">QF</MarketingFlight>
            <ClassOfService>Y</ClassOfService>
            <Departure>
              <DateTime>2020-03-20T13:00:00</DateTime>
              <CityCode name="MELBOURNE">MEL</CityCode>
            </Departure>
            <Arrival>
              <DateTime>2020-03-20T14:25:00</DateTime>
              <CityCode name="SYDNEY">SYD</CityCode>
            </Arrival>
          </Flight>
          <FareBasis>YFQX</FareBasis>
          <NotValidAfter>2021-03-20</NotValidAfter>
          <Baggage allowance="01" type="P" />
        </SegmentInfo>
         * */
        XElement sec;
        public PQSector(XElement naxelement)
        {
            sec = naxelement;
        }
        public int SectorNo
        {
            get
            {
                string secno = sec.GetAttribute("number")?.Value;
                return string.IsNullOrEmpty(secno) ? -2 : int.Parse(secno);
            }
        }

        public string SegmentType => sec.GetAttribute("type")?.Value;
        public string SegmentStatus => sec.GetAttribute("segmentStatus")?.Value;
        public bool Void => SegmentStatus.IsNullOrEmpty() && sec.GetAttribute("type")?.Value == "A";

        public string ConnectionIndicator => sec.GetFirstElement("Flight")?.GetAttribute("connectionIndicator")?.Value;
        public string MarketingFlight => sec.GetFirstElement("Flight")?.GetFirstElement("MarketingFlight")?.Value;
        public string MarketingFlightNumber => sec.GetFirstElement("Flight")?.GetFirstElement("MarketingFlight")?.Attribute("number")?.Value;
        public string ClassOfService => sec.GetFirstElement("Flight")?.GetFirstElement("ClassOfService")?.Value;
        public string DepartureDateTime => sec.GetFirstElement("Flight")?.GetFirstElement("Departure")?.GetFirstElement("DateTime")?.Value;
        public string DepartureCityName => sec.GetFirstElement("Flight")?.GetFirstElement("Departure")?.GetFirstElement("CityCode")?.GetAttribute("name")?.Value;
        public string DepartureCityCode => sec.GetFirstElement("Flight")?.GetFirstElement("Departure")?.GetFirstElement("CityCode")?.Value;
        public string ArrivalDateTime => sec.GetFirstElement("Flight")?.GetFirstElement("Arrival")?.GetFirstElement("DateTime")?.Value;
        public string ArrivalCityName => sec.GetFirstElement("Flight")?.GetFirstElement("Arrival")?.GetFirstElement("CityCode")?.GetAttribute("name")?.Value;
        public string ArrivalCityCode => sec.GetFirstElement("Flight")?.GetFirstElement("Arrival")?.GetFirstElement("CityCode")?.Value;
        public string FareBasis => sec.GetFirstElement("FareBasis")?.Value;
        public string NVB => sec.GetFirstElement("NotValidBefore")?.Value;
        public string NVA => sec.GetFirstElement("NotValidAfter")?.Value;
        public string Baggageallowance => sec.GetFirstElement("Baggage")?.GetAttribute("allowance")?.Value +
                                          sec.GetFirstElement("Baggage")?.GetAttribute("type")?.Value;
    }
}
