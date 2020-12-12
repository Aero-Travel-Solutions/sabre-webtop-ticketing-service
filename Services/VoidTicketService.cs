using Aeronology.CustomException;
using Aeronology.DTO.Interfaces;
using Aeronology.DTO.Models;
using Aeronology.Infrastructure;
using Aeronology.Sabre.SabreGDSObjects;
using Aeronology.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using VoidTicket;

namespace Aeronology.Sabre.SabreServices
{
    public class VoidTicketService : ConnectionStubs
    {
        private readonly ISessionDataSource sessionData;

        private readonly ILogger<GetReservationService> logger;

        private readonly string url;

        public VoidTicketService(
            ISessionDataSource sessionData,
            ILogger<GetReservationService> logger)
        {
            this.sessionData = sessionData;
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        public async Task<SabreVoidTicketResponse> VoidTicket(string locator, string tktno, string documenttype, string rphno, string token, Pcc pcc, string issuingpcc)
        {
            SabreVoidTicketResponse res = null;
            VoidTicketPortTypeClient client = null;
            try
            {
                EnableTLS();

                client = new VoidTicketPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                logger.LogInformation("VoidTicket\\VoidTicketRQAsync invoked.");
                var sw = Stopwatch.StartNew();

                #region Sample
                //first void call
                //return below
                //<VoidTicketRS xmlns="http://webservices.sabre.com/sabreXML/2011/10" Version="2.1.0" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:stl="http://services.sabre.com/STL/v01">
                //	<stl:ApplicationResults status="Complete">
                //		<stl:Success timeStamp="2013-06-25T10:06:35-05:00"/>
                //		<stl:Warning type="BusinessLogic">
                //			<stl:SystemSpecificResults>
                //				<stl:Message>REENT IF THESE TKT NBRS ARE TO BE VOIDED</stl:Message>
                //				<stl:ShortText>WARN.SWS.HOST.INTERMEDIATE_RESPONSE</stl:ShortText>
                //			</stl:SystemSpecificResults>
                //		</stl:Warning>
                //		<stl:Warning type="BusinessLogic">
                //			<stl:SystemSpecificResults>
                //				<stl:Message>0017232809667</stl:Message>
                //				<stl:ShortText>WARN.SWS.HOST.INTERMEDIATE_RESPONSE</stl:ShortText>
                //			</stl:SystemSpecificResults>
                //		</stl:Warning>
                //	</stl:ApplicationResults>
                //</VoidTicketRS>
                #endregion

                client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));

                var result = await client.
                                        VoidTicketRQAsync(
                                            CreateMessageHeader(issuingpcc),
                                            new Security1 { BinarySecurityToken = token },
                                            GetEtktVoidTicketRq(rphno));

                if (result.VoidTicketRS.ApplicationResults.status != CompletionCodes.Complete)
                {
                    res = new SabreVoidTicketResponse(tktno, documenttype, result, locator);
                    return res;
                }

                //second void call
                res = new SabreVoidTicketResponse(
                                tktno,
                                documenttype,
                                await client.
                                VoidTicketRQAsync(
                                    CreateMessageHeader(issuingpcc),
                                    new Security1 { BinarySecurityToken = token },
                                    GetEtktVoidTicketRq(rphno)),
                                "");


                logger.LogInformation($"VoidTicket\\VoidTicketRQAsync completed is {sw.ElapsedMilliseconds} ms.");
                sw.Stop();

                return res;
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
                logger.LogError(ex, "Void ticket failed");
                client.Abort();
                throw;
            }
        }

        private static MessageHeader CreateMessageHeader(string pcc)
        {
            return new MessageHeader()
            {
                version = Constants.VoidAirTicketVersion,
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
                Action = "VoidTicketLLSRQ",
                CPAId = pcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "StatefulVoidTicket"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.GetSabreDatetime()
                }
            };
        }

        private VoidTicketRQ GetEtktVoidTicketRq(string rph)
        {
            return new VoidTicketRQ()
            {
                Version = Constants.VoidAirTicketVersion,
                Ticketing = new VoidTicketRQTicketing
                {
                    RPH = rph
                }
            };
        }
    }

}
