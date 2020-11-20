using SabreWebtopTicketingService.Common;
using System.Xml.Linq;

namespace SabreWebtopTicketingService.Models
{
    internal class SabrePriceQuoteSummaryLine
    {
        XElement pqSummary;
        XElement nameassociation;
        public SabrePriceQuoteSummaryLine(XElement _pqSummary, XElement _nameassociation)
        {
            pqSummary = _pqSummary;
            nameassociation = _nameassociation;
        }

        public int PQNo => int.Parse(pqSummary.
                                GetAttribute("number").
                                Value);

        public string PricingType => pqSummary.
                            GetAttribute("pricingType").
                            Value;

        public string QuoteType => pqSummary.
                        GetAttribute("type").
                        Value;

        public bool IsExpired
        {
            get
            {
                if("M,A".Contains(PricingType))
                {
                    return true;
                }

                if (pqSummary.GetFirstElement("Indicators") == null ||
                    pqSummary.GetFirstElement("Indicators").GetAttribute("isExpired") == null)
                {
                    return false;
                }

                return bool.Parse(pqSummary.
                                    GetFirstElement("Indicators").
                                    GetAttribute("isExpired").
                                    Value);
            }
            private set { }
        }
        public string RequestedPaxType => pqSummary.
                        GetFirstElement("Passenger").
                        GetAttribute("requestedType").
                        Value;

        public string PaxType => pqSummary.
                                    GetFirstElement("Passenger").
                                    GetAttribute("type").
                                    Value;

        public string PaxCount => pqSummary.
                            GetFirstElement("Passenger").
                            GetAttribute("passengerTypeCount").
                            Value;
        public string TicketDesignator => pqSummary.
                                            GetFirstElement("TicketDesignator").
                                            Value;

        public string ValidatingCarrier => pqSummary.
                                            GetFirstElement("ValidatingCarrier").
                                            Value;
        public PQPassenger Passenger => new PQPassenger(nameassociation);

        public string Total => pqSummary.
                                    GetFirstElement("Amounts").
                                    GetFirstElement("Total").
                                    Value;

        public string CurrencyCode => pqSummary.
                                        GetFirstElement("Amounts").
                                        GetFirstElement("Total").
                                        GetAttribute("currencyCode").
                                        Value;

        public string DecimalPlace => pqSummary.
                                        GetFirstElement("Amounts").
                                        GetFirstElement("Total").
                                        GetAttribute("decimalPlace").
                                        Value;
    }
}
