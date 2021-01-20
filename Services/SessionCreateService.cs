using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using SessionCreate;

namespace SabreWebtopTicketingService.Services
{
    public class SessionCreateService : ConnectionStubs
    {
        private readonly IDbCache _dbCache;
        private readonly SessionDataSource _sessionDataSource;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger logger;
        private readonly ISessionManagementBackgroundTaskQueue _sessionTaskQueue;
        private readonly SessionCloseService _sessionCloseService;

        public SessionCreateService(
                        IDbCache dbCache,
                        ILogger _logger,
                        SessionDataSource sessionDataSource,
                        IHttpClientFactory httpClientFactory,
                        ISessionManagementBackgroundTaskQueue sessionTaskQueue,
                        SessionCloseService sessionCloseService)
        {
            _dbCache = dbCache;
            _sessionDataSource = sessionDataSource;
            _httpClientFactory = httpClientFactory;
            _sessionTaskQueue = sessionTaskQueue;
            logger = _logger;
            _sessionCloseService = sessionCloseService;
        }        

        public async Task<SabreSession> CreateStatefulSessionToken(Pcc defaultwspcc, string locator = "", bool NoCache = false)
        {
            SessionCreatePortTypeClient client = null;
            var userName = defaultwspcc.Username;
            var password = defaultwspcc.Password;
            string pcc = defaultwspcc.PccCode;
            var url = Constants.GetSoapUrl();
            string accessKey = $"{defaultwspcc.PccCode}-{locator}".EncodeBase64();
            try
            {

                SabreSession sabreSession;
                if (!NoCache || !string.IsNullOrEmpty(locator))
                {
                    #region Retrieve session from dynamo db
                    //Check token in cache
                    sabreSession = await _dbCache.Get<SabreSession>(accessKey, "sabre_session");

                    if (sabreSession != null && !sabreSession.Expired)
                    {
                        sabreSession.Stored = true;
                        await _dbCache.InsertOrUpdate(accessKey, sabreSession, "sabre_session");
                        return sabreSession;
                    }
                    #endregion
                }

                EnableTLS();

                client = new SessionCreatePortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = userName;
                client.ClientCredentials.UserName.Password = password;

                var header = CreateHeader(pcc);
                var security = CreateSecurityCredentials(userName, password, pcc);
                var request = CreateSessionRequest(pcc);

                var response = await client.SessionCreateRQAsync(header, security, request);

                if (response.SessionCreateRS.Errors != null && response.SessionCreateRS.Errors.Error != null)
                {
                    throw new Exception(response.SessionCreateRS.Errors.Error.ErrorInfo.Message);
                }

                string token = response.Security.BinarySecurityToken;

                await client.CloseAsync();

                #region store session in dynamo db cache
                sabreSession = new SabreSession()
                {
                    SessionID = token,
                    ConsolidatorPCC = defaultwspcc.PccCode,
                    CreatedDateTime = DateTime.Now,
                    CurrentPCC = defaultwspcc.PccCode,
                    Locator = locator,
                };

                if (!NoCache)
                {
                    //Check session limit and signout all expired if treshold is met
                    await CheckSessionLimits(defaultwspcc, sabreSession);

                    //Save to cache if session limit is not yet reached
                    if (!sabreSession.IsLimitReached)
                    {
                        await _dbCache.InsertOrUpdate(accessKey, sabreSession, "sabre_session");
                    }
                }
                #endregion

                return sabreSession;
            }
            catch (Exception ex)
            {
                client.Abort();
                throw ex;
            }
        }

        private async Task CheckSessionLimits(Pcc pcc, SabreSession sabreSession)
        {
            var pccKey = pcc.PccCode.EncodeBase64();
            var sessionsCount = await _dbCache.SabreSessionCount(pccKey);
            var sessionLimit = Convert.ToInt32(Environment.GetEnvironmentVariable(Constants.SABRE_SESSION_LIMIT) ?? "25");

            logger.LogInformation($"Consolidator: {pcc.ConsolidatorId}. Pcc: {pcc.PccCode}. SesssionCount: {sessionsCount}. SessionLimit:{sessionLimit}");

            if (sessionsCount >= sessionLimit)
            {
                sabreSession.IsLimitReached = true;
            }

            var sessionsInCache = await _dbCache.ListSabreSessions(pccKey, "sabre_session");
            IEnumerable<SabreSession> expiredSessions = sessionsInCache.Where(x => x.Expired);

            foreach (var sSession in expiredSessions)
            {
                _sessionTaskQueue.QueueBackgroundWorkItem(async signOut => {
                    await CloseSession(pcc, sSession);
                });
            }
        }

        private async Task CloseSession(Pcc pcc, SabreSession sabreSession)
        {
            try
            {
                await _sessionCloseService.SabreSignout(sabreSession.SessionID, pcc);
            }
            catch (Exception ex)
            {
                logger.LogError("An error occured closing the sabre session/removing from cache. {Errormessage} {Exception", ex.Message, ex);
            };
        }

        public async Task<Token> CreateStatelessSessionToken(Pcc pcc)
        {
            //Check in cache
            Token token = await _dbCache.Get<Token>($"{pcc.PccCode}-RESTTOKEN", "sabre_token");
            if (token != null)
            {
                return token;
            }

            //Reference: https://beta.developer.sabre.com/guides/travel-agency/how-to/get-token
            string url = Constants.GetRestUrl();
            var client = _httpClientFactory.CreateClient();

            //URL
            var uri = new Uri(url.Trim() + "/v2/auth/token");

            //Authorization Header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                SabreSharedServices.GetSabreToken(pcc.Username, pcc.Password, pcc.PccCode));

            // Add an Accept header for content type
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            var form = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" }
                };

            // List data response.
            HttpResponseMessage response = await client.PostAsync(uri, new FormUrlEncodedContent(form));

            response.EnsureSuccessStatusCode();

            using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            {
                var content = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(content)) { throw new GDSException("TOKEN_NOT_RETURN", "Stateless session token was not returned."); }

                token = JsonSerializer.Deserialize<Token>(content);

                //cache token                    
                await _dbCache.InsertOrUpdate($"{pcc.PccCode}-RESTTOKEN", token, "sabre_token");

                return token;
            }
        }

        private SessionCreateRQ CreateSessionRequest(string pcc)
        {
            return new SessionCreateRQ()
            {
                POS = new SessionCreateRQPOS()
                {
                    Source = new SessionCreateRQPOSSource()
                    {
                        PseudoCityCode = pcc
                    }
                }
            };
        }


        private MessageHeader CreateHeader(string pcc)
        {
            return new MessageHeader()
            {
                version = "1.0.0",
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
                Action = "SessionCreateRQ",
                CPAId = pcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "StatefulSessionCreateRQ"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd")
                }
            };
        }

        private Security CreateSecurityCredentials(string username, string password, string pcc)
        {
            return new Security()
            {
                UsernameToken = new SecurityUsernameToken()
                {
                    Username = username,
                    Password = password,
                    Organization = pcc,
                    Domain = "DEFAULT"
                }
            };
        }        
    }
}

