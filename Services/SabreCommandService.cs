
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

        public async Task<bool> ExecuteRefundMask(string token, Pcc pcc, string command, string agentPCC, OriginalTicket displayedtkt, decimal changefee)
        {
            SabreCommandLLSPortTypeClient client = null;

            try
            {
                EnableTLS();

                client = new SabreCommandLLSPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                logger.LogInformation("SabreCommandLLSRQAsync - WFA MASK SCREEN 10");
                var sw = Stopwatch.StartNew();

                var result = await client.
                                    SabreCommandLLSRQAsync(
                                        getMessageHeader(pcc, agentPCC),
                                        getSecurityHedder(token),
                                        getSabreCommandLLSRQ(command));

                logger.LogInformation($"SabreCommandLLSRQAsync - WFA MASK SCREEN 10 - completed is {sw.ElapsedMilliseconds} ms");
                sw.Stop();

                if (result.SabreCommandLLSRS.ErrorRS?.Errors?.Error?.ErrorMessage != null)
                {
                    var messages = result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorMessage;

                    throw new Exception(messages);
                }

                string mask1 = result.SabreCommandLLSRS.Response;
                WFRScreen10 screen10 = new WFRScreen10(mask1, displayedtkt);

                if (!screen10.Success)
                {
                    logger.LogInformation(mask1);
                    throw new GDSException("UNEXPECTED_MASK", "GDS return unexpected response.");
                }

                command = screen10.Command;

                logger.LogInformation("SabreCommandLLSRQAsync - WFA MASK SCREEN 10 filled");
                sw = Stopwatch.StartNew();

                result = await client.
                                    SabreCommandLLSRQAsync(
                                        getMessageHeader(pcc, agentPCC),
                                        getSecurityHedder(token),
                                        getSabreCommandLLSRQ(command));

                if (result.SabreCommandLLSRS.ErrorRS?.Errors?.Error?.ErrorMessage != null)
                {
                    var messages = result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorMessage;
                    throw new Exception(messages);
                }

                logger.LogInformation($"SabreCommandLLSRQAsync - WFA MASK SCREEN 10 filled - completed is {sw.ElapsedMilliseconds} ms");
                sw.Stop();

                string mask2 = result.SabreCommandLLSRS.Response;
                if(mask2.Contains("SCREEN 13"))
                {
                    WFRScreen13 screen13 = new WFRScreen13(mask2);

                    command = screen13.Command;

                    logger.LogInformation("SabreCommandLLSRQAsync - WFA MASK SCREEN 30 filled");
                    sw = Stopwatch.StartNew();

                    result = await client.
                                        SabreCommandLLSRQAsync(
                                            getMessageHeader(pcc, agentPCC),
                                            getSecurityHedder(token),
                                            getSabreCommandLLSRQ(command));

                    if (result.SabreCommandLLSRS.ErrorRS?.Errors?.Error?.ErrorMessage != null)
                    {
                        var messages = result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorMessage;
                        throw new Exception(messages);
                    }

                    logger.LogInformation($"SabreCommandLLSRQAsync - WFA MASK SCREEN 30 filled - completed is {sw.ElapsedMilliseconds} ms");
                    sw.Stop();

                    mask2 = result.SabreCommandLLSRS.Response;
                }
                WFRScreen30 scree30 = new WFRScreen30(mask2, changefee);

                if (!scree30.Success)
                {
                    logger.LogInformation(mask2);
                    throw new GDSException("UNEXPECTED_MASK", "GDS return unexpected response.");
                }

                command = scree30.Command;

                logger.LogInformation("SabreCommandLLSRQAsync - WFA MASK SCREEN 30 filled");
                sw = Stopwatch.StartNew();

                result = await client.
                                    SabreCommandLLSRQAsync(
                                        getMessageHeader(pcc, agentPCC),
                                        getSecurityHedder(token),
                                        getSabreCommandLLSRQ(command));

                if (result.SabreCommandLLSRS.ErrorRS?.Errors?.Error?.ErrorMessage != null)
                {
                    var messages = result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorMessage;
                    throw new Exception(messages);
                }

                logger.LogInformation($"SabreCommandLLSRQAsync - WFA MASK SCREEN 30 filled - completed is {sw.ElapsedMilliseconds} ms");
                sw.Stop();

                string mask3 = result.SabreCommandLLSRS.Response;

                if(mask3.Contains("SCREEN 33"))
                {
                    WFRScreen33 screen33 = new WFRScreen33(mask3, displayedtkt.Payment, screen10.TTL);

                    if (!screen33.Success)
                    {
                        logger.LogInformation(mask2);
                        throw new GDSException("UNEXPECTED_MASK", "GDS return unexpected response.");
                    }

                    command = screen33.Command;

                    logger.LogInformation("SabreCommandLLSRQAsync - WFA MASK SCREEN 33 reconfirmed.");
                    sw = Stopwatch.StartNew();

                    result = await client.
                                        SabreCommandLLSRQAsync(
                                            getMessageHeader(pcc, agentPCC),
                                            getSecurityHedder(token),
                                            getSabreCommandLLSRQ(command));

                    if (result.SabreCommandLLSRS.ErrorRS?.Errors?.Error?.ErrorMessage != null)
                    {
                        var messages = result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorMessage;
                        throw new Exception(messages);
                    }

                    logger.LogInformation($"SabreCommandLLSRQAsync - WFA MASK SCREEN 33 reconfirmed - completed is {sw.ElapsedMilliseconds} ms");
                    sw.Stop();

                    mask3 = result.SabreCommandLLSRS.Response;
                }

                if (mask3.Contains("SCREEN 30") && mask3.Contains("VERIFY TOTAL AMT-REENTER TO CONTINUE"))
                {
                    logger.LogInformation("SabreCommandLLSRQAsync - WFA MASK SCREEN 30 reconfirmed.");
                    sw = Stopwatch.StartNew();

                    result = await client.
                                        SabreCommandLLSRQAsync(
                                            getMessageHeader(pcc, agentPCC),
                                            getSecurityHedder(token),
                                            getSabreCommandLLSRQ(command));

                    if (result.SabreCommandLLSRS.ErrorRS?.Errors?.Error?.ErrorMessage != null)
                    {
                        var messages = result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorMessage;
                        throw new Exception(messages);
                    }

                    logger.LogInformation($"SabreCommandLLSRQAsync - WFA MASK SCREEN 30 reconfirmed - completed is {sw.ElapsedMilliseconds} ms");
                    sw.Stop();

                    mask3 = result.SabreCommandLLSRS.Response;
                }

                WFRScreen32 screen32 = new WFRScreen32(mask3);
                if (!screen32.Success)
                {
                    logger.LogInformation(mask2);
                    throw new GDSException("UNEXPECTED_MASK", "GDS return unexpected response.");
                }
            
                command = screen32.Command;

                logger.LogInformation("SabreCommandLLSRQAsync - WFA MASK SCREEN 32 filled");
                sw = Stopwatch.StartNew();

                result = await client.
                                    SabreCommandLLSRQAsync(
                                        getMessageHeader(pcc, agentPCC),
                                        getSecurityHedder(token),
                                        getSabreCommandLLSRQ(command));

                await client.CloseAsync();

                if (result.SabreCommandLLSRS.ErrorRS?.Errors?.Error?.ErrorMessage != null)
                {
                    var messages = result.SabreCommandLLSRS.ErrorRS.Errors.Error.ErrorMessage;
                    throw new Exception(messages);
                }

                return result.SabreCommandLLSRS.Response == "MASK PROCESSING COMPLETE - MASK TERMINATED";
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
