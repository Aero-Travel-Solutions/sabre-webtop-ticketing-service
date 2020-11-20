using Amazon.Runtime.Internal.Util;
using GetReservation;
using Newtonsoft.Json;
using Polly;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using SabreWebtopTicketingService.PollyPolicies;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrequentFlyer = SabreWebtopTicketingService.Models.FrequentFlyer;
using ILogger = SabreWebtopTicketingService.Common.ILogger;

namespace SabreWebtopTicketingService.Services
{
    public class SabreGDS
    {
        private readonly string url;
        private readonly SessionCreateService _sessionCreateService;
        private readonly IgnoreTransactionService _ignoreTransactionService;
        private readonly ChangeContextService _changeContextService;
        private readonly DisplayTicketService _displayTicket;
        private readonly ConsolidatorPccDataSource _consolidatorPccDataSource;
        private readonly TicketingPccDataSource _ticketingPccDataSource;
        private readonly SabreCrypticCommandService _sabreCommandService;
        private readonly TripSearchService _tripSearchService;
        private readonly SessionCloseService _sessionCloseService;
        private readonly GetReservationService _getReservationService;
        private readonly IGetTurnaroundPointDataSource _getTurnaroundPointDataSource;
        private readonly ICommissionDataService _commissionDataService;
        private readonly ILogger logger;
        private readonly DbCache _dbCache;
        private readonly IAsyncPolicy retryPolicy;

        public Pcc pcc { get; set; }
        public Agent agent { get; set; }

        public SabreGDS(
            SessionCreateService sessionCreateService,
            ILogger logger,
            DbCache dbCache,
            ConsolidatorPccDataSource consolidatorPccDataSource,
            TicketingPccDataSource ticketingPccDataSource,
            IgnoreTransactionService ignoreTransactionService,
            ChangeContextService changeContextService,
            DisplayTicketService displayTicket,
            SabreCrypticCommandService sabreCommandService,
            TripSearchService tripSearchService,
            SessionCloseService sessionCloseService,
            GetReservationService getReservationService,
            IGetTurnaroundPointDataSource getTurnaroundPointDataSource,
            ICommissionDataService commissionDataService,
            ExpiredTokenRetryPolicy expiredTokenRetryPolicy)
        {
            url = Constants.GetSoapUrl();
            _sessionCreateService = sessionCreateService;
            this.logger = logger;
            _dbCache = dbCache;
            _consolidatorPccDataSource = consolidatorPccDataSource;
            _ticketingPccDataSource = ticketingPccDataSource;
            _ignoreTransactionService = ignoreTransactionService;
            _changeContextService = changeContextService;
            _displayTicket = displayTicket;
            _sabreCommandService = sabreCommandService;
            _tripSearchService = tripSearchService;
            _sessionCloseService = sessionCloseService;
            _getReservationService = getReservationService;
            _getTurnaroundPointDataSource = getTurnaroundPointDataSource;
            _commissionDataService = commissionDataService;
            retryPolicy = expiredTokenRetryPolicy.ExpiredTokenPolicy;
        }

        public async Task<SearchPNRResponse> SearchPNR(SearchPNRRequest request)
        {
            string token = "";
            PNR pnr = new PNR();
            List<SabreSearchPNRResponse> res = new List<SabreSearchPNRResponse>();
            List<string> agentpccs = new List<string>();
            string ticketingpcc = GetTicketingPCC(agent == null ? "" : agent.TicketingPcc, pcc.PccCode);
            SabreSession sabreSession = null;

            try
            {
                var sw = Stopwatch.StartNew();

                //Get agent pccs
                agentpccs = agent?.PccList?.Select(s => s.PccCode).ToList() ?? new List<string> { "9SNJ" };//agent.PccList.Select(s=> s.PccCode).ToList();

                //Obtain session
                sabreSession = await _sessionCreateService.
                                                    CreateStatefulSessionToken(
                                                        pcc);
                //ignore session
                await _sabreCommandService.ExecuteCommand(sabreSession.SessionID, pcc, "I");

                //Context Change
                await _changeContextService.ContextChange(sabreSession, pcc, ticketingpcc);

                token = sabreSession.SessionID;

                if (request.SearchText.Length == 6 && request.SearchText.IsMatch(@"\w{6}"))
                {
                    //Retrieve PNR if only one match found
                    try
                    {
                        pnr = await retryPolicy.ExecuteAsync(() => GetPNR(token, request.SearchText, true, true, true, false, ticketingpcc));

                        logger.LogInformation($"Response parsing and validation @SearchPNR elapsed {sw.ElapsedMilliseconds} ms.");

                        return new SearchPNRResponse() { PNR = pnr };
                    }
                    catch (GDSException)
                    {
                        //if(ex.Message.StartsWith("PNR Restricted, caused by [PNR Restricted, code: 500324, severity: MODERATE]"))
                        //{
                        //    throw new GDSException("3000030", "PNR Restricted");
                        //}
                        //If search base on reloc failed we need to try and search it as name
                    }
                }

                if (agentpccs.IsNullOrEmpty()) { throw new AeronologyException("50000033", "Agent PCCs not found."); }

                //Search for PNRs
                res = await retryPolicy.ExecuteAsync(() => _tripSearchService.SearchPNR(request, token, pcc, agentpccs, ticketingpcc));

                if (res == null)
                {
                    throw new AeronologyException("50000017", "No record found for the given search request. Please ensure the correct record locator is entered and adequate branch access is granted before retrying.");
                }


                if (res.Count == 1)
                {
                    //Retrieve PNR if one PNR found
                    pnr = await retryPolicy.ExecuteAsync(() => GetPNR(token, res.First().Locator, true, true, true, false, ticketingpcc));

                    logger.LogInformation($"Response parsing and validation @ SearchPNR elapsed {sw.ElapsedMilliseconds} ms");

                    return new SearchPNRResponse() { PNR = pnr };
                }

                logger.LogInformation($"Response parsing @ SearchPNR elapsed {sw.ElapsedMilliseconds} ms");


                return new SearchPNRResponse()
                {
                    PNROptions = res.
                                    OrderBy(o => o.FirstDepartureDate).
                                    Select(s => new SearchPNRResponseOption()
                                    {
                                        Locator = s.Locator,
                                        PassengerNames = s.PassengerNames,
                                        DepartureDate = s.FirstDepartureDateString,
                                        ArrivalDate = s.LastArrivalDate
                                    }).
                                    ToList()
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw;
            }
            finally
            {
                if (sabreSession != null && sabreSession.IsLimitReached)
                {
                    await _sessionCloseService.SabreSignout(sabreSession.SessionID, pcc);
                }
            }

        }

        private string GetTicketingPCC(string ticketingpcc, string pccCode)
        {
            if (!string.IsNullOrEmpty(ticketingpcc))
            {
                return ticketingpcc;
            }

            ticketingpcc = pccCode;
            if ("0M4J|G4AK".Contains(ticketingpcc))
            {
                return "9SNJ";
            }
            else if (ticketingpcc == "R6G8")
            {
                return "2DAD";
            }

            return ticketingpcc;
        }

        public async Task<PNR> GetPNR(string sessionid, string locator, bool withpnrvalidation = false, bool getStoredCards = false, bool includeQuotes = false, bool includeexpiredquote = false, string ticketingpcc = "")
        {
            var pnrAccessKey = $"{ticketingpcc}-{locator}-pnr".EncodeBase64();
            var cardAccessKey = $"{ticketingpcc}-{locator}-card".EncodeBase64();

            //get reservation
            var response = await _getReservationService.RetrievePNR(locator, sessionid, pcc, ticketingpcc);

            //Post retrieval actions
            var result = ParseSabrePNR(response, sessionid, includeQuotes, includeexpiredquote);

            if (withpnrvalidation)
            {
                //PNR validation
                result.InvokePostPNRRetrivalActions();
            }


            //Save PNR in cache
            await cache.Set(pnrAccessKey, result, 15);

            if (getStoredCards)
            {
                var storedCreditCard = GetStoredCards(response);

                //Encrypt card number
                storedCreditCard.ForEach(c => c.CreditCard = dataProtector.Protect(c.CreditCard));

                await cache.Set(cardAccessKey, storedCreditCard, 15);
            }

            return result;
        }

        private PNR ParseSabrePNR(GetReservationRS result, string token, bool includeQuotes = false, bool includeexpiredquote = false)
        {
            DateTime? pcclocaldatetime = null;
            SabrePNR sabrepnr = new SabrePNR(result);
            if (includeQuotes)
            {
                string localdate = _sabreCommandService.
                                                    ExecuteCommand(token, pcc, $"T*{sabrepnr.PCCCityCode}").
                                                    GetAwaiter().
                                                    GetResult().
                                                SplitOn("*").
                                                Last().
                                                Trim();
                pcclocaldatetime = DateTime.
                                        ParseExact(
                                            localdate,
                                            "HHmm ddMMM",
                                            System.Globalization.CultureInfo.InvariantCulture);
            }
            PNR pnr = GeneratePNR(token, sabrepnr, pcclocaldatetime, includeQuotes, includeexpiredquote);

            pnr.LastQuoteNumber = pnr.Quotes.IsNullOrEmpty() ? 0 : pnr.Quotes.OrderBy(l => l.QuoteNo).Last().QuoteNo;
            return pnr;
        }

        private List<StoredCreditCard> GetStoredCards(GetReservationRS result)
        {
            List<StoredCreditCard> tempstoredCreditCards = new List<StoredCreditCard>();

            //Get credit cards from PNR
            SabrePNR sabrepnr = new SabrePNR(result);
            tempstoredCreditCards.AddRange(sabrepnr.StoredCreditCard);

            //Get credit cards from price quotes
            if (sabrepnr.PriceQuote != null && !sabrepnr.PriceQuote.PriceQuotes.IsNullOrEmpty())
            {
                tempstoredCreditCards.AddRange(sabrepnr.PriceQuote.PriceQuotes.SelectMany(s => CreditCardOperations.GetStoredCards(s.PricingCommand)).ToList());
            }

            //Remove duplicates
            return tempstoredCreditCards.
                        GroupBy(grp => grp.CreditCard).
                        Select(s => s.First()).
                        ToList();
        }

        private PNR GeneratePNR(string token, SabrePNR sabrepnr, DateTime? pcclocaldatetime, bool includeQuotes, bool includeexpiredquotes)
        {
            List<PNRSector> secs = GetSectors(sabrepnr.AirSectors, sabrepnr.ArunkSectors);
            List<PNRPassengers> paxs = GetPassengers(sabrepnr.Passengers);

            //Parse pnr infor to DTO 
            var pnr = new PNR()
            {
                GDSCode = "1W",
                Locator = sabrepnr.Locator,
                BookedPCC = sabrepnr.BookedPCC,
                BookedPCCCityCode = sabrepnr.PCCCityCode,
                CreatedDate = sabrepnr.CreatedDate,
                BookingTTL = sabrepnr.AirlineTTLs.FirstOrDefault(f => f.MostRestrictive)?.DisplayText,
                DKNumber = sabrepnr.DKNumber,
                HostUserId = sabrepnr.HostUserId,
                ExpiredQuotesExist = sabrepnr.PriceQuote != null && sabrepnr.PriceQuote.InvalidQuotesExist,
                Passengers = paxs,
                SSRs = GetSSRs(sabrepnr.SSRs, secs, paxs),
                Ancillaries = GetAncillaries(sabrepnr),
                Sectors = secs,
                StoredCards = sabrepnr.
                                StoredCreditCard.
                                Select(s => new PNRStoredCards()
                                {
                                    MaskedCardNumber = s.MaskedCardNumber,
                                    Expiry = s.Expiry,
                                }).
                                ToList(),
                Tickets = sabrepnr.
                            Tickets.
                            Select(s => new PNRTicket()
                            {
                                DocumentNumber = s.DocumentNumber,
                                DocumentType = s.DocumentType,
                                PassengerName = s.PassengerName,
                                TicketingPCC = s.TicketingPCC,
                                Voided = s.Voided,
                                RPH = s.RPH,
                                IssueDate = s.IssueDate,
                                IssueTime = s.IssueTime
                            }).
                            ToList()
            };

            //Generate Issue Ticket EMD Key 
            pnr.
                Ancillaries.
                Where(w => string.IsNullOrEmpty(w.InvalidErrorCode)).
                ToList().
                ForEach(emd => emd.IssueTicketEMDKey = GetIssueTicketEMDKey(emd));

            //Add unpaid seats to ancillary collection
            //pnr.Ancillaries.AddRange(AddUnpaidSeats(sabrepnr, secs, pnr));

            if (includeQuotes)
            {
                //Quote
                pnr.Quotes = GetQuotes(sabrepnr.PriceQuote, pnr, includeexpiredquotes, pcclocaldatetime.Value, token);
            }

            //adding quote form of payments to stored cards
            if (!(pnr.Quotes == null || pnr.Quotes.Where(w => w.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CC).IsNullOrEmpty()))
            {
                pnr.
                    StoredCards.
                    AddRange(
                        pnr.
                        Quotes.
                        Where(w => w.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CC).
                        Select(fop => new PNRStoredCards()
                        {
                            MaskedCardNumber = fop.QuotePassenger.FormOfPayment.CardNumber,
                            Expiry = fop.QuotePassenger.FormOfPayment.ExpiryDate
                        }).
                        ToList());

                //remove duplicates
                pnr.StoredCards = pnr.StoredCards.GroupBy(g => g.MaskedCardNumber).Select(s => s.First()).ToList();
            }

            var validAncillaries = pnr.Ancillaries.Where(w => string.IsNullOrEmpty(w.InvalidErrorCode)).ToList();
            if (!validAncillaries.IsNullOrEmpty())
            {
                //validAncillaries.ForEach(f => f.Ticketed = CheckForTickets(f, t))
            }

            return pnr;
        }

        private string GetIssueTicketEMDKey(Ancillary emd)
        {
            return JsonConvert.
                        SerializeObject
                        (
                            new IssueExpressTicketEMD()
                            {
                                EMDNo = emd.EMDNumber,
                                Commission = emd.Commission.HasValue ? emd.Commission.Value : 0.00M,
                                Fee = emd.Fee.HasValue ? emd.Fee.Value : 0.00M,
                                FeeGST = emd.FeeGST,
                                Total = emd.TotalPrice,
                                TotalTax = emd.TotalTax,
                                PassengerName = emd.PassengerName,
                                PlatingCarrier = emd.Carrier,
                                Route = emd.Route,
                                Ticketed = emd.TicketAssociated,
                                RFISC = emd.RFISC,
                                SectorCount = emd.Sectors.Count()
                            }
                        ).
                        EncodeBase64();
        }
        private List<PNRPassengers> GetPassengers(List<SabrePassenger> paxs)
        {
            List<PNRPassengers> passengers = paxs.
                                                Select(pax => new PNRPassengers()
                                                {
                                                    NameNumber = pax.NameNumber.Replace("0", ""),
                                                    Title = pax.Title,
                                                    Passengername = pax.PassengerName,
                                                    DOB = pax.DateOfBirth.HasValue ? pax.DateOfBirth.Value.GetISODateString() : "",
                                                    Gender = pax.Gender,
                                                    PaxType = pax.PaxType,
                                                    SecureFlightDataExist = pax.SecureFlightData != null &&
                                                                            pax.SecureFlightData.DateOfBirth.HasValue &&
                                                                            !string.IsNullOrEmpty(pax.SecureFlightData.Gender) &&
                                                                            !string.IsNullOrEmpty(pax.SecureFlightData.Forename +
                                                                                                  pax.SecureFlightData.MiddleName +
                                                                                                  pax.SecureFlightData.Surname),
                                                    FrequentFlyerDetails = pax.
                                                                            FrequentFlyer?.
                                                                            Select(s => new FrequentFlyer()
                                                                            {
                                                                                CarrierCode = s.CarrierCode,
                                                                                FrequentFlyerNo = s.FrequentFlyerNo,
                                                                                PartnerCarrierCodes = s.PartnerCarrierCodes
                                                                            }).
                                                                            ToList(),
                                                    AccompaniedByInfant = pax.AccompanieByInfant
                                                }).
                                                ToList();

            try
            {
                //populate INF DOB
                var paxwithinfdata = paxs.FirstOrDefault(w => w.PaxType == "ADT" && w.AssociatedINF != null);
                if (paxwithinfdata != null)
                {
                    var infdob = passengers.FirstOrDefault(f => f.Passengername == paxwithinfdata.AssociatedINF.INFName);
                    if (infdob != null)
                    {
                        infdob.DOB = DateTime.Parse(paxwithinfdata.AssociatedINF.INFDOB).GetISODateString();
                    }
                }
            }
            catch { }//Do not error as INF dob is not critical to issue tickets

            return passengers;
        }

        private List<PNRSector> GetSectors(List<SabreAirSector> airsecs, List<SabreArunkSector> arunkSectors)
        {
            List<PNRSector> secs = new List<PNRSector>();
            secs = airsecs.
                        Select(s => new PNRSector()
                        {
                            SectorNo = s.SequenceNo,
                            From = s.DepartureAirport,
                            To = s.ArrivalAirport,
                            Carrier = s.MarketingAirlineCode,
                            Flight = s.FlightNumber.PadLeft(4, '0'),
                            Class = s.ClassOfService,
                            DepartureDate = s.DepartureDateTime.GetISODateString(),
                            DepartureTime = s.DepartureDateTime.GetISOTimeString(),
                            Status = s.Status,
                            Equipment = s.EquipmentType,
                            Mileage = s.Mileage,
                            Cabin = s.Cabin.CabinCode,
                            ArrivalDate = s.ArrivalDateTime.GetISODateString(),
                            ArrivalTime = s.ArrivalDateTime.GetISOTimeString(),
                            OperatingCarrier = s.OperatingAirlineCode,
                            OperatingCarrierFlightNo = s.OperatingFlightNumber,
                            AirlineRecordLocator = s.AirlineRecordLocator,
                            CodeShare = s.CodeShare,
                            MarriageGroup = s.MarriageGrp,
                            FlightDuration = s.ElapsedTime,
                            Ticketed = s.Ticketed
                        }).
                        ToList();

            secs.AddRange(arunkSectors.
                            Select(s => new PNRSector()
                            {
                                SectorNo = s.SequenceNo,
                                From = "ARUNK"
                            }).
                            ToList()
            );

            return secs.OrderBy(o => o.SectorNo).ToList();
        }

        private List<Quote> GetQuotes(PriceQuoteXElement sabrequotes, PNR pnr, bool includeexpiredquotes, DateTime pcclocaltime, string sessionID)
        {
            if (sabrequotes == null) { return new List<Quote>(); }

            List<Quote> quotes = new List<Quote>();

            //Removed Expired File Fares
            var items = sabrequotes.
                            PQSummaryLines.
                            Where(w => w.QuoteType != "PQR" && (includeexpiredquotes || !w.IsExpired)).
                            Select(pqsummary => new
                            {
                                PQSummary = pqsummary,
                                PQ = sabrequotes.
                                        PriceQuotes.
                                        First(f =>
                                            f.PQNo == pqsummary.PQNo &&
                                            (includeexpiredquotes || !f.Expired)
                                        )
                            });

            //Parsing
            quotes = items.
                        Select((q, index) => new Quote()
                        {
                            QuoteNo = q.PQ.PQNo,
                            FiledFare = true,
                            LastPurchaseDate = q.PQ.LastDateToPurchase,
                            BaseFare = q.PQ.BaseFare,
                            CurrencyCode = q.PQ.CurrencyCode,
                            Endorsements = q.PQ.Endosements,
                            EquivFare = q.PQ.BaseFare,
                            FareCalculation = q.PQ.FareCalculation,
                            QuotePassenger = new QuotePassenger()
                            {
                                PassengerName = q.PQSummary.Passenger.LastName + "/" + q.PQSummary.Passenger.FisrtName,
                                PaxType = SabreSharedServices.GetPaxType(q.PQ.PaxType),
                                NameNumber = q.PQSummary.Passenger.NameNumber,
                                FormOfPayment = GetFOP(q.PQ.PricingCommand)
                            },
                            QuoteSectors = q.
                                            PQ.
                                            PQSectors.
                                            Where(w => w.SegmentType != "A").
                                            Select(s => new { PQSec = s, SecInfo = SabreSharedServices.GetSectorNo(s, pnr.Sectors) }).
                                            Select(s => new QuoteSector()
                                            {
                                                PQSectorNo = s.SecInfo.SectorNo,
                                                DepartureDate = s.SecInfo.DepartureDate,
                                                Void = s.SecInfo.Void,
                                                ArrivalCityCode = s.PQSec.ArrivalCityCode,
                                                Baggageallowance = SabreSharedServices.GetBaggageDiscription(s.PQSec.Baggageallowance),
                                                DepartureCityCode = s.PQSec.DepartureCityCode,
                                                FareBasis = s.PQSec.FareBasis,
                                                NVA = s.PQSec.NVA,
                                                NVB = s.PQSec.NVB
                                            }).
                                            ToList(),
                            Taxes = q.
                                    PQ.
                                    PQTaxes.
                                    Select(t => new Tax()
                                    {
                                        Code = t.Code,
                                        Amount = t.Amount,
                                        Paid = false
                                    }).
                                    ToList(),
                            ValidatingCarrier = q.PQ.ValidatingCarrier,
                            CreditCardFee = q.PQ.CreditCardFee,
                            Expired = q.PQSummary.IsExpired &&
                                      q.PQ.Expired &&
                                      DateTime.Parse(q.PQ.CreateDateTime).Day == pcclocaltime.Day &&
                                      DateTime.Parse(q.PQ.CreateDateTime).Month == pcclocaltime.Month,
                            Route = q.PQ.PqSectorData.Route,
                            SectorCount = q.PQ.PqSectorData.SectorCount,
                            BspCommissionRate = q.PQ.Commission,
                            TourCode = q.PQ.TourCode,
                            PricingCommand = q.PQ.PricingCommand.Mask(),
                            PriceCode = q.PQ.PriceCode
                        }).
                        ToList();

            //workout fuel surcharge taxcode
            GetFuelSurcharge(quotes);

            //check for tourcode before commission calculation
            bool notourcode = quotes.All(a => string.IsNullOrEmpty(a.TourCode));

            try
            {
                //Calculate Commission
                CalculateCommission(quotes.
                                        Where(w => !w.Expired && w.QuoteSectors.All(a => a.PQSectorNo > 0)).
                                        ToList(),
                                    pnr,
                                    pcc.PccCode,
                                    sessionID);
            }
            catch (AeronologyException ex)
            {
                quotes.ForEach(f =>
                {
                    if (f.Errors.IsNullOrEmpty())
                    {
                        f.Errors = new List<string>();
                    }

                    f.Errors.Add(ex.Message);
                });
            }

            var ApplySupressITFlag = notourcode && quotes.Any(a => !string.IsNullOrEmpty(a.TourCode));

            //Agent Price
            quotes.
                ForEach(f =>
                {
                    f.CreditCardFeeRate = f.CreditCardFee == 0.00M || f.TotalFare == 0.00M ?
                                            0.00M :
                                            Math.Round((f.CreditCardFee / f.TotalFare) * 100, 2);
                    f.AgentPrice = f.QuotePassenger.FormOfPayment == null ?
                                                    f.TotalFare + f.Fee + (f.FeeGST.HasValue ? f.FeeGST.Value : 0.00M) - f.Commission :
                                                    //Cash Only
                                                    f.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CA ?
                                                        f.TotalFare + f.Fee + (f.FeeGST.HasValue ? f.FeeGST.Value : 0.00M) - f.Commission :
                                                        //Part Cash part credit
                                                        f.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CC && f.QuotePassenger.FormOfPayment.CreditAmount < f.TotalFare ?
                                                            f.TotalFare - f.QuotePassenger.FormOfPayment.CreditAmount + f.Fee + (f.FeeGST.HasValue ? f.FeeGST.Value : 0.00M) - f.Commission :
                                                            //Credit only
                                                            f.Fee + (f.FeeGST.HasValue ? f.FeeGST.Value : 0.00M) - f.Commission;
                });

            //Generate the IssueTicketQuoteKey
            List<Quote> issuablequotes = quotes.Where(w => !w.Expired).ToList();

            if (!issuablequotes.IsNullOrEmpty())
            {
                GetTicketingPCCExpiaryIssueTicketKey(pnr, issuablequotes, ApplySupressITFlag, sessionID);
            }

            //Remove quotes that have invalid sector information and sort by paxtype
            return quotes.
                    Where(q => includeexpiredquotes || q.QuoteSectors.All(a => a.PQSectorNo != -2)).
                    OrderBy(o => o.QuotePassenger.PaxType.Substring(0, 1)).
                    ToList();
        }

        private void CalculateCommission(List<Quote> quotes, PNR pnr, string ticketingpcc, string sessionID)
        {
            var calculateCommissionTasks = new List<Task>();
            CancellationToken ct = new CancellationToken();

            ParallelOptions options = new ParallelOptions { CancellationToken = ct };

            Parallel.ForEach(quotes, options, (quote) =>
            {
                var secs = quote.
                            QuoteSectors.
                            Select(s => pnr.Sectors.First(f =>
                                        f.From == s.DepartureCityCode &&
                                        f.To == s.ArrivalCityCode &&
                                        f.DepartureDate == s.DepartureDate)).
                            Select(s => new TPSector()
                            {
                                From = s.From,
                                To = s.To
                            }).
                            ToList();

                quote.TurnaroundPoint = _getTurnaroundPointDataSource.
                                                GetTurnaroundPoint(
                                                    new GetTurnaroundPointRequest()
                                                    {
                                                        FareCalculation = quote.FareCalculation,
                                                        Sectors = secs
                                                    }).GetAwaiter().GetResult();


                if (quote.TurnaroundPoint == "err")
                {
                    throw new AeronologyException("5000087", "Turnaround point invalid");
                }

                var calculateCommissionRequest = new CalculateCommissionRequest()
                {
                    SessionId = sessionID,
                    GdsCode = "1W",
                    PlatingCarrier = quote.ValidatingCarrier,
                    IssueDate = DateTime.Now,
                    Origin = quote.QuoteSectors.First().DepartureCityCode,
                    Destination = quote.TurnaroundPoint,
                    DocumnentType = "TKT",
                    TourCode = quote.TourCode,
                    Sectors = (from pqsecs in quote.QuoteSectors
                               let sec = pnr.Sectors.First(f => f.SectorNo == pqsecs.PQSectorNo)
                               select new CalculateCommissionSectorRequest()
                               {
                                   SectorNumber = sec.SectorNo.ToString(),
                                   Origin = sec.From,
                                   Destination = sec.To,
                                   IsCodeshare = sec.CodeShare,
                                   DepartureDate = DateTime.Parse(sec.DepartureDate),
                                   Cabin = sec.Cabin,
                                   BookingClass = sec.Class,
                                   Mileage = sec.Mileage == 0M ? default(int?) : Convert.ToInt32(sec.Mileage.ToString()),
                                   MarketingCarrier = sec.Carrier,
                                   MarketingFlightNumber = sec.Flight,
                                   OperatingCarrier = sec.OperatingCarrier,
                                   OperatingFlightNumber = sec.OperatingCarrierFlightNo,
                                   ArrivalDate = DateTime.Parse(sec.ArrivalDate),
                                   FareBasis = pqsecs.FareBasis
                               }).
                              ToArray(),
                    Passengers = new CalculateCommissionPassengerRequest[]
                                 {
                                        new CalculateCommissionPassengerRequest()
                                        {
                                            PaxType = GetPaxType(quote.QuotePassenger.PaxType),
                                            PassengerNumber = quote.QuotePassenger.NameNumber
                                        }
                                 },
                    TicketingPcc = ticketingpcc,
                    AgentNumber = agent?.AgentId,
                    AgentUsername = pcc.Name,
                    ConsolidatorId = agent?.ConsolidatorId ?? "acn",
                    CountryOfSale = !string.IsNullOrEmpty(agent?.Consolidator?.CountryCode) ?
                                        agent.Consolidator.CountryCode :
                                        "AU",
                    AgentIata = agent?.Agent?.FinanceDetails?.IataNumber,
                    BspCommission = quote.BspCommissionRate,
                    FormOfPayment = quote.QuotePassenger.FormOfPayment != null && quote.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CC ?
                                        "CREDIT_CARD" :
                                        "CASH",
                    Quotes = new CalculateCommissionQuoteRequest[]
                    {
                            new CalculateCommissionQuoteRequest()
                            {
                                BaseFareAmount = quote.BaseFare,
                                FuelSurcharge = quote.Taxes?.FirstOrDefault(w=> w.Fuel)?.Amount??0.00M,
                                QuoteNumber = quote.QuoteNo.ToString(),
                                PassengerNumber = quote.QuotePassenger.NameNumber,
                                Currency = quote.CurrencyCode,
                                QuotedSectors = quote.QuoteSectors.Select(qsec => qsec.PQSectorNo.ToString()).ToArray()
                            }
                    }
                };

                var calculateCommissionResponse = _commissionDataService.Calculate(calculateCommissionRequest).GetAwaiter().GetResult();

                //read contextID
                quote.ContextID = _commissionDataService.ContextID;

                if (!(calculateCommissionResponse.PlatingCarrierBspRate.HasValue || (calculateCommissionResponse.PlatingCarrierAgentFee != null && calculateCommissionResponse.PlatingCarrierAgentFee.Amount.HasValue)))
                {
                    quote.Errors = new List<string>()
                    {
                        $"(Context ID - {_commissionDataService.ContextID}){Environment.NewLine}Commission or fee record not found. Please contact the consolidator\\ticket office for more information."
                    };
                    return;
                }

                decimal? bspCommission = calculateCommissionResponse.PlatingCarrierBspRate.HasValue ?
                                                Math.Round(Convert.ToDecimal(calculateCommissionResponse.PlatingCarrierBspRate.Value), 2) :
                                                default(decimal?);
                decimal fee = calculateCommissionResponse.PlatingCarrierAgentFee == null || !calculateCommissionResponse.PlatingCarrierAgentFee.Amount.HasValue ?
                                    default :
                                    Math.Round(Convert.ToDecimal(calculateCommissionResponse.PlatingCarrierAgentFee.Amount.Value), 2);
                quote.AgentCommissions = calculateCommissionResponse.AgentCommissions == null ? new List<AgentCommission>() : calculateCommissionResponse.AgentCommissions.ToList();
                quote.BspCommissionRate = bspCommission;
                quote.GSTRate = GetGSTPercentage(agent?.Consolidator?.CountryCode);
                quote.Fee = fee;
                quote.TourCode = string.IsNullOrEmpty(quote.TourCode) ? calculateCommissionResponse.PlatingCarrierTourCode : quote.TourCode;
            });

            if (quotes.All(a => !a.Errors.IsNullOrEmpty()))
            {
                throw new AeronologyException("COMMISSION_REC_NOT_FOUND",
                                                string.Join(",", quotes.SelectMany(q => q.Errors).Distinct()));
            }
        }

        private decimal? GetGSTPercentage(string country)
        {
            string countrycode = country;
            if (string.IsNullOrEmpty(countrycode))
            {
                countrycode = "AU";
            }

            switch (countrycode)
            {
                case "AU":
                    return 10M;
                //case "NZ":
                //    return 15M;
                default:
                    return default(decimal?);
            }
        }

        private string GetPaxType(string paxType)
        {
            if (paxType.StartsWith("A"))
            {
                return "ADT";
            }
            else if (paxType.StartsWith("C"))
            {
                return "CHD";
            }
            else if (paxType.StartsWith("I"))
            {
                return "INF";
            }
            else
            {
                return "ADT";
            }
        }

        private void GetFuelSurcharge(List<Quote> quotes)
        {
            CancellationToken ct = new CancellationToken();
            ParallelOptions options = new ParallelOptions { CancellationToken = ct };

            Parallel.ForEach(quotes, options, (quote) =>
            {
                var fueltaxes = quote.Taxes?.Where(t => "YR|YQ".Contains(t.Code));
                if (!fueltaxes.IsNullOrEmpty())
                {
                    if (fueltaxes.Count() > 1)
                    {
                        decimal maxfueltax = fueltaxes.Max(m => m.Amount);
                        quote.Taxes.First(t => t.Amount == maxfueltax).Fuel = true;
                        return;
                    }

                    quote.Taxes.First(t => "YR|YQ".Contains(t.Code)).Fuel = true;
                }
            });
        }

        private FOP GetFOP(string pricingcommand)
        {
            List<StoredCreditCard> cards = CreditCardOperations.GetStoredCards(pricingcommand);

            if (!cards.IsNullOrEmpty())
            {
                return new FOP()
                {
                    PaymentType = PaymentType.CC,
                    CardNumber = cards.First().CreditCard.Trim().MaskNumber(),
                    ExpiryDate = cards.First().Expiry
                };
            }

            string pattern = @"F(INV\w+)$";
            string Bcode = pricingcommand.LastMatch(pattern, "");

            return new FOP()
            {
                PaymentType = PaymentType.CA,
                BCode = Bcode
            };
        }

        private void GetTicketingPCCExpiaryIssueTicketKey(PNR pnr, List<Quote> issuablequotes, bool applySupressITFlag, string sessionID)
        {
            var resp = new List<Task>();
            CancellationToken ct = new CancellationToken();
            ParallelOptions options = new ParallelOptions { CancellationToken = ct };

            Parallel.ForEach(issuablequotes, options, (quote) =>
            {
                quote.TicketingPCC = GetPlateManagementPCC(quote, pnr, sessionID).GetAwaiter().GetResult();
                quote.Expired = quote.Expired || string.IsNullOrEmpty(quote.TicketingPCC);
                quote.IssueTicketQuoteKey = GetTicketingQuoteKey(quote, quote.QuotePassenger.FormOfPayment.BCode, applySupressITFlag);
            });
        }

        private string GetTicketingQuoteKey(Quote quote, string bcode = "", bool applySupressITFlag = false)
        {
            return JsonConvert.
                        SerializeObject
                        (
                            new IssueExpressTicketQuote()
                            {
                                BaseFare = quote.BaseFare,
                                AgentCommissions = quote.AgentCommissions,
                                AgentCommissionRate = quote.AgentCommissionRate,
                                BSPCommissionRate = quote.BspCommissionRate,
                                Fee = quote.Fee,
                                FeeGST = quote.FeeGST,
                                FiledFare = quote.FiledFare,
                                PlatingCarrier = quote.ValidatingCarrier,
                                Passenger = quote.QuotePassenger,
                                Sectors = quote.QuoteSectors,
                                TotalFare = quote.TotalFare,
                                TotalTax = quote.TotalTax,
                                QuoteNo = quote.QuoteNo,
                                Route = quote.Route,
                                SectorCount = quote.SectorCount,
                                PriceCode = quote.PriceCode,
                                TourCode = quote.TourCode,
                                TicketingPCC = quote.TicketingPCC,
                                BCode = bcode,
                                Endorsements = quote.Endorsements,
                                ApplySupressITFlag = applySupressITFlag
                            }
                        ).
                        EncodeBase64();
        }


        private async Task<string> GetPlateManagementPCC(Quote quote, PNR pnr, string sessionID)
        {
            List<PNRSector> secs = quote.
                                    QuoteSectors.
                                    Select(s => pnr.
                                                   Sectors.
                                                   Where(w => w.From != "ARUNK").
                                                   FirstOrDefault(f => f.From == s.DepartureCityCode &&
                                                                       f.To == s.ArrivalCityCode &&
                                                                       f.DepartureDate == s.DepartureDate)).
                                    Where(w => w != null).
                                    ToList();

            if (secs.IsNullOrEmpty()) { return ""; }

            PlateRuleTicketingPccRequest rq = new PlateRuleTicketingPccRequest()
            {
                GdsCode = "1W",
                AgentId = agent?.AgentId,
                ConsolidatorId = agent?.ConsolidatorId ?? "acn",
                BookingPcc = pnr.BookedPCC,
                PlatingCarrier = quote.ValidatingCarrier,
                Cabin = secs.First().Cabin,
                BookingClass = secs.Select(s => s.Class).Distinct().ToArray()
            };

            PlateRuleTicketingPccResponse res = await _ticketingPccDataSource.GetTicketingPccFromRules(rq, sessionID);

            return res.TicketingPccCode;
        }

        private List<SSR> GetSSRs(List<SabreOpenSSR> openSSRs, List<PNRSector> sectors, List<PNRPassengers> paxs)
        {
            if (sectors.IsNullOrEmpty() || paxs.IsNullOrEmpty())
            {
                return null;
            }

            List<SSR> ssrs = new List<SSR>();

            var query = from openssr in openSSRs
                        let firstsec = openssr.Sectors == null ? null : openssr.Sectors.First()
                        let secno = firstsec == null ?
                                        default(int?) :
                                        sectors.
                                            FirstOrDefault(f =>
                                                f.From == firstsec.From &&
                                                f.To == firstsec.To &&
                                                f.Flight == firstsec.FlightNumber &&
                                                f.Carrier == firstsec.Carrier)?.SectorNo
                        let flightsummary = firstsec == null ? "" : $"{firstsec.From} {firstsec.To} {firstsec.Carrier} {firstsec.FlightNumber} {firstsec.BookingClass}"
                        let paxname = openssr.Passengers == null ? "" : openssr.Passengers.First().PassengerName
                        let namenumber = openssr.Passengers == null ?
                                            "" :
                                                string.IsNullOrEmpty(openssr.Passengers.First().NameNumber) ?
                                                    !string.IsNullOrEmpty(paxname) ?
                                                        paxs.Where(w => w.Passengername == paxname).Count() > 0 ?
                                                            paxs.First(w => w.Passengername == paxname).NameNumber :
                                                            "" :
                                                        "" :
                                                    openssr.Passengers.First().NameNumber
                        select new SSR()
                        {
                            SSRCode = openssr.SSRCode,
                            Status = openssr.Status,
                            Carrier = openssr.Carrier,
                            NameNumber = namenumber,
                            PassengerName = paxname,
                            FreeText = openssr.FreeText,
                            SectorNo = secno,
                            AllSectors = openssr.Sectors == null,
                            FlightSummary = flightsummary
                        };

            ssrs.AddRange(query.ToList());

            var unconfirmedssrs = ssrs.
                Where(w => !string.IsNullOrEmpty(w.Status)).
                GroupBy(g => new { g.SSRCode, g.NameNumber, g.SectorNo }).
                Where(w => w.Count() > 1).
                SelectMany(s => s.ToList()).
                Where(w => !"KK|HK|NO|UC|UN|HX".Contains(w.Status));

            ssrs.RemoveAll(r => unconfirmedssrs.Contains(r));

            return ssrs.
                    GroupBy(grp => new { grp.SSRCode, grp.Carrier, grp.NameNumber, grp.SectorNo, grp.AllSectors, grp.Status }).
                    Select(s => s.First()).
                    OrderBy(o => o.NameNumber).
                    ThenBy(o => o.SectorNo).
                    ToList();
        }

        private List<Ancillary> GetAncillaries(SabrePNR sabrepnr)
        {
            return sabrepnr.
                        Passengers.
                        SelectMany(s => s.AncillaryServices).
                        Select(s => new
                        {
                            ancs = s,
                            pax = GetPassengers(s.AssociatedPassengerName, s.ApplicablePassengerTypeCode, sabrepnr.Passengers)
                        }).
                        Where(w => ExcludeRFISC(w.ancs.RFISC)).
                        Select(s => new Ancillary()
                        {
                            AncillaryGroup = s.ancs.GroupKey,
                            EMDNumber = s.ancs.EMDNumber,
                            ID = s.ancs.ID,
                            CommercialName = s.ancs.CommercialName,
                            SeatNumber = s.ancs.SeatNumber.TrimStart('0'),
                            RFISC = s.ancs.RFISC,
                            RFIC = s.ancs.RFIC,
                            Carrier = s.ancs.Carrier,
                            PassengerName = s.ancs.AssociatedPassengerName,
                            NameNumber = (s.pax?.NameNumber ?? "").Replace("0", ""),
                            TotalTax = s.ancs.Taxes?.Sum(t => t.TaxAmount) ?? 0.00M,
                            Taxes = s.ancs.Taxes?.
                                        Select(tax => new Tax()
                                        {
                                            Amount = tax.TaxAmount,
                                            Code = tax.TaxCode
                                        }).ToList(),
                            BasePrice = s.ancs.BasePrice,
                            TotalPrice = s.ancs.TotalPrice,
                            CurrencyCode = s.ancs.CurrencyCode,
                            Origin = s.ancs.Origin,
                            Destination = s.ancs.Destination,
                            Sectors = s.
                                        ancs.
                                        AssociatedSector.
                                        Select(asec => new AncillarySector()
                                        {
                                            SectorNo = asec.SectorNo,
                                            DepartureDate = asec.DepartureDate,
                                            FlightNo = asec.FlightNo,
                                            Origin = asec.From,
                                            Destination = asec.To,
                                            MarketingCarrier = asec.MarketingCarrier,
                                            OperatingCarrier = asec.OperatingCarrier
                                        }).
                                        ToList(),
                            InvalidErrorCode = GetAncillaryErrorCode(s.ancs.Guaranteed,
                                                                        s.ancs.Status,
                                                                        s.ancs.PurchaseByDate,
                                                                        s.ancs.RFIC),
                            AlreadyTicketed = s.ancs.Status == "HI",
                            PurchaseByDate = s.ancs.PurchaseByDate.HasValue ?
                                                s.ancs.PurchaseByDate.Value.GetISODateTime() :
                                                ""
                        }).
                        ToList();
        }

        private SabrePassenger GetPassengers(string associatedPassengerName, string applicablePassengerTypeCode, List<SabrePassenger> passengers)
        {
            return passengers.
                    FirstOrDefault(f => f.PassengerName.Contains(associatedPassengerName) &&
                               (f.PaxType.Substring(0, 1) == applicablePassengerTypeCode.Substring(0, 1) ||
                                (f.PaxType.Substring(0, 1) == "C" && applicablePassengerTypeCode.Substring(0, 1) == "A")));
        }

        private string GetAncillaryErrorCode(bool guaranteed, string status, DateTime? purchasebydate, string RFIC)
        {
            if (status == "HI")
            {
                return "50000105";
            }
            else if (status == "HK" && (purchasebydate.HasValue && purchasebydate.Value < DateTime.Now))
            {
                return "50000104";
            }
            else if (status == "HK")
            {
                return "50000106";
            }
            else if (status != "HD")
            {
                return "50000103";
            }
            else if (!guaranteed)
            {
                if (RFIC == "A")
                {
                    return "";
                }
                return "50000102";
            }
            else if (purchasebydate.HasValue && purchasebydate.Value < DateTime.Now)
            {
                return "50000104";
            }

            return "";
        }

        private bool ExcludeRFISC(string rfisc)
        {
            List<string> excludedrfiscs = new List<string>()
            {
                "PTD"
            };

            return !excludedrfiscs.Contains(rfisc);
        }
    }

    internal class sectorinfo
    {
        public int SectorNo { get; set; }
        public string DepartureDate { get; set; }
        public bool Void { get; set; }
    }
}
