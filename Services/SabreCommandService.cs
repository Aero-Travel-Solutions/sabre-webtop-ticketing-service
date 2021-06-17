
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

                client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));

                logger.LogMaskInformation($"{nameof(ExecuteCommand)} invoke SabreCommandLLSRQAsync-{command}");
                var sw = Stopwatch.StartNew();

                var result = await client.
                                    SabreCommandLLSRQAsync(
                                        getMessageHeader(pcc, agentPCC),
                                        getSecurityHedder(token),
                                        getSabreCommandLLSRQ(command));

                logger.LogInformation($"{nameof(ExecuteCommand)} SabreCommandLLSRQAsync completed is {sw.ElapsedMilliseconds} ms");
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
            logger.LogMaskInformation($"{nameof(ExecuteCommand)} invoke \"SabreCommandLLSRQAsync\". Command: {command}");
            client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));

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


        public async Task<GetQuoteTextResponse> GetQuoteText(string token, Pcc pcc, GetQuoteTextRequest rq, string agentPCC, DateTime pcclocaldatetime)
        {
            string pq = "";
            string pqs = "";
            string aeall = "";
            SabreCommandLLSPortTypeClient client = null;

            try
            {
                EnableTLS();

                client = new SabreCommandLLSPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                //Construct display pnr command
                string command = "";

                //LOGIC
                //null - all
                // 0 - nothing
                // number - only that quote or emd

                if ((rq.QuoteNo.HasValue && rq.QuoteNo.Value != 0) || !rq.QuoteNo.HasValue)
                {
                    string postfix = rq.QuoteNo.HasValue ? rq.QuoteNo.Value.ToString() : "";

                    command = $"*PQ{postfix}";

                    //Execute display additional data on pnr command
                    pq = await ExecCommand(token, pcc, agentPCC, client, command);

                    //remove COM PCT from text
                    pq = RemoveCOMPCT(pq);

                    //Execute display fop command
                    pqs = await ExecCommand(token, pcc, agentPCC, client, "*PQS");
                }

                if ((rq.EMDNo.HasValue && rq.EMDNo.Value != 0) || !rq.EMDNo.HasValue)
                {
                    string postfix = rq.EMDNo.HasValue ? rq.EMDNo.Value.ToString() : "";

                    //Get EMD quotes
                    aeall = await ExecCommand(token, pcc, agentPCC, client, "*AE*AES");
                }


                await client.CloseAsync();
                return ConstructQuoteTextResponse(pq.Mask(), pqs.Mask(), rq, aeall.Mask(), pcclocaldatetime);
            }
            catch (Exception)
            {
                client.Abort();
                throw;
            }
        }

        private string RemoveCOMPCT(string pq)
        {
            string result = pq;
            List<string> lines = result.SplitOn("\n").ToList();
            int index = lines.FindIndex(f => f.IsMatch(@"^COMM\s*PCT\s+\d+\s+$"));
            if (index > -1)
            {
                lines.Remove(lines[index]);
                result = string.Join("\n", lines);
            }
            return result;
        }

        private GetQuoteTextResponse ConstructQuoteTextResponse(string pq, string pqsummary, GetQuoteTextRequest rq, string aeall, DateTime pcclocaldatetime)
        {
            GetQuoteTextResponse res = new GetQuoteTextResponse()
            {
                QuoteData = new List<QuoteData>(),
            };

            if (pq.Contains("NO PQ RECORD SUMMARY OR DETAIL EXISTS"))
            {
                res.QuoteError = pq;
            }

            if (aeall.Contains("NO PSGR DATA"))
            {
                res.EMDError = aeall.ReplaceAllSabreSpecialChar();
            }

            if (!(string.IsNullOrEmpty(res.QuoteError) || string.IsNullOrEmpty(res.EMDError)))
            {
                return res;
            }

            string[] quotes = pq.SplitOnRegex(@"^(PQ\s+\d+.*)");
            PQSummary pqs = new PQSummary(pqsummary);
            for (int i = 1; i < quotes.Count(); i += 2)
            {
                int quoteno = int.Parse(quotes[i].LastMatch(@"^PQ\s+(\d+).*"));
                res.
                    QuoteData.
                    Add(new QuoteData()
                    {
                        QuoteNo = quoteno,
                        QuoteText = string.Join("", new string[] { quotes[i], quotes[i + 1] }).ReplaceAllSabreSpecialChar(),
                        Expired = pqs.PQSummaryLines.First(f => f.PQNumber == quoteno).Expired ||
                                  pqs.PQSummaryLines.First(f => f.PQNumber == quoteno).StoredDate !=
                                  pcclocaldatetime.ToString("ddMMM").ToUpper()
                    });
            }

            //AEALL aeallobj = new AEALL(aeall);
            //res.EMDData = aeallobj.
            //                AEALLItems.
            //                Where(a => (rq.EMDNo.HasValue && rq.EMDNo.Value != 0 && a.EMDNumber == rq.EMDNo.Value) || !rq.EMDNo.HasValue).
            //                Select(e => new EMDData()
            //                {
            //                    EMDNo = e.EMDNumber,
            //                    EMDText = e.AEALLText,
            //                    Expired = e.Expired
            //                }).
            //                ToList();

            return res;
        }

        public void Dispose()
        {

        }
    }
}
