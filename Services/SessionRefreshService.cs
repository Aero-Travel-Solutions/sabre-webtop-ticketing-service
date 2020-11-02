using SabreWebtopTicketingService.Common;
using SessionPingService;
using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Linq;
using SabreWebtopTicketingService.Models;
using System.Reflection.Metadata;

namespace SabreWebtopTicketingService.Services
{
    public class SessionRefreshService : ConnectionStubs
    {
        public SessionRefreshService()
        {

        }

        public async Task<bool> RefreshSessionToken(string token, Pcc defaultwspcc)
        {            
            OTA_PingPortTypeClient client = null;

            var userName = defaultwspcc.Username;
            var password = defaultwspcc.Password;
            string pcc = defaultwspcc.PccCode;
            var url = Constants.GetSoapUrl();

            try
            {

                EnableTLS();

                client = new OTA_PingPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = userName;
                client.ClientCredentials.UserName.Password = password;

                var header = CreateHeader(pcc);
                var security = CreateSecurityCredentials(token);
                var request = CreateSessionRequest();

                var response = await client.OTA_PingRQAsync(header, security, request.OTA_PingRQ);

                if (response is { } && response.OTA_PingRS is { } && !response.OTA_PingRS.Items.IsNullOrEmpty())
                {
                    var successType = response.OTA_PingRS.Items.Where(s => s is SuccessType).FirstOrDefault();
                    var echoToken = response.OTA_PingRS.Items.Where(e => e is string && (e as string) == "Aero Ping").FirstOrDefault();

                    await client.CloseAsync();

                    if (successType is { } && echoToken is { })
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    throw new Exception($"Unable to refresh seession {token}");
                }                
            }
            catch (Exception ex)
            {
                client.Abort();
                throw ex;
            }
        }

        private OTA_PingRQRequest CreateSessionRequest()
        {
            return new OTA_PingRQRequest()
            {
                OTA_PingRQ = new OTA_PingRQ()
                {
                    EchoData = "Aero Ping",
                    Version = "1.0.0",
                    TimeStamp = DateTime.UtcNow
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
                Action = "OTA_PingRQ",
                CPAId = pcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "OTA_PingRQ"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd")
                }
            };
        }

        private Security CreateSecurityCredentials(string token)
        {
            return new Security()
            {
                BinarySecurityToken = new SecurityBinarySecurityToken
                {
                    Value = token
                }
            };
        }
    }
}
