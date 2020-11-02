using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using SessionClose;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Services
{
    public class SessionCloseService : ConnectionStubs
    {
        private readonly string url;
        private readonly ILogger logger;

        public SessionCloseService(
            ILogger _logger)
        {
            url = Constants.GetSoapUrl();
            logger = _logger;
        }

        public async Task SabreSignout(string token, Pcc pcc)
        {
            if (token.IsNullOrEmpty()) { return; }
            SessionClosePortTypeClient client = null;

            try
            {
                EnableTLS();

                client = new SessionClosePortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                var header = CreateHeader(pcc.PccCode);
                var request = CreateRequest(pcc.PccCode);

                var resp = await client.SessionCloseRQAsync(header, new Security { BinarySecurityToken = token }, request);

                if (resp.SessionCloseRS.status != "Approved")
                {
                    throw new Exception(resp.SessionCloseRS.Errors.Error.ErrorMessage);
                }

                client.Close();
            }
            catch (TimeoutException timeProblem)
            {
                logger.LogError(timeProblem);
                client.Abort();
                throw new GDSException("30000025", "Sabre system timeout. Please try again!");
            }
            catch (FaultException unknownFault)
            {
                logger.LogError(unknownFault);
                client.Abort();
                throw new GDSException("30000026", $"Sabre System Exception: {unknownFault.Message + (unknownFault.InnerException == null ? "" : Environment.NewLine + unknownFault.InnerException.Message)}");
            }
            catch (CommunicationException commProblem)
            {
                logger.LogError(commProblem);
                client.Abort();
                throw new GDSException("30000027", "There is a communication issue with Sabre. Please try again later!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                client.Abort();
                throw;
            }
        }

        private MessageHeader CreateHeader(string pcc)
        {
            return new MessageHeader()
            {
                version = Constants.SessionCloseVersion,
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
                Action = "SessionCloseRQ",
                CPAId = pcc,
                ConversationId = Guid.NewGuid().ToString(),
                Service = new Service()
                {
                    Value = "Stateful"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.GetSabreDatetime()
                }
            };
        }

        private SessionCloseRQ CreateRequest(string pcc)
        {
            return new SessionCloseRQ()
            {
                POS = new SessionCloseRQPOS
                {
                    Source = new SessionCloseRQPOSSource()
                    {
                        PseudoCityCode = pcc
                    }
                }
            };
        }
    }
}
