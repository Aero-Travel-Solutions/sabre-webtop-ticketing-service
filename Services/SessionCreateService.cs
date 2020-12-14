﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Text.Json;
using System.Threading.Tasks;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using SessionCreate;

namespace SabreWebtopTicketingService.Services
{
    public class SessionCreateService : ConnectionStubs
    {
        private readonly DbCache _dbCache;
        private readonly SessionDataSource _sessionDataSource;
        private readonly IHttpClientFactory _httpClientFactory;

        public SessionCreateService(
                        DbCache dbCache,
                        SessionDataSource sessionDataSource,
                        IHttpClientFactory httpClientFactory)
        {
            _dbCache = dbCache;
            _sessionDataSource = sessionDataSource;
            _httpClientFactory = httpClientFactory;
        }        

        public async Task<SabreSession> CreateStatefulSessionToken(Pcc defaultwspcc, string locator, bool NoCache = false)
        {
            SessionCreatePortTypeClient client = null;
            var userName = defaultwspcc.Username;
            var password = defaultwspcc.Password;
            string pcc = defaultwspcc.PccCode;
            var url = Constants.GetSoapUrl();
            string accessKey = $"{defaultwspcc.PccCode}-{locator}".EncodeBase64();

            SabreSession sabreSession = null;

            try
            {
                if (!NoCache)
                {
                    #region Retrieve ession from dynamo db
                    //Check token in cache
                    sabreSession = await _dbCache.Get<SabreSession>(accessKey);

                    if (sabreSession != null && !sabreSession.Expired)
                    {
                        sabreSession.Stored = true;
                        await _dbCache.InsertUpdateSabreSession(sabreSession, accessKey);
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

                await _dbCache.InsertUpdateSabreSession(sabreSession, accessKey);
                #endregion

                return sabreSession;
            }            
            catch (Exception ex)
            {                
                client.Abort();
                throw ex;
            }
        }

        public async Task<Token> CreateStatelessSessionToken(Pcc pcc)
        {
            //Check in cache
            Token token = await _dbCache.Get<Token>($"{pcc.PccCode}-RESTTOKEN");
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
                await _dbCache.Set<Token>(token, $"{pcc.PccCode}-RESTTOKEN", 10080);

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

