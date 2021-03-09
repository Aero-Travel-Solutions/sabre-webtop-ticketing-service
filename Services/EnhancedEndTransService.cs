using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using EnhancedEndTransactionService;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;

namespace SabreWebtopTicketingService.Services
{
    public class EnhancedEndTransService : ConnectionStubs
    {
        private readonly SessionDataSource sessionData;

        private readonly ConsolidatorPccDataSource pccSource;

        private readonly ILogger logger;

        private readonly string url;

        public EnhancedEndTransService(
            SessionDataSource sessionData,
            ConsolidatorPccDataSource pccSource,
            ILogger logger)
        {
            this.sessionData = sessionData;
            this.pccSource = pccSource;
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        public async Task EndTransaction(string token, string contextID, string receivedby, string SessionID = "", bool receiveChanges = false, Models.Pcc webservicepcc = null)
        {
            EnhancedEndTransactionPortTypeClient client = null;
            try
            {
                EnableTLS();

                var pcc = webservicepcc ?? (string.IsNullOrEmpty(SessionID) ?
                                    throw new AeronologyException("NO_SESSION_ID", "Session ID not found.") :
                                    await pccSource.GetWebServicePccByGdsCode("1W", contextID, token));

                client = new EnhancedEndTransactionPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));
                
                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;
                client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));
                logger.LogInformation($"{nameof(EndTransaction)} invoke \"EnhancedEndTransactionRQAsync\"");
                var sw = Stopwatch.StartNew();

                var result = await client.
                                    EnhancedEndTransactionRQAsync(
                                        CreateHeader(pcc.PccCode),
                                        new Security { BinarySecurityToken = token },
                                        CreateRequest(receivedby, receiveChanges));

                logger.LogInformation($"{nameof(EndTransaction)} \"EndTransactionRQAsync\" completed is {sw.ElapsedMilliseconds} ms");
                sw.Stop();

                if (result == null || result.EnhancedEndTransactionRS.ApplicationResults.status != CompletionCodes.Complete)
                {
                    List<MessageCondition> errormessages = new List<MessageCondition>();
                    errormessages.
                        AddRange(result.
                                    EnhancedEndTransactionRS.
                                    ApplicationResults.
                                    Error.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message).
                                    ToList());

                    errormessages.
                        AddRange(result.
                                    EnhancedEndTransactionRS.
                                    ApplicationResults.
                                    Warning.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message).
                                    ToList());

                    throw new GDSException(
                                "END_TRANSACT_ERROR",
                                string.Join(",",
                                            errormessages.
                                            Select(s => s.Value).
                                            Where(w => !w.Contains("Unable to recover from EndTransactionLLSRQ") ||
                                                       !w.Contains("Please see below messages for details") ||
                                                       !w.Contains("EndTransactionLLSRQ: CQT RCRD")).
                                            Distinct()));

                }

                client.Close();
            }
            catch (TimeoutException timeProblem)
            {
                logger.LogError(timeProblem.Message);
                if (client != null)
                {
                    client.Abort();
                }
                throw new GDSException("SABRE_TIMEOUT", "Sabre system timeout. Please try again!");
            }
            catch (FaultException unknownFault)
            {
                logger.LogError(unknownFault.Message);
                if (client != null)
                {
                    client.Abort();
                }
                throw new GDSException("SABRE_SYSTEM_EXCEPTION", $"Sabre System Exception: {unknownFault.Message + (unknownFault.InnerException == null ? "" : Environment.NewLine + unknownFault.InnerException.Message)}");
            }
            catch (CommunicationException commProblem)
            {
                logger.LogError(commProblem.Message);
                if (client != null)
                {
                    client.Abort();
                }
                throw new GDSException("SABRE_COMMUNICATION_EXCEPTION", "There is a communication issue with Sabre. Please try again later!");
            }
            catch (Exception ex)
            {
                logger.LogError("End Transaction failed.");
                logger.LogError(ex);
                if (client != null)
                {
                    client.Abort();
                }
                throw;
            }
        }

        private static MessageHeader CreateHeader(string pcc)
        {
            return new MessageHeader()
            {
                version = Constants.EnhancedEndTransactionVersion,
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
                Action = "EnhancedEndTransactionRQ",
                CPAId = pcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "StatefulEnhancedEndTransactionRQ"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.GetSabreDatetime()
                }
            };
        }

        private static EnhancedEndTransactionRQ CreateRequest(string receivedby, bool receievechanges = false)
        {
            EnhancedEndTransactionRQ enhancedEndTransactionRQ = new EnhancedEndTransactionRQ
            {
                version = Constants.EnhancedEndTransactionVersion,
                EndTransaction = new EnhancedEndTransactionRQEndTransaction()
                {
                    Ind = true
                }
            };
            
            if(receievechanges)
            {
                enhancedEndTransactionRQ.Source = new EnhancedEndTransactionRQSource()
                {
                    ReceivedFrom = receivedby
                };
            }

            return enhancedEndTransactionRQ;
        }
    }
}
