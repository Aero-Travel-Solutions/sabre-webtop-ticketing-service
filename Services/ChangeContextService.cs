using System;
using ContextChange;
using System.ServiceModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;

namespace SabreWebtopTicketingService.Services
{
    public class ChangeContextService: ConnectionStubs
    {
        private readonly ILogger logger;
        private readonly DbCache _dbCache;
        private readonly string url;

        public ChangeContextService(
            ILogger logger,
            DbCache dbCache)
        {
            this.logger = logger;
            _dbCache = dbCache;
            url = Constants.GetSoapUrl();
        }

        public async Task ContextChange(SabreSession token, Pcc pcc, string emulatetopcc, string ticketnumber = "")
        {
            ContextChangePortTypeClient client = null;
            try
            {
                if (string.IsNullOrEmpty(emulatetopcc) || pcc.PccCode == emulatetopcc) { return; }

                EnableTLS();

                client = new ContextChangePortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                logger.LogInformation($"{nameof(ContextChange)} invoked");
                var sw = Stopwatch.StartNew();

                var result = await client.
                                    ContextChangeRQAsync(
                                        CreateMessageHeader(pcc.PccCode),
                                        GetSecurityToken(token.SabreSessionID),
                                        GetContextChangeRQ(emulatetopcc));
                client.Close();

                logger.LogInformation($"{nameof(ContextChange)} completed in {sw.ElapsedMilliseconds} ms");
                sw.Stop();

                if (result == null || result.ContextChangeRS.ApplicationResults.status != CompletionCodes.Complete)
                {
                    var messages = result.
                                        ContextChangeRS.
                                        ApplicationResults.
                                        Error.
                                        SelectMany(s => s.SystemSpecificResults).
                                        SelectMany(s => s.Message);

                    messages.
                        Where(w => w.Value.ReplaceAllSabreSpecialChar().Contains("FORMAT")).
                        ToList().
                        ForEach(f => f.Value = "\"C\" Level branch access is required");


                    throw new GDSException(
                                "50000065",
                                string.Join(Environment.NewLine, messages.Select(s => s.Value)));
                }

                if (!string.IsNullOrEmpty(ticketnumber))
                {

                    //remove current session
                    string accessKey = $"{pcc.PccCode}-{ticketnumber}";
                    accessKey = accessKey.EncodeBase64();
                    SabreSession session = await _dbCache.GetSession(accessKey, pcc);
                    if (session != null)
                    {
                        await _dbCache.DeleteSabreSession(accessKey);
                    }

                    //insert new session
                    accessKey = $"{emulatetopcc}-{ticketnumber}";
                    accessKey = accessKey.EncodeBase64();
                    await _dbCache.InsertSabreSession(token.SabreSessionID, accessKey);
                }
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

        private static ContextChangeRQ GetContextChangeRQ(string emulatetopcc)
        {
            return new ContextChangeRQ()
            {
                ChangeAAA = new ContextChangeRQChangeAAA()
                {
                    PseudoCityCode = emulatetopcc
                }
            };
        }

        private static Security1 GetSecurityToken(string token)
        {
            return new Security1 { BinarySecurityToken = token };
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
                Action = "ContextChangeLLSRQ",
                CPAId = pcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "StatefullContextChangeLLSRQ"
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
