using GetElectronicDocumentService;
using GetReservation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
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
using JsonSerializer = System.Text.Json.JsonSerializer;

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
        private readonly VoidTicketService voidTicketService;
        private readonly SabreUpdatePNRService updatePNRService;
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
            VoidTicketService _voidTicketService,
            SabreUpdatePNRService _updatePNRService,
            IDataProtectionProvider dataProtectionProvider,
            IAgentPccDataSource agentPccDataSource,
            SessionDataSource session,
            INotificationHelper notificationHelper,
            IMerchantDataSource _merchantDataSource,
            IBCodeDataSource _bCodeDataSource,
            IBackofficeDataSource backofficeDataSource,
            IOptions<BackofficeOptions> backofficeOptions,
            IOrdersTransactionDataSource ordersTransactionDataSource)
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
            voidTicketService = _voidTicketService;
            updatePNRService = _updatePNRService;
            _backofficeOptions = backofficeOptions?.Value;
            _ordersTransactionDataSource = ordersTransactionDataSource;
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

            if (user?.Agent != null)
            {
                user.Agent.CustomerNo = agent.CustomerNo;
            }
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
            agent = null;
            if (!string.IsNullOrEmpty(request.AgentID))
            {
                logger.LogInformation($"AgentID not null. {request.AgentID}");
                agent = await getAgentData(
                            request.SessionID,
                            user.ConsolidatorId,
                            request.AgentID,
                            pcc.PccCode);
            }

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

        internal async Task<List<WebtopWarning>> ValidateCommission(ValidateCommissionRQ rq, string contextid)
        {
            List<WebtopWarning> webtopWarnings = new List<WebtopWarning>();
            user = await session.GetSessionUser(rq.SessionID);
            pcc = await _consolidatorPccDataSource.GetWebServicePccByGdsCode("1W", contextid, rq.SessionID);
            agent = await getAgentData(
                                    rq.SessionID,
                                    user.ConsolidatorId,
                                    rq.AgentID,
                                    pcc.PccCode);
            string ticketingpcc = GetTicketingPCC(agent?.TicketingPcc, pcc.PccCode);

            List<CommissionData> oldcommdata = rq.
                                                Quotes.
                                                Select(q => new CommissionData()
                                                {
                                                    QuoteNo = q.QuoteNo,
                                                    BspCommissionRate = q.BspCommissionRate,
                                                    AgentCommissionRate = q.AgentCommissionRate
                                                }).
                                                ToList();

            //workout fuel surcharge taxcode
            GetFuelSurcharge(rq.Quotes);

            //calculate commission
            CalculateCommission(rq.Quotes, rq.Pnr, ticketingpcc, rq.SessionID);

            //check for any missmatch in commission
            rq.
                Quotes.
                ForEach(q => 
                {
                    CommissionData oldquotecomm = oldcommdata.First(f => f.QuoteNo == q.QuoteNo);

                    if((oldquotecomm.BspCommissionRate.HasValue || q.BspCommissionRate.HasValue) &&
                       oldquotecomm.BspCommissionRate.Value != q.BspCommissionRate.Value)
                    {
                        webtopWarnings.
                            Add(new WebtopWarning()
                            {
                                code = "BSP_COMM_MISSMATCH",
                                message = $"Bsp commission rate missmatch (system:{q.BspCommissionRate.Value}, user: {oldquotecomm.BspCommissionRate.Value}). Please verify before proceeding to ticketing."
                            });
                    }

                    if((oldquotecomm.AgentCommissionRate.HasValue || q.AgentCommissionRate.HasValue) &&
                       q.AgentCommissionRate.Value != oldquotecomm.AgentCommissionRate)
                    {
                        webtopWarnings.
                            Add(new WebtopWarning()
                            {
                                code = "AGENT_COMM_MISSMATCH",
                                message = $"Agency commission rate missmatch (system:{q.AgentCommissionRate.Value}, user: {oldquotecomm.AgentCommissionRate.Value}). Please verify before proceeding to ticketing."
                            });
                    }
                });

            return webtopWarnings;
        }

        public async Task<PNR> GetPNR(string sabresessionid, string sessionid, string locator, bool withpnrvalidation = false, bool getStoredCards = false, bool includeQuotes = false, bool includeexpiredquote = false, string ticketingpcc = "")
        {
            var pnrAccessKey = $"{ticketingpcc}-{locator}-pnr".EncodeBase64();
            var cardAccessKey = $"{ticketingpcc}-{locator}-card".EncodeBase64();

            //get reservation
            GetReservationRS response = await _getReservationService.RetrievePNR(locator, sabresessionid, pcc, ticketingpcc);

            //booking pcc
            string bookingpcc = ((ReservationPNRB)response.Item).POS.Source.PseudoCityCode;

            PNR pnr = new PNR();
            List<PNRAgent> agents = new List<PNRAgent>();
            if (agent == null || string.IsNullOrEmpty(agent.AgentId))
            {
                logger.LogInformation("Invoke GetAgents()");
                agents = GetAgents(sessionid, bookingpcc);
                if (agents.IsNullOrEmpty() || agents.Count() == 1)
                {
                    agent = await getAgentData(
                                    sessionid,
                                    user.ConsolidatorId,
                                    agents.First().AgentId,
                                    pcc.PccCode);
                }

                logger.LogInformation($"{agents.Count()} found.");

                if (agents.Count > 1)
                {
                    pnr.Agents = agents;
                    pnr.BookedPCC = bookingpcc;
                    return pnr;
                }
            }

            logger.LogInformation(sessionid);
            logger.LogInformation($"Agent {agent.AgentId} found.");


            if (agent != null && !string.IsNullOrEmpty(agent.AgentId))
            {
                pnr = ParseSabrePNR(response, sabresessionid, sessionid, includeQuotes, includeexpiredquote);

                if (withpnrvalidation)
                {
                    //PNR validation
                    pnr.InvokePostPNRRetrivalActions();
                }

                if (!agents.IsNullOrEmpty())
                {
                    pnr.Agents = agents;
                }
                else
                {
                    pnr.Agents = new List<PNRAgent>()
                    {
                        new PNRAgent()
                        {
                            AgentId = agent.AgentId,
                            Name = agent.Name
                        }
                    };
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
            }

            return pnr;
        }

        public async Task<string> GetPNRText(SearchPNRRequest request, string contextID)
        {
            pcc = await _consolidatorPccDataSource.GetWebServicePccByGdsCode("1W", contextID, request.SessionID);
            SabreSession sabreSession = null;
            try
            {
                //Obtain session
                sabreSession = await _sessionCreateService.CreateStatefulSessionToken(pcc, request.SearchText, true);
                string token = sabreSession.SessionID;
                Task<string> pnrtext = _sabreCommandService.
                                                GetPNRText(token, pcc, request.SearchText, pcc.PccCode);

                Task<GetQuoteTextResponse> getQuoteTextResponse = GetQuoteText(new GetQuoteTextRequest()
                {
                    GDSCode = request.GDSCode,
                    Locator = request.SearchText
                },
                contextID);

                await Task.WhenAll(pnrtext, getQuoteTextResponse);

                string text = pnrtext.Result;
                GetQuoteTextResponse quotetextres = getQuoteTextResponse == null ? null : getQuoteTextResponse.Result;

                if (quotetextres != null &&
                   string.IsNullOrEmpty(quotetextres.QuoteError) &&
                   !quotetextres.QuoteData.IsNullOrEmpty() &&
                   quotetextres.QuoteData.Any(a => !a.Expired))
                {
                    text += Environment.NewLine;
                    text += Environment.NewLine;
                    text += Environment.NewLine;
                    text += string.
                                Join(
                                    Environment.NewLine,
                                    quotetextres.
                                    QuoteData.
                                    Where(w => !w.Expired).
                                    Select(s => s.QuoteText));
                }

                return text.ReplaceAllSabreSpecialChar().Mask();
            }
            finally
            {
                if (sabreSession != null && string.IsNullOrEmpty(sabreSession.SessionID))
                {
                    await _sessionCloseService.SabreSignout(sabreSession.SessionID, pcc);
                }
            }
        }

        public async Task<GetQuoteTextResponse> GetQuoteText(GetQuoteTextRequest rq, string contextID)
        {
            pcc = await _consolidatorPccDataSource.GetWebServicePccByGdsCode("1W", contextID, rq.SessionID);
            SabreSession sabreSession = null;
            try
            {
                //Obtain session
                sabreSession = await _sessionCreateService.CreateStatefulSessionToken(pcc, rq.Locator, true);

                string token = sabreSession.SessionID;

                //GetPNR
                GetReservationRS result = await _getReservationService.
                                                    RetrievePNR(rq.Locator, token, pcc);

                List<SegmentTypePNRBSegmentAir> airsegs = ((ReservationPNRB)result.Item).
                                                                PassengerReservation?.
                                                                Segments?.
                                                                Segment?.
                                                                Where(w => w.Item.GetType() == typeof(SegmentTypePNRBSegmentAir)).
                                                                Select(s => (SegmentTypePNRBSegmentAir)s.Item).ToList();

                if (airsegs.IsNullOrEmpty() ||
                    !airsegs.Any(a => "HK,KK,KL,RR,TK,EK".Contains(a.ActionCode)))
                {
                    return null;
                }

                DateTime pcclocaldatetime = GetPCCLocalTime(token, result);

                //Construct the response
                return await _sabreCommandService.GetQuoteText(token, pcc, rq, agent?.AgentPCC, pcclocaldatetime);
            }
            finally
            {
                if (sabreSession != null && string.IsNullOrEmpty(sabreSession.SessionID))
                {
                    await _sessionCloseService.SabreSignout(sabreSession.SessionID, pcc);
                }
            }
        }

        private DateTime GetPCCLocalTime(string token, GetReservationRS result)
        {
            string pcccity = ((ReservationPNRB)result.Item).
                                    PhoneNumbers.
                                    FirstOrDefault().
                                    CityCode;

            string localdate = _sabreCommandService.
                                    ExecuteCommand(token, pcc, $"T*{pcccity}").
                                    GetAwaiter().
                                    GetResult().
                                    SplitOn("*").
                                    Last().
                                    Trim();

            DateTime pcclocaldatetime = DateTime.
                                            ParseExact(
                                                localdate,
                                                "HHmm ddMMM",
                                                System.Globalization.CultureInfo.InvariantCulture);
            return pcclocaldatetime;
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
                if (token != null && token.IsLimitReached)
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
                          PriceType = Models.PriceType.Manual
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

        internal async Task<ConvertCurrencyResponse> CurrencyConvert(ConvertCurrencyRequest request, string contextID)
        {
            ConvertCurrencyResponse convertCurrencyResponse = new ConvertCurrencyResponse();
            try
            {
                string sessionID = request.SessionID;
                user = await session.GetSessionUser(sessionID);
                pcc = await _consolidatorPccDataSource.GetWebServicePccByGdsCode("1W", contextID, sessionID);

                //Obtain session (if found from cache, else directly from sabre)
                SabreSession sabresession = await _sessionCreateService.CreateStatefulSessionToken(pcc);

                string command = $"DC¥{request.FromCurrency}{request.Amount}/{request.ToCurrency}";

                string res = await _sabreCommandService.ExecuteCommand(sabresession.SessionID, pcc, command);

                SabreCurrencyConvert sabreCurrencyConvert = new SabreCurrencyConvert(res);

                convertCurrencyResponse.CurrencyCode = sabreCurrencyConvert.CurrencyCode;
                convertCurrencyResponse.Amount = sabreCurrencyConvert.Amount;
            }
            catch(GDSException gdsex)
            {
                logger.LogError(gdsex.Message);
                convertCurrencyResponse.Error = new WebtopError()
                {
                    code = gdsex.ErrorCode,
                    message = gdsex.Message
                };
            }

            return convertCurrencyResponse;
        }

        private List<PNRAgent> GetAgents(string sessionid, string bookingpcc)
        {
            List<DataAgent> agents = _agentPccDataSource.RetrieveAgents(user?.ConsolidatorId, sessionid).GetAwaiter().GetResult();
            var agentlist = agents.
                                Where(w => w.pcc_code == bookingpcc && w.gds_code == "1W").
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

            passengers.
                ForEach(f => f.PassengerKey = JsonConvert.
                                                        SerializeObject( new QuotePassenger()
                                                        {
                                                            NameNumber = f.NameNumber,
                                                            PassengerName = f.Passengername,
                                                            DOB = string.IsNullOrEmpty(f.DOB) ? default : DateTime.Parse(f.DOB),
                                                            PaxType = f.PaxType
                                                        }).EncodeBase64());
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

            if(agent != null)
            {
                logger.LogInformation($"AgentID: {agent.AgentId}");
                logger.LogInformation($"Consolidator ID: {agent.ConsolidatorId}");
            }

            string ticketingprinter = GetTicketingPrinter(pcc?.TicketPrinterAddress, pcc.PccCode);
            string printerbypass = GetPrinterByPass(string.IsNullOrEmpty(pcc?.CountryCode) ? "" : pcc?.CountryCode.SplitOn("|").First(), pcc.PccCode);

            string agentpcc = "";

            try
            {
                if (user == null)
                {
                    throw new ExpiredSessionException(request.SessionID, "50000401", "Invalid session.");
                }

                if (!agent?.Agent?.Permission?.AllowTicketing ?? false)
                {
                    throw new AeronologyException("50000020", "Ticketing access is not provided for your account. Please contact your consolidator to request access.");
                }

                //Populate request collections
                ReconstructRequestFromKeys(request);

                string ticketingpcc = request.Quotes.IsNullOrEmpty() ? GetTicketingPCC(agent?.TicketingPcc, pcc.PccCode) : request.Quotes.First().TicketingPCC;
                if (string.IsNullOrEmpty(ticketingpcc))
                {
                    throw new ExpiredSessionException(request.SessionID, "50000401", "Invalid ticketing pcc.");
                }

                agentpcc = (await getagentpccs(user?.AgentId, pcc.PccCode, request.SessionID)).FirstOrDefault();
                if (user?.Agent != null) { user.Agent.CustomerNo = agent.CustomerNo; }

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

                //Check if the filed fares are partially issued
                var filedfares = request.Quotes.Where(w => w.FiledFare).GroupBy(g=> g.QuoteNo).ToList();
                if (!filedfares.IsNullOrEmpty())
                {
                    filedfares.
                        ForEach(f =>
                        {
                            if (pnr.Quotes.Where(w => w.QuoteNo == f.Key).Count() > f.Count())
                            {
                                f.Select(s => s).ToList().ForEach(f => f.PartialIssue = true);
                            }
                        });
                }

                //Stored cards
                GetStoredCards(request, getReservationRS);

                var pendingquotes = request.Quotes.Where(w => !w.FiledFare && w.PriceType != Models.PriceType.Manual);
                var manualquotes = request.Quotes.Where(w => w.PriceType == Models.PriceType.Manual);
                var pendingsfdata = request.Quotes.Where(a => a.PendingSfData);

                //Manual build
                if(!manualquotes.IsNullOrEmpty())
                {
                    await ManualBuild(
                                pcc, 
                                request, 
                                statefultoken, 
                                manualquotes, 
                                pnr, 
                                ticketingpcc, 
                                contextID,
                                ticketingprinter,
                                printerbypass);
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
                transactionData = await HandleMerchantPayment(request.SessionID, request, transactionData, contextID);

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


        private async Task<IssueExpressTicketRS> ParseSabreTicketData(List<issueticketresponse> responselist, IssueExpressTicketRQ rq, string statefultoken, Pcc pcc, string ticketingpcc, PNR pnr, string bcode, Token statelesstoken, User user)
        {
            List<IssueTicketDetails> issueTicketDetails = new List<IssueTicketDetails>();
            IssueExpressTicketRS issueExpressTicketRS = new IssueExpressTicketRS()
            {
                GDSCode = "1W",
                GrandPriceItAmount = rq.GrandPriceItAmount.HasValue ? rq.GrandPriceItAmount.Value : default(decimal?)
            };
            bool isccfop = !rq.Quotes.IsNullOrEmpty() && rq.Quotes.First().Passenger.FormOfPayment.PaymentType == PaymentType.CC ||
                           !rq.EMDs.IsNullOrEmpty() && rq.EMDs.First().FormOfPayment.PaymentType == PaymentType.CC;

            //TODO: workout ticketing pcc timezone

            foreach (var response in responselist)
            {
                var res = JsonSerializer.
                            Deserialize<AirTicketRQResponse>(
                                response.GDSResponse,
                                new System.Text.Json.JsonSerializerOptions()
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                List<IssueTicketError> errors = HandleTicketingErrors(rq, response, res);

                issueTicketDetails.AddRange(GetTicketDataThroughTicketDisplay(errors, statefultoken, pcc, ticketingpcc));

                if (!issueTicketDetails.IsNullOrEmpty())
                {
                    GetQuoteEMDData(issueTicketDetails, response);
                }

                issueExpressTicketRS.Errors = errors;

                issueExpressTicketRS.Locator = rq.Locator;


                if (res.AirTicketRS.ApplicationResults.status != "Complete")
                {
                    issueExpressTicketRS.Tickets = issueTicketDetails;
                    return issueExpressTicketRS;
                }

                var query = from result in res.AirTicketRS.Summary
                            let quote = response.
                                        QuoteNos.
                                        FirstOrDefault(f => f.PassengerName.RegexReplace(@"\s*").
                                                            Contains($"{result.LastName}/{result.FirstName}".RegexReplace(@"\s*")) &&
                                                            Math.Round(f.TotalFare, 2) == Math.Round(decimal.Parse(result.TotalAmount.content), 2))
                            let emd = response.
                                        EMDNos.
                                        FirstOrDefault(f => f.PassengerName.RegexReplace(@"\s*").
                                                Contains($"{result.LastName}/{result.FirstName}".RegexReplace(@"\s*")) &&
                                                Math.Round(f.TotalFare, 2) == Math.Round(decimal.Parse(result.TotalAmount.content), 2))
                            select new IssueTicketDetails()
                            {
                                DocumentNumber = result.DocumentNumber,
                                DocumentType = result.DocumentType,
                                IssuingPCC = result.IssuingLocation,
                                LocalIssueDateTime = result.LocalIssueDateTime.GetISODateTime(),
                                PassengerName = $"{result.LastName}/{result.FirstName}",
                                TotalAmount = decimal.Parse(result.TotalAmount.content),
                                CurrencyCode = result.TotalAmount.currencyCode,
                                AgentPrice = result.DocumentType == "TKT" && quote != null ?
                                                quote.AgentPrice :
                                                result.DocumentType == "EMD" && emd != null ?
                                                     emd.AgentPrice :
                                                     decimal.Parse(result.TotalAmount.content),
                                GrossPrice = decimal.Parse(result.TotalAmount.content),
                                QuoteRefNo = result.DocumentType == "TKT" && quote != null ? quote.DocumentNo : -1,
                                EMDNumber = new List<int> { result.DocumentType == "EMD" && emd != null ? emd.DocumentNo : -1 },
                                Route = result.DocumentType == "TKT" && quote != null ?
                                            quote.Route :
                                            result.DocumentType == "EMD" && emd != null ?
                                                emd.Route :
                                                "",
                                PriceIt = rq.GrandPriceItAmount == decimal.MinValue ? 0.00M :
                                            result.DocumentType == "TKT" && quote != null ?
                                                quote.PriceIt :
                                                 result.DocumentType == "EMD" && emd != null ?
                                                    emd.PriceIt :
                                                    0.00M,
                                CashAmount = result.DocumentType == "TKT" && quote != null ?
                                                quote.FormOfPayment == null || quote.FormOfPayment.PaymentType == PaymentType.CA ?
                                                        decimal.Parse(result.TotalAmount.content) - quote.TotalTax :
                                                        quote.FormOfPayment.PaymentType == PaymentType.CC && decimal.Parse(result.TotalAmount.content) > quote.FormOfPayment.CreditAmount ?
                                                                quote.TotalTax > quote.FormOfPayment.CreditAmount ?
                                                                        decimal.Parse(result.TotalAmount.content) - quote.TotalTax :
                                                                        decimal.Parse(result.TotalAmount.content) - quote.FormOfPayment.CreditAmount :
                                                            0.00M :
                                                result.DocumentType == "EMD" && emd != null ?
                                                    emd.FormOfPayment == null || emd.FormOfPayment.PaymentType == PaymentType.CA ?
                                                        decimal.Parse(result.TotalAmount.content) - emd.TotalTax :
                                                        emd.FormOfPayment.PaymentType == PaymentType.CC && decimal.Parse(result.TotalAmount.content) > emd.FormOfPayment.CreditAmount ?
                                                            emd.TotalTax > emd.FormOfPayment.CreditAmount ?
                                                                        decimal.Parse(result.TotalAmount.content) - emd.TotalTax :
                                                                        decimal.Parse(result.TotalAmount.content) - emd.FormOfPayment.CreditAmount :
                                                            0.00M :
                                                decimal.Parse(result.TotalAmount.content),
                                FormOfPayment = result.DocumentType == "TKT" && quote != null ?
                                                    new FOP()
                                                    {
                                                        PaymentType = quote.FormOfPayment.PaymentType,
                                                        CardNumber = quote.FormOfPayment.PaymentType == PaymentType.CC ?
                                                                        dataProtector.Protect(quote.FormOfPayment.CardNumber) :
                                                                        "",
                                                        CardType = quote.FormOfPayment.PaymentType == PaymentType.CC ? CreditCardOperations.GetCreditCardType(quote.FormOfPayment.CardNumber) : "",
                                                        ExpiryDate = quote.FormOfPayment.ExpiryDate,
                                                        CreditAmount = quote.FormOfPayment.PaymentType == PaymentType.CC ?
                                                                            quote.FormOfPayment.CreditAmount :
                                                                            0.00M,
                                                        BCode = string.IsNullOrEmpty(quote.FormOfPayment.BCode) ? bcode : quote.FormOfPayment.BCode
                                                    } :
                                                result.DocumentType == "EMD" && emd != null ?
                                                     new FOP()
                                                     {
                                                         PaymentType = emd.FormOfPayment.PaymentType,
                                                         CardNumber = emd.FormOfPayment.PaymentType == PaymentType.CC ?
                                                                        dataProtector.Protect(emd.FormOfPayment.CardNumber) :
                                                                        "",
                                                         CardType = emd.FormOfPayment.PaymentType == PaymentType.CC ? CreditCardOperations.GetCreditCardType(emd.FormOfPayment.CardNumber) : "",
                                                         ExpiryDate = emd.FormOfPayment.ExpiryDate,
                                                         CreditAmount = emd.FormOfPayment.PaymentType == PaymentType.CC ?
                                                                            emd.FormOfPayment.CreditAmount :
                                                                            0.00M,
                                                         BCode = string.IsNullOrEmpty(emd.FormOfPayment.BCode) ? bcode : quote.FormOfPayment.BCode
                                                     } :
                                                     null,
                                TotalCreditAmount = rq.MerchantData != null ?
                                                        0.00M :
                                                        result.DocumentType == "TKT" && quote != null ?
                                                            quote.FormOfPayment.CreditAmount :
                                                            result.DocumentType == "EMD" && emd != null ?
                                                                emd.FormOfPayment.CreditAmount :
                                                                0.00M,
                                CreditAmount = result.DocumentType == "TKT" && quote != null ?
                                                quote.FormOfPayment == null || quote.FormOfPayment.PaymentType == PaymentType.CA ?
                                                            0.00M :
                                                            quote.FormOfPayment.PaymentType == PaymentType.CC && decimal.Parse(result.TotalAmount.content) > quote.FormOfPayment.CreditAmount ?
                                                                    quote.TotalTax > quote.FormOfPayment.CreditAmount ?
                                                                        quote.FormOfPayment.CreditAmount :
                                                                        quote.FormOfPayment.CreditAmount - quote.TotalTax :
                                                                    quote.FormOfPayment.CreditAmount - quote.TotalTax :
                                                result.DocumentType == "EMD" && emd != null ?
                                                    emd.FormOfPayment == null || emd.FormOfPayment.PaymentType == PaymentType.CA ?
                                                            0.00M :
                                                            emd.FormOfPayment.PaymentType == PaymentType.CC && decimal.Parse(result.TotalAmount.content) > emd.FormOfPayment.CreditAmount ?
                                                                emd.TotalTax > emd.FormOfPayment.CreditAmount ?
                                                                    emd.FormOfPayment.CreditAmount :
                                                                    emd.FormOfPayment.CreditAmount - emd.TotalTax :
                                                                emd.FormOfPayment.CreditAmount - emd.TotalTax :
                                                0.00M
                            };

                List<IssueTicketDetails> ticketdata = query.ToList();

                //check if the same quote number used for multiple tickets
                HandleSameQuoteNumber(rq, statefultoken, pcc, response, ticketdata);

                //check if the same emd no assign to multiple tickets
                //if EMD grouping happens on sabre one or more EMD can get issued agaist one ticket number
                HandleGroupedEMDs(rq, statefultoken, pcc, response, errors, ticketdata, bcode);

                var quotenonotused = response.
                                        QuoteNos.
                                        Where(w => w.DocumentType == "QUOTE" &&
                                                    !ticketdata.
                                                        Where(t =>
                                                            t.DocumentType == "TKT" &&
                                                            t.QuoteRefNo != -1 &&
                                                            !string.IsNullOrEmpty(t.DocumentNumber) &&
                                                            int.TryParse(t.DocumentNumber.Trim(), out int docno)).
                                                        Select(s => int.Parse(s.DocumentNumber.Trim())).
                                                        ToList().
                                                        Contains(w.DocumentNo));

                //Quote number not found
                NoQuoteNoTicketDisplay(ticketdata, quotenonotused);

                //Conjunction
                GetConjunctionPostfix(rq, issueTicketDetails, ticketdata);
            }

            issueExpressTicketRS.Tickets = issueTicketDetails;

            issueExpressTicketRS.GrandTotal = issueTicketDetails.Sum(s => s.TotalAmount);

            //Get approval code
            IssueTicketDetails doc = new IssueTicketDetails();
            if (isccfop && !issueTicketDetails.IsNullOrEmpty())
            {
                //display irst ticket/ emd
                GetElectronicDocumentRS displayticketres = _displayTicket.DisplayDocument(statefultoken, pcc, issueTicketDetails.First().DocumentNumber, ticketingpcc).GetAwaiter().GetResult();

                doc = _displayTicket.PraseDisplatTicketResponseforIssueTicket(displayticketres.DocumentDetailsDisplay.Item, ticketingpcc);

            }

            issueExpressTicketRS.ApprovalCode = doc.FormOfPayment?.ApprovalCode;

            return issueExpressTicketRS;
        }

        private static void GetConjunctionPostfix(IssueExpressTicketRQ rq, List<IssueTicketDetails> issueTicketDetails, List<IssueTicketDetails> ticketdata)
        {
            List<IIssueExpressTicketDocument> tktconjs = new List<IIssueExpressTicketDocument>();
            List<IIssueExpressTicketDocument> emdconjs = new List<IIssueExpressTicketDocument>();
            //tickets
            if (!rq.Quotes.IsNullOrEmpty())
            {
                tktconjs.AddRange(rq.Quotes.Where(w => w.SectorCount > 4));
            }
            //emds
            if (!rq.EMDs.IsNullOrEmpty())
            {
                emdconjs.AddRange(rq.EMDs.Where(w => w.SectorCount > 4));
            }

            if (!tktconjs.IsNullOrEmpty())
            {
                foreach (var conj in tktconjs)
                {
                    int noofticketsallocated = Convert.ToInt32(Math.Ceiling((double)(conj.SectorCount / 4)));
                    var selectedtkt = ticketdata.FirstOrDefault(f => f.DocumentType == "TKT" && f.PassengerName.StartsWith(conj.PassengerName));
                    if (selectedtkt != null)
                    {
                        selectedtkt.ConjunctionPostfix = (int.Parse(selectedtkt.DocumentNumber.Trim().Last(3)) + noofticketsallocated).ToString();
                    }
                }
            }


            if (!emdconjs.IsNullOrEmpty())
            {
                foreach (var conj in emdconjs)
                {
                    int noofticketsallocated = Convert.ToInt32(Math.Ceiling((double)(conj.SectorCount / 4)));
                    var selectedtkt = ticketdata.FirstOrDefault(f => f.DocumentType == "EMD" && f.PassengerName.StartsWith(conj.PassengerName));
                    if (selectedtkt != null)
                    {
                        selectedtkt.ConjunctionPostfix = (int.Parse(selectedtkt.DocumentNumber.Trim().Last(3)) + noofticketsallocated).ToString();
                    }
                }
            }

            issueTicketDetails.AddRange(ticketdata);
        }

        private void NoQuoteNoTicketDisplay(List<IssueTicketDetails> ticketdata, IEnumerable<IssueTicketDocumentData> quotenonotused)
        {
            var noquotenumtkt = ticketdata.Where(w => w.DocumentType == "TKT" && w.QuoteRefNo == -1);

            if (noquotenumtkt.IsNullOrEmpty()) { return; }

            foreach (var tkt in noquotenumtkt)
            {
                var quote = quotenonotused.FirstOrDefault(f => f.PassengerName.RegexReplace(@"\s*").Contains(tkt.PassengerName.RegexReplace(@"\s*")));

                if (quote == null) { continue; }

                tkt.QuoteRefNo = quote.DocumentNo;
                tkt.AgentPrice = quote.AgentPrice >= tkt.TotalAmount ?
                                    quote.AgentPrice :
                                    tkt.TotalAmount;
                tkt.Route = quote.Route;
                tkt.PriceIt = quote.PriceIt >= tkt.TotalAmount ?
                                    quote.PriceIt :
                                    tkt.TotalAmount;
                tkt.CashAmount = quote.FormOfPayment == null || quote.FormOfPayment.PaymentType == PaymentType.CA ?
                                        tkt.TotalAmount - quote.TotalTax :
                                        quote.FormOfPayment.PaymentType == PaymentType.CC && tkt.TotalAmount > quote.FormOfPayment.CreditAmount ?
                                                quote.TotalTax > quote.FormOfPayment.CreditAmount ?
                                                        tkt.TotalAmount - quote.TotalTax :
                                                        tkt.TotalAmount - quote.FormOfPayment.CreditAmount :
                                            0.00M;
                tkt.FormOfPayment = new FOP()
                {
                    PaymentType = quote.FormOfPayment.PaymentType,
                    CardNumber = quote.FormOfPayment.PaymentType == PaymentType.CC ?
                                                                                            quote.FormOfPayment.CardNumber.MaskNumber() :
                                                                                            "",
                    CardType = quote.FormOfPayment.CardType,
                    ExpiryDate = quote.FormOfPayment.ExpiryDate,
                    CreditAmount = quote.FormOfPayment.PaymentType == PaymentType.CC ?
                                                                                                quote.FormOfPayment.CreditAmount :
                                                                                                0.00M
                };
                tkt.TotalCreditAmount = quote.FormOfPayment.CreditAmount;
                tkt.CreditAmount = quote.FormOfPayment == null || quote.FormOfPayment.PaymentType == PaymentType.CA ?
                                        0.00M :
                                        quote.FormOfPayment.PaymentType == PaymentType.CC && tkt.TotalAmount > quote.FormOfPayment.CreditAmount ?
                                                quote.TotalTax > quote.FormOfPayment.CreditAmount ?
                                                    quote.FormOfPayment.CreditAmount :
                                                    quote.FormOfPayment.CreditAmount - quote.TotalTax :
                                                quote.FormOfPayment.CreditAmount - quote.TotalTax;

                if (tkt.TotalAmount != quote.TotalFare)
                {
                    tkt.Warning = new WebtopWarning()
                    {
                        code = "TOTAL_MISSMATCH",
                        message = $"Alert! The tickets have been issued at a different price level({tkt.TotalAmount}) than the original price quote price({quote.TotalFare}) in the booking. " +
                                Environment.NewLine +
                                "Please verify and void the tickets before midnight if necessary."
                    };
                }
            }
        }

        private void HandleGroupedEMDs(IssueExpressTicketRQ rq, string token, Pcc pcc, issueticketresponse response, List<IssueTicketError> errors, List<IssueTicketDetails> ticketdata, string bcode)
        {
            var sameemdtkt = ticketdata.
                                GroupBy(grp => grp.EMDNumber.First()).
                                Where(w => w.Count() > 1).
                                ToList();

            if (!sameemdtkt.IsNullOrEmpty())
            {
                List<int> usedemdno = sameemdtkt.Select(s => s.Key).ToList();
                foreach (var item in sameemdtkt)
                {
                    foreach (var ticketitem in item.Skip(1))
                    {
                        var emd = response.
                                        EMDNos.
                                        Where(w => !usedemdno.Contains(w.DocumentNo)).
                                        FirstOrDefault(f => f.PassengerName.Contains(ticketitem.PassengerName) &&
                                                            Math.Round(f.TotalFare, 2) == ticketitem.TotalAmount);

                        if (emd == null)
                        {
                            ticketitem.EMDNumber = new List<int> { -1 };
                            continue;
                        }

                        ticketitem.EMDNumber = new List<int> { emd.DocumentNo };
                        ticketitem.PriceIt = rq.GrandPriceItAmount == decimal.MinValue ? 0.00M : emd.PriceIt;
                        ticketitem.Route = emd.Route;
                        usedemdno.Add(emd.DocumentNo);
                    }
                }
            }

            List<IssueTicketDetails> emds = ticketdata.
                            Where(w => w.DocumentType == "EMD" && w.QuoteRefNo == -1 && w.EMDNumber.First() == -1).
                            ToList();

            List<int> errEMDs = errors.
                                    Where(doc => doc.DocumentType == Models.DocumentType.EMD).
                                    SelectMany(s => s.DocumentNumber).
                                    ToList();

            if (!emds.IsNullOrEmpty())
            {
                foreach (var emd in emds)
                {
                    //Display ticket
                    GetElectronicDocumentRS displaytktesponse = _displayTicket.DisplayDocument(token, pcc, emd.DocumentNumber, emd.IssuingPCC).GetAwaiter().GetResult();
                    var doc = _displayTicket.PraseDisplatEMDResponse(displaytktesponse);

                    var emdtkt = response.
                                EMDNos.
                                Where(w => !errEMDs.Contains(w.DocumentNo) &&
                                           w.PassengerName.Contains(doc.Passenger.PassengerName) &&
                                           doc.PlatingCarrier == w.PlatingCarrier &&
                                           doc.EMDCoupons.FirstOrDefault()?.Reason == w.RFISC);

                    var tkt = ticketdata.First(f => f.DocumentNumber == emd.DocumentNumber);
                    tkt.EMDNumber = emdtkt.Select(s => s.DocumentNo).ToList();
                    tkt.ConjunctionPostfix = doc.ConjunctionPostfix;
                    tkt.AgentPrice = emdtkt.First().FormOfPayment.PaymentType == PaymentType.CA ?
                                            doc.TotalAmount - emdtkt.Sum(s => s.Commission) + emdtkt.Sum(s => s.Fee) + emdtkt.Where(w => w.FeeGST.HasValue).Sum(s => s.FeeGST.Value) :
                                            emdtkt.Sum(s => s.Fee) - emdtkt.Sum(s => s.Commission);
                    tkt.Route = GetRoute(doc.EMDCoupons.ToList<ICoupon>());
                    tkt.PriceIt = rq.GrandPriceItAmount == decimal.MinValue ? 0.00M : emdtkt.Sum(s => s.PriceIt);
                    tkt.TotalAmount = doc.TotalAmount;
                    tkt.GrossPrice = doc.TotalAmount;
                    tkt.CashAmount = emdtkt.All(a => a.FormOfPayment == null) ?
                                                    doc.TotalAmount - doc.EMDCoupons.Where(w => !w.Taxes.IsNullOrEmpty()).SelectMany(s => s.Taxes).Sum(t => t.Amount) :
                                                    emdtkt.All(a => a.FormOfPayment.PaymentType == PaymentType.CA) ?
                                                        doc.TotalAmount - doc.EMDCoupons.Where(w => !w.Taxes.IsNullOrEmpty()).SelectMany(s => s.Taxes).Sum(t => t.Amount) :
                                                    emdtkt.All(a => a.FormOfPayment.PaymentType == PaymentType.CC) && doc.TotalAmount > emdtkt.Sum(s => s.FormOfPayment.CreditAmount) ?
                                                            doc.EMDCoupons.Where(w => !w.Taxes.IsNullOrEmpty()).SelectMany(s => s.Taxes).Sum(t => t.Amount) > emd.FormOfPayment.CreditAmount ?
                                                                        doc.TotalAmount - doc.EMDCoupons.Where(w => !w.Taxes.IsNullOrEmpty()).SelectMany(s => s.Taxes).Sum(t => t.Amount) :
                                                                        doc.TotalAmount - emd.FormOfPayment.CreditAmount :
                                                        0.00M;
                    tkt.CreditAmount = emdtkt.All(a => a.FormOfPayment == null) ?
                                                        0.00M :
                                                        emdtkt.All(a => a.FormOfPayment.PaymentType == PaymentType.CA) ?
                                                            0.00M :
                                                            emdtkt.All(a => a.FormOfPayment.PaymentType == PaymentType.CC) && doc.TotalAmount > emdtkt.Sum(s => s.FormOfPayment.CreditAmount) ?
                                                                doc.EMDCoupons.Where(w => !w.Taxes.IsNullOrEmpty()).SelectMany(s => s.Taxes).Sum(t => t.Amount) > emdtkt.Sum(s => s.FormOfPayment.CreditAmount) ?
                                                                    emdtkt.Sum(s => s.FormOfPayment.CreditAmount) :
                                                                    emdtkt.Sum(s => s.FormOfPayment.CreditAmount) - doc.EMDCoupons.Where(w => !w.Taxes.IsNullOrEmpty()).SelectMany(s => s.Taxes).Sum(t => t.Amount) :
                                                                emdtkt.Sum(s => s.FormOfPayment.CreditAmount) - doc.EMDCoupons.Where(w => !w.Taxes.IsNullOrEmpty()).SelectMany(s => s.Taxes).Sum(t => t.Amount);
                    tkt.TotalCreditAmount = emdtkt.Sum(s => s.FormOfPayment.CreditAmount);
                    tkt.FormOfPayment = new FOP()
                    {
                        PaymentType = emdtkt.First().FormOfPayment.PaymentType,
                        CardNumber = emdtkt.First().FormOfPayment.PaymentType == PaymentType.CC ?
                                            emdtkt.First().FormOfPayment.CardNumber.MaskNumber() :
                                            "",
                        CardType = emdtkt.First().FormOfPayment.CardType,
                        ExpiryDate = emdtkt.First().FormOfPayment.ExpiryDate,
                        CreditAmount = emdtkt.First().FormOfPayment.PaymentType == PaymentType.CC ?
                                        emdtkt.First().FormOfPayment.CreditAmount - doc.EMDCoupons.Where(w => !w.Taxes.IsNullOrEmpty()).SelectMany(s => s.Taxes).Sum(s => s.Amount) :
                                        0.00M,
                        BCode = string.IsNullOrEmpty(emdtkt.First().FormOfPayment.BCode) ? bcode : emdtkt.First().FormOfPayment.BCode
                    };
                }
            }
        }

        private void HandleSameQuoteNumber(IssueExpressTicketRQ rq, string token, Pcc pcc, issueticketresponse response, List<IssueTicketDetails> ticketdata)
        {
            var samequotenotkt = ticketdata.
                                Where(w => w.DocumentType == "TKT").
                                GroupBy(grp => grp.QuoteRefNo).
                                Where(w => w.Count() > 1).
                                ToList();

            if (samequotenotkt.IsNullOrEmpty()) { return; }

            foreach (var currenttkt in samequotenotkt.SelectMany(s => s))
            {
                //display ticket
                GetElectronicDocumentRS displaytktesponse = _displayTicket.
                                                                DisplayDocument(
                                                                    token,
                                                                    pcc,
                                                                    currenttkt.DocumentNumber,
                                                                    currenttkt.IssuingPCC).
                                                                GetAwaiter().
                                                                GetResult();
                var doc = _displayTicket.PraseDisplatTicketResponse(displaytktesponse);

                var quotetkt = response.
                                QuoteNos.
                                FirstOrDefault(w => w.PassengerName.Contains(doc.Passenger.PassengerName) &&
                                                    w.TotalFare == doc.TotalFare &&
                                                    doc.PlatingCarrier == w.PlatingCarrier &&
                                                    GetRoute(doc.Coupons.ToList<ICoupon>()) == w.Route);

                if (quotetkt == null) { continue; }

                var tkt = ticketdata.First(f => f.DocumentNumber == doc.DocumentNumber);
                tkt.QuoteRefNo = quotetkt.DocumentNo;
                tkt.ConjunctionPostfix = doc.ConjunctionPostfix;
                tkt.AgentPrice = quotetkt.AgentPrice;
                tkt.Route = GetRoute(doc.Coupons.ToList<ICoupon>());
                tkt.PriceIt = quotetkt.PriceIt;
                tkt.FormOfPayment = new FOP()
                {
                    PaymentType = quotetkt.FormOfPayment.PaymentType,
                    CardNumber = quotetkt.FormOfPayment.PaymentType == PaymentType.CC ?
                                                                        quotetkt.FormOfPayment.CardNumber.MaskNumber() :
                                                                        "",
                    CardType = quotetkt.FormOfPayment.CardType,
                    ExpiryDate = quotetkt.FormOfPayment.ExpiryDate,
                    CreditAmount = quotetkt.FormOfPayment.PaymentType == PaymentType.CC ?
                                                                            quotetkt.FormOfPayment.CreditAmount :
                                                                            0.00M
                };
                tkt.CashAmount = quotetkt.FormOfPayment == null || quotetkt.FormOfPayment.PaymentType == PaymentType.CA ?
                                        0.00M :
                                        quotetkt.FormOfPayment.PaymentType == PaymentType.CC && doc.TotalFare > quotetkt.FormOfPayment.CreditAmount ?
                                                quotetkt.TotalTax > quotetkt.FormOfPayment.CreditAmount ?
                                                    quotetkt.FormOfPayment.CreditAmount :
                                                    quotetkt.FormOfPayment.CreditAmount - quotetkt.TotalTax :
                                                quotetkt.FormOfPayment.CreditAmount - quotetkt.TotalTax;
                tkt.CreditAmount = quotetkt.FormOfPayment == null || quotetkt.FormOfPayment.PaymentType == PaymentType.CA ?
                                                            0.00M :
                                                            quotetkt.FormOfPayment.PaymentType == PaymentType.CC && doc.TotalFare > quotetkt.FormOfPayment.CreditAmount ?
                                                                    quotetkt.TotalTax > quotetkt.FormOfPayment.CreditAmount ?
                                                                        quotetkt.FormOfPayment.CreditAmount :
                                                                        quotetkt.FormOfPayment.CreditAmount - quotetkt.TotalTax :
                                                                    quotetkt.FormOfPayment.CreditAmount - quotetkt.TotalTax;
                tkt.TotalCreditAmount = quotetkt.FormOfPayment.CreditAmount;
            }
        }

        private string GetRoute(List<ICoupon> tktcoupons)
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

        private List<IssueTicketError> HandleTicketingErrors(IssueExpressTicketRQ rq, issueticketresponse response, AirTicketRQResponse res)
        {
            List<string> excludedwarnings = new List<string>()
            {
                "TTY REQ PEND",
                "PAC TO VERIFY CORRECT NBR ",
                @"EndTransactionLLSRQ:\s+\*PAC TO VERIFY CORRECT NBR OF ACCTG LINES",
                "No new tickets have been issued",
                "CREDIT VERIFICATION IN PROGRESS",
                @"UNABLE TO PROCESS\s+-\s+CORRECT/RETRY",
                "VERIFY ORDER OF ITINERARY SEGMENTS",
                @"PNR HAS BEEN UPDATED\s+-\s+IGN AND RETRY",
                "has been issued successfully but it has not been committed to the PNR.",
                @"INVOICED\s+-\s+NUMBER\s+NONE",
                "NO COMMISSION WAS ENTERED",
                @"REQUEST PRINTING\s+-\s+\d+\s+INVOICE",
                "ETR MESSAGE PROCESSED",
                @"OK\s+\d+[\.\d]*",
                @"AE ITEMS EXIST\s+-\s+USE W‡EMD ENTRY TO FULFILL",
                "US INS INSPECTION AND CUSTOMS FEES INCLUDED",
                @".*‡UTP-\d+‡",
                @"USE ‡DUPE TO OVERRIDE AND ISSUE TICKET OR CORRECT AND RETRY",
                @"INVOICED\s+-\s+NUMBER\s*\d+"
            };

            List<IssueTicketError> errors = new List<IssueTicketError>();

            //Warnings
            if (res.AirTicketRS.ApplicationResults.Warning != null)
            {
                var err = res.
                                AirTicketRS.
                                ApplicationResults.
                                Warning.
                                SelectMany(s => s.SystemSpecificResults).
                                SelectMany(s => s.Message).
                                Select(s => s.content);
                errors.
                    AddRange(err.
                                Where(w => excludedwarnings.All(a => !w.IsMatch(a))).
                                Select(s => GetIssueTicketError(s, response.GDSRequest, rq)).
                                Where(w => !string.IsNullOrEmpty(w.Error?.message)).
                                ToList());
            }

            List<string> excludeerrors = new List<string>()
            {
                "No new tickets have been issued",
            };

            //Errors
            if (res.AirTicketRS.ApplicationResults.Error != null)
            {
                var err = res.
                            AirTicketRS.
                            ApplicationResults.
                            Error.
                            SelectMany(s => s.SystemSpecificResults).
                            SelectMany(s => s.Message).
                            Select(s => s.content);

                errors.
                    AddRange(err.
                                Where(w => excludeerrors.All(a => !w.IsMatch(a))).
                                Select(s => GetIssueTicketError(s, response.GDSRequest, rq)).
                                Where(w => !string.IsNullOrEmpty(w.Error?.message)).
                                ToList());
            }

            //group errors by document number and type
            errors = errors.
                        GroupBy(g => new { DocumentNumbers = g.DocumentNumber.IsNullOrEmpty() ? "" : string.Join(",", g.DocumentNumber), g.DocumentType }).
                        Select(s => new IssueTicketError()
                        {
                            DocumentNumber = s.Where(w => !w.DocumentNumber.IsNullOrEmpty()).SelectMany(m => m.DocumentNumber).Distinct().ToList(),
                            DocumentType = s.Key.DocumentType,
                            Error = new WebtopError()
                            {
                                code = "TICKETING_ERROR",
                                message = string.Join(", ", s.Select(p => p.Error).ToList().Distinct())
                            }
                        }).
                        ToList();

            if (res.AirTicketRS.ApplicationResults.status != "Complete")
            {
                //if()
                ModifyErrors(rq, errors);
            }

            return errors;
        }

        private static void ModifyErrors(IssueExpressTicketRQ rq, List<IssueTicketError> errors)
        {
            if (errors.IsNullOrEmpty()) { return; }
            var err = errors.Where(s => s.Error != null && s.Error.message.Contains("AUTH CARRIER INVLD"));
            if (!err.IsNullOrEmpty())
            {
                throw new AeronologyException(
                    "50000016",
                    $"Airline Plate for { (rq.EMDs != null ? rq.EMDs.First().PlatingCarrier : rq.Quotes.First().PlatingCarrier)} inactive for ticketing");
            }
            err = errors.Where(s => s.Error != null && s.Error.message.Contains("Unable to process the stateless transaction. Please retry"));
            if (!err.IsNullOrEmpty())
            {
                throw new AeronologyException(
                    "50000019",
                    "Unable to process the ticketing request due to GDS connectivity issue. Please try again later.");
            }
        }

        private static void GetQuoteEMDData(List<IssueTicketDetails> issueTicketDetails, issueticketresponse response)
        {
            issueTicketDetails.
                ForEach(f =>
                {
                    if (f.DocumentType == "TKT")
                    {
                        var quote = response.
                                        QuoteNos.
                                        FirstOrDefault(i => i.DocumentType == "QUOTE" && i.PassengerName.StartsWith(f.PassengerName));
                        if (quote != null)
                        {
                            f.QuoteRefNo = quote.DocumentNo;
                        }
                    }
                    else if (f.DocumentType == "EMD")
                    {
                        var emd = response.
                                    EMDNos.
                                    FirstOrDefault(i => i.DocumentType == "EMD" && i.PassengerName.StartsWith(f.PassengerName));
                        if (emd != null)
                        {
                            f.EMDNumber = new List<int>() { emd.DocumentNo };
                        }
                    }
                });
        }

        private List<IssueTicketDetails> GetTicketDataThroughTicketDisplay(List<IssueTicketError> errors, string token, Pcc pcc, string ticketingpcc)
        {
            List<IssueTicketDetails> ticketdata = new List<IssueTicketDetails>();
            var etfailederror = errors.
                                    Where(s => s.Error != null && s.Error.message.
                                        Contains("found. This document(s) was not present during initial reservation retrieval and "));

            if (etfailederror.IsNullOrEmpty())
            {
                return ticketdata;
            }

            List<string> tktnumbers = etfailederror.
                                        Where(w=> !string.IsNullOrEmpty(w.Error?.message)).
                                        SelectMany(s => s.Error.message.AllMatches(@"(\d{13})")).
                                        ToList();

            CancellationToken ct = new CancellationToken();

            ParallelOptions options = new ParallelOptions { CancellationToken = ct };

            Parallel.ForEach(tktnumbers, options, (tktnumber) =>
            {
                GetElectronicDocumentRS displayticketres = _displayTicket.DisplayDocument(token, pcc, tktnumber, ticketingpcc).GetAwaiter().GetResult();

                var doc = _displayTicket.PraseDisplatTicketResponseforIssueTicket(displayticketres.DocumentDetailsDisplay.Item, ticketingpcc);

                ticketdata.Add(doc);
            });

            return ticketdata;
        }

        private IssueTicketError GetIssueTicketError(string warning, EnhancedAirTicket.AirTicketRQ gDSRequest, IssueExpressTicketRQ rq)
        {
            IssueTicketError issueTicketError = new IssueTicketError();
            string temperroritemno = warning.LastMatch(@"^AirTicketLLS failed for \/Ticketing\[(\d+)\]");
            if (!temperroritemno.IsNullOrEmpty())
            {
                var erroreditem = gDSRequest.Ticketing[int.Parse(temperroritemno) - 1];
                issueTicketError.DocumentType = erroreditem.PricingQualifiers != null ?
                                                    erroreditem.PricingQualifiers.PriceQuote != null ?
                                                        Models.DocumentType.Quote :
                                                    erroreditem.MiscQualifiers.AirExtras != null ?
                                                        Models.DocumentType.EMD :
                                                        Models.DocumentType.Unknown :
                                                    Models.DocumentType.Unknown;
                issueTicketError.DocumentNumber = erroreditem.PricingQualifiers != null ?
                                                    erroreditem.PricingQualifiers.PriceQuote != null &&
                                                    erroreditem.PricingQualifiers.PriceQuote != null ?
                                                            erroreditem.PricingQualifiers.PriceQuote.SelectMany(m => m.Record).Select(s => s.Number).Distinct().ToList() :
                                                    erroreditem.PricingQualifiers.NameSelect != null ?
                                                            rq.Quotes.
                                                            Where(w =>
                                                                erroreditem.PricingQualifiers.NameSelect.Select(s => s.NameNumber).Contains(w.Passenger.NameNumber)).
                                                            Select(s => s.QuoteNo).
                                                            Distinct().
                                                                ToList() :
                                                            erroreditem.MiscQualifiers.AirExtras != null ?
                                                                erroreditem.MiscQualifiers.AirExtras.Select(e => e.Number).Distinct().ToList() :
                                                                new List<int>() :
                                                            new List<int>();
            }

            string[] delimiters =  {
                                @"AirTicketLLSRQ: ",
                                @"with Cause: ",
                                @"DesignatePrinterLLSRQ: ",
                                @"SabreCommandLLSRQ: ",
                                @"TicketingDocumentServicesRQ: "
                              };
            issueTicketError.Error = new WebtopError()
            {
                code = "TICKETING_ERROR",
                message = warning.
                            Split(delimiters, StringSplitOptions.RemoveEmptyEntries).
                            Last().
                            ReplaceAllSabreSpecialChar()
            };

            return issueTicketError;
        }


        private async Task AddDOBRemarks(IssueExpressTicketRQ request, string ticketingpcc, User user, Token token)
        {
            var dobquotes = request.Quotes.Where(w => w.Passenger.DOBChanged && w.Passenger.DOB.HasValue);
            if (!dobquotes.IsNullOrEmpty())
            {
                List<Remark> remarks = GetRemarks(
                                        dobquotes.
                                            Select(s => new Remark()
                                            {
                                                SegmentNumber = string.Join(",", dobquotes.First().Sectors.Select(sec => sec.PQSectorNo.ToString()).Distinct()),
                                                RemarkText = $"Date of birth for {s.Passenger.PassengerName} was updated by {user?.FullName ?? "Aeronology"} to {s.Passenger.DOB.Value:ddMMMyyyy}"
                                            }).
                                            ToList());

                AddRemarkRequest remarkrequest = new AddRemarkRequest()
                {
                    Locator = request.Locator,
                    Remarks = remarks
                };

                //Add Remarks for DOB
                await updatePNRService.AddGeneralDOBRemarks(token, ticketingpcc, remarkrequest);
            }
        }

        private async Task AddAgentCommissionRemarks(IssueExpressTicketRS ticketData, IssueExpressTicketRQ request, string ticketingpcc, User user, Token token)
        {
            var agtcomdata = from ticket in ticketData.Tickets.Where(w => w.DocumentType == "TKT" && w.QuoteRefNo != -1)
                             let quote = request.Quotes.First(f => f.QuoteNo == ticket.QuoteRefNo)
                             select new AgentCommissionData()
                             {
                                 DocumentNumber = ticket.DocumentNumber,
                                 IsFee = !quote.AgentCommissionRate.HasValue,
                                 AgentCommissionRate = quote.AgentCommissionRate,
                                 AgentFee = quote.Fee + (quote.FeeGST.HasValue ? quote.FeeGST.Value : 0.00M),
                                 CurrencyCode = "AUD"
                             };

            if (!agtcomdata.IsNullOrEmpty())
            {
                List<Remark> remarks = GetAGTCOMMRemarks(
                                        agtcomdata.
                                            Select(s => new Remark()
                                            {
                                                RemarkText = s.IsFee ?
                                                                $"AGTFEE TKT NO {s.DocumentNumber} {s.CurrencyCode}{s.AgentFee.Value}" :
                                                                $"AGTCOMM TKT NO {s.DocumentNumber} COMM {s.AgentCommissionRate:0.##}PCT"
                                            }).
                                            ToList());

                AddRemarkRequest remarkrequest = new AddRemarkRequest()
                {
                    Locator = request.Locator,
                    Remarks = remarks
                };

                //Add Remarks for agent commissions
                await updatePNRService.AddGeneralAGTCOMMRemarks(token, ticketingpcc, remarkrequest);
            }
        }


        private List<Remark> GetAGTCOMMRemarks(List<Remark> rems)
        {
            List<Remark> remarks = new List<Remark>();

            remarks.
                AddRange(
                    $"Aeronology Robotics-{DateTime.Now:ddMMMyyyy HHmm}".
                    SplitInChunk(65).
                    Select(rem => new Remark()
                    {
                        RemarkText = rem
                    }).
                    ToList());

            remarks.
                AddRange(
                    (from agtcommremark in rems
                     from agtcommremarkline in agtcommremark.RemarkText.SplitInChunk(65)
                     select new Remark()
                     {
                         RemarkText = agtcommremarkline
                     }).
                    ToList());
            return remarks;
        }

        private List<Remark> GetRemarks(List<Remark> rems)
        {
            List<Remark> remarks = new List<Remark>();

            remarks.
                AddRange(
                    $"Aeronology Robotics-{DateTime.Now:ddMMMyyyy HHmm}".
                    SplitInChunk(65).
                    Select(rem => new Remark()
                    {
                        RemarkText = rem,
                        SegmentNumber = string.Join(",", rems.SelectMany(s => s.SegmentNumber.Split(",")).Distinct())
                    }).
                    ToList());

            remarks.
                AddRange(
                    (from dobremark in rems
                     let segno = dobremark.SegmentNumber
                     from dobremarkline in dobremark.RemarkText.SplitInChunk(65)
                     select new Remark()
                     {
                         RemarkText = dobremarkline,
                         SegmentNumber = segno
                     }).
                    ToList());
            return remarks;
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

        private async Task ManualBuild(Pcc pcc, IssueExpressTicketRQ request, string statefultoken, IEnumerable<IssueExpressTicketQuote> manualquotes, PNR pnr, string ticketingpcc, string contextID, string ticketingprinter, string printerbypass)
        {
            //Assign printer
            await _sabreCommandService.
                        ExecuteCommand(
                            statefultoken,
                            pcc,
                            $"W*{printerbypass}",
                            ticketingpcc);

            await _sabreCommandService.
                        ExecuteCommand(
                            statefultoken,
                            pcc,
                            $"PTR/{ticketingprinter}",
                            ticketingpcc);

            int groupindex = 1;
            foreach (var quotegrp in request.Quotes.GroupBy(grp => grp.Passenger.PaxType))
            {
                IssueExpressTicketQuote quote = quotegrp.First();

                string command1 = $"W¥C¥" +
                                    //paxtype
                                    $"P{quote.Passenger.PaxType}" +
                                    //namenumber
                                    $"¥N{string.Join("/", quotegrp.Select(q=> q.Passenger.NameNumber))}" +
                                    //sectors
                                    $"¥S{string.Join("/", quote.Sectors.Select(s => s.PQSectorNo))}" +
                                    //plating carrier
                                    $"¥A{quote.PlatingCarrier.Trim().ToUpper()}";

                string response1 = await _sabreCommandService.
                        ExecuteCommand(
                            statefultoken,
                            pcc,
                            command1,
                            ticketingpcc);

                logger.LogInformation($"##### Manual build command 1 : {command1}");
                logger.LogInformation($"##### Manual build command 1 response : {response1}");

                string command2 = $"W¥I{groupindex}";

                //loop per sector to get farebasis NVA, NVB, Baggage
                int index = 1;
                foreach (var quoteSector in quote.Sectors)
                {
                    if (quoteSector.DepartureCityCode != "ARUNK")
                    {
                        string baggageallowance = string.IsNullOrEmpty(quoteSector.Baggageallowance) ? "" : $"*BA{quoteSector.Baggageallowance.RegexReplace(@"\s+", "").Replace("KG", "K")}";
                        string nvbnva = string.IsNullOrEmpty(quoteSector.NVB) && string.IsNullOrEmpty(quoteSector.NVA) ? "" : $"*{quoteSector.NVB}{quoteSector.NVA}";
                        command2 += $"¥L{index}" +//connection indicator
                                                 //farebasis
                                        $"-{quoteSector.FareBasis}" +
                                        //NVB, NVA
                                        nvbnva +
                                        //baggage allowance
                                        baggageallowance;
                    }
                    index++;
                }
                //base fare and currency
                command2 += $"¥Y{quote.BaseFareCurrency.Trim().ToUpper()}{quote.BaseFare}";

                //equiv fare and currency
                if (quote.EquivFare.HasValue && quote.EquivFare.Value > 0 && !string.IsNullOrEmpty(quote.EquivFareCurrency))
                {
                    command2 += $"¥E{quote.EquivFareCurrency.Trim().ToUpper()}{quote.EquivFare.Value}";
                }

                //taxes
                List<Tax> taxes = quote.
                                    Taxes.
                                    GroupBy(grp => grp.Code.Substring(0,2)).
                                    Select(s => new Tax()
                                    {
                                        Code = s.Key,
                                        Amount = s.Sum(t => t.Amount)
                                    }).
                                    ToList();

                if (!taxes.IsNullOrEmpty())
                {
                    if (taxes.Count > 16)
                    {
                        taxes = GroupTax(taxes);
                    }

                    command2 += string.Join("", taxes.Select(tax => $"/{tax.Amount}{tax.Code.Trim().ToUpper()}"));
                }

                //commission
                //int commission = quote.BSPCommissionRate.HasValue ? (int)quote.BSPCommissionRate.Value : 0;
                //command += $"¥KP{commission}";

                //tourcode
                string tourcodeprefix = GetTourCodePrefix(quote.FareType);
                if (!string.IsNullOrEmpty(quote.TourCode))
                {
                    command2 += $"¥{tourcodeprefix}*{quote.TourCode.Trim().ToUpper()}";
                }

                //farecalc  
                //max char limit = 246
                if(quote.FareCalculation.Trim().Count() > 246)
                {
                    throw new AeronologyException("FARECALC_TOO_LONG", "Fare calculation is too long.(max characters permited: 246)");
                }

                command2 += $"¥C{quote.FareCalculation.Trim().ToUpper()}";

                //endorsements
                //max char limit = 58
                string endos = string.Join("/", quote.Endorsements).Trim().ToUpper();
                if (endos.Trim().Count() > 246)
                {
                    throw new AeronologyException("FARECALC_TOO_LONG", "Fare calculation is too long.(max characters permited: 246)");
                }

                    command2 += $"¥EO/{endos}";
                    //command2 += $"¥EO/{endos.RegexReplace(@"\s*", "").Replace("NONREFUNDABLE", "NONREF").Replace("CARRIER", "CXR").Substring(0, 58)}";


                string response2 = await _sabreCommandService.
                                                ExecuteCommand(
                                                    statefultoken,
                                                    pcc,
                                                    command2,
                                                    ticketingpcc);


                logger.LogInformation($"##### Manual build command 1 : {command2}");
                logger.LogInformation($"##### Manual build command 1 response : {response2}");

                if (response2 != "OK")
                {
                    logger.LogInformation("##### INVALID_MANUAL_BUILD_RESPONSE #####");
                    logger.LogInformation(response2);
                    throw new GDSException("INVALID_MANUAL_BUILD_RESPONSE", "Unknown response received from GDS.");
                }
                groupindex++;
            }

            #region Mask
            //foreach (var quote in request.Quotes.GroupBy(grp => grp.Passenger.PaxType))
            //{
            //    string command = "W¥CTKT";
            //    //pax type and quantity added
            //    command += $"¥P{quote.First().Passenger.PaxType}" +
            //               $"¥N{string.Join("/", quote.Select(quo => quo.Passenger.NameNumber).Distinct())}";
            //    //sector no
            //    command += $"¥S{string.Join("/", quote.First().Sectors.Select(s => s.PQSectorNo))}";
            //    //validating carrier
            //    command += $"¥A{quote.First().PlatingCarrier}";


            //    //Create manual price quote shell
            //    string mask1 = await _sabreCommandService.
            //                            ExecuteCommand(
            //                                statefultoken,
            //                                pcc,
            //                                command,
            //                                ticketingpcc);

            //    SabreManualBuildScreen1 screen1 = new SabreManualBuildScreen1(mask1, quote.First());
            //    command = screen1.Command;

            //    string mask2 = await _sabreCommandService.
            //                            ExecuteCommand(
            //                                statefultoken,
            //                                pcc,
            //                                command,
            //                                ticketingpcc);

            //    string mask3 = "";
            //    if (screen1.AdditionalTaxPresent)
            //    {
            //        //Additional taxes
            //        List<Tax> taxes = quote.
            //                            First().
            //                            Taxes.
            //                            GroupBy(grp => grp.Code).
            //                            Skip(6).
            //                            Select(tax => new Tax()
            //                            {
            //                                Code = tax.Key,
            //                                Amount = tax.Sum(s => s.Amount)
            //                            }).
            //                            ToList();

            //        int additionaltaxcount = (int)Math.Ceiling((decimal)(taxes.Count() / 22));

            //        for (int i = 0; i < additionaltaxcount; i++)
            //        {
            //            int skipcount = additionaltaxcount > 1 ? (22 * (additionaltaxcount - i)) : 0;
            //            List<Tax> selectedtaxes = taxes.
            //                                        Skip(skipcount).
            //                                        Take(22).
            //                                        ToList();

            //            mask3 = await HandleAdditionalTax(
            //                                                                selectedtaxes, 
            //                                                                mask2,
            //                                                                statefultoken,
            //                                                                pcc,
            //                                                                ticketingpcc);
            //        }
            //    }

            //    //connectionindicator, farebasis, NVA, NVB, baggage, farecalc
            //    SabreManualBuildScreen3 screen3 = new SabreManualBuildScreen3(string.IsNullOrEmpty(mask3) ? mask2 : mask3, quote.First());
            //    command = screen3.Command;

            //    string mask4 = await _sabreCommandService.
            //                            ExecuteCommand(
            //                                statefultoken,
            //                                pcc,
            //                                command,
            //                                ticketingpcc);

            //    SabreManualBuildScreen4 screen4 = new SabreManualBuildScreen4(mask4, quote.First());
            //    command = screen4.Command;

            //    string mask5 = await _sabreCommandService.
            //                            ExecuteCommand(
            //                                statefultoken,
            //                                pcc,
            //                                command,
            //                                ticketingpcc);

            //    if (quote.First().FareType == FareType.IT ||
            //       quote.First().FareType == FareType.BT ||
            //       !string.IsNullOrEmpty(quote.First().TourCode))
            //    {
            //        command = "W¥I¥";
            //        //faretype - IT
            //        if (quote.First().FareType == FareType.IT)
            //        {
            //            command += "¥UX*";
            //        }
            //        //faretype - BT
            //        else if (quote.First().FareType == FareType.BT)
            //        {
            //            command += "¥UB*";
            //        }
            //        else
            //        {
            //            command += "¥UN*";
            //        }
            //        //tourcode
            //        if (!string.IsNullOrEmpty(quote.First().TourCode))
            //        {
            //            command += $"{quote.First().TourCode}";
            //        }

            //        string response = await _sabreCommandService.
            //                                    ExecuteCommand(
            //                                        statefultoken,
            //                                        pcc,
            //                                        command,
            //                                        ticketingpcc);
            //    }
            //}
            #endregion

            //receieve and end transact
            await enhancedEndTransService.EndTransaction(statefultoken, contextID, agent?.FullName ?? "Aeronology", true, pcc);
        }

        private string GetTourCodePrefix(FareType fareType)
        {
            switch (fareType)
            {
                case FareType.IT:
                    return "UI";
                case FareType.BT:
                    return "UB";
                case FareType.Published:
                case FareType.NR:
                default:
                    return "UN";
            };
        }

        private List<Tax> GroupTax(List<Tax> taxes)
        {
            throw new AeronologyException("TAXES_MORE_THAN_16", "More than 16 tax codes found. Please group the tax codes before procceed.");
        }

        private async Task<string> HandleAdditionalTax(
                List<Tax> taxes, 
                string mask2,
                string statefultoken,
                Pcc pcc,
                string ticketingpcc)
        {
            SabreManualBuildAdditinalTax screen2 = new SabreManualBuildAdditinalTax(
                                                                mask2,
                                                                taxes);

            return await _sabreCommandService.
                            ExecuteCommand(
                                statefultoken,
                                pcc,
                                screen2.Command,
                                ticketingpcc);
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

                IEnumerable<IssueExpressTicketQuote> quotequery = null;
                if (request.Quotes.IsNullOrEmpty())
                {
                    quotequery = requestquotes;
                }
                else
                {
                    quotequery = from quo in request.Quotes
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
                                     BaseFareCurrency = rqquo.BaseFareCurrency,
                                     EquivFare = rqquo.EquivFare,
                                     EquivFareCurrency = rqquo.EquivFareCurrency,
                                     FiledFare = rqquo.FiledFare,
                                     PendingSfData = rqquo.PendingSfData,
                                     PlatingCarrier = rqquo.PlatingCarrier,
                                     Route = rqquo.Route,
                                     SectorCount = rqquo.SectorCount,
                                     Sectors = quo.Sectors.IsNullOrEmpty() ? rqquo.Sectors : quo.Sectors,
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
                                     ApplySupressITFlag = rqquo.ApplySupressITFlag,
                                     PriceType = rqquo.PriceType,
                                     Taxes = rqquo.Taxes,
                                     FareCalculation = quo.FareCalculation
                                 };
                }

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

        private async Task<IssueTicketTransactionData> HandleMerchantPayment(string sessionid, IssueExpressTicketRQ request, IssueTicketTransactionData tickettransData, string contextId)
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
                                    Select(tkt => new Models.VoidTicket()
                                    {
                                        DocumentNumber = tkt.DocumentNumber,
                                        DocumentType = tkt.DocumentType,
                                        IssuingPCC = tkt.IssuingPCC
                                    }).
                                    ToList()
                    },
                    contextId);

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

        public async Task<List<VoidTicketResponse>> VoidTicket(VoidTicketRequest request, string contextId)
        {
            SabreSession token = null;

            List<SabreVoidTicketResponse> sabreVoidTicketResponses = new List<SabreVoidTicketResponse>();
            List<VoidTicketResponse> voidTicketResponses = new List<VoidTicketResponse>();

            try
            {
                //Obtain session
                token = await _sessionCreateService.CreateStatefulSessionToken(pcc, request.Locator);

                //remove duplicate tickets in the request
                request.Tickets = request.
                                    Tickets.
                                    GroupBy(d => new { d.DocumentNumber, d.DocumentType }).
                                    Select(s => s.First()).
                                    ToList();

                //Get existing tickets on PNR
                List<TicketData> pnrtkts = await GetTicketsFromGDS(request.Locator, token.SessionID);


                foreach (var tkt in request.Tickets.GroupBy(g => g.IssuingPCC))
                {
                    try
                    {
                        //Emulate to issuing PCC
                        await _changeContextService.ContextChange(token, pcc, tkt.Key, request.Locator);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "PLEASE FINISH OR IGNORE THE CURRENT TRANSACTION")
                        {
                            //Ignore Transaction
                            await _ignoreTransactionService.Ignore(token.SessionID, pcc);

                            //Emulate to issuing PCC
                            await _changeContextService.ContextChange(token, pcc, tkt.Key, request.Locator);

                        }
                        throw;
                    }

                    //Execute display price quote command
                    string text = await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, $"*{request.Locator}", tkt.Key);

                    if ((text.Contains("ADDR") ||
                        text.Contains("UTL PNR") ||
                        text.Contains("SECURED PNR")))
                    {
                        throw new GDSException("50000035", text);
                    }

                    var tktitems = tkt.
                                    Where(p => !string.IsNullOrEmpty(p.DocumentNumber)).
                                    Select(s => s.DocumentNumber.SplitOn("|").First()).
                                    Distinct().
                                    ToList();

                    bool inpnrevaluated = false;
                    bool inpnr = true;
                    int counter = 1;

                    foreach (string tktno in tktitems.OrderByDescending(o => o))
                    {
                        string doctype = tkt.First(f => f.DocumentNumber == tktno).DocumentType;
                        if (!inpnr)
                        {
                            //Execute display pnr command
                            text = await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, $"*{request.Locator}", tkt.Key);

                            if ((text.Contains("ADDR") ||
                                text.Contains("UTL PNR") ||
                                text.Contains("SECURED PNR")))
                            {
                                throw new GDSException("50000035", text);
                            }
                        }

                        var pnrtkt = pnrtkts.FirstOrDefault(f => f.DocumentNumber == tktno);

                        if (pnrtkt == null)
                        {
                            sabreVoidTicketResponses.
                                Add(new SabreVoidTicketResponse
                                        (
                                            tktno,
                                            doctype,
                                            null,
                                            request.Locator,
                                            $"Ticket {tktno} not found on PNR({request.Locator})."
                                        )
                                    );
                            continue;
                        }

                        if (pnrtkt != null && pnrtkt.Voided)
                        {
                            sabreVoidTicketResponses.
                               Add(new SabreVoidTicketResponse
                                   (
                                        tktno,
                                        doctype,
                                        null,
                                        request.Locator,
                                        $"Ticket {pnrtkt.DocumentNumber} already voided.",
                                        true)
                                   );
                            continue;
                        }

                        string rphno = pnrtkt.RPH.ToString();

                        //void ticket
                        SabreVoidTicketResponse voidres = await voidTicketService.
                                                                    VoidTicket(
                                                                        request.Locator,
                                                                        tktno,
                                                                        doctype,
                                                                        rphno,
                                                                        token.SessionID,
                                                                        pcc, tkt.Key);

                        //reissue ticket diplay for coupon status update on original ticket 
                        if (request.Tickets.First(f => f.DocumentNumber == tktno).DocumentType.ToUpper() == "REI" && counter < tktitems.Count())
                        {
                            await Task.Delay(2000);
                        }

                        if (!inpnrevaluated)
                        {
                            //check if we are still on PNR
                            string response = await _sabreCommandService.ExecuteCommand(token.SessionID, pcc, "*R", tkt.Key);

                            inpnr = response != "NO DATA";
                            inpnrevaluated = true;
                        }

                        sabreVoidTicketResponses.
                               Add(voidres);
                        counter++;
                    }

                    if (!sabreVoidTicketResponses.IsNullOrEmpty() &&
                    sabreVoidTicketResponses.Any(a => a.Success))
                    {
                        try
                        {
                            //End the transaction
                            await enhancedEndTransService.EndTransaction(token.SessionID, contextId, "Aeronology", true);
                        }
                        catch (GDSException gdsex)
                        {
                            logger.LogError(gdsex);
                            sabreVoidTicketResponses.
                                Where(w => w.Success).
                                ToList().
                                ForEach(f => f.Errors = new List<string>() { $"End Transaction Error: {gdsex.Message}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                sabreVoidTicketResponses.
                                Add(new SabreVoidTicketResponse
                                    (
                                      "",
                                      "",
                                      null,
                                      request.Locator,
                                      $"There was a GDS issue encountered while voiding the ticket(s) on PNR {request.Locator}."));
            }
            finally
            {
                if (token != null && token.IsLimitReached)
                {
                    await _sessionCloseService.SabreSignout(token.SessionID, pcc);
                }
            }

            var query = from voidtkt in request.Tickets
                        let voidtktres = sabreVoidTicketResponses.FirstOrDefault(f => voidtkt.DocumentNumber.StartsWith(f.DocumentNumber))
                        where voidtktres != null
                        select new VoidTicketResponse()
                        {
                            DocumentNumber = voidtkt.DocumentNumber,
                            DocumentType = voidtkt.DocumentType,
                            AlreadyVoided = voidtktres.Alreadyvoided,
                            Voided = voidtktres.Success,
                            Errors = voidtktres.Errors
                        };

            voidTicketResponses = query.ToList();

            //reporting Reissue EMDs
            return HandleReissueEMD(request, voidTicketResponses);
        }

        private async Task<List<TicketData>> GetTicketsFromGDS(string locator, string token)
        {
            //Execute display price quote command
            string text = await _sabreCommandService.ExecuteCommand(token, pcc, $"*{locator}", "9SNJ");

            if ((text.Contains("ADDR") ||
                text.Contains("UTL PNR") ||
                text.Contains("SECURED PNR")))
            {
                throw new GDSException("50000035", text);
            }

            text = await _sabreCommandService.ExecuteCommand(token, pcc, "*T", "9SNJ");

            await _sabreCommandService.ExecuteCommand(token, pcc, "I", "9SNJ");

            List<TicketData> pnrtkts = new T(text).Tickets;
            return pnrtkts;
        }


        private static List<VoidTicketResponse> HandleReissueEMD(VoidTicketRequest request, List<VoidTicketResponse> voidTicketResponses)
        {
            List<VoidTicketResponse> tempvoidTicketResponses = new List<VoidTicketResponse>();
            tempvoidTicketResponses.AddRange(voidTicketResponses);
            var reitkts = voidTicketResponses.Where(w => w.DocumentType.ToUpper() == "REI");
            foreach (var reitkt in reitkts.Select(s => s.DocumentNumber))
            {
                Models.VoidTicket voidtktrq = request.Tickets.First(f => f.DocumentNumber == reitkt);

                if (!voidtktrq.LinkedDocuments.IsNullOrEmpty())
                {
                    tempvoidTicketResponses.
                        AddRange(voidtktrq.
                                    LinkedDocuments.
                                    Select(linkdoc => new VoidTicketResponse()
                                    {
                                        DocumentNumber = linkdoc.DocumentNumber,
                                        DocumentType = "EMD",
                                        Voided = true
                                    }).
                                    ToList());

                }
            }

            return tempvoidTicketResponses;
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
                            ROE = string.IsNullOrEmpty(q.PQ.FareCalculation) ? "1" : q.PQ.FareCalculation.LastMatch(@"ROE\s*([\d\.]+)", "1"),
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
                            Where(w => w.DepartureCityCode != "ARUNK").
                            Select(s => pnr.
                                            Sectors.
                                            First(f =>  f.From == s.DepartureCityCode &&
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
                    Sectors = (from pqsecs in quote.QuoteSectors.Where(w => w.DepartureCityCode != "ARUNK")
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
                    BspCommission = quote.CAT35 ? quote.BspCommissionRate: default,
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
                                BaseFareCurrency = string.IsNullOrEmpty(quote.CurrencyCode) ? "AUD" : quote.CurrencyCode,
                                EquivFare = quote.EquivFare,
                                EquivFareCurrency = quote.EquivFareCurrencyCode,
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
                                ApplySupressITFlag = applySupressITFlag,
                                Taxes = quote.Taxes,
                                PriceType = quote.PriceType
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
                BookingClass = secs.Select(s => s.Class).Distinct().ToArray(),
                FareBasis = quote.QuoteSectors.Select(pqsec => pqsec.FareBasis).Distinct().ToArray()
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

    internal class CommissionData
    {
        public int QuoteNo { get; set; }
        public decimal? AgentCommissionRate { get; set; }
        public decimal? BspCommissionRate { get; set; }
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

    internal class AgentCommissionData
    {
        public string DocumentNumber { get; set; }
        public bool IsFee { get; set; }
        public decimal? AgentCommissionRate { get; set; }
        public string CurrencyCode { get; set; }
        public decimal? AgentFee { get; set; }
    }


    #region AirTicketRQResponse JSON proxy
    public class Success
    {
        public DateTime timeStamp { get; set; }
    }

    public class Message
    {
        public string code { get; set; }
        public string content { get; set; }
    }

    public class SystemSpecificResult
    {
        public List<Message> Message { get; set; }
    }

    public class Warning
    {
        public string type { get; set; }
        public DateTime timeStamp { get; set; }
        public List<SystemSpecificResult> SystemSpecificResults { get; set; }
    }

    public class Error
    {
        public string type { get; set; }
        public DateTime timeStamp { get; set; }
        public List<SystemSpecificResult> SystemSpecificResults { get; set; }
    }

    public class ApplicationResults
    {
        public string status { get; set; }
        public List<Success> Success { get; set; }
        public List<Warning> Warning { get; set; }
        public List<Error> Error { get; set; }
    }

    public class Reservation
    {
        public string content { get; set; }
    }

    internal class TotalAmount
    {
        public string currencyCode { get; set; }
        public int decimalPlace { get; set; }
        public string content { get; set; }
    }

    internal class Summary
    {
        public string DocumentNumber { get; set; }
        public DateTime LocalIssueDateTime { get; set; }
        public string DocumentType { get; set; }
        public string IssuingLocation { get; set; }
        public Reservation Reservation { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public TotalAmount TotalAmount { get; set; }
    }

    internal class AirTicketRS
    {
        public ApplicationResults ApplicationResults { get; set; }
        public List<Summary> Summary { get; set; }
    }

    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
    }

    internal class AirTicketRQResponse
    {
        public AirTicketRS AirTicketRS { get; set; }
        public List<Link> Links { get; set; }
    }
    #endregion
}
