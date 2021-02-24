using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using GetElectronicDocumentService;
using System.Collections.Generic;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;

namespace SabreWebtopTicketingService.Services
{
    public class DisplayTicketService : ConnectionStubs, IDisposable
    {
        private readonly ILogger logger;
        private readonly string url;

        public DisplayTicketService(
            ILogger logger)
        {
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        internal MessageHeader getMessageHeader(string agentPcc)
        {
            return new MessageHeader()
            {
                version = Constants.TKT_ElectronicDocumentVersion,
                From = new From()
                {
                    PartyId = new PartyId[]
                {
                        new PartyId()
                        {
                            Value = "Aeronology"
                        }
                }
                },
                To = new To()
                {
                    PartyId = new PartyId[]
                    {
                        new PartyId()
                        {
                            Value = "SWS"
                        }
                    }
                },
                Action = "TKT_ElectronicDocumentServicesRQ",
                CPAId = agentPcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "TKT_ElectronicDocumentServicesRQ",
                    type = "OTA"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.GetSabreDatetime()
                }
            };
        }

        internal Security getSecurityHedder(string token)
        {
            return new Security()
            {
                BinarySecurityToken = new SecurityBinarySecurityToken()
                {
                    Value = token
                }
            };
        }

        internal async Task<GetElectronicDocumentRS> DisplayDocument(string token, Pcc pcc, string ticketnumber, string agentpcc)
        {

            GetElectronicDocumentPortTypeClient client = null;
            GetElectronicDocumentRQResponse result = null;
            try
            {
                EnableTLS();

                client = new GetElectronicDocumentPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));
                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                logger.LogInformation($"Start {nameof(DisplayDocument)} execution");
                client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));

                var sw = Stopwatch.StartNew();

                result = await client.
                                GetElectronicDocumentRQAsync(
                                    getMessageHeader(string.IsNullOrEmpty(agentpcc) ? pcc.PccCode : agentpcc),
                                    getSecurityHedder(token),
                                    getTicketRq(agentpcc, ticketnumber));

                await client.CloseAsync();

                logger.LogInformation($"{nameof(DisplayDocument)} completed in {sw.ElapsedMilliseconds} ms");
                sw.Stop();

                if (result != null && result.GetElectronicDocumentRS.STL_HeaderRS.Results.Success == null)
                {
                    var errors = result.
                                    GetElectronicDocumentRS.
                                    STL_HeaderRS.
                                    Results.
                                    Error.
                                    SystemSpecificResults.
                                    Select(s => s.ErrorMessage).
                                    Select(s => s.Value).
                                    Distinct();

                    throw new GDSException("30000100", string.Join(",", errors));
                }

            }
            catch (Exception ex)
            {
                client.Abort();
                logger.LogError(ex);
                throw (ex);
            }

            return result.GetElectronicDocumentRS;
        }

        private GetElectronicDocumentRQ getTicketRq(string agentpcc, string ticketnumber)
        {
            GetElectronicDocumentRQ getElectronicDocumentRQ = new GetElectronicDocumentRQ()
            {
                Version = Constants.TKT_ElectronicDocumentVersion,
                POS = new POS()
                {
                    Pseudo = agentpcc
                },
                requestType = "W",
                SearchParameters = new ElectronicDocumentSearchParameters()
                {
                    DocumentNumber = ticketnumber
                }
            };
            return getElectronicDocumentRQ;
        }

        //internal OriginalTicket PraseDisplatTicketResponsexxx(GetElectronicDocumentRS getElectronicDocumentRS)
        //{
        //    OriginalTicket doc = null;

        //    TicketingAgentED ticketingAgent = getElectronicDocumentRS.Agent;
        //    TicketingDocumentTicketED ticket = (TicketingDocumentTicketED)getElectronicDocumentRS.DocumentDetailsDisplay.Item;

        //    if (ticket != null)
        //    {
        //        doc = new OriginalTicket()
        //        {
        //            DocumentNumber = ticket.number,
        //            TicketingPCC = ticketingAgent.WorkLocation,
        //            TicketingIATA = ticketingAgent.IataNumber,
        //            Locator = ticket.Details.Reservation.Sabre,
        //            IssueDateTime = ticket.Details.LocalIssueDateTime.GetISODateTime(),
        //            Payment = GetTicketFOP(
        //                            ticket.Payment, 
        //                            ticket.Amounts.New.Total.Amount.Value),
        //            Exchanged = ticket.RelatedDocument?.Exchange != null && !string.IsNullOrEmpty(ticket.RelatedDocument.Exchange.First().Number),
        //            CommissionPercentage = ticket.Amounts?.Other?.Commission?.PercentageRateSpecified??false ?
        //                                        ticket.Amounts.Other.Commission.PercentageRate:
        //                                        default,
        //            CommissionAmount = ticket.Amounts.Other?.Commission?.Amount == null?
        //                                        default :
        //                                        ticket.Amounts.Other.Commission.Amount.Value,
        //            TicketPassenger = new TicketPassenger()
        //            {
        //                ExternalNumber = ticket.Customer.Traveler.ExternalNumber,
        //                LastName = ticket.Customer.Traveler.LastName,
        //                FirstName = ticket.Customer.Traveler.FirstName,
        //                FrequentFlyer = ticket.Affinity?.FrequentFlyer == null ?
        //                                    new FrequentFlyer() :
        //                                    new FrequentFlyer()
        //                                    {
        //                                        FrequentFlyerNo = ticket.Affinity.FrequentFlyer.Number,
        //                                        CarrierCode = ticket.Affinity.FrequentFlyer.Provider
        //                                    },
        //                PaxType = GetPaxType(
        //                                ticket.Customer.Traveler.ExternalNumber,
        //                                ticket.ServiceCoupon.Where(w => w.type != "A").Select(s => s.FareBasis).Distinct())
        //            },
        //            CurrencyCode = ticket.Amounts.New.Equivalent == null ? ticket.Amounts.New.Base.Amount.currencyCode : ticket.Amounts.New.Equivalent.Amount.currencyCode,
        //            BaseFare = ticket.Amounts.New.Equivalent == null ?
        //                            ticket.Amounts.New.Base.Amount.Value :
        //                            ticket.Amounts.New.Equivalent.Amount.Value,
        //            TotalFare = ticket.Amounts.New.Total.Amount.Value,
        //            Taxes = ticket.
        //                        Taxes.
        //                        New.
        //                        Where(t => t.Text != "ALL TAXES EXEMPTED").
        //                        Select(t => new Tax()
        //                        {
        //                            Code = t.code,
        //                            Amount = t.Amount.Value
        //                        }).
        //                        ToList(),
        //            ValidatingCarrier = ticketingAgent.TicketingProvider,
        //            Endosements = ticket.Remark.Endorsements.OrderBy(o => o.sequence).Select(s => s.Value).ToList(),
        //            FareCalculation = ticket.FareCalculation.New.Value,
        //            TicketCoupons = ticket.
        //                        ServiceCoupon.
        //                        Select(c => new Coupon()
        //                        {
        //                            CouponNumber = c.coupon,
        //                            From = c.StartLocation == null && c.type == "A" ? "ARUNK" : c.StartLocation.Value,
        //                            To = c.EndLocation == null ? "" : c.EndLocation.Value,
        //                            DepartureDate = c.StartDateTimeSpecified ?
        //                                                c.StartDateTime.GetISODateString() :
        //                                                "",
        //                            DepartureTime = c.StartDateTimeSpecified ?
        //                                                c.StartDateTime.GetISOTimeString() :
        //                                                "",
        //                            ArivalDate = c.EndDateTimeSpecified ?
        //                                            c.EndDateTime.GetISODateTime() :
        //                                            "",
        //                            MarketingCarrier = c.MarketingProvider == null ? "" : c.MarketingProvider.Value,
        //                            OperatingCarrier = c.OperatingProvider == null ? "" : c.OperatingProvider.Value,
        //                            FlightNumber = c.MarketingFlightNumber,
        //                            BookingClass = c.ClassOfService == null ? "" : c.ClassOfService.Value,
        //                            FareBasis = c.FareBasis,
        //                            TicketDesignator = c.TicketDesignator,
        //                            BagAllowance = c.BagAllowance == null ?
        //                            null :
        //                            new BaggageAllowance()
        //                            {
        //                                Code = GetCode(c.BagAllowance.code),
        //                                Amount = c.BagAllowance.amountSpecified ?
        //                                            c.BagAllowance.amount :
        //                                            default(int?)
        //                            },
        //                            NotValidAfterDate = c.NotValidAfterDateSpecified ?
        //                                                    c.NotValidBeforeDate.GetISODateTime() :
        //                                                    "",
        //                            NotValidBeforeDate = c.NotValidBeforeDateSpecified ?
        //                                                    c.NotValidBeforeDate.GetISODateTime() :
        //                                                    "",
        //                            BookedStatus = c.BookingStatus == null ? "" : c.BookingStatus.Value,
        //                            CurrentStatus = c.CurrentStatus
        //                        }).
        //                        ToList()
        //        };
        //    }

        //    doc.Route = GetRoute(doc.TicketCoupons);

        //    return doc;
        //}

        internal IssueTicketDetails PraseDisplatTicketResponseforIssueTicket(object tkt, string ticketingpcc)
        {
            IssueTicketDetails doc = new IssueTicketDetails();
            if (tkt is TicketingDocumentTicketED ticket)
            {
                doc.DocumentNumber = ticket.number;
                doc.DocumentType = ticket.type;
                doc.EMDNumber = new List<int>() { -1 };
                doc.PassengerName = $"{ticket.Customer.Traveler.LastName}/{ticket.Customer.Traveler.FirstName}";
                doc.IssuingPCC = ticketingpcc;
                doc.LocalIssueDateTime = ticket.Details.LocalIssueDateTime.GetISODateTime();
                doc.TotalAmount = ticket.Amounts.New.Total.Amount.Value;
                doc.ConjunctionPostfix = ticket.RelatedDocument?.Conjunctive.Last().Number.Last(3);
                doc.FormOfPayment = GetTicketFOP(ticket.Payment);
            }
            else if (tkt is TicketingDocumentEMDED emd)
            {
                doc.DocumentNumber = emd.number;
                doc.DocumentType = emd.type;
                doc.QuoteRefNo = -1;
                doc.PassengerName = $"{emd.Customer.Traveler.LastName}/{emd.Customer.Traveler.FirstName}";
                doc.IssuingPCC = ticketingpcc;
                doc.LocalIssueDateTime = emd.Details.LocalIssueDateTime.GetISODateTime();
                doc.TotalAmount = emd.Amounts.New.Total.Amount.Value;
                doc.ConjunctionPostfix = "";
                doc.FormOfPayment = GetTicketFOP(emd.Payment);
            }

            return doc;
        }


        internal PriceItTicket PraseDisplatTicketResponse(GetElectronicDocumentRS getElectronicDocumentRS)
        {
            PriceItTicket doc = null;

            TicketingAgentED ticketingAgent = getElectronicDocumentRS.Agent;
            TicketingDocumentTicketED ticket = (TicketingDocumentTicketED)getElectronicDocumentRS.DocumentDetailsDisplay.Item;

            if (ticket != null)
            {
                doc = new PriceItTicket()
                {
                    DocumentNumber = ticket.number,
                    IsConjunction = ticket.RelatedDocument?.Conjunctive != null,
                    ConjunctionPostfix = ticket.RelatedDocument?.Conjunctive != null ? ticket.RelatedDocument.Conjunctive.Last().Number.Last(3) : "",
                    TicketingPCC = ticketingAgent.WorkLocation,
                    TicketingIATA = ticketingAgent.IataNumber,
                    IssueDateTime = ticket.Details.LocalIssueDateTime.GetISODateTime(),
                    PlatingCarrier = ticketingAgent.TicketingProvider,
                    Passenger = new PriceItPassenger()
                    {
                        FirstName = ticket.Customer.Traveler.FirstName,
                        LastName = ticket.Customer.Traveler.LastName,
                        PassengerName = $"{ticket.Customer.Traveler.LastName}/{ticket.Customer.Traveler.FirstName}",
                        FrequentFlyerNumber = ticket.Affinity?.FrequentFlyer?.Number,
                        FrequentFlyerProvider = ticket.Affinity?.FrequentFlyer?.Provider
                    },
                    FareCalculation = ticket.FareCalculation.New.Value,
                    Coupons = ticket.
                                ServiceCoupon.
                                Select(c => new Coupon()
                                {
                                    CouponNumber = c.coupon,
                                    From = c.StartLocation == null && c.type == "A" ? "ARUNK" : c.StartLocation.Value,
                                    To = c.EndLocation == null ? "" : c.EndLocation.Value,
                                    DepartureDate = c.StartDateTimeSpecified ?
                                                        c.StartDateTime.GetISODateTime() :
                                                        "",
                                    ArivalDate = c.EndDateTimeSpecified ?
                                                    c.EndDateTime.GetISODateTime() :
                                                    "",
                                    MarketingCarrier = c.MarketingProvider == null ? "" : c.MarketingProvider.Value,
                                    OperatingCarrier = c.OperatingProvider == null ? "" : c.OperatingProvider.Value,
                                    FlightNumber = c.MarketingFlightNumber,
                                    BookingClass = c.ClassOfService == null ? "" : c.ClassOfService.Value,
                                    FareBasis = c.FareBasis,
                                    TicketDesignator = c.TicketDesignator,
                                    BagAllowance = c.BagAllowance == null ?
                                    null :
                                    new BaggageAllowance()
                                    {
                                        Code = GetCode(c.BagAllowance.code),
                                        Amount = c.BagAllowance.amountSpecified ?
                                                    c.BagAllowance.amount :
                                                    default(int?)
                                    },
                                    NotValidAfterDate = c.NotValidAfterDateSpecified ?
                                                            c.NotValidBeforeDate.GetISODateTime() :
                                                            "",
                                    NotValidBeforeDate = c.NotValidBeforeDateSpecified ?
                                                            c.NotValidBeforeDate.GetISODateTime() :
                                                            "",
                                    BookedStatus = c.BookingStatus == null ? "" : c.BookingStatus.Value
                                }).
                                ToList(),
                    TotalFare = ticket.Amounts.New.Total.Amount.Value,
                    Payment = GetTicketFOP(ticket.Payment)
                };
            }

            return doc;
        }

        internal PriceItEMD PraseDisplatEMDResponse(GetElectronicDocumentRS getElectronicDocumentRS)
        {
            PriceItEMD doc = null;

            try
            {
                TicketingAgentED emdAgent = getElectronicDocumentRS.Agent;
                TicketingDocumentEMDED emd = (TicketingDocumentEMDED)getElectronicDocumentRS.DocumentDetailsDisplay.Item;

                if (emd != null)
                {
                    doc = new PriceItEMD()
                    {
                        DocumentNumber = emd.number,
                        IsConjunction = emd.RelatedDocument?.Conjunctive != null,
                        ConjunctionPostfix = emd.RelatedDocument?.Conjunctive != null ? emd.RelatedDocument.Conjunctive.Last().Number.Last(3) : "",
                        TicketingPCC = emdAgent.WorkLocation,
                        TicketingIATA = emdAgent.IataNumber,
                        IssueDateTime = emd.Details.LocalIssueDateTime.GetISODateTime(),
                        PlatingCarrier = emdAgent.TicketingProvider,
                        Passenger = new PriceItPassenger()
                        {
                            FirstName = emd.Customer.Traveler.FirstName,
                            LastName = emd.Customer.Traveler.LastName,
                            PassengerName = $"{emd.Customer.Traveler.LastName}/{emd.Customer.Traveler.FirstName}"
                        },
                        EMDCoupons = emd.
                                        Miscellaneous.
                                        Select(e => new EMDCoupon()
                                        {
                                            From = e.CouponDetails.StartLocation.Value,
                                            To = e.CouponDetails.EndLocation.Value,
                                            Carrier = e.CouponDetails.MarketingProvider.Value,
                                            ICW = e.AssociatedTicketNumber.Value,
                                            SSR = e.OptionalService.ssr,
                                            PresentAt = e.OptionalService.PresentAt?.Value,
                                            PresentTo = e.OptionalService.PresentTo?.Value,
                                            GroupCode = e.OptionalService.group,
                                            GroupDescription = e.OptionalService.groupDescription,
                                            Reason = e.OptionalService.reason,
                                            Consumed = e.OptionalService.Indicators?.consumed,
                                            TaxExcempt = (e.OptionalService.Indicators?.taxExemptSpecified ?? false) && (e.OptionalService.Indicators?.taxExempt ?? false),
                                            FeeOverride = (e.OptionalService.Indicators?.feeOverrideSpecified ?? false) && (e.OptionalService.Indicators?.feeOverride ?? false),
                                            JourneyType = e.OptionalService.journeyType,
                                            CurrencyCode = e.Fee.Total.Amount.currencyCode,
                                            TotalAmount = e.Fee.Total.Amount.Value,
                                            Taxes = e.Tax != null ?
                                                        e.Tax.
                                                        Select(t => new Tax()
                                                        {
                                                            Code = t.code,
                                                            Amount = t.Amount.Value
                                                        }).
                                                        ToList() :
                                                        new List<Tax>()
                                        }).
                                        ToList(),
                        TotalAmount = emd.
                                        Amounts.
                                        New.
                                        Total.
                                        Amount.
                                        Value,
                        Payment = GetTicketFOP(emd.Payment)
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Displat ticket error({ex.Message}).");
                throw;
            }

            return doc;
        }

        private FOP GetTicketFOP(TicketingDocumentPaymentED[] payment)
        {
            if (payment.IsNullOrEmpty()) 
            {
                return new FOP()
                {
                    PaymentType = PaymentType.CA
                };
            }

            return payment.Count() == 1 ?
                            (payment.First().Cash != null && payment.First().Cash.cashIndicator) || "CA|MS".Contains(payment.First().type) ?
                                new FOP()
                                {
                                    PaymentType = PaymentType.CA
                                } :
                            payment.First().Card != null || payment.First().type == "CC" ?
                                new FOP()
                                {
                                    PaymentType = PaymentType.CC,
                                    CardNumber = payment.First().Card.MaskedCardNumber,
                                    CardType = payment.First().Card.cardType,
                                    ExpiryDate = payment.First().Card.ExpireDate,
                                    ApprovalCode = payment.First().Card.ApprovalCode == null?
                                                        "":
                                                        payment.First().Card.ApprovalCode.Value
                                } :
                            throw new AeronologyException("PAYMENT_NOT_SUPPORTED", "Payment method not supported!") :
                            payment.Count() == 2 ?
                                new FOP()
                                {
                                    PaymentType = PaymentType.CC,
                                    CardNumber = payment.First(p => p.Card != null || p.type == "CC").Card.MaskedCardNumber,
                                    CardType = payment.First(p => p.Card != null || p.type == "CC").Card.cardType,
                                    ExpiryDate = payment.First(p => p.Card != null || p.type == "CC").Card.ExpireDate,
                                    ApprovalCode = payment.First(p => p.Card != null || p.type == "CC").Card.ApprovalCode == null ?
                                                                "":
                                                                payment.First(p => p.Card != null || p.type == "CC").Card.ApprovalCode.Value
                                } :
                                throw new AeronologyException("PAYMENT_NOT_SUPPORTED", "Payment method not supported!");
        }


        private string GetPaxType(string externalno, IEnumerable<string> farebasis)
        {
            if (!string.IsNullOrEmpty(externalno))
            {
                if (externalno.Trim().StartsWith("C"))
                {
                    return "CHD";
                }
                else if (externalno.Trim().StartsWith("I"))
                {
                    return "INF";
                }
            }

            if (farebasis.Any(a => a.IsMatch(@".*CH\d{0,2}")))
            {
                return "CHD";
            }

            if (farebasis.Any(a => a.IsMatch(@".*IN\d{0,2}")))
            {
                return "INF";
            }

            return "ADT";
        }

        private string GetRoute(List<Coupon> coupons)
        {
            string route = "";
            for (int i = 0; i < coupons.Count(); i++)
            {
                if (i == 0)
                {
                    route += coupons[i].From;
                    route += "-" + coupons[i].To;
                    continue;
                }

                if (i < coupons.Count()
                    && coupons[i - 1].To != coupons[i].From)
                {
                    route += "//";
                    route += coupons[i].To;
                    route += "-" + coupons[i].From;
                    continue;
                }

                route += "-" + coupons[i].To;
            }

            return route;
        }

        private List<FormOfPayment> GetTicketFOP(TicketingDocumentPaymentED[] payment, decimal total)
        {
            return payment.Count() == 1 ?
                            (payment.First().Cash != null && payment.First().Cash.cashIndicator) || payment.First().type == "CA" ?
                                new List<FormOfPayment>()
                                {
                                    new FormOfPayment()
                                    {
                                        PaymentType = PaymentType.CA
                                    }
                                } :
                            payment.First().Card != null || payment.First().type == "CC" ?
                                new List<FormOfPayment>()
                                {
                                    new FormOfPayment()
                                    {
                                        PaymentType = PaymentType.CC,
                                        CardNumber = payment.First().Card.MaskedCardNumber,
                                        CardType = payment.First().Card.cardType,
                                        ExpiryDate = payment.First().Card.ExpireDate,
                                        CreditAmount = total,
                                        ApprovalCode = payment.First().Card.ApprovalCode.Value
                                    }
                                } :
                            throw new AeronologyException("50000100", "Original ticket payment method not supported!") :
                            payment.Count() == 2 ?
                                payment.
                                Select(p => new FormOfPayment()
                                {
                                    PaymentType = (p.Cash != null && p.Cash.cashIndicator) || p.type == "CA" ? 
                                                        PaymentType.CA : 
                                                        p.Card != null || p.type == "CC" ?
                                                            PaymentType.CC :
                                                            throw new AeronologyException("50000100", "Original ticket payment method not supported!"),
                                    CardNumber = p.Card != null || p.type == "CC" ? 
                                                    p.Card.MaskedCardNumber : 
                                                    "",
                                    CardType = p.Card != null || p.type == "CC" ?
                                                    p.Card.cardType :
                                                    "",
                                    ExpiryDate = p.Card != null || p.type == "CC" ?
                                                    p.Card.ExpireDate :
                                                    "",
                                    ApprovalCode = p.Card != null || p.type == "CC" ?
                                                        p.Card.ApprovalCode.Value :
                                                        "",
                                    CreditAmount = p.Card != null || p.type == "CC" ?
                                                        decimal.
                                                            Round(
                                                                p.Total.Amount.Value,
                                                                int.Parse(string.IsNullOrEmpty(p.Total.Amount.decimalPlace) ? "2" : p.Total.Amount.decimalPlace),
                                                                MidpointRounding.AwayFromZero) :
                                                        0.00M
                                }).
                                ToList() :
                                throw new AeronologyException("50000100", "Original ticket payment method not supported!");
        }

        private string GetCode(CodeBaggageAllowanceCode code)
        {
            switch (code)
            {
                case CodeBaggageAllowanceCode.K:
                    return "KG";
                case CodeBaggageAllowanceCode.L:
                    return "LB";
                case CodeBaggageAllowanceCode.PC:
                    return "PC";
                case CodeBaggageAllowanceCode.NIL:
                    return "Nill";
                case CodeBaggageAllowanceCode.Item10K:
                    return "10KG";
                default:
                    return "";
            }
        }

        public void Dispose()
        {

        }
    }
}
