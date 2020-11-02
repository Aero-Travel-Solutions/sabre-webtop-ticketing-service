using System;
using System.ServiceModel;
using System.Threading.Tasks;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Models;
using SessionCreate;

namespace SabreWebtopTicketingService.Services
{
    public class SessionCreateService : ConnectionStubs
    {
        public SessionCreateService()
        {
            
        }        

        public async Task<SabreSession> CreateStatefulSessionToken(Pcc defaultwspcc)
        {
            SessionCreatePortTypeClient client = null;
            var userName = defaultwspcc.Username;
            var password = defaultwspcc.Password;
            string pcc = defaultwspcc.PccCode;
            var url = Constants.GetSoapUrl();

            try
            {                

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

                SabreSession sabreSession = new SabreSession()
                {
                    SabreSessionID = token
                };
                return sabreSession;
            }            
            catch (Exception ex)
            {                
                client.Abort();
                throw ex;
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

