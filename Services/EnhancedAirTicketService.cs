using EnhancedAirTicket;
using Newtonsoft.Json;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace SabreWebtopTicketingService.Services
{
    public class EnhancedAirTicketService: ConnectionStubs
    {
        private readonly SessionDataSource sessionData;

        private readonly ILogger logger;

        private readonly string url;

        public EnhancedAirTicketService(
            SessionDataSource sessionData,
            ILogger logger)
        {
            this.sessionData = sessionData;
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        private AirTicketRQ GetAirTicketRQ(
            string locator,
            List<IssueExpressTicketQuote> quotes, 
            List<IssueExpressTicketEMD> emds,
            string pcc, 
            string ticketingpcc,
            string ticketprinter,
            string printerbypass,
            string bcode,
            bool enableextendedendo,
            string decimalformatstring)
        {
            List<AirTicketRQTicketing> ticketing = new List<AirTicketRQTicketing>();

            if (!quotes.IsNullOrEmpty())
            {
                //add quotes to ticketing
                ticketing.
                    AddRange(quotes.
                                GroupBy(g => new { g.QuoteNo, g.PlatingCarrier, g.QuotePassenger.FormOfPayment.PaymentType, g.QuotePassenger.FormOfPayment.CardNumber, g.QuotePassenger.FormOfPayment.CreditAmount }).
                                Select(quotegrp => new AirTicketRQTicketing()
                                {
                                    FOP_Qualifiers = getFormOfPayment(
                                                        quotegrp.Select(s => s.QuotePassenger.FormOfPayment).ToList(),
                                                        decimalformatstring,
                                                        quotegrp.Sum(s => s.TotalFare),
                                                        bcode),
                                    MiscQualifiers = GetMiscQualifiers(
                                                            quotegrp.ToList(),
                                                            null,
                                                            quotegrp.Select(s => s.QuotePassenger.FormOfPayment).All(a=> a.PaymentType == PaymentType.CA) ?
                                                                "":
                                                                bcode,
                                                            enableextendedendo),
                                    PricingQualifiers = GetPricingInstruction(quotegrp.ToList()),
                                    FlightQualifiers = new AirTicketRQTicketingFlightQualifiers()
                                    {
                                        VendorPrefs = new AirTicketRQTicketingFlightQualifiersVendorPrefs()
                                        {
                                            Airline = new AirTicketRQTicketingFlightQualifiersVendorPrefsAirline()
                                            {
                                                Code = quotegrp.Key.PlatingCarrier
                                            }
                                        }
                                    }                                    
                                }).
                                ToList());
            }

            if (!emds.IsNullOrEmpty())
            {
                //add emds to ticketing
                ticketing.
                    AddRange(
                        emds.
                            GroupBy(g => new {g.PlatingCarrier, g.FormOfPayment.PaymentType, g.FormOfPayment.CardNumber }).
                            Select(emdgrp => new AirTicketRQTicketing()
                            {
                                FOP_Qualifiers = getFormOfPayment(
                                                    emdgrp.Select(s => s.FormOfPayment).Distinct().ToList(),
                                                    decimalformatstring,
                                                    emdgrp.Sum(s => s.Total),
                                                    ""),
                                MiscQualifiers = GetMiscQualifiers(
                                                        null, 
                                                        emdgrp.ToList(),
                                                        "")
                            }).
                            ToList());
            }

            if (ticketing.IsNullOrEmpty())
            {
                throw new AeronologyException("50000061", "No documents were requested to issue.");
            }


            return new AirTicketRQ()
            {
                targetCity = GetTargetCity(pcc, ticketingpcc),
                version = Constants.EnhancedAirTicketVersion,
                DesignatePrinter = new AirTicketRQDesignatePrinter()
                {
                    Printers = new AirTicketRQDesignatePrinterPrinters()
                    {
                        Ticket = new AirTicketRQDesignatePrinterPrintersTicket()
                        {
                            CountryCode = printerbypass
                        },
                        Hardcopy = new AirTicketRQDesignatePrinterPrintersHardcopy()
                        {
                            LNIATA = ticketprinter
                        },
                        InvoiceItinerary = new AirTicketRQDesignatePrinterPrintersInvoiceItinerary()
                        {
                            LNIATA = ticketprinter
                        }
                    }
                },
                Itinerary = new AirTicketRQItinerary()
                {
                    ID = locator
                },
                Ticketing = ticketing.ToArray(),
                PostProcessing = new AirTicketRQPostProcessing()
                {
                    EndTransaction = new AirTicketRQPostProcessingEndTransaction()
                    {
                        Source = new AirTicketRQPostProcessingEndTransactionSource()
                        {
                            ReceivedFrom = "Aerotickets"
                        }
                    },
                    GhostTicketCheck = new AirTicketRQPostProcessingGhostTicketCheck()
                    {
                        waitInterval = 1000,
                        numAttempts = 2
                    },
                    acceptPriceChangesSpecified = true,
                    acceptPriceChanges = false
                }
            };
        }



        private static string GetTargetCity(string pcc, string ticketingpcc)
        {
            return string.IsNullOrEmpty(ticketingpcc) ? "" : ticketingpcc == pcc ? "" : ticketingpcc;
        }

        private static AirTicketRQTicketingMiscQualifiers GetMiscQualifiers(
            List<IssueExpressTicketQuote> quotes,
            List<IssueExpressTicketEMD> emds = null,
            string bcode = "",
            bool enableextendedendo = false)
        {
            AirTicketRQTicketingMiscQualifiers miscQualifiers = new AirTicketRQTicketingMiscQualifiers();

            //Endorsements
            if(!string.IsNullOrEmpty(bcode))
            {
                string endosement = "";

                if (!quotes.First().Endorsements.IsNullOrEmpty())
                {
                    endosement = string.Join(" ", quotes.First().Endorsements);
                    endosement = (bcode.Trim().StartsWith("INV") ? bcode.Trim().Substring(3) : bcode) + "/" + endosement;
                }
                else
                {
                    endosement = bcode.Trim().StartsWith("INV") ? bcode.Trim().Substring(3) : bcode;
                }

                int endosementength = enableextendedendo ? 
                                            endosement.Length > 990 ? 990 : endosement.Length : 
                                            endosement.Length > 58 ? 58 : endosement.Length;

                miscQualifiers.Endorsement = new AirTicketRQTicketingMiscQualifiersEndorsement()
                {
                    OverrideSpecified = true,
                    Override = true,
                    Text = endosement.Trim().Substring(0, endosementength)
                };
            }

            //Ticket
            miscQualifiers.Ticket = new AirTicketRQTicketingMiscQualifiersTicket()
            {
                Type = emds.IsNullOrEmpty() ? "ETR" : "EMD"
            };

            //Commission
            if (emds.IsNullOrEmpty())
            {
                miscQualifiers.Commission = new AirTicketRQTicketingMiscQualifiersCommission()
                {
                    PercentSpecified = true,
                    Percent = quotes.First().BSPCommissionRate.HasValue ? 
                                        Convert.ToInt32(quotes.First().BSPCommissionRate.Value):
                                        0
                };

                if(!string.IsNullOrEmpty(quotes.First().TourCode) )
                {
                    miscQualifiers.TourCode = new AirTicketRQTicketingMiscQualifiersTourCode()
                    {
                        Text = quotes.First().TourCode,
                        SuppressIT = quotes.Any(a=> a.ApplySupressITFlag) ? 
                                            new  AirTicketRQTicketingMiscQualifiersTourCodeSuppressIT()
                                            {
                                                Ind = true
                                            }:
                                            null
                    };
                }
            }
            

            //EMDs
            if (!emds.IsNullOrEmpty())
            {
                miscQualifiers.AirExtras = emds.
                                            OrderBy(o => o.EMDNo).
                                            Select(emd =>
                                                    new AirTicketRQTicketingMiscQualifiersAirExtras()
                                                    {
                                                        Number = emd.EMDNo
                                                    }).
                                            ToArray();
            }

            return miscQualifiers;
        }

        private static AirTicketRQTicketingPricingQualifiers GetPricingInstruction(List<IssueExpressTicketQuote> quotes)
        {
            AirTicketRQTicketingPricingQualifiers pricinginstructions = new AirTicketRQTicketingPricingQualifiers();

            if (quotes.IsNullOrEmpty()) { return pricinginstructions; }

            if (quotes.Any(a=> a.PartialIssue) || 
                quotes.
                    Any(a=> 
                           a.QuotePassenger.FormOfPayment.CreditAmount != 
                           quotes.First().QuotePassenger.FormOfPayment.CreditAmount))
            {
                //specify passengers
                pricinginstructions.NameSelect = quotes.
                                                    Select(s => new AirTicketRQTicketingPricingQualifiersNameSelect()
                                                    {
                                                        NameNumber = s.QuotePassenger.NameNumber
                                                    }).ToArray();

                //specify sectors
                pricinginstructions.ItineraryOptions = new AirTicketRQTicketingPricingQualifiersItineraryOptions()
                {
                    SegmentSelect = quotes.
                                        First().
                                        QuoteSectors.
                                        Select(s => new AirTicketRQTicketingPricingQualifiersItineraryOptionsSegmentSelect()
                                        {
                                            Number = s.PQSectorNo
                                        }).ToArray()
                };
            }
            else
            {
                pricinginstructions.PriceQuote = new AirTicketRQTicketingPricingQualifiersPriceQuote[]
                                                    {
                                                        new AirTicketRQTicketingPricingQualifiersPriceQuote()
                                                                    {
                                                                        Record = quotes.
                                                                                    Select(s=> s.QuoteNo).
                                                                                    Distinct().
                                                                                    Select(s => new AirTicketRQTicketingPricingQualifiersPriceQuoteRecord()
                                                                                    {
                                                                                            Number = s
                                                                                    }).
                                                                                    ToArray()
                                                                    }
                                                    };
            }

            return pricinginstructions;
        }

        private static AirTicketRQTicketingFOP_Qualifiers getFormOfPayment(List<FOP> fops, string decimalformatstring, decimal total = 0M, string bcode = "")
        {
            AirTicketRQTicketingFOP_Qualifiers res = new AirTicketRQTicketingFOP_Qualifiers();

            if (fops.All(a => a.PaymentType == PaymentType.CA))
            {
                //Full cash
                res.BasicFOP = new AirTicketRQTicketingFOP_QualifiersBasicFOP()
                {
                    Type = fops.All(a => a.PaymentType == PaymentType.CA) && fops.Any(a => !string.IsNullOrEmpty(a.BCode)) ?
                               fops.First(f => !string.IsNullOrEmpty(f.BCode)).BCode.Trim() :
                               string.IsNullOrEmpty(bcode) ?
                                    "CA":
                                    bcode.Trim().ToUpper()
                };
                return res;
            }
            else if (fops.All(a => a.PaymentType == PaymentType.CC))
            {
                if (fops.All(a => a.CardNumber == fops.First().CardNumber))
                {
                    //Single CC
                    if (fops.All(a => a.CreditAmount == 0M) ||
                        total == 0M ||
                        total == fops.Sum(s => s.CreditAmount))
                    {
                        //Full credit on one card
                        return new AirTicketRQTicketingFOP_Qualifiers()
                        {
                            BasicFOP = new AirTicketRQTicketingFOP_QualifiersBasicFOP()
                            {
                                //Note Type = "CC" is not required, if pased cause "FOP RESTRICTED TO CREDIT TYPE ONLY"
                                CC_Info = new AirTicketRQTicketingFOP_QualifiersBasicFOPCC_Info()
                                {
                                    PaymentCard = new AirTicketRQTicketingFOP_QualifiersBasicFOPCC_InfoPaymentCard()
                                    {
                                        Number = long.Parse(fops.First().CardNumber),
                                        ExpireDate = DateTime.
                                                        ParseExact(
                                                            fops.
                                                                First().
                                                                ExpiryDate.
                                                                ReplaceAll(new string[] { "-", "/", "_", "\\", "~", " ", "  " }, ""),
                                                                "MMyy",
                                                                System.Globalization.CultureInfo.InvariantCulture).
                                                                ToString("yyyy-MM"),
                                        Code = CreditCardOperations.
                                                    GetCreditCardType(
                                                        fops.
                                                        First().
                                                        CardNumber.
                                                        ToString())
                                    }
                                }
                            }
                        };
                    }
                    else
                    {
                        //Part cash part credit
                        decimal cashamount = total - fops.Sum(s => s.CreditAmount);
                        return new AirTicketRQTicketingFOP_Qualifiers()
                        {
                            BSP_Ticketing = new AirTicketRQTicketingFOP_QualifiersBSP_Ticketing()
                            {
                                MultipleFOP = new AirTicketRQTicketingFOP_QualifiersBSP_TicketingMultipleFOP()
                                {
                                    FOP_One = new AirTicketRQTicketingFOP_QualifiersBSP_TicketingMultipleFOPFOP_One()
                                    {
                                        CC_Info = new AirTicketRQTicketingFOP_QualifiersBSP_TicketingMultipleFOPFOP_OneCC_Info()
                                        {
                                            PaymentCard = new AirTicketRQTicketingFOP_QualifiersBSP_TicketingMultipleFOPFOP_OneCC_InfoPaymentCard()
                                            {
                                                Number = long.Parse(fops.First().CardNumber),
                                                ExpireDate = DateTime.
                                                            ParseExact(
                                                                fops.
                                                                    First().
                                                                    ExpiryDate,
                                                                    "MMyy",
                                                                    System.Globalization.CultureInfo.InvariantCulture).
                                                                    ToString("yyyy-MM"),
                                                Code = CreditCardOperations.
                                                        GetCreditCardType(
                                                            fops.
                                                            First().
                                                            CardNumber.
                                                            ToString()),
                                                //ManualOBFee = 
                                            }
                                        }
                                    },
                                    FOP_Two = new AirTicketRQTicketingFOP_QualifiersBSP_TicketingMultipleFOPFOP_Two()
                                    {
                                        Type = "CA"
                                    },
                                    Fare = new AirTicketRQTicketingFOP_QualifiersBSP_TicketingMultipleFOPFare()
                                    {
                                        Amount = cashamount.ToString(decimalformatstring)
                                    }
                                }
                            }
                        };
                    }
                }
                else
                {
                    //Multiple CC
                    FOP cc1 = fops.First();
                    FOP cc2 = fops.First(f => f.CardNumber != fops.First().CardNumber);
                    return new AirTicketRQTicketingFOP_Qualifiers()
                    {
                        MultipleCC_FOP = new AirTicketRQTicketingFOP_QualifiersMultipleCC_FOP()
                        {
                            CC_One = new AirTicketRQTicketingFOP_QualifiersMultipleCC_FOPCC_One()
                            {
                                CC_Info = new AirTicketRQTicketingFOP_QualifiersMultipleCC_FOPCC_OneCC_Info()
                                {
                                    PaymentCard = new AirTicketRQTicketingFOP_QualifiersMultipleCC_FOPCC_OneCC_InfoPaymentCard()
                                    {
                                        Number = long.Parse(cc1.CardNumber),
                                        ExpireDate = DateTime.
                                                        ParseExact(
                                                            cc1.
                                                                ExpiryDate,
                                                                "MMyy",
                                                                System.Globalization.CultureInfo.InvariantCulture).
                                                                ToString("yyyy-MM"),
                                        Code = CreditCardOperations.
                                                    GetCreditCardType(
                                                        cc1.
                                                        CardNumber.
                                                        ToString())
                                    }
                                }
                            },
                            CC_Two = new AirTicketRQTicketingFOP_QualifiersMultipleCC_FOPCC_Two()
                            {
                                CC_Info = new AirTicketRQTicketingFOP_QualifiersMultipleCC_FOPCC_TwoCC_Info()
                                {
                                    PaymentCard = new AirTicketRQTicketingFOP_QualifiersMultipleCC_FOPCC_TwoCC_InfoPaymentCard()
                                    {
                                        Number = long.Parse(cc2.CardNumber),
                                        ExpireDate = DateTime.
                                                        ParseExact(
                                                            cc2.
                                                                ExpiryDate,
                                                                "MMyy",
                                                                System.Globalization.CultureInfo.InvariantCulture).
                                                                ToString("yyyy-MM"),
                                        Code = CreditCardOperations.
                                                    GetCreditCardType(
                                                        cc2.
                                                        CardNumber.
                                                        ToString())
                                    }
                                }
                            },
                            Fare = new AirTicketRQTicketingFOP_QualifiersMultipleCC_FOPFare()
                            {
                                Amount = cc2.CreditAmount.ToString(decimalformatstring)
                            }
                        }
                    };
                }
            }

            return res;
        }

        public async Task<List<issueticketresponse>> IssueTicket(
            IssueExpressTicketRQ issueExpressTicketRQ,
            string pcc, 
            Token token, 
            string ticketingpcc,
            string ticketingprinter,
            string printerbypass,
            string bcode,
            bool enableextendedendo,
            string decimalformatstring)
        {
            List<issueticketresponse> issueticketresponses = new List<issueticketresponse>();
            EnableTLS();

            await IssueDocument(issueExpressTicketRQ, pcc, token, ticketingpcc, issueticketresponses, ticketingprinter, printerbypass, bcode, enableextendedendo, decimalformatstring);

            return issueticketresponses;
        }

        private async Task IssueDocument(IssueExpressTicketRQ issueExpressTicketRQ, string pcc, Token token, 
                string ticketingpcc, List<issueticketresponse> issueticketresponses, string ticketprinter, 
                string printerbypass, string bcode, bool enableextendedendo, string decimalformatstring)
        {
            List<IssueExpressTicketQuote> quote = issueExpressTicketRQ.Quotes;

            //generate request object
            AirTicketRQ AirTicketRQ = GetAirTicketRQ(
                                        issueExpressTicketRQ.Locator,
                                        quote,
                                        issueExpressTicketRQ.EMDs,
                                        pcc,
                                        ticketingpcc,
                                        ticketprinter,
                                        printerbypass,
                                        bcode,
                                        enableextendedendo,
                                        decimalformatstring);

            //generate json request
            string json = JsonConvert.
                            SerializeObject(
                                new { AirTicketRQ },
                                Formatting.Indented,
                                new JsonSerializerSettings()
                                {
                                    DefaultValueHandling = DefaultValueHandling.Ignore,
                                    NullValueHandling = NullValueHandling.Ignore
                                });

            logger.LogInformation($"##### TICKETING REQUEST : {json.MaskLog()} #####");
            
            //invoke issue ticket REST service
            string response = await SabreSharedServices.InvokeRestAPI(token, SabreSharedServices.RestServices.EnhanceIssueTicket, json, logger);
            
            logger.LogInformation($"##### TICKETING RESPONSE : {response.MaskLog()} #####");

            GenerateIssueTicketResponse(issueExpressTicketRQ, issueticketresponses, quote, AirTicketRQ, response);
        }

        private static void GenerateIssueTicketResponse(IssueExpressTicketRQ issueExpressTicketRQ, List<issueticketresponse> issueticketresponses, List<IssueExpressTicketQuote> quote, AirTicketRQ AirTicketRQ, string response)
        {
            issueticketresponses.
                Add(new issueticketresponse()
                {
                    QuoteNos = quote.IsNullOrEmpty() ?
                                new List<IssueTicketDocumentData>() :
                                quote.Select(s => new IssueTicketDocumentData()
                                {
                                    DocumentNo = s.QuoteNo,
                                    DocumentType = "QUOTE",
                                    PassengerName = s.QuotePassenger.PassengerName,
                                    Route = s.Route,
                                    PriceIt = s.TotalFare,
                                    PlatingCarrier = s.PlatingCarrier,
                                    TotalFare = s.BaseFare + s.TotalTax,
                                    TotalTax = s.TotalTax,
                                    Commission = Math.Round(((s.BaseFare * s.AgentCommissionRate ?? 0.00M) / 100), 2, MidpointRounding.AwayFromZero),
                                    Fee = s.Fee,
                                    FeeGST = s.FeeGST,
                                    FormOfPayment = s.QuotePassenger.FormOfPayment,
                                    AgentPrice = s.QuotePassenger.FormOfPayment == null || s.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CA ?
                                                        s.TotalFare + s.Fee + +(s.FeeGST.HasValue ? s.FeeGST.Value : 0.00M) - Math.Round(((s.BaseFare * s.AgentCommissionRate ?? 0.00M) / 100), 2, MidpointRounding.AwayFromZero) :
                                                        s.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CC && s.QuotePassenger.FormOfPayment.CreditAmount < (s.BaseFare + s.TotalTax) ?
                                                            s.TotalFare - s.QuotePassenger.FormOfPayment.CreditAmount + s.Fee + (s.FeeGST.HasValue ? s.FeeGST.Value : 0.00M) - Math.Round(((s.BaseFare * s.AgentCommissionRate ?? 0.00M) / 100), 2, MidpointRounding.AwayFromZero) :
                                                            s.Fee + (s.FeeGST.HasValue ? s.FeeGST.Value : 0.00M) - Math.Round(((s.BaseFare * s.AgentCommissionRate ?? 0.00M) / 100), 2, MidpointRounding.AwayFromZero)
                                }).ToList(),
                    EMDNos = issueExpressTicketRQ.EMDs.IsNullOrEmpty() ?
                                new List<IssueTicketDocumentData>() :
                                issueExpressTicketRQ.EMDs.Select(s => new IssueTicketDocumentData()
                                {
                                    DocumentNo = s.EMDNo,
                                    DocumentType = "EMD",
                                    PassengerName = s.PassengerName,
                                    Route = s.Route,
                                    PriceIt = s.Total,
                                    PlatingCarrier = s.PlatingCarrier,
                                    RFISC = s.RFISC,
                                    TotalFare = s.Total,
                                    TotalTax = s.TotalTax,
                                    Commission = s.Commission,
                                    Fee = s.Fee,
                                    FeeGST = s.FeeGST,
                                    FormOfPayment = s.FormOfPayment,
                                    AgentPrice = s.FormOfPayment == null || s.FormOfPayment.PaymentType == PaymentType.CA ?
                                                        (s.Total + s.Fee + +(s.FeeGST.HasValue ? s.FeeGST.Value : 0.00M)) - s.Commission :
                                                        s.FormOfPayment.PaymentType == PaymentType.CC && s.FormOfPayment.CreditAmount < s.Total ?
                                                            s.Total - s.FormOfPayment.CreditAmount + s.Fee + (s.FeeGST.HasValue ? s.FeeGST.Value : 0.00M) - s.Commission :
                                                            s.Fee + (s.FeeGST.HasValue ? s.FeeGST.Value : 0.00M) - s.Commission
                                }).ToList(),
                    GDSResponse = response,
                    GDSRequest = AirTicketRQ
                });
        }
    }

    public class issueticketresponse
    {
        public List<IssueTicketDocumentData> QuoteNos { get; set; }
        public List<IssueTicketDocumentData> EMDNos { get; set; }
        public string GDSResponse { get; set; }
        public AirTicketRQ GDSRequest { get; set; }
    }


    public class IssueTicketDocumentData
    {
        public int DocumentNo { get; set; }
        public string DocumentType { get; set; }
        public string PassengerName { get; set; }
        public string Route { get; set; }
        public string PlatingCarrier { get; set; }
        public string RFISC { get; set; }
        public decimal TotalFare { get; set; }
        public decimal TotalTax { get; set; }
        public decimal PriceIt { get; set; }
        public decimal Commission { get; set; }
        public decimal Fee { get; set; }
        public decimal? FeeGST { get; set; }
        public decimal AgentPrice { get; set; }
        public FOP FormOfPayment { get; set; }
    }
}
