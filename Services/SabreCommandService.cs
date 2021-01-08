
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using SabreCommandService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Services
{
    public class SabreCrypticCommandService : ConnectionStubs, IDisposable
    {
        private readonly SessionDataSource sessionData;
        private readonly ILogger logger;
        private readonly string url;

        public SabreCrypticCommandService(
            SessionDataSource sessionData,
            ILogger logger)
        {
            this.sessionData = sessionData;
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        internal MessageHeader getMessageHeader(Pcc pcc, string agentPcc)
        {
            return new MessageHeader()
            {
                version = Constants.SabreCommandVersion,
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
                Action = "SabreCommandLLSRQ",
                CPAId = string.IsNullOrEmpty(agentPcc) ? pcc.PccCode : agentPcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "StatefulSabreCommand"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.GetSabreDatetime()
                }
            };
        }

        internal Security getSecurityHedder(string token)
        {
            return new Security()
            {
                BinarySecurityToken = token
            };
        }

        internal SabreCommandLLSRQ getSabreCommandLLSRQ(string command)
        {
            return new SabreCommandLLSRQ()
            {
                Version = Constants.SabreCommandVersion,
                Target =  SabreCommandLLSRQTarget.Production,
                Request = new SabreCommandLLSRQRequest()
                {
                    Output = SabreCommandLLSRQRequestOutput.SCREEN,
                    CDATA = true,
                    HostCommand = command
                }
            };
        }


        public async Task<string> ExecuteCommand(string token, Pcc pcc, string command, string agentPCC = "")
        {
            SabreCommandLLSPortTypeClient client = null;

            try
            {

                EnableTLS();

                client = new SabreCommandLLSPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));
                
                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                logger.LogInformation($"{nameof(ExecuteCommand)} invoke \"SabreCommandLLSRQAsync\"");
                var sw = Stopwatch.StartNew();

                var result = await client.
                                    SabreCommandLLSRQAsync(
                                        getMessageHeader(pcc, agentPCC),
                                        getSecurityHedder(token),
                                        getSabreCommandLLSRQ(command));

                logger.LogInformation($"{nameof(ExecuteCommand)} \"SabreCommandLLSRQAsync\" completed is {sw.ElapsedMilliseconds} ms");
                sw.Stop();

                if (result.SabreCommandLLSRS.ErrorRS?.Errors?.Error?.ErrorMessage != null)
                {
                    var messages = result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorMessage;

                    throw new Exception(messages);
                }

                await client.CloseAsync();
                return result.SabreCommandLLSRS.Response;
                
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

        public async Task<string> GetPNRText(string token, Pcc pcc, string locator, string agentPCC)
        {
            string response = "";
            SabreCommandLLSPortTypeClient client = null;
            try
            {
                EnableTLS();

                client = new SabreCommandLLSPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                //Construct display pnr command
                string command = string.Format("*{0}", locator);
                string temptext = await ExecCommand(token, pcc, agentPCC, client, command);

                if (!(temptext.Contains("ADDR") ||
                    temptext.Contains("UTL PNR") ||
                    temptext.Contains("SECURED PNR")))
                {
                    command = "*R*T*P3*P4*P3S*P4S*P3D*P4D*PD*AE*FF*PE";

                    //Execute display additional data on pnr command
                    response = await ExecCommand(token, pcc, agentPCC, client, command);

                    command = "*FOP";

                    //Execute display fop command
                    temptext = await ExecCommand(token, pcc, agentPCC, client, command);

                    if (!temptext.Contains("NO FORM OF PAYMENT DATA"))
                    {
                        response += (Environment.NewLine + temptext);
                    }


                }

                await client.CloseAsync();

                return response;
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

        private async Task<string> ExecCommand(string token, Pcc pcc, string agentPCC, SabreCommandLLSPortTypeClient client, string command)
        {
            logger.LogInformation($"{nameof(ExecuteCommand)} invoke \"SabreCommandLLSRQAsync\"");
            var sw = Stopwatch.StartNew();

            var result = await client.
                                SabreCommandLLSRQAsync(
                                    getMessageHeader(pcc, agentPCC),
                                    getSecurityHedder(token),
                                    getSabreCommandLLSRQ(command));

            logger.LogInformation($"{nameof(ExecuteCommand)} \"SabreCommandLLSRQAsync\" completed is {sw.ElapsedMilliseconds} ms");
            sw.Stop();

            if (result.SabreCommandLLSRS.ErrorRS?.Errors?.Error?.ErrorMessage != null)
            {
                var messages = result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorMessage;

                throw new GDSException(messages, result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorCode);
            }

            return result.SabreCommandLLSRS.Response;
        }



        public void Dispose()
        {

        }
    }
}
