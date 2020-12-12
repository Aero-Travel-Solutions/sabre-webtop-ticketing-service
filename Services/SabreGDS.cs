using Amazon.Runtime.Internal.Util;
using GetReservation;
using Microsoft.AspNetCore.DataProtection;
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
        private readonly SessionCloseService _sessionCloseService;
        private readonly GetReservationService _getReservationService;
        private readonly EnhancedAirBookService _enhancedAirBookService;
        private readonly EnhancedAirTicketService enhancedAirTicketService;
        private readonly EnhancedEndTransService enhancedEndTransService;
        private readonly IGetTurnaroundPointDataSource _getTurnaroundPointDataSource;
        private readonly ICommissionDataService _commissionDataService;
        private readonly IAgentPccDataSource _agentPccDataSource;
        private readonly ILogger logger;
        private readonly DbCache _dbCache;
        private readonly IAsyncPolicy retryPolicy;
        private readonly IDataProtector dataProtector;
        private readonly SessionDataSource session;
        private readonly IBackofficeDataSource _backofficeDataSource;
        private readonly IOrdersTransactionDataSource _ordersTransactionDataSource;
        private readonly IAsyncPolicy _getOrderSequenceFailedRetryPolicy;
        private readonly BackofficeOptions _backofficeOptions;
        private readonly IMerchantDataSource merchantDataSource;
        private readonly IBCodeDataSource bCodeDataSource;
        private readonly INotificationHelper _notificationHelper;

        public User user { get; set; }
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
            SessionCloseService sessionCloseService,
            GetReservationService getReservationService,
            EnhancedAirBookService enhancedAirBookService,
            EnhancedAirTicketService _enhancedAirTicketService,
            EnhancedEndTransService _enhancedEndTransService,
            IGetTurnaroundPointDataSource getTurnaroundPointDataSource,
            ICommissionDataService commissionDataService,
            ExpiredTokenRetryPolicy expiredTokenRetryPolicy,
            IDataProtectionProvider dataProtectionProvider,
            IAgentPccDataSource agentPccDataSource,
            SessionDataSource session,
            INotificationHelper notificationHelper,
            IMerchantDataSource _merchantDataSource,
            IBCodeDataSource _bCodeDataSource,
            IBackofficeDataSource backofficeDataSource)
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
            _sessionCloseService = sessionCloseService;
            _getReservationService = getReservationService;
            _enhancedAirBookService = enhancedAirBookService;
            _getTurnaroundPointDataSource = getTurnaroundPointDataSource;
            _commissionDataService = commissionDataService;
            retryPolicy = expiredTokenRetryPolicy.ExpiredTokenPolicy;
            dataProtector = dataProtectionProvider.CreateProtector("CCDataProtector");
            _agentPccDataSource = agentPccDataSource;
            _notificationHelper = notificationHelper;
            merchantDataSource = _merchantDataSource;
            bCodeDataSource = _bCodeDataSource;
            _backofficeDataSource = backofficeDataSource;
            enhancedAirTicketService = _enhancedAirTicketService;
            enhancedEndTransService = _enhancedEndTransService;
            this.session = session;
        }

        private async Task<Agent> getAgentData(string sessionid, string consolidatorid, string agentid, string webservicepcc)
        {
            var agentDetails = await _agentPccDataSource.RetrieveAgentDetails(consolidatorid, agentid, sessionid);

            if(agentDetails == null) { throw new AeronologyException("AGENT_NOT_FOUND", "Agent data extraction fail."); }
            
            Agent agent = new Agent()
            {
                TicketingQueue = webservicepcc == "G4AK" ? "30" : "",
                PccList = (await _agentPccDataSource.RetrieveAgentPccs(agentid, sessionid)).PccList,
                AgentId = agentid,
                Agent = agentDetails,
                ConsolidatorId = consolidatorid,
                Consolidator = agentDetails?.Consolidator,
                Permissions = agentDetails?.Permission,
                CustomerNo = agentDetails?.CustomerNo,
                FullName = agentDetails?.Name,
                Name = agentDetails?.Name,
                CreditLimit = agentDetails.AccounDetails?.CreditLimit,
                Address = agentDetails?.Address,
                TicketingPcc = await GetTicketingPCC(sessionid)
            };       

            user.Agent.CustomerNo = agent.CustomerNo;
            return agent;
        }

        public async Task<SearchPNRResponse> SearchPNR(SearchPNRRequest request, string contextID)
        {
            string token = "";
            PNR pnr = new PNR();
            List<SabreSearchPNRResponse> res = new List<SabreSearchPNRResponse>();
            SabreSession sabreSession = null;
            user = await session.GetSessionUser(request.SessionID);
            pcc = await _consolidatorPccDataSource.GetWebServicePccByGdsCode("1W", contextID, request.SessionID);

            try
            {
                var sw = Stopwatch.StartNew();

                //Obtain session
                sabreSession = await _sessionCreateService.
                                                    CreateStatefulSessionToken(
                                                        pcc,
                                                        request.SearchText);
                //ignore session
                await _sabreCommandService.ExecuteCommand(sabreSession.SessionID, pcc, "I");

                token = sabreSession.SessionID;

                //Retrieve PNR if only one match found
                try
                {
                    pnr = await retryPolicy.ExecuteAsync(() => GetPNR(token, request.SessionID, request.SearchText, true, true, true, false));

                    logger.LogInformation($"Response parsing and validation @SearchPNR elapsed {sw.ElapsedMilliseconds} ms.");

                    return new SearchPNRResponse() { PNR = pnr };
                }
                catch (GDSException gdsex)
                {
                    if (gdsex.Message.StartsWith("PNR Restricted, caused by [PNR Restricted, code: 500324, severity: MODERATE]"))
                    {
                        throw new GDSException("PNR_RESTRICTED", "PNR Restricted");
                    }
                    throw (gdsex);
                }
            }
            finally
            {
                if (sabreSession != null && sabreSession.IsLimitReached)
                {
                    await _sessionCloseService.SabreSignout(sabreSession.SessionID, pcc);
                }
            }

        }

        private async Task<string> GetTicketingPCC(string sessionID)
        {
            var res = await _ticketingPccDataSource.
                            GetDefaultTicketingPccByGdsCode("1W", sessionID);

            return res?.PccCode;
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

        internal Task<List<WebtopWarning>> ValidateCommission(GetQuoteRQ rq, string contextid)
        {
            throw new NotImplementedException();
        }

        public async Task<PNR> GetPNR(string sabresessionid, string sessionid, string locator, bool withpnrvalidation = false, bool getStoredCards = false, bool includeQuotes = false, bool includeexpiredquote = false, string ticketingpcc = "")
        {
            var pnrAccessKey = $"{ticketingpcc}-{locator}-pnr".EncodeBase64();
            var cardAccessKey = $"{ticketingpcc}-{locator}-card".EncodeBase64();

            //get reservation
            GetReservationRS response = await _getReservationService.RetrievePNR(locator, sabresessionid, pcc, ticketingpcc);

            //booking pcc
            string bookingpcc = ((ReservationPNRB)response.Item).POS.Source.PseudoCityCode;

            PNR pnr = null;
            List<PNRAgent> agents = new List<PNRAgent>();

            CancellationToken ct = new CancellationToken();

            ParallelOptions options = new ParallelOptions { CancellationToken = ct };

            Parallel.
                Invoke(
                    //Post retrieval actions
                    () => pnr = ParseSabrePNR(response, sabresessionid, sessionid, includeQuotes, includeexpiredquote),
                    //Retrieve agencies
                    () => agents = GetAgents(sessionid, bookingpcc)
                );

            if(!agents.IsNullOrEmpty())
            {
                pnr.Agents = agents;
            }

            if (withpnrvalidation)
            {
                    //PNR validation
                    pnr.InvokePostPNRRetrivalActions();
            }

            //Save PNR in cache
            await _dbCache.InsertPNR(pnrAccessKey, pnr, 15);

            if (getStoredCards)
            {
                var storedCreditCard = GetStoredCards(response);

                //Encrypt card number
                storedCreditCard.ForEach(c => c.CreditCard = dataProtector.Protect(c.CreditCard));

                await _dbCache.InsertStoreCC(cardAccessKey, storedCreditCard, 15);
            }

            return pnr;
        }

        public async Task<List<Quote>> BestBuy(GetQuoteRQ request, string contextID)
        {
            request.Validate();
            SabreSession token = null;
            PNR pnr = null;

            try
            {
                string sessionID = request.SessionID;
                user = await session.GetSessionUser(sessionID);
                pcc = await _consolidatorPccDataSource.GetWebServicePccByGdsCode("1W", contextID, sessionID);
                agent = await getAgentData(
                                        request.SessionID,
                                        user.ConsolidatorId,
                                        request.AgentID,
                                        pcc.PccCode);

                List<StoredCreditCard> storedCreditCards = null;
                string ticketingpcc = GetTicketingPCC(agent?.TicketingPcc, pcc.PccCode);
                var pnrAccessKey = $"{ticketingpcc}-{request.Locator}-pnr".EncodeBase64();

                //Obtain session (if found from cache, else directly from sabre)
                token = await _sessionCreateService.CreateStatefulSessionToken(pcc, request.Locator);

                //Check to see if the session is from cache and usable
                if (token.Stored && !token.Expired)
                {
                    var cardAccessKey = $"{ticketingpcc}-{request.Locator}-card".EncodeBase64();

                    //Try get PNR in cache               
                    pnr = await _dbCache.Get<PNR>(pnrAccessKey);

                    if (request.SelectedPassengers.Any(q => q.FormOfPayment.PaymentType == PaymentType.CC && q.FormOfPayment.CardNumber.Contains("XXX")))
                    {
                        //Try get stored cards
                        storedCreditCards = await _dbCache.Get<List<StoredCreditCard>>(cardAccessKey);
                        if (!storedCreditCards.IsNullOrEmpty())
                        {
                            storedCreditCards.Where(w => w.CreditCard != null).ToList().ForEach(cc => cc.CreditCard = dataProtector.Unprotect(cc.CreditCard));
                            GetStoredCardDetails(request.SelectedPassengers, null, storedCreditCards);
                        }
                    }
                }

                if (pnr == null)
                {
                    if ((ticketingpcc != pcc.PccCode) ||
                        (token.Stored &&
                         !token.Expired &&
                         (string.IsNullOrEmpty(token.CurrentPCC) ? token.ConsolidatorPCC : token.CurrentPCC) != ticketingpcc))
                    {
                        //ignore session
                        await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, "I");

                        //Context Change
                        await _changeContextService.ContextChange(token, pcc, ticketingpcc, request.Locator);
                    }

                    //Retrieve PNR
                    GetReservationRS result = await _getReservationService.RetrievePNR(request.Locator, token.SessionID, pcc);

                    //Parse PNR++
                    pnr = ParseSabrePNR(result, token.SessionID, sessionID);

                    //Get stored card data
                    var maskedcards = request.
                                        SelectedPassengers.
                                        Where(q =>
                                            q.FormOfPayment.PaymentType == PaymentType.CC &&
                                            q.FormOfPayment.CardNumber.Contains("XXX"));
                    if (maskedcards.Count() > 0 &&
                        (storedCreditCards.IsNullOrEmpty() || storedCreditCards.Count == maskedcards.Count()))
                    {
                        GetStoredCardDetails(request.SelectedPassengers, result);
                    }
                }

                if (pnr == null) { throw new AeronologyException("50000017", "PNR data not found"); }

                if (pnr.Sectors.IsNullOrEmpty()) { throw new AeronologyException("50000002", "No flights found in the PNR"); }

                //published quote
                List<Quote> quotes = new List<Quote>();

                //ignore session
                await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, "IR");

                //workout plating carrier
                string platingcarrier = GetManualPlatingCarrier(pnr, request);
                logger.LogInformation($"Plating carrier {platingcarrier}.");

                //Generate bestbuy command
                string command = GetBestbuyCommand(request, platingcarrier);
                logger.LogInformation($"Sabre bestbuy command: {command}.");

                //sabre best buy
                string bestbuyresponse = await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, command);

                quotes = ParseFQBBResponse(bestbuyresponse, request, pnr, platingcarrier);

                return quotes;
            }
            catch(Exception ex)
            {
                if (ex is GDSException || ex is AeronologyException)
                {
                    return new List<Quote>()
                    {
                        new Quote()
                        {
                            Errors = new List<WebtopError>()
                            {
                                new WebtopError()
                                {
                                    code = ((AeronologyException)ex).ErrorCode,
                                    message = ex.Message
                                }
                            }
                        }
                    };
                }

                return new List<Quote>()
                {
                    new Quote()
                    {
                        Errors = new List<WebtopError>()
                        {
                            new WebtopError()
                            {
                                code = "UNKNOWN_ERROR",
                                message = ex.Message
                            }
                        }
                    }
                };
            }
            finally
            {
                if (token.IsLimitReached)
                {
                    await _sessionCloseService.SabreSignout(token.SessionID, pcc);
                }
            }
        }

        public async Task<List<Quote>> GetQuote(GetQuoteRQ request, string contextID, bool IsPriceOverride = false)
        {
            request.Validate();

            SabreSession token = null;

            PNR pnr = null;
            string sessionID = request.SessionID;
            user = await session.GetSessionUser(sessionID);
            pcc = await _consolidatorPccDataSource.GetWebServicePccByGdsCode("1W", contextID, sessionID);
            agent = await getAgentData(
                                    request.SessionID,
                                    user.ConsolidatorId,
                                    request.AgentID,
                                    pcc.PccCode);

            List<StoredCreditCard> storedCreditCards = null;
            string ticketingpcc = GetTicketingPCC(agent?.TicketingPcc, pcc.PccCode);
            var pnrAccessKey = $"{ticketingpcc}-{request.Locator}-pnr".EncodeBase64();

            //Obtain session (if found from cache, else directly from sabre)
            token = await _sessionCreateService.CreateStatefulSessionToken(pcc, request.Locator);

            //Check to see if the session is from cache and usable
            if (token.Stored && !token.Expired)
            {
                var cardAccessKey = $"{ticketingpcc}-{request.Locator}-card".EncodeBase64();

                //Try get PNR in cache               
                pnr = await _dbCache.Get<PNR>(pnrAccessKey);

                if (request.SelectedPassengers.Any(q => q.FormOfPayment.PaymentType == PaymentType.CC && q.FormOfPayment.CardNumber.Contains("XXX")))
                {
                    //Try get stored cards
                    storedCreditCards = await _dbCache.Get<List<StoredCreditCard>>(cardAccessKey);
                    if (!storedCreditCards.IsNullOrEmpty())
                    {
                        storedCreditCards.Where(w => w.CreditCard != null).ToList().ForEach(cc => cc.CreditCard = dataProtector.Unprotect(cc.CreditCard));
                        GetStoredCardDetails(request.SelectedPassengers, null, storedCreditCards);
                    }
                }
            }

            if (pnr == null)
            {
                if ((ticketingpcc != pcc.PccCode) ||
                    (token.Stored &&
                     !token.Expired &&
                     (string.IsNullOrEmpty(token.CurrentPCC) ? token.ConsolidatorPCC : token.CurrentPCC) != ticketingpcc))
                {
                    //ignore session
                    await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, "I");

                    //Context Change
                    await _changeContextService.ContextChange(token, pcc, ticketingpcc, request.Locator);
                }

                //Retrieve PNR
                GetReservationRS result = await _getReservationService.RetrievePNR(request.Locator, token.SessionID, pcc);

                //Parse PNR++
                pnr = ParseSabrePNR(result, token.SessionID, sessionID);

                //Get stored card data
                var maskedcards = request.
                                    SelectedPassengers.
                                    Where(q =>
                                        q.FormOfPayment.PaymentType == PaymentType.CC &&
                                        q.FormOfPayment.CardNumber.Contains("XXX"));
                if (maskedcards.Count() > 0 &&
                    (storedCreditCards.IsNullOrEmpty() || storedCreditCards.Count == maskedcards.Count()))
                {
                    GetStoredCardDetails(request.SelectedPassengers, result);
                }
            }

            if (pnr == null) { throw new AeronologyException("50000017", "PNR data not found"); }

            if (pnr.Sectors.IsNullOrEmpty()) { throw new AeronologyException("50000002", "No flights found in the PNR"); }

            //published quote
            List<Quote> quotes = new List<Quote>();

            try
            {
                quotes = await _enhancedAirBookService.PricePNR(request, token.SessionID, pcc, pnr, ticketingpcc, IsPriceOverride);

                //Check for pax type differences
                quotes.
                    Where(w => !w.DifferentPaxType.IsNullOrEmpty()).
                    ToList().
                    ForEach(f =>
                        f.Errors = new List<WebtopError>()
                        {
                        new WebtopError()
                        {
                            code = "DIFF_PAX_TYPE",
                            message = $"No fare found for passenger type used \"{string.Join(",", f.DifferentPaxType)}\"." +
                                            $"Please consider amending the passenger type in Sabre to ADT before trying again."
                        }
                        });

                if (!quotes.IsNullOrEmpty())
                {
                    //update pnr in cache
                    pnr.Quotes = new List<Quote>();
                    pnr.Quotes.AddRange(quotes);

                    //Save PNR in cache
                    await _dbCache.InsertPNR(pnrAccessKey, pnr, 15);
                }

                //redislpay price quotes
                await RedisplayGeneratedQuotes(token.SessionID, quotes);

                //ignore session
                await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, "I");

                //workout fuel surcharge taxcode
                GetFuelSurcharge(quotes);

                //Calculate Commission
                CalculateCommission(quotes, pnr, ticketingpcc, sessionID);

                //Agent Price
                quotes.
                    ForEach(f => f.AgentPrice = f.QuotePassenger.FormOfPayment == null ?
                                                f.DefaultPriceItAmount - f.Commission :
                                                //Cash Only
                                                f.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CA ?
                                                    f.DefaultPriceItAmount - f.Commission :
                                                    //Part Cash part credit
                                                    f.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CC && f.QuotePassenger.FormOfPayment.CreditAmount < f.TotalFare ?
                                                        f.TotalFare - f.QuotePassenger.FormOfPayment.CreditAmount + f.Fee + (f.FeeGST ?? 0.00M) - f.Commission :
                                                        //Credit only
                                                        f.Fee + (f.FeeGST ?? 0.00M) - f.Commission);

                //Generate the IssueTicketQuoteKey
                quotes.
                    ForEach(quote =>
                    {
                        quote.TicketingPCC = GetPlateManagementPCC(quote, pnr, sessionID).GetAwaiter().GetResult();
                        quote.IssueTicketQuoteKey = GetTicketingQuoteKey(quote);
                        quote.QuotePassenger.FormOfPayment.CardNumber = quote.QuotePassenger.FormOfPayment.CardNumber?.MaskNumber();
                    });
            }
            //catch (GDSException gdsex)
            //{
            //    logger.LogInformation($"EnhancedAirBookService return {gdsex}");

            //    //ignore session
            //    await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, "IR");

            //    //workout plating carrier
            //    string platingcarrier = GetManualPlatingCarrier(pnr, request);
            //    logger.LogInformation($"Plating carrier {platingcarrier}.");

            //    //Generate bestbuy command
            //    string command = GetBestbuyCommand(request, platingcarrier);
            //    logger.LogInformation($"Sabre bestbuy command: {command}.");

            //    //sabre best buy
            //    string bestbuyresponse = await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, command);

            //    quotes = ParseFQBBResponse(bestbuyresponse, request, pnr, platingcarrier);
            //}
            finally
            {
                if (token.IsLimitReached)
                {
                    await _sessionCloseService.SabreSignout(token.SessionID, pcc);
                }
            }

            return quotes;
        }

        public async Task<List<Quote>> ForceFBQuote(ForceFBQuoteRQ request, string contextID)
        {
            request.Validate();

            SabreSession token = null;

            PNR pnr = null;
            string sessionID = request.SessionID;
            user = await session.GetSessionUser(sessionID);
            pcc = await _consolidatorPccDataSource.GetWebServicePccByGdsCode("1W", contextID, sessionID);
            agent = await getAgentData(
                                    request.SessionID,
                                    user.ConsolidatorId,
                                    request.AgentID,
                                    pcc.PccCode);

            List<StoredCreditCard> storedCreditCards = null;
            string ticketingpcc = GetTicketingPCC(agent?.TicketingPcc, pcc.PccCode);
            var pnrAccessKey = $"{ticketingpcc}-{request.Locator}-pnr".EncodeBase64();

            //Obtain session (if found from cache, else directly from sabre)
            token = await _sessionCreateService.CreateStatefulSessionToken(pcc, request.Locator);

            //Check to see if the session is from cache and usable
            if (token.Stored && !token.Expired)
            {
                var cardAccessKey = $"{ticketingpcc}-{request.Locator}-card".EncodeBase64();

                //Try get PNR in cache               
                pnr = await _dbCache.Get<PNR>(pnrAccessKey);

                if (request.SelectedPassengers.Any(q => q.FormOfPayment.PaymentType == PaymentType.CC && q.FormOfPayment.CardNumber.Contains("XXX")))
                {
                    //Try get stored cards
                    storedCreditCards = await _dbCache.Get<List<StoredCreditCard>>(cardAccessKey);
                    if (!storedCreditCards.IsNullOrEmpty())
                    {
                        storedCreditCards.Where(w => w.CreditCard != null).ToList().ForEach(cc => cc.CreditCard = dataProtector.Unprotect(cc.CreditCard));
                        GetStoredCardDetails(request.SelectedPassengers, null, storedCreditCards);
                    }
                }
            }

            if (pnr == null)
            {
                if ((ticketingpcc != pcc.PccCode) ||
                    (token.Stored &&
                     !token.Expired &&
                     (string.IsNullOrEmpty(token.CurrentPCC) ? token.ConsolidatorPCC : token.CurrentPCC) != ticketingpcc))
                {
                    //ignore session
                    await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, "I");

                    //Context Change
                    await _changeContextService.ContextChange(token, pcc, ticketingpcc, request.Locator);
                }

                //Retrieve PNR
                GetReservationRS result = await _getReservationService.RetrievePNR(request.Locator, token.SessionID, pcc);

                //Parse PNR++
                pnr = ParseSabrePNR(result, token.SessionID, sessionID);

                //Get stored card data
                var maskedcards = request.
                                    SelectedPassengers.
                                    Where(q =>
                                        q.FormOfPayment.PaymentType == PaymentType.CC &&
                                        q.FormOfPayment.CardNumber.Contains("XXX"));
                if (maskedcards.Count() > 0 &&
                    (storedCreditCards.IsNullOrEmpty() || storedCreditCards.Count == maskedcards.Count()))
                {
                    GetStoredCardDetails(request.SelectedPassengers, result);
                }
            }

            if (pnr == null) { throw new AeronologyException("PNR_NOT_FOUND", "PNR data not found"); }

            if (pnr.Sectors.IsNullOrEmpty()) { throw new AeronologyException("NO_FLIGHTS", "No flights found in the PNR"); }

            //published quote
            List<Quote> quotes = new List<Quote>();

            try
            {
                quotes = await _enhancedAirBookService.ForceFarebasis(request, token.SessionID, pcc, pnr, ticketingpcc);

                //Check for pax type differences
                quotes.
                    Where(w => !w.DifferentPaxType.IsNullOrEmpty()).
                    ToList().
                    ForEach(f =>
                        f.Errors = new List<WebtopError>()
                        {
                        new WebtopError()
                        {
                            code = "DIFF_PAX_TYPE",
                            message = $"No fare found for passenger type used \"{string.Join(",", f.DifferentPaxType)}\"." +
                                            $"Please consider amending the passenger type in Sabre to ADT before trying again."
                        }
                        });

                if (!quotes.IsNullOrEmpty())
                {
                    //update pnr in cache
                    pnr.Quotes = new List<Quote>();
                    pnr.Quotes.AddRange(quotes);

                    //Save PNR in cache
                    await _dbCache.InsertPNR(pnrAccessKey, pnr, 15);
                }

                //redislpay price quotes
                await RedisplayGeneratedQuotes(token.SessionID, quotes);

                //ignore session
                await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, "I");

                //workout fuel surcharge taxcode
                GetFuelSurcharge(quotes);

                //Calculate Commission
                CalculateCommission(quotes, pnr, ticketingpcc, sessionID);

                //Agent Price
                quotes.
                    ForEach(f => f.AgentPrice = f.QuotePassenger.FormOfPayment == null ?
                                                f.DefaultPriceItAmount - f.Commission :
                                                //Cash Only
                                                f.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CA ?
                                                    f.DefaultPriceItAmount - f.Commission :
                                                    //Part Cash part credit
                                                    f.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CC && f.QuotePassenger.FormOfPayment.CreditAmount < f.TotalFare ?
                                                        f.TotalFare - f.QuotePassenger.FormOfPayment.CreditAmount + f.Fee + (f.FeeGST ?? 0.00M) - f.Commission :
                                                        //Credit only
                                                        f.Fee + (f.FeeGST ?? 0.00M) - f.Commission);

                //Generate the IssueTicketQuoteKey
                quotes.
                    ForEach(quote =>
                    {
                        quote.TicketingPCC = GetPlateManagementPCC(quote, pnr, sessionID).GetAwaiter().GetResult();
                        quote.IssueTicketQuoteKey = GetTicketingQuoteKey(quote);
                        quote.QuotePassenger.FormOfPayment.CardNumber = quote.QuotePassenger.FormOfPayment.CardNumber?.MaskNumber();
                    });
            }
            finally
            {
                if (token.IsLimitReached)
                {
                    await _sessionCloseService.SabreSignout(token.SessionID, pcc);
                }
            }

            return quotes;
        }

        private List<Quote> ParseFQBBResponse(string bestbuyresponse, GetQuoteRQ request, PNR pnr, string platingcarrier)
        {
            List<Quote> quotes = new List<Quote>();

            SabreBestBuyQuote bestbuyquote = new SabreBestBuyQuote(bestbuyresponse);

            int index = 0;
            quotes = (from pax in request.SelectedPassengers
                      let s = bestbuyquote.BestBuyItems.FirstOrDefault(f => f.PaxType == pax.PaxType)
                      select new Quote()
                      {
                          QuoteNo = index++,
                          Taxes = s == null ? new List<Tax>(): s.Taxes,
                          PricingHint = s == null ? "" : s.PriceHint,
                          PriceCode = request.PriceCode,
                          QuotePassenger = pax,
                          QuoteSectors = (from selsec in request.SelectedSectors
                                         let pnrsec = pnr.Sectors.First(f=> f.SectorNo == selsec.SectorNo)
                                         select new QuoteSector()
                                         {
                                             PQSectorNo = selsec.SectorNo,
                                             Arunk = pnrsec.From == "ARUNK",
                                             DepartureCityCode = pnrsec.From == "ARUNK" ? "" : pnrsec.From,
                                             ArrivalCityCode = pnrsec.From == "ARUNK" ? "" : pnrsec.To,
                                             DepartureDate = pnrsec.From == "ARUNK" ? "" : pnrsec.DepartureDate
                                         }).
                                         ToList(),
                          ValidatingCarrier = platingcarrier,
                          SectorCount = request.SelectedSectors.Count,
                          Route = GetRoute(pnr.Sectors.Where(w=> request.SelectedSectors.Select(s=> s.SectorNo).Contains(w.SectorNo)).ToList()),
                      }).
                      ToList();
            return quotes;
        }

        private string GetRoute(List<PNRSector> tktcoupons)
        {
            string route = "";
            for (int i = 0; i < tktcoupons.Count(); i++)
            {
                if (i == 0)
                {
                    route += tktcoupons[i].From;
                    route += "-" + tktcoupons[i].To;
                    continue;
                }

                if (i < tktcoupons.Count()
                    && tktcoupons[i - 1].To != tktcoupons[i].From)
                {
                    route += "//";
                    route += tktcoupons[i].From;
                    route += "-" + tktcoupons[i].To;
                    continue;
                }

                route += "-" + tktcoupons[i].To;
            }
            return route;
        }

        private string GetBestbuyCommand(GetQuoteRQ request, string platingcarrier)
        {
            string command = $"WPNC¥A{platingcarrier}¥S{string.Join("/", request.SelectedSectors.Select(s=> s.SectorNo))}";

            if(!string.IsNullOrEmpty(request.PriceCode))
            {
                command += $"¥AC*{request.PriceCode}";
            }
            return command;
        }

        private string GetManualPlatingCarrier(PNR pnr, GetQuoteRQ request)
        {
            string platingcarrier = pnr.
                                      Sectors.
                                      First(f => f.SectorNo == request.SelectedSectors.Select(s=>s.SectorNo).OrderBy(o => o).First()).
                                      Carrier;               

            return platingcarrier;
        }

        private void GetStoredCardDetails(List<QuotePassenger> quotePassengers, GetReservationRS res, List<StoredCreditCard> storedCreditCards = null)
        {
            //if stored card been used extract card info

            //1. Extract stored cards from PNR
            var storedcards = storedCreditCards ?? GetStoredCards(res);

            foreach (var item in quotePassengers.Where(q => q.FormOfPayment.PaymentType == PaymentType.CC && q.FormOfPayment.CardNumber.Contains("XXX")))
            {
                //2. Match masked cards and extract card details
                var value = storedcards.
                                FirstOrDefault(f =>
                                    f.MaskedCardNumber == item.FormOfPayment.CardNumber);

                if (value == null)
                {
                    throw new AeronologyException("50000050", "Card data not found.");
                }

                item.FormOfPayment.PaymentType = PaymentType.CC;
                item.FormOfPayment.CardNumber = value.CreditCard;
                item.FormOfPayment.ExpiryDate = item.FormOfPayment.ExpiryDate;
            }
        }

        private async Task RedisplayGeneratedQuotes(string token, List<Quote> quotes)
        {
            string pqtext = await _sabreCommandService.ExecuteCommand(token, pcc, "PQ");
            List<PQTextResp> applicabledpqres = ParsePQText(pqtext);
            if (applicabledpqres.Any(w => w.PQNo != -1))
            {
                applicabledpqres.
                    Where(w => w.PQNo != -1).
                    ToList().
                    ForEach(f => quotes.
                                    Where(q => q.QuotePassenger.PaxType == f.PassengerType ||
                                               (q.QuotePassenger.PaxType.StartsWith("C") && q.QuotePassenger.PaxType.Substring(0, 1) == f.PassengerType.Substring(0, 1))).
                                    ToList().
                                    ForEach(qf =>
                                    {
                                        qf.QuoteNo = f.PQNo;
                                        qf.BspCommissionRate = f.BSPCommission;
                                        qf.TourCode = f.TourCode;
                                    }));
            }
        }

        private List<PQTextResp> ParsePQText(string pqtext)
        {
            List<PQTextResp> result = new List<PQTextResp>();
            List<string> diffquotes = pqtext.SplitOnRegex(@"(PQ\s\d+)").Skip(1).ToList();
            for (int i = 0; i < diffquotes.Count(); i += 2)
            {
                var bsprate = diffquotes[i + 1].
                                   LastMatch(@"COMM\sPCT\s*(\d+)", "");

                string pricecommandline = diffquotes[i + 1].SplitOn("BASE FARE").First().Trim().Replace("\n", "");
                string paxtype = pricecommandline.LastMatch(@"ÂP([ACI][D\dN][T\dNF])");
                if (string.IsNullOrEmpty(paxtype))
                {
                    paxtype = diffquotes[i + 1].LastMatch(@"\s+INPUT\s+PTC\s*-\s*(.*)");
                }

                result.
                Add(new PQTextResp()
                {
                    PQNo = int.Parse(diffquotes[i].LastMatch(@"PQ\s(\d+)", "-1")),
                    PassengerType = paxtype,
                    TourCode = diffquotes[i + 1].
                                LastMatch(@"TOUR\sCODE-(\w*)") ?? "",
                    BSPCommission = string.IsNullOrEmpty(bsprate) ?
                                    default(decimal?) :
                                    decimal.Parse(bsprate)
                });
            }
            return result;
        }

        private List<PNRAgent> GetAgents(string sessionid, string bookingpcc)
        {
            List<DataAgent> agents = _agentPccDataSource.RetrieveAgents(user?.ConsolidatorId, sessionid).GetAwaiter().GetResult();
            var agentlist = agents.
                                Where(w => w.pcc_code == bookingpcc).
                                DistinctBy(d=> d.agent_id).
                                Select(agt => new PNRAgent()
                                {
                                    AgentId = agt.agent_id
                                }).
                                ToList();

            CancellationToken ct = new CancellationToken();

            ParallelOptions options = new ParallelOptions { CancellationToken = ct };

            Parallel.ForEach(agentlist, options, (agent) =>
            {
                agent.Name = _agentPccDataSource.RetrieveAgentDetails(user?.ConsolidatorId, agent.AgentId, sessionid).GetAwaiter().GetResult().Name;
            });

            return agentlist;
        }

        private PNR ParseSabrePNR(GetReservationRS result, string token, string sessionid, bool includeQuotes = false, bool includeexpiredquote = false)
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

            PNR pnr = GeneratePNR(token, sabrepnr, pcclocaldatetime, includeQuotes, includeexpiredquote, sessionid);

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

                private void GetStoredCardDetails(IssueExpressTicketRQ request, GetReservationRS res = null, IEnumerable<StoredCreditCard> storedCreditCards = null)
        {
            //if stored card been used extract card info

            //1. Extract stored cards from PNR
            var storedcards = storedCreditCards ?? GetStoredCards(res);

            if (!request.Quotes.IsNullOrEmpty())
            {
                foreach (var item in request.Quotes.Where(q => q.Passenger.FormOfPayment.PaymentType == PaymentType.CC && q.Passenger.FormOfPayment.CardNumber.Contains("XXX")))
                {
                    //2. Match masked cards and extract card details
                    var value = storedcards.
                                    FirstOrDefault(f =>
                                        f.MaskedCardNumber == item.Passenger.FormOfPayment.CardNumber);

                    if (value == null)
                    {
                        throw new AeronologyException("50000050", "Card data not found.");
                    }

                    item.Passenger.FormOfPayment.PaymentType = PaymentType.CC;
                    item.Passenger.FormOfPayment.CardNumber = value.CreditCard;
                    item.Passenger.FormOfPayment.ExpiryDate = item.Passenger.FormOfPayment.ExpiryDate;
                }
            }

            if (!request.EMDs.IsNullOrEmpty())
            {
                foreach (var item in request.EMDs.Where(q => q.FormOfPayment.PaymentType == PaymentType.CC && q.FormOfPayment.CardNumber.Contains("XXX")))
                {
                    //2. Match masked cards and extract card details
                    var value = storedcards.
                                    FirstOrDefault(f =>
                                        f.MaskedCardNumber == item.FormOfPayment.CardNumber);

                    if (value == null)
                    {
                        throw new AeronologyException("50000050", "Card data not found.");
                    }

                    item.FormOfPayment.PaymentType = PaymentType.CC;
                    item.FormOfPayment.CardNumber = value.CreditCard;
                    item.FormOfPayment.ExpiryDate = item.FormOfPayment.ExpiryDate;
                }
            }
        }

        private PNR GeneratePNR(string token, SabrePNR sabrepnr, DateTime? pcclocaldatetime, bool includeQuotes, bool includeexpiredquotes, string sessionid)
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
                pnr.Quotes = GetQuotes(sabrepnr.PriceQuote, pnr, includeexpiredquotes, pcclocaldatetime.Value, token, sessionid);
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

        public async Task<IssueExpressTicketRS> IssueExpressTicket(IssueExpressTicketRQ request, string contextID)
        {

            SabreSession sabreSession = null;
            string statefultoken = "";
            PNR pnr = null;
            string sessionID = request.SessionID;
            User user = await session.GetSessionUser(sessionID);
            pcc = await _consolidatorPccDataSource.GetWebServicePccByGdsCode("1W", contextID, sessionID);
            Agent agent = await getAgentData(
                                    request.SessionID,
                                    user.ConsolidatorId,
                                    request.AgentID,
                                    pcc.PccCode);
            string ticketingprinter = GetTicketingPrinter(pcc?.TicketPrinterAddress, pcc.PccCode);
            string printerbypass = GetPrinterByPass(string.IsNullOrEmpty(pcc?.CountryCode) ? "" : pcc?.CountryCode.SplitOn("|").First(), pcc.PccCode);

            string agentpcc = "";

            try
            {
                if (user == null)
                {
                    throw new ExpiredSessionException(request.SessionID, "50000401", "Invalid session.");
                }

                if (!((agent?.Agent?.Permission?.AllowTicketing ?? false) &&
                      (user.Permissions?.AllowTicketing ?? false)))
                {
                    throw new AeronologyException("50000020", "Ticketing access is not provided for your account. Please contact your consolidator to request access.");
                }

                //Populate request collections
                if (!(request.IssueTicketQuoteKeys.IsNullOrEmpty() || request.IssueTicketEMDKeys.IsNullOrEmpty()))
                {
                    ReconstructRequestFromKeys(request);
                }
                string ticketingpcc = request.Quotes.IsNullOrEmpty() ? GetTicketingPCC(agent?.TicketingPcc, pcc.PccCode) : request.Quotes.First().TicketingPCC;
                if (string.IsNullOrEmpty(ticketingpcc))
                {
                    throw new ExpiredSessionException(request.SessionID, "50000401", "Invalid ticketing pcc.");
                }

                agentpcc = (await getagentpccs(user?.AgentId, pcc.PccCode, request.SessionID)).FirstOrDefault();
                user.Agent.CustomerNo = agent.CustomerNo;

                //Obtain SOAP session
                sabreSession = await _sessionCreateService.CreateStatefulSessionToken(pcc, request.Locator);
                statefultoken = sabreSession.SessionID;

                if (sabreSession.Stored) { await _sabreCommandService.ExecuteCommand(statefultoken, pcc, "I"); }

                //Context Change
                await _changeContextService.ContextChange(sabreSession, pcc, ticketingpcc, request.Locator);

                //Retrieve PNR
                GetReservationRS getReservationRS = null;
                getReservationRS = await _getReservationService.RetrievePNR(request.Locator, statefultoken, pcc);
                pnr = ParseSabrePNR(getReservationRS, statefultoken, request.SessionID, true, true);

                //Stored cards
                GetStoredCards(request, getReservationRS);

                var pendingquotes = request.Quotes.Where(w => !w.FiledFare|| w.PriceType != Models.PriceType.Manual);
                var manualquotes = request.Quotes.Where(w => w.PriceType == Models.PriceType.Manual);
                var pendingsfdata = request.Quotes.Where(a => a.PendingSfData);

                //Manual build
                if(!manualquotes.IsNullOrEmpty())
                {
                    await ManualBuild(pcc, request, statefultoken, manualquotes, pnr, ticketingpcc, contextID);
                }

                //GDS quote
                if (!pendingquotes.IsNullOrEmpty())
                {
                    //Quoting
                    await QuotingForTickting(pcc, request, statefultoken, pendingquotes, pnr, ticketingpcc, contextID);
                }

                //adding secure flight data for passengers if required - US itineraries
                if (!pendingsfdata.IsNullOrEmpty())
                {
                    //Add secure flight data
                    //await AddSFDataForTickting(request, statefultoken, pendingsfdata);
                }

                _backofficeOptions.ConsolidatorsBackofficeProcess.TryGetValue(agent.ConsolidatorId, out var backofficeProcess);

                //credit check
                if (request.MerchantData == null &&
                    (!(request.Quotes?.Any(q => q.Passenger.FormOfPayment.PaymentType == PaymentType.CC) ?? true) ||
                    !(request.EMDs?.Any(e => e.FormOfPayment.PaymentType == PaymentType.CC) ?? true)) &&
                    !string.IsNullOrEmpty(agent.CustomerNo) && (backofficeProcess?.CreditLimitCheck ?? false))
                {
                    CreditCheck(request, agent.CustomerNo, request.SessionID);
                }

                //REST session
                Token token = await _sessionCreateService.CreateStatelessSessionToken(pcc);

                //adding dob remarks
                await AddDOBRemarks(request, ticketingpcc, user, token);

                string bcode = "";
                if (!request.Quotes.IsNullOrEmpty())
                {
                    //BCode extraction
                    bcode = await bCodeDataSource.
                                            RetrieveBCode(
                                                    request.SessionID,
                                                    request.Quotes.First().PlatingCarrier,
                                                    pnr.DKNumber);
                }

                bool enableextendedendo = false;
                //Check if endorsement 
                if (!string.IsNullOrEmpty(bcode))
                {
                    string resp = await _sabreCommandService.ExecuteCommand(statefultoken, pcc, "W/LRGEND¥*");
                    enableextendedendo = resp.Contains("EXPANDED ENDORSEMENT - ON");
                }

                //issue ticket
                List<issueticketresponse> response = await enhancedAirTicketService.
                                                                IssueTicket(
                                                                    request,
                                                                    pcc.PccCode,
                                                                    token,
                                                                    ticketingpcc,
                                                                    ticketingprinter,
                                                                    printerbypass,
                                                                    bcode,
                                                                    enableextendedendo);

                var ticketData = await ParseSabreTicketData(response, request, statefultoken, pcc, ticketingpcc, pnr, bcode, token, user);

                if (agent?.ConsolidatorId == "expresstravelgroup")
                {
                    //adding ETG Agency commission PNR remarks
                    await AddAgentCommissionRemarks(ticketData, request, ticketingpcc, user, token);
                }

                var transactionData = new IssueTicketTransactionData
                {
                    SessionId = request.SessionID,
                    User = user,
                    Pnr = pnr,
                    TicketingResult = ticketData
                };

                if (!string.IsNullOrEmpty(request.MerchantData?.OrderID))
                {
                    transactionData.TicketingResult.OrderId = request.MerchantData.OrderID;
                }
                else
                {
                    //Get order sequence in transaction database
                    try
                    {
                        var orderSequence = await _getOrderSequenceFailedRetryPolicy.ExecuteAsync(() => _ordersTransactionDataSource.GetOrderSequence());
                        transactionData.TicketingResult.OrderId = orderSequence;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"GETORDERSEQUENCE_ERROR : {ex.Message} : PNR : {transactionData.TicketingResult.Locator} [EXCEPTION]:", ex);
                        transactionData.TicketingResult.OrderId = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{new Random().Next(9000, 10000)}";
                    }
                }

                //merchant
                transactionData = await HandleMerchantPayment(request.SessionID, request, transactionData);

                if (transactionData.TicketingResult.Tickets.IsNullOrEmpty() && !transactionData.TicketingResult.Errors.IsNullOrEmpty())
                {
                    throw new GDSException(
                                "ALL_TICKETS_FAILED",
                                string.Join(
                                    Environment.NewLine,
                                    transactionData.TicketingResult.Errors.Select(s => s.Error).Distinct()));
                }

                //Queue tickets on SQS for invoicing
                #region Invoicing              
                //Queue back for ETG
                #region QueueBack
                //if (pcc.PccCode == "0M4J")
                //{
                //    user.Agent.TicketingQueue = "30";
                //}

                //if (!(ticketData.Tickets.IsNullOrEmpty() || string.IsNullOrEmpty(user.Agent.TicketingQueue)))
                //{
                //    await queuePlaceService.
                //                QueueMove(
                //                    statefultoken, 
                //                    pcc, 
                //                    user.Agent.TicketingQueue, 
                //                    agentpcc, 
                //                    new List<string>() { ticketData.Locator });
                //}
                #endregion

                //Download to backoffice
                if (!string.IsNullOrEmpty(agent.CustomerNo) && (backofficeProcess?.DownloadDocuments ?? false))
                {
                    await _backofficeDataSource.Book(transactionData);
                }
                else
                {
                    await _notificationHelper.NotifyTicketIssued(transactionData);
                }

                #endregion

                return ticketData;
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

        private async Task<List<string>> getagentpccs(string agentid, string pcc, string sessionId)
        {
            if (string.IsNullOrEmpty(agentid))
            {
                return new List<string>() { pcc };
            }

            var agentpcc = await _agentPccDataSource.RetrieveAgentPccs(agentid, sessionId);

            if (agentpcc == null)
            {
                throw new AeronologyException("50000015", "Agent PCC not found");
            }

            return agentpcc.PccList.Select(s => s.PccCode).ToList();
        }

        private async Task QuotingForTickting(Pcc pcc, IssueExpressTicketRQ request, string statefultoken, IEnumerable<IssueExpressTicketQuote> pendingquotes, PNR pnr, string ticketingpcc, string contextID)
        {
            List<Quote> quotes = new List<Quote>();
            List<IssueExpressTicketQuote> newissueticketquotes = new List<IssueExpressTicketQuote>();

            //Generate Quoting request
            GetQuoteRQ getQuoteRQ = new GetQuoteRQ()
            {
                GDSCode = request.GDSCode,
                Locator = request.Locator,
                SelectedPassengers = pendingquotes.Select(s => s.Passenger).Distinct().ToList(),
                SelectedSectors = pendingquotes.
                                        First().
                                        Sectors.
                                        Select(s => new SelectedQuoteSector() 
                                        { 
                                            SectorNo = s.PQSectorNo 
                                        }).
                                        ToList(),
                PriceCode = string.IsNullOrEmpty(request.PriceCode) ?
                                pendingquotes.FirstOrDefault(f => !string.IsNullOrEmpty(f.PriceCode))?.PriceCode :
                                request.PriceCode,
            };

            //Quote
            quotes = await _enhancedAirBookService.PricePNRForTicketing(getQuoteRQ, statefultoken, pcc, pnr, ticketingpcc);

            var user = await session.GetSessionUser(statefultoken);

            //Price check
            if (Math.Abs((pendingquotes.Sum(s => s.BaseFare) + pendingquotes.Sum(s => s.TotalTax)) - (quotes.Sum(s => s.BaseFare) + quotes.Sum(s => s.TotalTax))) > (user == null || user.Consolidator == null ? 20 : user.Consolidator.ticketpricedeviationtolerance))
            {
                //ignore session
                await _sabreCommandService.ExecuteCommand(statefultoken, pcc, "I");

                throw new AeronologyException("PRICE_MISSMATCH", string.Format(
                    "Price missmatch found between quoted price({0}) and ticketed price({1}). Please re-quote and try again.",
                    (pendingquotes.Sum(s => s.BaseFare) + pendingquotes.Sum(s => s.TotalTax)).ToString(),
                    (quotes.Sum(s => s.BaseFare) + quotes.Sum(s => s.TotalTax)).ToString()));
            }

            //redislpay price quotes
            await RedisplayGeneratedQuotes(statefultoken, quotes);

            //workout fuel surcharge taxcode
            GetFuelSurcharge(quotes);

            if (quotes.First().BspCommissionRate.HasValue)
            {
                logger.LogInformation($"BSP commission from GDS: {quotes.First().BspCommissionRate.Value}");
            }

            //check for tourcode before commission calculation
            bool notourcode = quotes.All(a => string.IsNullOrEmpty(a.TourCode));

            //Get commission data
            CalculateCommission(quotes, pnr, ticketingpcc, request.SessionID);

            //commission or fee check
            if (!pendingquotes.Select(q => q.BSPCommissionRate).All(quotes.Select(s => s.BspCommissionRate).Contains))
            {
                throw new AeronologyException("BSP_COMM_MISSMATCH", "Tickets not issued. BSP commission discrepancy found. Please contact the consolidator\\ticketing center for more information.");
            }
            if (!pendingquotes.Select(q => q.AgentCommissionRate).All(quotes.Select(s => s.AgentCommissionRate).Contains))
            {
                throw new AeronologyException("AGENT_COMM_MISSMATCH", "Tickets not issued. Agent commission discrepancy found. Please contact the consolidator\\ticketing center for more information.");
            }
            if (!pendingquotes.Select(q => q.Fee).All(quotes.Select(s => s.Fee).Contains))
            {
                throw new AeronologyException("AGENT_FEE_MISSMATCH", "Tickets not issued. Fee discrepancy found. Please contact the consolidator\\ticketing center for more information.");
            }

            //re-map all fields to avoid retetion of old data
            newissueticketquotes.
                AddRange(
                    quotes.
                    Select(q =>
                        new IssueExpressTicketQuote()
                        {
                            QuoteNo = q.QuoteNo,
                            BaseFare = q.BaseFare,
                            TotalFare = q.TotalFare,
                            AgentCommissionRate = q.AgentCommissionRate,
                            BSPCommissionRate = q.BspCommissionRate,
                            AgentCommissions = q.AgentCommissions,
                            Fee = q.Fee,
                            FeeGST = q.FeeGST,
                            Passenger = new QuotePassenger()
                            {
                                NameNumber = q.QuotePassenger.NameNumber,
                                PassengerName = q.QuotePassenger.PassengerName,
                                PaxType = q.QuotePassenger.PaxType,
                                PriceIt = q.QuotePassenger.PriceIt,
                                DOB = getQuoteRQ.SelectedPassengers.First(f => f.NameNumber == q.QuotePassenger.NameNumber).DOB,
                                DOBChanged = getQuoteRQ.SelectedPassengers.First(f => f.NameNumber == q.QuotePassenger.NameNumber).DOBChanged,
                                FormOfPayment = getQuoteRQ.SelectedPassengers.First(f => f.NameNumber == q.QuotePassenger.NameNumber).FormOfPayment,
                            },
                            Sectors = q.QuoteSectors,
                            SectorCount = q.SectorCount,
                            PlatingCarrier = q.ValidatingCarrier,
                            TotalTax = q.TotalTax,
                            Route = q.Route,
                            PriceIt = request.
                                        Quotes.
                                        First(iq => iq.Passenger.NameNumber == q.QuotePassenger.NameNumber).
                                        PriceIt,
                            Endorsements = q.Endorsements,
                            MerchantFee = request.MerchantData == null ?
                                            0.00M :
                                            request.MerchantData.MerchantFeeData.First(f => f.DocumentType == Models.DocumentType.Quote &&
                                           f.DocumentNumber == request.Quotes.First(iq => iq.Passenger.NameNumber == q.QuotePassenger.NameNumber).QuoteNo).
                                            MerchantFee,
                            TourCode = q.TourCode,
                            ApplySupressITFlag = notourcode && quotes.Any(a => !string.IsNullOrEmpty(a.TourCode))
                        }).ToList());

            if (!pendingquotes.Any(a => a.FiledFare))
            {
                //End transaction
                await enhancedEndTransService.EndTransaction(statefultoken, contextID, agent?.FullName ?? "Aeronology", true);
            }

            //Workout the need for inclusion of NameSelection section in ticketing request
            PopulatePartialIssueFlag(request, newissueticketquotes);

            //Remove the client added quotes
            request.Quotes = new List<IssueExpressTicketQuote>();
            request.Quotes.AddRange(newissueticketquotes);

            //Agent Price
            quotes.
                ForEach(f => f.AgentPrice = f.QuotePassenger.FormOfPayment == null ?
                                            f.DefaultPriceItAmount - f.Commission :
                                            //Cash Only
                                            f.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CA ?
                                                f.DefaultPriceItAmount - f.Commission :
                                                //Part Cash part credit
                                                f.QuotePassenger.FormOfPayment.PaymentType == PaymentType.CC && f.QuotePassenger.FormOfPayment.CreditAmount < f.TotalFare ?
                                                    f.TotalFare - f.QuotePassenger.FormOfPayment.CreditAmount + f.Fee + (f.FeeGST.HasValue ? f.FeeGST.Value : 0.00M) - f.Commission :
                                                    //Credit only
                                                    f.Fee + (f.FeeGST.HasValue ? f.FeeGST.Value : 0.00M) - f.Commission);

            //update the pnr quotes
            pnr.Quotes = quotes;

        }

        private static void PopulatePartialIssueFlag(IssueExpressTicketRQ request, List<IssueExpressTicketQuote> newissueticketquotes)
        {
            //Pax type base 
            newissueticketquotes.
                GroupBy(g => g.QuoteNo).
                Where(grp => !request.
                                Quotes.
                                Where(w => w.Passenger.PaxType == grp.First().Passenger.PaxType).
                                Select(s => s.Passenger.NameNumber).
                                All(a => grp.Select(s => s.Passenger.NameNumber).Contains(a))).
                 SelectMany(m => m).
                 ToList().
                 ForEach(quo => quo.PartialIssue = true);

            //Form of payment base
            newissueticketquotes.
                GroupBy(g => new { g.QuoteNo, g.Passenger.FormOfPayment.PaymentType }).
                Where(w => w.Count() > 1).
                Select(s => s.ToList()).
                Where(w => !w.All(a => a.Passenger.FormOfPayment.CardNumber == w.First().Passenger.FormOfPayment.CardNumber ||
                                      a.Passenger.FormOfPayment.CreditAmount == w.First().Passenger.FormOfPayment.CreditAmount)).
                SelectMany(m => m).
                ToList().
                ForEach(quo => quo.PartialIssue = true);
        }


        private void CreditCheck(IssueExpressTicketRQ request, string customerNo, string sesionId)
        {
            //Credit Check    
            var agencyCreditLimitResponse = _backofficeDataSource.GetAvailaCreditLimit(customerNo, sesionId).GetAwaiter().GetResult();

            if (agencyCreditLimitResponse?.Error != null)
            {
                throw new AeronologyException("50000052", $"An error occurred getting available credit. PS Error Message : {agencyCreditLimitResponse.Error.Detail}");
            }

            if (agencyCreditLimitResponse?.AgencyCreditLimit != null && agencyCreditLimitResponse.CreditLimitCheckRequired)
            {
                if (agencyCreditLimitResponse.AgencyCreditLimit.Amount < ((request.Quotes?.Sum(s => s.TotalFare) ?? 0) + (request.EMDs?.Sum(s => s.Total) ?? 0)))
                {
                    logger.LogInformation($"Customer no. {customerNo} has available credit limit {agencyCreditLimitResponse.AgencyCreditLimit.Amount}. SessionId {sesionId}. Issue ticket request PNR {request.Locator}.");
                    throw new AeronologyException("LOW_CREDIT", "Low credit available.");
                }
            }
        }

        private Task ManualBuild(Pcc pcc, IssueExpressTicketRQ request, string statefultoken, IEnumerable<IssueExpressTicketQuote> manualquotes, PNR pnr, string ticketingpcc, string contextID)
        {
            throw new NotImplementedException();
        }

        private static void ReconstructRequestFromKeys(IssueExpressTicketRQ request)
        {
            if (!request.IssueTicketQuoteKeys.IsNullOrEmpty())
            {
                List<IssueExpressTicketQuote> requestquotes = new List<IssueExpressTicketQuote>();
                request.
                    IssueTicketQuoteKeys.
                    ForEach(f =>
                    {
                        string key = f.DecodeBase64();
                        requestquotes.Add(
                            JsonConvert.
                                    DeserializeObject<IssueExpressTicketQuote>(key));
                    });


                var quotequery = from quo in request.Quotes
                                 let rqquo = requestquotes.First(f => quo.QuoteNo == f.QuoteNo &&
                                                                      quo.Passenger.PassengerName == f.Passenger.PassengerName &&
                                                                      quo.TotalTax == f.TotalTax)
                                 select new IssueExpressTicketQuote()
                                 {
                                     QuoteNo = quo.QuoteNo,
                                     Passenger = new QuotePassenger()
                                     {
                                         DOB = quo.Passenger.DOB,
                                         DOBChanged = quo.Passenger.DOBChanged,
                                         NameNumber = quo.Passenger.NameNumber,
                                         PassengerName = quo.Passenger.PassengerName,
                                         PaxType = quo.Passenger.PaxType,
                                         Passport = quo.Passenger.Passport,
                                         PriceIt = quo.Passenger.PriceIt,
                                         FormOfPayment = new FOP()
                                         {
                                             BCode = quo.FiledFare ? rqquo.Passenger.FormOfPayment.BCode : quo.Passenger.FormOfPayment.BCode,
                                             CardNumber = quo.Passenger.FormOfPayment.CardNumber,
                                             ExpiryDate = quo.Passenger.FormOfPayment.ExpiryDate,
                                             CardType = quo.Passenger.FormOfPayment.CardType,
                                             PaymentType = quo.Passenger.FormOfPayment.PaymentType,
                                             CreditAmount = quo.Passenger.FormOfPayment.CreditAmount
                                         }
                                     },
                                     PriceIt = quo.PriceIt,
                                     PartialIssue = quo.PartialIssue,
                                     BaseFare = rqquo.BaseFare,
                                     FiledFare = rqquo.FiledFare,
                                     PendingSfData = rqquo.PendingSfData,
                                     PlatingCarrier = rqquo.PlatingCarrier,
                                     Route = rqquo.Route,
                                     SectorCount = rqquo.SectorCount,
                                     Sectors = rqquo.Sectors,
                                     TotalFare = rqquo.TotalFare,
                                     TotalTax = rqquo.TotalTax,
                                     AgentCommissionRate = rqquo.AgentCommissionRate,
                                     BSPCommissionRate = rqquo.BSPCommissionRate,
                                     AgentCommissions = rqquo.AgentCommissions,
                                     Fee = rqquo.Fee,
                                     FeeGST = rqquo.FeeGST,
                                     PriceCode = rqquo.PriceCode,
                                     TourCode = rqquo.TourCode,
                                     TicketingPCC = rqquo.TicketingPCC,
                                     Endorsements = rqquo.Endorsements,
                                     BCode = rqquo.BCode,
                                     MerchantFee = request.MerchantData == null ?
                                                        0.00M :
                                                        request.
                                                            MerchantData.
                                                            MerchantFeeData.
                                                            First(f =>
                                                                f.DocumentType == Models.DocumentType.Quote &&
                                                                f.DocumentNumber == rqquo.QuoteNo).
                                                            MerchantFee,
                                     ApplySupressITFlag = rqquo.ApplySupressITFlag
                                 };

                request.Quotes = quotequery.ToList();
            }

            if (!request.IssueTicketEMDKeys.IsNullOrEmpty())
            {
                List<IssueExpressTicketEMD> requestemds = new List<IssueExpressTicketEMD>();
                request.
                    IssueTicketEMDKeys.
                    ForEach(f =>
                    {
                        string key = f.DecodeBase64();
                        requestemds.Add(
                            JsonConvert.
                                    DeserializeObject<IssueExpressTicketEMD>(key));
                    });

                var emdquery = from emd in request.EMDs
                               let rqemd = requestemds.First(f => emd.EMDNo == f.EMDNo)
                               select new IssueExpressTicketEMD()
                               {
                                   EMDNo = rqemd.EMDNo,
                                   Commission = rqemd.Commission,
                                   Fee = rqemd.Fee,
                                   FeeGST = rqemd.FeeGST,
                                   PassengerName = rqemd.PassengerName,
                                   PlatingCarrier = rqemd.PlatingCarrier,
                                   Route = rqemd.Route,
                                   Ticketed = rqemd.Ticketed,
                                   TotalTax = rqemd.TotalTax,
                                   Total = rqemd.Total,
                                   RFISC = rqemd.RFISC,
                                   FormOfPayment = emd.FormOfPayment,
                                   PriceIt = emd.PriceIt,
                                   SectorCount = rqemd.SectorCount
                               };

                request.EMDs = emdquery.ToList();
            }
        }

        private static string GetTicketingPrinter(string ticketingprinter, string pcc)
        {
            if (!string.IsNullOrEmpty(ticketingprinter)) { return ticketingprinter; }

            return pcc == "0M4J" ?
                                "B8DDCC" :
                            pcc == "G4AK" ?
                                "B8DDCC" :
                            pcc == "F7Z7" ?
                                "F113D3" :
                            pcc == "R6G8" ?
                                "3B74F1" :
                            pcc == "5DXJ" ?
                                "227AB0" :
                                throw new AeronologyException("5000025", "Printer not found for ticketing!");
        }

        private static string GetPrinterByPass(string printerbypass, string pcc)
        {
            if (!string.IsNullOrEmpty(printerbypass)) { return printerbypass; }

            return "0M4J|F7Z7|G4AK".Contains(pcc) ?
                        "AU" :
                        pcc == "R6G8" ?
                            "HK" :
                        pcc == "5DXJ" ?
                            "SG" :
                            throw new AeronologyException("5000022", "Printer bypass not found");
        }

        private async Task<IssueTicketTransactionData> HandleMerchantPayment(string sessionid, IssueExpressTicketRQ request, IssueTicketTransactionData tickettransData)
        {
            IssueTicketTransactionData returntickettransactiondata = tickettransData;
            MerchantLambdaResponse result = null;

            //Not a merchant payment
            if (request.MerchantData == null)
            {
                return returntickettransactiondata;
            }


            //CancelHold - Reverse fund reserved
            if (returntickettransactiondata.TicketingResult.Tickets.IsNullOrEmpty())
            {
                Stopwatch sw = Stopwatch.StartNew();
                logger.LogInformation("CancelHold invoked");
                result = await CancelHold(sessionid, request);
                sw.Stop();
                logger.LogInformation($"CancelHold completed in {sw.ElapsedMilliseconds}ms");
                var resp = result != null && result.Success ? "success" : "faliure";
                logger.LogInformation($"CancelHold is a {resp}.");
                return returntickettransactiondata;
            }


            //CapturePayment - Process the payment on card
            try
            {
                logger.LogInformation("Initiate capture payment.");

                //workout amount to collect
                decimal merchantfee = request.MerchantData.MerchantFeeData.Sum(s => s.MerchantFee);
                decimal merchantamount = GetAmountOnMerchant(returntickettransactiondata.TicketingResult, request);

                IEnumerable<int> quotenos = tickettransData.TicketingResult.Tickets.Where(w => w.DocumentType == "TKT")?.Select(s => s.QuoteRefNo);
                IEnumerable<int> emdnos = tickettransData.TicketingResult.Tickets.Where(w => w.DocumentType == "EMD")?.SelectMany(s => s.EMDNumber);

                if (((!quotenos.IsNullOrEmpty() && request.Quotes.Select(q => q.QuoteNo).All(a => quotenos.Contains(a))) &&
                      (!emdnos.IsNullOrEmpty() && request.EMDs != null && request.EMDs.Select(emd => emd.EMDNo).All(a => emdnos.Contains(a)))))
                {
                    //recalculate merchant fee
                    merchantfee = merchantamount * request.MerchantData.MerchantFeeRate / 100;
                }

                //process the payment
                string desc = $"{returntickettransactiondata.TicketingResult.GDSCode}-{returntickettransactiondata.TicketingResult.Locator}-{string.Join(",", returntickettransactiondata.TicketingResult.Tickets.Select(s => s.DocumentNumber))}";
                Stopwatch sw = Stopwatch.StartNew();
                logger.LogInformation("CapturePayment invoked.");
                result = await merchantDataSource.
                                        CapturePayment(
                                            sessionid,
                                            agent?.ConsolidatorId ?? "travelconnexion",
                                            request.MerchantData.PaymentSessionID,
                                            request.MerchantData.OrderID,
                                            merchantamount + merchantfee,
                                            desc);
                sw.Stop();
                logger.LogInformation($"CapturePayment completed in {sw.ElapsedMilliseconds}ms.");
                var resp = result != null && result.Success ? "success" : "faliure";
                logger.LogInformation($"CapturePayment is a {resp}.");

                if (!(result == null || result.Success))
                {
                    logger.LogInformation("Merchant payment failed - void the tickets");

                    //Void the tickets as payment was not successfull
                    await VoidTicket(new VoidTicketRequest()
                    {
                        GDSCode = request.GDSCode,
                        Locator = request.Locator,
                        Tickets = returntickettransactiondata.
                                    TicketingResult.
                                    Tickets.
                                    Select(tkt => new VoidTicket()
                                    {
                                        DocumentNumber = tkt.DocumentNumber,
                                        DocumentType = tkt.DocumentType,
                                        IssuingPCC = tkt.IssuingPCC
                                    }).
                                    ToList()
                    });

                    //CancelHold - Reverse fund reserved
                    sw = Stopwatch.StartNew();
                    logger.LogInformation("CancelHold invoked");
                    result = await CancelHold(sessionid, request);
                    sw.Stop();
                    logger.LogInformation($"CancelHold completed in {sw.ElapsedMilliseconds}ms");
                    resp = result != null && result.Success ? "success" : "faliure";
                    logger.LogInformation($"CancelHold is a {resp}.");

                    throw new AeronologyException("VOID_TICKET_MERCHANT_FAIL", "TICKETS DID NOT GET ISSUED. Merchant payment failed. Please rectify the issue or use an alternative payment option to proceed.");
                }

                PopulateMerchantFOP(request, returntickettransactiondata, result, merchantfee);
            }
            catch (AeronologyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError($"Merchant capture payment - {ex.Message}");
                returntickettransactiondata.
                    TicketingResult.
                    Tickets.
                    ForEach(f => f.Warning = new WebtopWarning()
                    {
                        code = "MERCHANT_FAIL",
                        message = $"Merchant payment was unsuccessful for tickets {string.Join(",", returntickettransactiondata.TicketingResult.Tickets.Select(s => s.DocumentNumber))}. " +
                                           $"Please void the tickets or make other arrangements for payment."
                    });
            }

            return returntickettransactiondata;
        }

        private static void PopulateMerchantFOP(IssueExpressTicketRQ request, IssueTicketTransactionData returntickettransactiondata, MerchantLambdaResponse result, decimal merchantfee)
        {
            returntickettransactiondata.TicketingResult.ApprovalCode = result.ApprovalCode;
            returntickettransactiondata.TicketingResult.MerchantFee = Math.Round(merchantfee, 2, MidpointRounding.AwayFromZero);

            //tickets
            returntickettransactiondata.
                TicketingResult.
                Tickets.
                Where(w => w.DocumentType == "TKT").
                ToList().
                ForEach(tkt =>
                {
                    var quote = request.Quotes.First(f => f.QuoteNo == tkt.QuoteRefNo && f.Passenger.PassengerName == tkt.PassengerName);

                    tkt.MerchantFOP = new MerchantFOP()
                    {
                        PaymentSessionID = request.MerchantData.PaymentSessionID,
                        OrderID = request.MerchantData.OrderID,
                        CardNumber = result.CardNumber,
                        CardType = result.CardType,
                        ExpiryDate = result.CardExpiry,
                        TotalCreditAmount = quote.Passenger.FormOfPayment.CreditAmount,
                        CashAmount = 0.00M,
                        MerchantFee = quote.MerchantFee
                    };
                });

            //emds
            returntickettransactiondata.
                TicketingResult.
                Tickets.
                Where(w => w.DocumentType == "EMD").
                ToList().
                ForEach(tkt =>
                {
                    var firstemd = request.EMDs.First(f => tkt.EMDNumber.Contains(f.EMDNo));
                    tkt.MerchantFOP = new MerchantFOP()
                    {
                        PaymentSessionID = request.MerchantData.PaymentSessionID,
                        OrderID = request.MerchantData.OrderID,
                        CardNumber = result.CardNumber,
                        CardType = result.CardType,
                        ExpiryDate = result.CardExpiry,
                        TotalCreditAmount = request.EMDs.Where(f => tkt.EMDNumber.Contains(f.EMDNo)).Sum(s => s.FormOfPayment.CreditAmount),
                        CashAmount = 0.00M,
                        MerchantFee = request.
                                        EMDs.
                                        Where(f => tkt.EMDNumber.Contains(f.EMDNo)).
                                        Sum(s => request.MerchantData.MerchantFeeData.First(f => f.DocumentType == Models.DocumentType.EMD && f.DocumentNumber == s.EMDNo).MerchantFee)
                    };
                });
        }

        private decimal GetAmountOnMerchant(IssueExpressTicketRS ticketData, IssueExpressTicketRQ request)
        {
            decimal merchantamount = 0.00M;
            foreach (var tkt in ticketData.Tickets)
            {
                if (tkt.DocumentType == "TKT" && tkt.QuoteRefNo != -1)
                {
                    var quote = request.
                                        Quotes.
                                        First(f => f.QuoteNo == tkt.QuoteRefNo && f.Passenger.PassengerName == tkt.PassengerName);

                    merchantamount += (quote.Passenger.FormOfPayment.CreditAmount - quote.MerchantFee);
                }

                if (tkt.DocumentType == "EMD" && !tkt.EMDNumber.IsNullOrEmpty())
                {
                    merchantamount += tkt.
                                        EMDNumber.
                                        Sum(s => request.EMDs.First(f => f.EMDNo == s).FormOfPayment.CreditAmount -
                                                    request.MerchantData.MerchantFeeData.Where(w => w.DocumentType == Models.DocumentType.EMD &&
                                                    w.DocumentNumber == s).Sum(e => e.MerchantFee));
                }
            }

            return merchantamount;
        }

        private async Task<MerchantLambdaResponse> CancelHold(string sessionid, IssueExpressTicketRQ request)
        {
            int count = 1;
            MerchantLambdaResponse result = null;
            try
            {
                //if no tickets issued reverse the fund block
                logger.LogInformation($"CancelHold - attempt {count}");
                result = await merchantDataSource.
                                        CancelHold(
                                            sessionid,
                                            agent?.ConsolidatorId ?? "travelconnexion",
                                            request.MerchantData.PaymentSessionID,
                                            request.MerchantData.OrderID);
                string resp = result != null && result.Success ? "success" : "faliure";
                logger.LogInformation($"CancelHold is a {resp}.");

                //retry
                while (result != null && !result.Success && count < 3)
                {
                    //apply 2 sec delay
                    await Task.Delay(2000);

                    logger.LogInformation($"CancelHold - attempt {count + 1}");
                    result = await merchantDataSource.
                                            CancelHold(
                                                sessionid,
                                                agent?.ConsolidatorId ?? "travelconnexion",
                                                request.MerchantData.PaymentSessionID,
                                                request.MerchantData.OrderID);
                    resp = result != null && result.Success ? "success" : "faliure";
                    logger.LogInformation($"CancelHold is a {resp}.");

                    count++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Merchant cancel hold - {ex.Message}");
                //error not throw back as bank will release funds
            }

            return result;
        }

        private void GetStoredCards(IssueExpressTicketRQ request, GetReservationRS getReservationRS)
        {
            if (!request.Quotes.IsNullOrEmpty() &&
                 request.Quotes.Any(q => q.Passenger.FormOfPayment.PaymentType == PaymentType.CC && q.Passenger.FormOfPayment.CardNumber.Contains("XXX")) ||
                !request.EMDs.IsNullOrEmpty() &&
                request.EMDs.Any(q => q.FormOfPayment.PaymentType == PaymentType.CC && q.FormOfPayment.CardNumber.Contains("XXX")))
            {
                GetStoredCardDetails(request, getReservationRS);
            }
        }

        private List<Quote> GetQuotes(PriceQuoteXElement sabrequotes, PNR pnr, bool includeexpiredquotes, DateTime pcclocaltime, string sabresessionID, string sessionid)
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
                            ROE = q.PQ.FareCalculation.LastMatch(@"ROE\s*([\d\.]+)", "1"),
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
                            PricingCommand = q.PQ.PricingCommand.Mask().EncodeBase64(),
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
                                    sessionid);
            }
            catch { } //Errors will not be thrown as we need file -ares with errors to be displayed

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
                GetTicketingPCCExpiaryIssueTicketKey(pnr, issuablequotes, ApplySupressITFlag, sessionid);
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
                    AgentIata = user?.Agent?.FinanceDetails?.IataNumber??"",
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
                    quote.Errors = new List<WebtopError>()
                    {
                        new WebtopError()
                        {
                            code = "COMM_REC_NOT_FOUND",
                            message = $"(Context ID - {_commissionDataService.ContextID}){Environment.NewLine}Commission or fee record not found." +
                                                $" Please contact the consolidator\\ticket office for more information."
                        }
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

    internal class PQTextResp
    {
        public int PQNo { get; set; }
        public string PassengerType { get; set; }
        public decimal? BSPCommission { get; set; }
        public string TourCode { get; set; }
    }
}
