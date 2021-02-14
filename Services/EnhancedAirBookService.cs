using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EnhancedAirBook;
using Microsoft.Extensions.Logging;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;

namespace SabreWebtopTicketingService.Services
{
    public class EnhancedAirBookService : ConnectionStubs
    {
        private readonly SessionDataSource sessionData;

        private readonly ILogger<EnhancedAirBookService> logger;

        private readonly string url;

        public EnhancedAirBookService(
            SessionDataSource sessionData,
            ILogger<EnhancedAirBookService> logger)
        {
            this.sessionData = sessionData;
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        public async Task<List<Quote>> PricePNRForTicketing(GetQuoteRQ quoteRequest, string token, Pcc pcc, PNR pnr, string ticketingpcc)
        {
            var response = await PricePNR(CreatePriceByPaxRequest(quoteRequest, pnr), token, pcc, ticketingpcc);

            List<pnrquotedata> quotenos = new List<pnrquotedata>();

            if (!pnr.Quotes.IsNullOrEmpty())
            {
                //read the quote number assuming no quotes get over written by sabre
                quotenos.AddRange(
                    pnr.
                        Quotes.
                        Where(w => w.FiledFare).
                        GroupBy(g => g.QuoteNo).
                        Select(s => new pnrquotedata()
                        {
                            QuoteNo = s.Key,
                            Expired = s.First().Expired,
                            PaxType = s.First().QuotePassenger.PaxType,
                            PassengerNameNumbers = s.Select(q => q.QuotePassenger.NameNumber).ToList(),
                            Sectors = s.First().QuoteSectors.Select(sec => sec.PQSectorNo).ToList(),
                            Total = s.First().TotalFare,
                            LastDateToPurchase = s.First().LastPurchaseDate
                        }).
                        ToList());
            }

            return ParseSabreQuote(
                        response.OTA_AirPriceRS, 
                        quoteRequest.SelectedPassengers,
                        quoteRequest.SelectedSectors.Select(s => new SectorData() { SectorNo = s.SectorNo }).ToList(),
                        pnr,
                        Models.PriceType.Published,
                        quotenos);
        }

        public async Task<List<Quote>> PricePNR(GetQuoteRQ quoteRequest, string token, Pcc pcc, PNR pnr, string ticketingpcc, bool IsPriceOverride)
        {
            var response = await PricePNR(CreatePriceByPaxRequest(quoteRequest, pnr, IsPriceOverride), token, pcc, ticketingpcc);

            return ParseSabreQuote(
                            response.OTA_AirPriceRS, 
                            quoteRequest.SelectedPassengers, 
                            quoteRequest.SelectedSectors.Select(s=> new SectorData() { SectorNo = s.SectorNo}).ToList(),
                            pnr,
                            Models.PriceType.Published);
        }

        private static MessageHeader CreateHeader(string pcc, string ticketingpcc)
        {
            return new MessageHeader()
            {
                version = Constants.EnhancedAirBookVersion,
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
                Action = "EnhancedAirBookRQ",
                CPAId = string.IsNullOrEmpty(ticketingpcc) ? pcc : ticketingpcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "StatefulEnhancedAirBookRQ"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.GetSabreDatetime()
                }
            };
        }

        private EnhancedAirBookRQ CreatePriceByPaxRequest(GetQuoteRQ quoteRequest, PNR pnr, bool IsPriceOveride = false, bool inquoting = false)
        {
            EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersPassengerType[] passengerTypes = GetPaxTypeData(quoteRequest.SelectedPassengers, pnr);
            EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersNameSelect[] nameselect =
                            quoteRequest.
                                SelectedPassengers.
                                Select(pax => new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersNameSelect()
                                {
                                    NameNumber = pax.NameNumber
                                }).
                                ToArray();

            EnhancedAirBookRQ enhancedAirBookRQ = new EnhancedAirBookRQ()
            {
                version = Constants.EnhancedAirBookVersion,
                PreProcessing = new EnhancedAirBookRQPreProcessing()
                {
                    IgnoreBeforeSpecified = true,
                    IgnoreBefore = true,
                    UniqueID = new EnhancedAirBookRQPreProcessingUniqueID()
                    {
                        ID = quoteRequest.Locator
                    }
                },
                PostProcessing = new EnhancedAirBookRQPostProcessing()
                {
                    IgnoreAfterSpecified = true,
                    IgnoreAfter = false,
                    RedisplayReservation = new EnhancedAirBookRQPostProcessingRedisplayReservation()
                    {
                        WaitInterval = "100"
                    }
                },
                OTA_AirPriceRQ = new EnhancedAirBookRQOTA_AirPriceRQ[]
                {
                        new EnhancedAirBookRQOTA_AirPriceRQ()
                        {
                            PriceRequestInformation = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformation()
                            {
                                AlternativePricingSpecified = quoteRequest.AlternativePricing,
                                AlternativePricing = quoteRequest.AlternativePricing,
                                OptionalQualifiers = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiers()
                                {
                                    PricingQualifiers = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiers()
                                    {
                                        RoundTheWorldSpecified = quoteRequest.IsRTW,
                                        RoundTheWorld = quoteRequest.SelectedSectors.Count() >= 3 ? quoteRequest.IsRTW: false,
                                        PassengerType = passengerTypes,
                                        NameSelect = nameselect,
                                        //Sector Selection
                                        ItineraryOptions = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersItineraryOptions()
                                        {
                                            SegmentSelect = IsPriceOveride ?
                                                                quoteRequest.
                                                                    SelectedSectors.
                                                                    Where(w=> !"SURFACE|ARUNK".Contains(pnr.Sectors.First(f => f.SectorNo == w.SectorNo).From)).
                                                                    Select(s=> new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersItineraryOptionsSegmentSelect()
                                                                    {
                                                                        RPH = pnr.Sectors.First(f => f.SectorNo == s.SectorNo).Carrier == quoteRequest.PlatingCarrier?
                                                                                    s.SectorNo.ToString():
                                                                                    null,
                                                                        Number = s.SectorNo.ToString()
                                                                    }).
                                                                    ToArray():
                                                                quoteRequest.
                                                                    SelectedSectors.
                                                                    Where(w=> !"SURFACE|ARUNK".Contains(pnr.Sectors.First(f => f.SectorNo == w.SectorNo).From)).
                                                                    Select(s=> new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersItineraryOptionsSegmentSelect()
                                                                    {
                                                                        Number = s.SectorNo.ToString()
                                                                    }).
                                                                    ToArray()
                                        },
                                        //Enable Specific Penalty
                                        SpecificPenalty = IsPriceOveride ?
                                                                null:
                                                                new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersSpecificPenalty()
                                                                {
                                                                    AdditionalInfoSpecified = true,
                                                                    AdditionalInfo = true
                                                                },
                                        Account = string.IsNullOrEmpty(quoteRequest.PriceCode) ?
                                                    null:
                                                    new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersAccount()
                                                    {
                                                        //Removed Force flag as this will stop Sabre returning and fare if the price code is not applicable
                                                        //Force = "true",
                                                        Code = new string[]
                                                        {
                                                            quoteRequest.PriceCode
                                                        }
                                                    },
                                        Overrides = IsPriceOveride ?
                                                            new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersOverrides()
                                                            {
                                                                GoverningCarrierOverride = quoteRequest.
                                                                SelectedSectors.
                                                                Where(w=> !"SURFACE|ARUNK".Contains(pnr.Sectors.First(f => f.SectorNo == w.SectorNo).From) &&
                                                                          pnr.Sectors.First(f => f.SectorNo == w.SectorNo).Carrier == quoteRequest.PlatingCarrier).
                                                                Select(s => new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersOverridesGoverningCarrierOverride()
                                                                    {
                                                                        RPH = s.SectorNo.ToString(),
                                                                        Airline = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersOverridesGoverningCarrierOverrideAirline()
                                                                        {
                                                                            Code = quoteRequest.PlatingCarrier
                                                                        }
                                                                    }
                                                                ).
                                                                ToArray()
                                                            }:
                                                            null
                                    },
                                    FOP_Qualifiers = getFormOfPayment(quoteRequest.SelectedPassengers.First().FormOfPayment),
                                    MiscQualifiers = quoteRequest.SelectedPassengers.First().FormOfPayment.PaymentType == PaymentType.CC &&
                                                     !string.IsNullOrEmpty(quoteRequest.SelectedPassengers.First().FormOfPayment.BCode) ?
                                                     new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersMiscQualifiers()
                                                     {
                                                         Endorsements = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersMiscQualifiersEndorsements()
                                                         {
                                                             Text = quoteRequest.SelectedPassengers.First().FormOfPayment.BCode
                                                         }
                                                     }:
                                                     null,
                                    FlightQualifiers = string.IsNullOrEmpty(quoteRequest.PlatingCarrier) ? 
                                                                null :
                                                                new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFlightQualifiers()
                                                                {
                                                                    VendorPrefs = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFlightQualifiersVendorPrefs()
                                                                    {
                                                                        Airline = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFlightQualifiersVendorPrefsAirline()
                                                                        {
                                                                            Code = quoteRequest.PlatingCarrier
                                                                        }
                                                                    }
                                                                }
                                }
                            }
                        }
                }
            };

            if (quoteRequest.QuoteDate.HasValue)
            {
                enhancedAirBookRQ.
                        OTA_AirPriceRQ.
                        First().
                        PriceRequestInformation.
                        OptionalQualifiers.
                        PricingQualifiers.
                        BuyingDate = quoteRequest.QuoteDate.Value.ToString("yyyy-MM-dd");
            }

            return enhancedAirBookRQ;
        }

        private static EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_Qualifiers getFormOfPayment(FOP fop)
        {
            EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_Qualifiers res = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_Qualifiers()
            {
                BasicFOP = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_QualifiersBasicFOP()
                {
                    Type = string.IsNullOrEmpty(fop.BCode) ?
                                "CA" :
                                fop.BCode.Trim()                                               
                }
            };

            if (fop.PaymentType == PaymentType.CA)
            {
                return res;
            }
            else if (fop.PaymentType == PaymentType.CC)
            {
                res = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_Qualifiers()
                {
                    BasicFOP = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_QualifiersBasicFOP()
                    {
                        //Note Type = "CC" is not required, if pased cause "FOP RESTRICTED TO CREDIT TYPE ONLY"
                        CC_Info = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_QualifiersBasicFOPCC_Info()
                        {
                            PaymentCard = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_QualifiersBasicFOPCC_InfoPaymentCard()
                            {
                                Number = fop.CardNumber,
                                ExpireDate = DateTime.
                                                ParseExact(
                                                    fop.
                                                        ExpiryDate,
                                                    "MMyy",
                                                    CultureInfo.InvariantCulture).
                                                    ToString("yyyy-MM"),
                                Code = CreditCardOperations.
                                            GetCreditCardType(
                                                fop.
                                                CardNumber.
                                                ToString())
                            }
                        }
                    }
                };
            }

            return res;
        }

        private static EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersPassengerType[] GetPaxTypeData(List<QuotePassenger> selpax, PNR pnr)
        {
            DateTime FirstDepartureDate = GetFirstDepartureDate(pnr);

            EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersPassengerType[] passengerTypes =
                                        selpax.
                                            GroupBy(g => g.PaxType.StartsWith("C") ?
                                                            g.PaxType.IsMatch(@"C\d+") ?
                                                                g.PaxType :
                                                                g.DOB.HasValue && g.DOB.Value != DateTime.MinValue && FirstDepartureDate != DateTime.MinValue ?
                                                                    "C" + GetAge(FirstDepartureDate, g.DOB.Value).ToString().PadLeft(2, '0') :
                                                                    "CNN" :
                                                            g.PaxType).
                                            Select(s => new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersPassengerType()
                                            {
                                                Code = s.Key,
                                                Quantity = s.Count().ToString(),
                                                //Removed Force flag as this will stop Sabre defaulting to ADT if the specified pax type is not available
                                                //ForceSpecified = true,
                                                //Force = true
                                            }).
                                            ToArray();

            return passengerTypes;
        }

        private static int GetAge(DateTime firstDepartureDate, DateTime dob)
        {
            int age = 0;
            age = firstDepartureDate.Year - dob.Year;
            if (DateTime.Now.DayOfYear < dob.DayOfYear)
            {
                age -= 1;
            }

            return age;
        }

        private static DateTime GetFirstDepartureDate(PNR pnr)
        {
            DateTime FirstDepartureDate = DateTime.MinValue;

            var secs = pnr.
                        Sectors.
                        Where(w => w.From != "ARUNK");

            if (!secs.IsNullOrEmpty())
            {
                FirstDepartureDate = DateTime.
                                        Parse(secs.
                                                First().
                                                DepartureDate);
            }

            return FirstDepartureDate;
        }

        private EnhancedAirBookRQ CreateBookRequest(PNR pnr)
        {
            return new EnhancedAirBookRQ()
            {
                version = Constants.EnhancedAirBookVersion,
                IgnoreOnErrorSpecified = true,
                IgnoreOnError = true,
                HaltOnErrorSpecified = true,
                HaltOnError = true,

                PreProcessing = new EnhancedAirBookRQPreProcessing()
                {
                    IgnoreBeforeSpecified = true,
                    IgnoreBefore = true,
                },
                PostProcessing = new EnhancedAirBookRQPostProcessing()
                {
                    IgnoreAfterSpecified = true,
                    IgnoreAfter = false,
                    RedisplayReservation = new EnhancedAirBookRQPostProcessingRedisplayReservation()
                    {
                        UnmaskCreditCardSpecified = true,
                        UnmaskCreditCard = false
                    },
                    ARUNK_RQ = ""
                },
                OTA_AirBookRQ = new EnhancedAirBookRQOTA_AirBookRQ()
                {
                    HaltOnStatus = "HL,HN,LL,NN,PN,NO,UC,UN,US,UU,HX".
                                    SplitOn(",").
                                    Select(s => new EnhancedAirBookRQOTA_AirBookRQHaltOnStatus() { Code = s }).
                                    ToArray(),
                    RedisplayReservation = new EnhancedAirBookRQOTA_AirBookRQRedisplayReservation()
                    {
                        NumAttempts = "5",
                        WaitInterval = "200"
                    }

                },
                OTA_AirPriceRQ = new EnhancedAirBookRQOTA_AirPriceRQ[]
                {
                    new EnhancedAirBookRQOTA_AirPriceRQ()
                    {
                        PriceRequestInformation = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformation()
                        {
                            RetainSpecified = true,
                            Retain = true,
                            OptionalQualifiers = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiers()
                            {
                                PricingQualifiers = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiers()
                                {
                                    RoundTheWorldSpecified = true,
                                    RoundTheWorld =true,
                                    PassengerType =  pnr.
                                                        Passengers.
                                                        GroupBy(g=> g.PaxType).
                                                        Select(s => new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersPassengerType()
                                                        {
                                                            Code = s.Key,
                                                            Quantity = s.Count().ToString()
                                                            //Removed Force flag as this will stop Sabre defaulting to ADT if the specified pax type is not available
                                                            //ForceSpecified = true,
                                                            //Force = true
                                                        }).
                                                     ToArray()
                                },
                                FOP_Qualifiers = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_Qualifiers()
                                {
                                    BasicFOP = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFOP_QualifiersBasicFOP()
                                    {
                                        Type = "CA"
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private EnhancedAirBookRQ CreatePriceByForceFBRequest(ForceFBQuoteRQ quoteRequest, PNR pnr)
        {
            EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersPassengerType[] passengerTypes = GetPaxTypeData(quoteRequest.SelectedPassengers, pnr);
            EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersNameSelect[] nameselect =
                            quoteRequest.
                                SelectedPassengers.
                                Select(pax => new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersNameSelect()
                                {
                                    NameNumber = pax.NameNumber
                                }).
                                ToArray();

            EnhancedAirBookRQ enhancedAirBookRQ = new EnhancedAirBookRQ()
            {
                version = Constants.EnhancedAirBookVersion,
                PreProcessing = new EnhancedAirBookRQPreProcessing()
                {
                    IgnoreBeforeSpecified = true,
                    IgnoreBefore = true,
                    UniqueID = new EnhancedAirBookRQPreProcessingUniqueID()
                    {
                        ID = quoteRequest.Locator
                    }
                },
                PostProcessing = new EnhancedAirBookRQPostProcessing()
                {
                    IgnoreAfterSpecified = true,
                    IgnoreAfter = false,
                    RedisplayReservation = new EnhancedAirBookRQPostProcessingRedisplayReservation()
                    {
                        WaitInterval = "100"
                    }
                },
                OTA_AirPriceRQ = new EnhancedAirBookRQOTA_AirPriceRQ[]
                {
                        new EnhancedAirBookRQOTA_AirPriceRQ()
                        {
                            PriceRequestInformation = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformation()
                            {
                                AlternativePricingSpecified = quoteRequest.AlternativePricing,
                                AlternativePricing = quoteRequest.AlternativePricing,
                                OptionalQualifiers = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiers()
                                {
                                    PricingQualifiers = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiers()
                                    {
                                        //RoundTheWorldSpecified = true,
                                        //RoundTheWorld = quoteRequest.SelectedSectors.Count() >=3 ? true: false,
                                        PassengerType = passengerTypes,
                                        NameSelect = nameselect,
                                        //Sector Selection
                                        ItineraryOptions = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersItineraryOptions()
                                        {
                                            SegmentSelect = quoteRequest.
                                                                SelectedSectors.
                                                                Where(w=> pnr.Sectors.First(f=> f.SectorNo == w.SectorNo).From != "SURFACE").
                                                                Select(s=> new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersItineraryOptionsSegmentSelect()
                                                                {
                                                                    RPH = string.IsNullOrEmpty(s.FareBasis) ? null : s.SectorNo.ToString(),
                                                                    Number = s.SectorNo.ToString()
                                                                }).
                                                                ToArray()
                                        },
                                        
                                        //Enable Specific Penalty
                                        //SpecificPenalty = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersSpecificPenalty()
                                        //{
                                        //    AdditionalInfoSpecified = true,
                                        //    AdditionalInfo = true,
                                        //},
                                        Account = string.IsNullOrEmpty(quoteRequest.PriceCode) ?
                                                    null:
                                                    new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersAccount()
                                                    {
                                                        //Removed Force flag as this will stop Sabre returning and fare if the price code is not applicable
                                                        //Force = "true",
                                                        Code = new string[]
                                                        {
                                                            quoteRequest.PriceCode
                                                        }
                                                    },
                                        CommandPricing = quoteRequest.
                                                            SelectedSectors.
                                                            Where(w=> !string.IsNullOrEmpty(w.FareBasis)).
                                                            Select(sec => new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersCommandPricing()
                                                            {
                                                                RPH = sec.SectorNo.ToString(),
                                                                FareBasis = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersPricingQualifiersCommandPricingFareBasis()
                                                                {
                                                                    Code = sec.FareBasis
                                                                }
                                                            }).
                                                            ToArray()
                                    },
                                    FOP_Qualifiers = getFormOfPayment(quoteRequest.SelectedPassengers.First().FormOfPayment),
                                    MiscQualifiers = quoteRequest.SelectedPassengers.First().FormOfPayment.PaymentType == PaymentType.CC &&
                                                     !string.IsNullOrEmpty(quoteRequest.SelectedPassengers.First().FormOfPayment.BCode) ?
                                                     new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersMiscQualifiers()
                                                     {
                                                         Endorsements = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersMiscQualifiersEndorsements()
                                                         {
                                                             Text = quoteRequest.SelectedPassengers.First().FormOfPayment.BCode
                                                         }
                                                     }:
                                                     null,
                                    FlightQualifiers = string.IsNullOrEmpty(quoteRequest.PlatingCarrier) ?
                                                                null :
                                                                new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFlightQualifiers()
                                                                {
                                                                    VendorPrefs = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFlightQualifiersVendorPrefs()
                                                                    {
                                                                        Airline = new EnhancedAirBookRQOTA_AirPriceRQPriceRequestInformationOptionalQualifiersFlightQualifiersVendorPrefsAirline()
                                                                        {
                                                                            Code = quoteRequest.PlatingCarrier
                                                                        }
                                                                    }
                                                                }
                                }
                            }
                        }
                }
            };

            if(quoteRequest.QuoteDate.HasValue)
            {
                enhancedAirBookRQ.
                        OTA_AirPriceRQ.
                        First().
                        PriceRequestInformation.
                        OptionalQualifiers.
                        PricingQualifiers.
                        BuyingDate = quoteRequest.QuoteDate.Value.ToString("yyyy-MM-dd");
            }

            return enhancedAirBookRQ;
        }

        internal async Task<List<Quote>> ForceFarebasis(ForceFBQuoteRQ request, string sessionID, Pcc pcc, PNR pnr, string ticketingpcc)
        {
            var response = await ExecForceFarebasis(request, sessionID, pcc, pnr, ticketingpcc);

            return ParseSabreQuote(
                            response.OTA_AirPriceRS, 
                            request.SelectedPassengers,
                            request.SelectedSectors.Select(s => new SectorData() { SectorNo = s.SectorNo }).ToList(),
                            pnr,
                            Models.PriceType.Manual);
        }

        private async Task<EnhancedAirBookRS> PricePNR(EnhancedAirBookRQ request, string token, Pcc pcc, string ticketingpcc)
        {
            EnhancedAirBookPortTypeClient client = null;

            try
            {
                EnableTLS();

                client = new EnhancedAirBookPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                MethodInfo method = typeof(XmlSerializer).
                                        GetMethod(
                                            "set_Mode",
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Static);

                method.Invoke(null, new object[] { 1 });

                logger.LogInformation($"{nameof(PricePNR)} {request}");
                var sw = Stopwatch.StartNew();
                client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));
                var result = await client.
                                        EnhancedAirBookRQAsync(
                                                CreateHeader(pcc.PccCode, ticketingpcc),
                                                new Security { BinarySecurityToken = token },
                                                request);
                sw.Stop();

                if (result == null || result.EnhancedAirBookRS.ApplicationResults.status != CompletionCodes.Complete)
                {
                    var messages = result.
                                    EnhancedAirBookRS.
                                    ApplicationResults.
                                    Error.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message);

                    throw new GDSException(
                                string.Join(Environment.NewLine, messages.Select(s => s.code.LastMatch(@"^(\d+)$").Trim()).Distinct()),
                                string.Join(Environment.NewLine, messages.Select(s => s.Value).Distinct()));
                }

                //if quoting failed you still get the status as Complete
                //However under warning you will be presented with error
                if (result.EnhancedAirBookRS.ApplicationResults.Warning != null)
                {
                    var messages = result.
                                    EnhancedAirBookRS.
                                    ApplicationResults.
                                    Warning.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message);

                    //ETG price quote warning bypass
                    if (messages.Select(s => s.Value).ToList().Contains("REPRICE - NO CORPORATE NEGOTIATED FARES EXIST") &&
                        result.EnhancedAirBookRS.OTA_AirPriceRS != null &&
                        !result.EnhancedAirBookRS.OTA_AirPriceRS.SelectMany(s => s.PriceQuote.PricedItinerary.AirItineraryPricingInfo).IsNullOrEmpty())
                    {
                        return result.EnhancedAirBookRS;
                    }

                    string code = string.Join(Environment.NewLine, messages.Select(s => s.code.LastMatch(@"^(\d+)$")).Where(w => !string.IsNullOrEmpty(w)).Distinct());
                    if (string.IsNullOrEmpty(code)) { code = Guid.NewGuid().ToString(); }

                    throw new GDSException(
                                code,
                                string.Join(Environment.NewLine, messages.Select(s => s.Value).Distinct()));
                }

                await client.CloseAsync();

                return result.EnhancedAirBookRS;

            }
            catch (TimeoutException timeProblem)
            {
                logger.LogError(timeProblem, timeProblem.Message);
                client.Abort();
                throw new GDSException("30000025", "Sabre system timeout. Please try again!");
            }
            catch (FaultException unknownFault)
            {
                logger.LogError(unknownFault, unknownFault.Message);
                client.Abort();
                throw new GDSException("30000026", $"Sabre System Exception: {unknownFault.Message + (unknownFault.InnerException == null ? "" : Environment.NewLine + unknownFault.InnerException.Message)}");
            }
            catch (CommunicationException commProblem)
            {
                logger.LogError(commProblem, commProblem.Message);
                client.Abort();
                throw new GDSException("30000027", "There is a communication issue with Sabre. Please try again later!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PricePNR failed");
                client.Abort();
                throw;
            }
        }

        internal async Task<EnhancedAirBookRS> ExecForceFarebasis(ForceFBQuoteRQ request, string sessionID, Pcc pcc, PNR pnr, string ticketingpcc)
        {
            EnhancedAirBookPortTypeClient client = null;

            try
            {
                EnableTLS();

                client = new EnhancedAirBookPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                MethodInfo method = typeof(XmlSerializer).
                                        GetMethod(
                                            "set_Mode",
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Static);

                method.Invoke(null, new object[] { 1 });

                logger.LogInformation($"{nameof(PricePNR)} {request}");
                var sw = Stopwatch.StartNew();
                client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));
                var result = await client.
                                        EnhancedAirBookRQAsync(
                                                CreateHeader(pcc.PccCode, ticketingpcc),
                                                new Security { BinarySecurityToken = sessionID },
                                                CreatePriceByForceFBRequest(request, pnr));
                sw.Stop();

                if (result == null || result.EnhancedAirBookRS.ApplicationResults.status != CompletionCodes.Complete)
                {
                    var messages = result.
                                    EnhancedAirBookRS.
                                    ApplicationResults.
                                    Error.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message);

                    throw new GDSException(
                                string.Join(Environment.NewLine, messages.Select(s => s.code.LastMatch(@"^(\d+)$").Trim()).Distinct()),
                                string.Join(Environment.NewLine, messages.Select(s => s.Value).Distinct()));
                }

                //if quoting failed you still get the status as Complete
                //However under warning you will be presented with error
                if (result.EnhancedAirBookRS.ApplicationResults.Warning != null)
                {
                    var messages = result.
                                    EnhancedAirBookRS.
                                    ApplicationResults.
                                    Warning.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message);

                    //ETG price quote warning bypass
                    if (messages.Select(s => s.Value).ToList().Contains("REPRICE - NO CORPORATE NEGOTIATED FARES EXIST") &&
                        result.EnhancedAirBookRS.OTA_AirPriceRS != null &&
                        !result.EnhancedAirBookRS.OTA_AirPriceRS.SelectMany(s => s.PriceQuote.PricedItinerary.AirItineraryPricingInfo).IsNullOrEmpty())
                    {
                        return result.EnhancedAirBookRS;
                    }

                    string code = string.Join(Environment.NewLine, messages.Select(s => s.code.LastMatch(@"^(\d+)$")).Where(w => !string.IsNullOrEmpty(w)).Distinct());
                    if (string.IsNullOrEmpty(code)) { code = Guid.NewGuid().ToString(); }

                    throw new GDSException(
                                code,
                                string.Join(Environment.NewLine, messages.Select(s => s.Value).Distinct()));
                }

                await client.CloseAsync();

                return result.EnhancedAirBookRS;

            }
            catch (TimeoutException timeProblem)
            {
                logger.LogError(timeProblem, timeProblem.Message);
                client.Abort();
                throw new GDSException("30000025", "Sabre system timeout. Please try again!");
            }
            catch (FaultException unknownFault)
            {
                logger.LogError(unknownFault, unknownFault.Message);
                client.Abort();
                throw new GDSException("30000026", $"Sabre System Exception: {unknownFault.Message + (unknownFault.InnerException == null ? "" : Environment.NewLine + unknownFault.InnerException.Message)}");
            }
            catch (CommunicationException commProblem)
            {
                logger.LogError(commProblem, commProblem.Message);
                client.Abort();
                throw new GDSException("30000027", "There is a communication issue with Sabre. Please try again later!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PricePNR failed");
                client.Abort();
                throw;
            }
        }

        private List<Quote> ParseSabreQuote(EnhancedAirBookRSOTA_AirPriceRS[] res, List<QuotePassenger> paxsdata, List<SectorData> sectordata, PNR pnr, Models.PriceType priceType, List<pnrquotedata> pqnos = null)
        {
            int index = pqnos.IsNullOrEmpty() ?
                            1 :
                            pnr.LastQuoteNumber + 1;

            List<Quote> quotes = new List<Quote>();

            if (res == null) { return quotes; }

            var returnedPTCs = res.
                                    SelectMany(r => r.
                                                    PriceQuote.
                                                    PricedItinerary.
                                                    AirItineraryPricingInfo.
                                                    Select(a => a.PassengerTypeQuantity.Code).
                                                    Distinct());

            //Quote Data
            var airpricepqs = from resp in res
                              let validatingcarrier = resp.PriceQuote.MiscInformation.ValidatingCarrier == null ?
                                                            resp.PriceQuote.PricedItinerary.AirItineraryPricingInfo.First().FareCalculationBreakdown.First().FareBasis.FilingCarrier :
                                                            resp.PriceQuote.MiscInformation.ValidatingCarrier.First().Ticket.First().CarrierCode
                              let isCat35 = resp.PriceQuote.MiscInformation.HeaderInformation.SelectMany(s => s.Text).Where(w => w == "PRIVATE ¤").Count() > 0
                              from pqs in resp.PriceQuote.PricedItinerary.AirItineraryPricingInfo
                              let lastpurchasedate = getLastPurchaseDate(resp, pqs)
                              let sectors = GetQuoteSectors(sectordata, pnr, pqs)
                              let obfee = pqs.
                                            TicketingFees?.
                                            FirstOrDefault(f => f.FeeInformation.ServiceType == "OB" &&
                                                                f.FeeInformation.FunctionCode.IsMatch(@"^F[C|D]A$") &&
                                                                decimal.Parse(f.FeeInformation.Amount) > 0M)?.
                                            FeeInformation.
                                            Amount
                              let obfeerate = pqs.
                                                AncillaryFees?.
                                                FirstOrDefault(w => !string.IsNullOrEmpty(w.FeeInformation?.ServiceFeePercentage))?.
                                                FeeInformation?.
                                                ServiceFeePercentage
                              let pqsecdata = GetSectorData(pqs.FareCalculationBreakdown)
                              select new TempQuote()
                              {
                                  Quote = new Quote()
                                  {
                                      QuoteNo = GetQuoteNumber(pqnos, paxsdata, sectors, pqs),
                                      PlatingCarrier = validatingcarrier,
                                      BaseFare = pqs.ItinTotalFare.EquivFare == null ?
                                                decimal.Parse(pqs.ItinTotalFare.BaseFare.Amount) :
                                                decimal.Parse(pqs.ItinTotalFare.EquivFare.Amount),
                                      BaseFareCurrency = pqs.ItinTotalFare.EquivFare == null ?
                                                        pqs.ItinTotalFare.BaseFare.CurrencyCode :
                                                        pqs.ItinTotalFare.EquivFare.CurrencyCode,
                                      Taxes = pqs.
                                                 ItinTotalFare.Taxes?.Tax?.Select(t => new Tax()
                                                 {
                                                     Code = t.TicketingTaxCode.Substring(0, 2),
                                                     Amount = decimal.Parse(t.Amount)
                                                 }).
                                                 ToList(),
                                      PrivateFare = pqs.ItinTotalFare.PrivateFare?.Ind == "Y" || isCat35,
                                      NonRefundable = pqs.ItinTotalFare.NonRefundableInd,
                                      FareCalculation = pqs.FareCalculation.Text,
                                      ROE = pqs.FareCalculation.Text.LastMatch(@"ROE\s*([\d\.]+)", "1"),
                                      Endorsements = pqs.ItinTotalFare.Endorsements?.ToList(),
                                      SpecificPenalties = pqs.
                                                            SpecificPenalty?.
                                                            Where(s => !string.IsNullOrEmpty(s.PenaltyInformation.Amount)).
                                                            Select(p => new SpecificPenalty()
                                                            {
                                                                FareBasis = p.PenaltyInformation.FareBasisCode == null ? "" : string.Join(",", p.PenaltyInformation.FareBasisCode),
                                                                FareComponent = p.PenaltyInformation.FareComponent == null ? "" : string.Join(",", p.PenaltyInformation.FareComponent),
                                                                PenaltyAmount = decimal.Parse(p.PenaltyInformation.Amount),
                                                                CurrencyCode = p.PenaltyInformation.Currency,
                                                                PenaltyTypeDescription = GetPenaltyTypeDescription(p.PenaltyInformation.Type),
                                                                PenaltyTypeCode = GetPenaltyTypeCode(p.PenaltyInformation.Type),
                                                                SourcedFromCAT16 = p.PenaltyInformation.Cat16Specified && p.PenaltyInformation.Cat16
                                                            }).
                                                            ToList(),
                                      EquivFare = pqs.ItinTotalFare.EquivFare == null ?
                                                    0.00M :
                                                    decimal.Parse(pqs.ItinTotalFare.EquivFare.Amount),
                                      EquivFareCurrencyCode = pqs.ItinTotalFare.EquivFare == null ?
                                                        "" :
                                                        pqs.ItinTotalFare.EquivFare.CurrencyCode,
                                      QuoteSectors = sectors,
                                      CreditCardFee = string.IsNullOrEmpty(obfee) ? 0.00M : decimal.Parse(obfee),
                                      CreditCardFeeRate = string.IsNullOrEmpty(obfeerate) ?
                                                                decimal.Parse(pqs.ItinTotalFare.TotalFare.Amount) == 0.00M || string.IsNullOrEmpty(obfee) ?
                                                                    0.00M :
                                                                    Math.Round((decimal.Parse(obfee) / decimal.Parse(pqs.ItinTotalFare.TotalFare.Amount)) * 100, 2) :
                                                                decimal.Parse(obfeerate),
                                      BspCommissionRate = string.IsNullOrEmpty(pqs.ItinTotalFare.Commission?.Percent) ?
                                                                default(decimal?) :
                                                                decimal.Parse(pqs.ItinTotalFare.Commission.Percent),
                                      SectorCount = pqsecdata.SectorCount,
                                      Route = pqsecdata.Route,
                                      LastPurchaseDate = lastpurchasedate,
                                  },
                                  PaxType = pqs.PassengerTypeQuantity.Code,
                                  Qty = int.Parse(pqs.PassengerTypeQuantity.Quantity),
                              };

            List<TempQuote> tempQuotes = airpricepqs.ToList();

            //Quotes with single passenger
            var singlepaxquotes = from s in tempQuotes.Where(w => w.Qty == 1)
                                  let pax = MatchPaxTypes(s.PaxType, pnr, paxsdata).First()
                                  select new Quote()
                                  {
                                      PlatingCarrier = s.Quote.PlatingCarrier,
                                      BaseFare = s.Quote.BaseFare,
                                      BaseFareCurrency = s.Quote.BaseFareCurrency,
                                      Endorsements = s.Quote.Endorsements,
                                      EquivFare = s.Quote.EquivFare,
                                      FareCalculation = s.Quote.FareCalculation,
                                      ROE = s.Quote.ROE,
                                      NonRefundable = s.Quote.NonRefundable,
                                      PrivateFare = s.Quote.PrivateFare,
                                      QuotePassenger = new QuotePassenger()
                                      {
                                          PaxType = s.PaxType,
                                          PassengerName = pax.PassengerName,
                                          NameNumber = pax.NameNumber,
                                          DOB = pax.DOB,
                                          DOBChanged = paxsdata.
                                                        FirstOrDefault(f =>
                                                                f.NameNumber.Replace("0", "") ==
                                                                pax.NameNumber.Replace("0", ""))?.DOBChanged ?? false,
                                          FormOfPayment = paxsdata.
                                                            FirstOrDefault(f =>
                                                                    f.NameNumber.Replace("0", "") ==
                                                                    pax.NameNumber.Replace("0", ""))?.
                                                            FormOfPayment
                                      },
                                      QuoteSectors = s.Quote.QuoteSectors,
                                      Taxes = s.Quote.Taxes,
                                      QuoteNo = s.Quote.QuoteNo,
                                      CreditCardFee = s.Quote.CreditCardFee,
                                      CreditCardFeeRate = s.Quote.CreditCardFeeRate,
                                      BspCommissionRate = s.Quote.BspCommissionRate,
                                      SectorCount = s.Quote.SectorCount,
                                      Route = s.Quote.Route,
                                      SpecificPenalties = s.Quote.SpecificPenalties,
                                      LastPurchaseDate = s.Quote.LastPurchaseDate,
                                      DifferentPaxType = returnedPTCs.Any(a => (a.StartsWith("C") && a.Substring(0, 1) == pax.PaxType.Substring(0, 1)) || a == pax.PaxType) ? 
                                                                new List<string>() : 
                                                                returnedPTCs.Distinct().Except(paxsdata.Select(S=> s.PaxType).Distinct()).ToList()
                                  };

            quotes.AddRange(singlepaxquotes.ToList());

            //Quotes with multiple passengers - split
            foreach (var item in tempQuotes.Where(w => w.Qty > 1))
            {
                List<QuotePassenger> paxs = MatchPaxTypes(item.PaxType, pnr, paxsdata);

                for (int i = 0; i < item.Qty; i++)
                {
                    TempQuote multipaxquotes = new TempQuote();
                    multipaxquotes.Quote = new Quote()
                    {
                        Warnings = item.Quote.Warnings,
                        BaseFare = item.Quote.BaseFare,
                        BaseFareCurrency = item.Quote.BaseFareCurrency,
                        Endorsements = item.Quote.Endorsements,
                        EquivFare = item.Quote.EquivFare,
                        FareCalculation = item.Quote.FareCalculation,
                        ROE = item.Quote.ROE,
                        QuoteSectors = item.Quote.QuoteSectors,
                        Fee = item.Quote.Fee,
                        LastPurchaseDate = item.Quote.LastPurchaseDate,
                        NonRefundable = item.Quote.NonRefundable,
                        PrivateFare = item.Quote.PrivateFare,
                        SpecificPenalties = item.Quote.SpecificPenalties,
                        PlatingCarrier = item.Quote.PlatingCarrier,
                        Taxes = item.Quote.Taxes,
                        QuotePassenger = new QuotePassenger()
                        {
                            PaxType = item.PaxType,
                            NameNumber = paxs[i].NameNumber,
                            PassengerName = paxs[i].PassengerName,
                            DOB = paxs[i].DOB,
                            DOBChanged = paxs[i].DOBChanged,
                            FormOfPayment = paxsdata.
                                                First(f =>
                                                        f.NameNumber.Replace("0", "") ==
                                                        paxs[i].NameNumber.Replace("0", "")).
                                                FormOfPayment
                        },
                        QuoteNo = item.Quote.QuoteNo,
                        BspCommissionRate = item.Quote.BspCommissionRate,
                        CreditCardFee = item.Quote.CreditCardFee,
                        CreditCardFeeRate = item.Quote.CreditCardFeeRate,
                        SectorCount = item.Quote.SectorCount,
                        Route = item.Quote.Route,
                        DifferentPaxType = returnedPTCs.Any(a => (a.StartsWith("C") && a.Substring(0,1) == item.PaxType.Substring(0,1)) || a == item.PaxType) ?
                                                                new List<string>() :
                                                                returnedPTCs.Distinct().Except(paxsdata.Select(s => s.PaxType).Distinct()).ToList()
                    };

                    quotes.Add(multipaxquotes.Quote);
                }
            }

            //set PriceType
            quotes.
                ForEach(f =>
                {
                    f.PriceType = priceType;
                    f.Warnings = new List<WebtopWarning>();
                    f.Errors = new List<WebtopError>();
                });

            //Order quotes
            quotes = quotes.
                        OrderBy(o => o.QuotePassenger.PaxType.Substring(0, 1)).
                        ToList();

            return quotes;
        }

        private string getLastPurchaseDate(EnhancedAirBookRSOTA_AirPriceRS resp, EnhancedAirBookRSOTA_AirPriceRSPriceQuotePricedItineraryAirItineraryPricingInfo pqs)
        {
            if (string.IsNullOrEmpty(resp.PriceQuote.MiscInformation.HeaderInformation.First(f => f.SolutionSequenceNmbr == pqs.SolutionSequenceNmbr).LastTicketingDate))
            {
                return "";
            }

            string lastpurchdatetimestr = resp.PriceQuote.MiscInformation.HeaderInformation.First(f => f.SolutionSequenceNmbr == pqs.SolutionSequenceNmbr).LastTicketingDate;
            lastpurchdatetimestr = DateTime.Now.Year + "-" + lastpurchdatetimestr.SplitOn("T").First() + "T" + lastpurchdatetimestr.SplitOn("T").Last();

            DateTime lastpurchdate = DateTime.
                                            ParseExact(
                                                lastpurchdatetimestr,
                                                "yyyy-MM-ddTHH:mm",
                                                CultureInfo.InvariantCulture);

            if (lastpurchdate < DateTime.Now)
            {
                lastpurchdate.AddYears(1);
            }

            return lastpurchdate.GetISODateTime();
        }

        private PQSecData GetSectorData(EnhancedAirBookRSOTA_AirPriceRSPriceQuotePricedItineraryAirItineraryPricingInfoFareCalculationBreakdown[] fareCalculationBreakdown)
        {
            int seccount = 0;
            string route = "";
            for (int i = 0; i < fareCalculationBreakdown.Count(); i++)
            {
                if (i == 0)
                {
                    route += fareCalculationBreakdown[i].Departure.AirportCode;
                    route += "-" + fareCalculationBreakdown[i].Departure.ArrivalAirportCode;
                    seccount++;
                    continue;
                }

                if (i < fareCalculationBreakdown.Count()
                    && fareCalculationBreakdown[i - 1].Departure.ArrivalAirportCode != fareCalculationBreakdown[i].Departure.AirportCode)
                {
                    //Arunk
                    route += "//";
                    route += fareCalculationBreakdown[i].Departure.AirportCode;
                    route += "-" + fareCalculationBreakdown[i].Departure.ArrivalAirportCode;
                    seccount += 2;
                    continue;
                }

                //Surface
                route += (string.IsNullOrEmpty(fareCalculationBreakdown[i].FareBasis.SurfaceSegment) ?
                            "-" :
                            fareCalculationBreakdown[i].FareBasis.SurfaceSegment) +
                            fareCalculationBreakdown[i].Departure.ArrivalAirportCode;
                seccount++;
            }

            return new PQSecData()
            {
                SectorCount = seccount,
                Route = route
            };
        }

        private int GetQuoteNumber(List<pnrquotedata> pqnos, List<QuotePassenger> paxs, List<QuoteSector> sectors, EnhancedAirBookRSOTA_AirPriceRSPriceQuotePricedItineraryAirItineraryPricingInfo pqs)
        {
            var quoteno = pqnos?.
                        LastOrDefault(f =>
                            //Match the pax type
                            f.PaxType.Substring(0, 1) == pqs.PassengerTypeQuantity.Code.Substring(0, 1) &&
                            //Match passenger name numbers
                            f.
                                PassengerNameNumbers.
                                All(a => paxs.
                                            Where(w => w.PaxType.Substring(0, 1) == pqs.PassengerTypeQuantity.Code.Substring(0, 1) &&
                                            w.NameNumber == a).
                                Any()) &&
                            //Match sectors
                            f.
                                Sectors.
                                All(fsecno =>
                                    //Sector number check
                                    sectors.Where(s => s.PQSectorNo == fsecno).Any()) &&
                            //Check for total
                            f.Total == decimal.Parse(pqs.ItinTotalFare.TotalFare.Amount)
                            )?.
                        QuoteNo ??
                        -1;

            return quoteno;
        }

        private PenaltyType GetPenaltyTypeCode(string type)
        {
            switch (type)
            {
                case "CPBD":
                    return PenaltyType.CPBD;
                case "CPAD":
                    return PenaltyType.CPAD;
                case "RPBD":
                    return PenaltyType.RPBD;
                case "RPAD":
                    return PenaltyType.RPAD;
                default:
                    return PenaltyType.NONE;
            }
        }

        private string GetPenaltyTypeDescription(string type)
        {
            //"CPBD" - Change Penalty Before Departure; 
            //"CPAD" - Change Penalty After Departure; 
            //"RPBD" - Refund Penalty Before Departure;
            //"RPAD" - Refund Penalty After Departure.
            switch (type)
            {
                case "CPBD":
                    return "Change Penalty Before Departure";
                case "CPAD":
                    return "Change Penalty After Departure";
                case "RPBD":
                    return "Refund Penalty Before Departure";
                case "RPAD":
                    return "Refund Penalty After Departure";
                default:
                    return "";
            }
        }

        private static List<QuoteSector> GetQuoteSectors(List<SectorData> sectors, PNR pnr, EnhancedAirBookRSOTA_AirPriceRSPriceQuotePricedItineraryAirItineraryPricingInfo pqs)
        {
            List<QuoteSector> quoteSectors = new List<QuoteSector>();

            var validpqs = pqs.FareCalculationBreakdown.ToList();//.Where(w => string.IsNullOrEmpty(w.FareBasis.SurfaceSegment))

            int index = 0;
            foreach (var s in sectors)
            {
                bool isARUNK = pnr.Sectors.First(f => f.SectorNo == s.SectorNo).From == "ARUNK";
                QuoteSector sec = new QuoteSector()
                {
                    PQSectorNo = s.SectorNo,
                    DepartureCityCode = isARUNK ? "ARUNK" : validpqs[index].Departure.AirportCode,
                    ArrivalCityCode = isARUNK ? "" : validpqs[index].Departure.ArrivalAirportCode,
                    DepartureDate = isARUNK ? "" : pnr.Sectors.First(f => f.SectorNo == s.SectorNo).DepartureDate,
                    FareBasis = isARUNK ? "" : validpqs[index].FareBasis.Code,
                    Baggageallowance = isARUNK ? "" : SabreSharedServices.GetBaggageDiscription(validpqs[index].FreeBaggageAllowance)
                };

                quoteSectors.Add(sec);

                if (!isARUNK || validpqs[index].FareBasis.SurfaceSegment == "/-") { index++; }
            }

            return quoteSectors;
        }

        private List<QuotePassenger> MatchPaxTypes(string quotepaxType, PNR pnr, List<QuotePassenger> quotepaxs)
        {
            DateTime FirstDepartureDate = GetFirstDepartureDate(pnr);

            List<QuotePassenger> result = new List<QuotePassenger>();

            if (quotepaxType.StartsWith("C"))
            {
                if (quotepaxType.IsMatch(@"C\d+"))
                {
                    List<QuotePassenger> pax = quotepaxs.
                                                    Where(w => w.DOB.HasValue && FirstDepartureDate != DateTime.MinValue && quotepaxType == "C" + GetAge(FirstDepartureDate, w.DOB.Value).ToString().PadLeft(2, '0')).
                                                    ToList();

                    if (pax == null)
                    {
                        pax = quotepaxs.
                                    Where(w => (w.PaxType.IsMatch(@"C\d+") && w.PaxType == quotepaxType) ||
                                              w.PaxType == "CHD").
                                    ToList();
                    }
                    result.
                        AddRange(pax);
                }
                else
                {
                    result.AddRange(quotepaxs.
                                        Where(w => (w.PaxType.StartsWith("C") && w.PaxType.Substring(0, 1) == quotepaxType.Substring(0, 1)) ||
                                                    w.PaxType == quotepaxType).
                                        ToList());
                }

                return result;
            }
            result = quotepaxs.
                        Where(w => w.PaxType == quotepaxType).
                        ToList();

            if (result.IsNullOrEmpty() && quotepaxs.All(a => a.PaxType != quotepaxType))
            {
                result = quotepaxs.
                            Where(w => !(w.PaxType.StartsWith("C") || w.PaxType.StartsWith("I"))).
                            ToList();
            }

            if (result.IsNullOrEmpty())
            {
                throw new AeronologyException("50000036", "Matching passenger type not found");
            }

            return result;
        }
    }

    internal class SectorData
    {
        public int SectorNo { get; set; }
    }

    public class PQSecData
    {
        public int SectorCount { get; set; }
        public string Route { get; set; }
    }

    internal class TempQuote
    {
        public Quote Quote { get; set; }
        public string PaxType { get; set; }
        public int Qty { get; set; }
    }

    public enum EnhancedAirBookType
    {
        AirBook,
        Price
    }

    public class pnrquotedata
    {
        public int QuoteNo { get; set; }
        public bool Expired { get; set; }
        public string PaxType { get; set; }
        public List<int> Sectors { get; set; }
        public List<string> PassengerNameNumbers { get; set; }
        public decimal Total { get; set; }
        public string LastDateToPurchase { get; set; }
    }
}
