using GetReservation;
using Microsoft.Extensions.Logging;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Services
{
    public class GetReservationService: ConnectionStubs
    {
        private readonly SessionDataSource sessionData;
        private readonly ILogger<GetReservationService> logger;
        private readonly string url;

        public GetReservationService(
            SessionDataSource sessionData,
            ILogger<GetReservationService> logger)
        {
            this.sessionData = sessionData;
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        public async Task<GetReservationRS> RetrievePNR(string locator, string token, Pcc pcc, string agentpcc = "")
        {
            GetReservationPortTypeClient client = null;
            try
            {
                EnableTLS();

                client = new GetReservationPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));
                
                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                var messageHeader = CreateMessageHeader(agentpcc.IsNullOrEmpty() ? pcc.PccCode : agentpcc);
                var request = CreateRequest(locator);

                logger.LogInformation($"{nameof(RetrievePNR)} invoke \"GetReservationOperationAsync\"");
                var sw = Stopwatch.StartNew();

                client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));

                var result = await client.GetReservationOperationAsync(
                                        messageHeader,
                                        new Security { BinarySecurityToken = token },
                                        request);

                logger.LogInformation($"{nameof(RetrievePNR)} \"GetReservationOperationAsync\" completed is {sw.ElapsedMilliseconds} ms");
                sw.Stop();

                if (result != null && !result.GetReservationRS.Errors.IsNullOrEmpty())
                {
                    var errors = result.GetReservationRS.Errors;
                    if(errors.Any(error => error.Code == Constants.EXPIRED_SESSION_CODE 
                    || error.Message.Contains(Constants.EXPIRED_SESSION_CODE)))
                    {
                        throw new ExpiredTokenException($"{pcc.PccCode}-{locator}".EncodeBase64(), errors?.First()?.Code, errors?.First().Message);
                    }

                    throw new GDSException(
                        string.Join(Environment.NewLine, errors.Select(s => s.Code).Distinct()),
                        string.Join(Environment.NewLine, errors.Select(s => s.Message).Distinct()));
                }
                
                await client.CloseAsync();
                return result.GetReservationRS;              
            }
            catch (TimeoutException timeProblem)
            {
                logger.LogError(timeProblem, timeProblem.Message);
                client.Abort();
                throw new GDSException("30000025", "Sabre system timeout. Please try again!");
            }
            catch (FaultException unknownFault)
            {
                logger.LogError(unknownFault, unknownFault.Message);
                client.Abort();
                throw new GDSException("30000026", $"Sabre System Exception: {unknownFault.Message + (unknownFault.InnerException == null ? "" : Environment.NewLine + unknownFault.InnerException.Message)}");
            }
            catch (CommunicationException commProblem)
            {
                logger.LogError(commProblem, commProblem.Message);
                client.Abort();
                throw new GDSException("30000027", "There is a communication issue with Sabre. Please try again later!");
            }
            catch (Exception ex)
            {
                client.Abort();
                
                logger.LogError(ex, "Retrieve PNR failed.");

                if(ex is FaultException fe && 
                    (fe.Code.Name.Contains("Client.InvalidSecurityToken") ||
                    fe.Message.Contains("Invalid or Expired binary security token")))
                {
                    throw new ExpiredTokenException($"{pcc.PccCode}-{locator}".EncodeBase64(), fe.Code.Name, fe.Message);
                }

                throw;
            }
        }

        private static GetReservationRQ CreateRequest(string locator)
        {
            return new GetReservationRQ()
            {
                Version = Constants.GetReservationVersion,
                Locator = locator,
                RequestType = "Stateful",
                ReturnOptions = new ReturnOptionsPNRB()
                {
                    PriceQuoteServiceVersion = Constants.GetReservationPriceQuoteVersion,
                    ViewName = "Full",
                    SubjectAreas = new string[] { "RECORD_LOCATOR", "ANCILLARY", "PASSENGERDETAILS", "REMARKS",  "PRICE_QUOTE","TICKETING", "DK_NUMBER" }, 
                    UnmaskCreditCard = true,
                    ShowTicketStatus = true,
                    ResponseFormat = "STL"
                }
            };
        }

        private static MessageHeader CreateMessageHeader(string pcc)
        {
            return new MessageHeader()
            {
                version = Constants.GetReservationVersion,
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
                Action = "getReservationRQ",
                CPAId = pcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "StatelessGetReservation"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.GetSabreDatetime()
                }
            };
        }
    }
}
