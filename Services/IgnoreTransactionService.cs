using System.Threading.Tasks;
using System.ServiceModel;
using System.Linq;
using SabreWebtopTicketingService.Common;
using System;
using IgnoreTransaction;
using SabreWebtopTicketingService.Models;
using SabreWebtopTicketingService.CustomException;

namespace SabreWebtopTicketingService.Services
{
    public class IgnoreTransactionService: ConnectionStubs, IDisposable
    {
        private readonly SessionDataSource sessionData;
        private readonly ILogger logger;
        private readonly string url;

        public IgnoreTransactionService(
            SessionDataSource sessionData,
            ILogger logger)
        {
            this.sessionData = sessionData;
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        internal Security1 getsecurityheader(string token)
        {
            return new Security1()
            {
                BinarySecurityToken = token
            };
        }

        internal MessageHeader getMessageHeader(Pcc pcc)
        {
            return new MessageHeader()
            {
                version = Constants.IgnoreTransactionVersion,
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
                Action = "IgnoreTransactionLLSRQ",
                CPAId = pcc.PccCode,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "StatefulIgnoreTransaction"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.GetSabreDatetime()
                }
            };
        }

        internal IgnoreTransactionRQ airPriceRQ => new IgnoreTransactionRQ()
        {
            Version = Constants.IgnoreTransactionVersion
        };


        public async Task<bool> Ignore(string token, Pcc pcc)
        {
            IgnoreTransactionPortTypeClient client = null;
            try
            {
                EnableTLS();

                client = new IgnoreTransactionPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));
                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                var result = await client.
                                    IgnoreTransactionRQAsync(
                                        getMessageHeader(pcc),
                                        getsecurityheader(token),
                                        airPriceRQ);


                if (result.IgnoreTransactionRS.ApplicationResults.status != CompletionCodes.Complete)
                {
                    var messages = result.
                                    IgnoreTransactionRS.
                                    ApplicationResults.
                                    Error.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(r => r.Message);

                    throw new GDSException(
                        string.Join(Environment.NewLine, messages.Select(s => s.code)),
                        string.Join(Environment.NewLine, messages.Select(s => s.Value)));
                }

                return result.IgnoreTransactionRS.ApplicationResults.status == CompletionCodes.Complete;

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

        public void Dispose()
        {

        }
    }
}
