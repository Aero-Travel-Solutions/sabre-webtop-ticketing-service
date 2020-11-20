using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using TripSearch;

namespace SabreWebtopTicketingService.Services
{
    public class TripSearchService: ConnectionStubs
    {
        private readonly SessionDataSource sessionData;

        private readonly ILogger<TripSearchService> logger;

        private readonly string url;

        public TripSearchService(
            SessionDataSource sessionData,
            ILogger<TripSearchService> logger)
        {
            this.sessionData = sessionData;
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        public async Task<List<SabreSearchPNRResponse>> SearchPNR(SearchPNRRequest request, string token, Models.Pcc pcc, List<string> agentpccs, string ticketingpcc = "")
        {
            EnableTLS();

            TripSearchPortTypeClient client = null;

            try
            {
                client = new TripSearchPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));
                
                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                logger.LogInformation($"SearchPNR\\TripSearchRQAsync invoked.");
                var sw = Stopwatch.StartNew();

                //client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));

                var result = await client.
                                        TripSearchRQAsync(
                                            CreateHeader(string.IsNullOrEmpty(ticketingpcc) ? pcc.PccCode : ticketingpcc),
                                            new Security { BinarySecurityToken = token },
                                            GetRequest(request, pcc.PccCode, agentpccs));

                logger.LogInformation($"SearchPNR\\TripSearchRQAsync completed in {sw.ElapsedMilliseconds} ms.");
                sw.Stop();

                if (result == null || result.Trip_SearchRS.Success != "Success")
                {
                    var errors = result.
                                    Trip_SearchRS.
                                    Errors;

                    var dbtimeout = errors.Where(w => w.ErrorMessage.Contains("DB search timeout"));
                    if (!dbtimeout.IsNullOrEmpty())
                    {
                        throw new AeronologyException("50000014", "Connection to Sabre is currently unavailable. Please try again later.");
                    }

                    errors.
                        Where(w => w.ErrorMessage.Contains("Too many results - specify more data;")).
                        ToList().
                        ForEach(s => s.ErrorMessage = "Too many records found! Please refine your search text by in-corporating first name or first initial and try again.");

                    throw new GDSException("20000252", errors.Select(s => s.ErrorMessage).FirstOrDefault());
                }

                await client.CloseAsync();

                return ((Reservations)
                            ((Trip_SearchRSReservationsList)result.Trip_SearchRS.Items.FirstOrDefault()).
                            Items.
                            First()).
                            Reservation?.
                            Select(s => new SabreSearchPNRResponse(s.Locator, s.Item)).
                            ToList();
                
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
                logger.LogError(ex, "Search PNR failed.");
                client.Abort();
                throw;
            }

        }

        private Trip_SearchRQ GetRequest(SearchPNRRequest rq, string pcc, List<string> agentpccs, bool IsManageFlight= false)
        {
            PNRSearchType searchType;
            string firstName = "", lastName = "";

            if (rq.SearchText.IsMatch(@"\d+"))
            {
                searchType = PNRSearchType.PhoneNumber;
            }
            else if (rq.SearchText.Contains("@"))
            {
                searchType = PNRSearchType.Email;
            }
            else
            {
                searchType = PNRSearchType.Name;

                string name = rq.SearchText.TrimEnd('/');

                if (name.Contains("/"))
                {
                    string[] names = name.SplitOn("/");
                    lastName = names[0].Trim();
                    firstName = names[1].Trim();
                }
                else
                {
                    lastName = name;
                }
            }

            switch (searchType)
            {
                case PNRSearchType.Name:
                    return CreateRequestByName(firstName, lastName, agentpccs, IsManageFlight);
                case PNRSearchType.PhoneNumber:
                    return CreateRequestByPhone(rq.SearchText, agentpccs, IsManageFlight);
                case PNRSearchType.Email:
                    return CreateRequestByEmail(rq.SearchText, agentpccs, IsManageFlight);
                default:
                    throw new AeronologyException("50000001", "Unknown PNR search type");
            }
        }

        private static Trip_SearchRQ CreateRequestByName(string firstName, string lastName, List<string> agentpcc, bool isManageFlight)
        {
            return new Trip_SearchRQ
            {
                Version = Constants.TripSearchVersion,
                ReadRequests = new Trip_SearchRQReadRequests()
                {
                    Item = new Trip_SearchRQReadRequestsReservationReadRequest()
                    {
                        NameCriteria = new Trip_SearchRQReadRequestsReservationReadRequestName[]
                        {
                            new Trip_SearchRQReadRequestsReservationReadRequestName()
                            {
                                LastName = new StringWithMatchMode()
                                {
                                    MatchMode = MatchMode.START,
                                    Value = lastName
                                },
                                FirstName = string.IsNullOrEmpty(firstName) ?
                                            null :
                                            new StringWithMatchMode()
                                            {
                                                MatchMode = MatchMode.START,
                                                Value = firstName
                                            }
                            }
                        },
                        SegmentCriteria = new Trip_SearchRQReadRequestsReservationReadRequestSegmentCriteria()
                        {
                            Air = new SegmentCriterion()
                            {
                                ExistsSpecified = true,
                                Exists = true
                            }
                        },
                        ReturnOptions = new Trip_SearchRQReadRequestsReservationReadRequestReturnOptions()
                        {
                            SearchType = SearchType.ACTIVE,
                            ResponseFormat = "STL",
                            ViewName = "TripSearchBlobTN",
                            MaxItemsReturned = "500",
                            SubjectAreas = isManageFlight ?
                            new string[]
                            {
                                "NAME",
                                "RECORD_LOCATOR",
                                "AFAX",
                                "GFAX",
                                "ITINERARY"
                            } :
                            new string[]
                            {
                                "NAME",
                                "RECORD_LOCATOR"
                            }
                        },
                        PosCriteria = new Trip_SearchRQReadRequestsReservationReadRequestPosCriteria()
                        {
                            AnyBranch = true,
                            Pcc = agentpcc.ToArray()  
                        }
                    }
                }
            };
        }

        private static Trip_SearchRQ CreateRequestByPhone(string searchText, List<string> agentpcc, bool isManageFlight)
        {
            return new Trip_SearchRQ
            {
                Version = Constants.TripSearchVersion,
                ReadRequests = new Trip_SearchRQReadRequests()
                {
                    Item = new Trip_SearchRQReadRequestsReservationReadRequest()
                    {
                        TelephoneCriteria = new StringWithMatchMode[]
                    {
                        new StringWithMatchMode()
                        {
                            MatchMode = MatchMode.EXACT,
                            Value = searchText
                        }
                    },
                        SegmentCriteria = new Trip_SearchRQReadRequestsReservationReadRequestSegmentCriteria()
                        {
                            Air = new SegmentCriterion()
                            {
                                ExistsSpecified = true,
                                Exists = true
                            }
                        },
                        ReturnOptions = new Trip_SearchRQReadRequestsReservationReadRequestReturnOptions()
                        {
                            SearchType = SearchType.ACTIVE,
                            ResponseFormat = "STL",
                            ViewName = "TripSearchBlobTN",
                            MaxItemsReturned = "50",
                            SubjectAreas = new string[]
                        {
                            "NAME",
                            "RECORD_LOCATOR"
                        }
                        },
                        PosCriteria = new Trip_SearchRQReadRequestsReservationReadRequestPosCriteria()
                        {
                            Pcc = agentpcc.ToArray()
                        }
                    }
                }
            };
        }

        private static Trip_SearchRQ CreateRequestByEmail(string searchText, List<string> agentpcc, bool isManageFlight)
        {
            return new Trip_SearchRQ
            {
                Version = Constants.TripSearchVersion,
                ReadRequests = new Trip_SearchRQReadRequests()
                {
                    Item = new Trip_SearchRQReadRequestsReservationReadRequest()
                    {
                        EmailCriteria = new StringWithMatchMode[]
                    {
                        new StringWithMatchMode()
                        {
                            MatchMode = MatchMode.EXACT,
                            Value = searchText
                        }
                    },
                        SegmentCriteria = new Trip_SearchRQReadRequestsReservationReadRequestSegmentCriteria()
                        {
                            Air = new SegmentCriterion()
                            {
                                ExistsSpecified = true,
                                Exists = true
                            }
                        },
                        ReturnOptions = new Trip_SearchRQReadRequestsReservationReadRequestReturnOptions()
                        {
                            SearchType = SearchType.ACTIVE,
                            ResponseFormat = "STL",
                            ViewName = "TripSearchBlobTN",
                            MaxItemsReturned = "500",
                            SubjectAreas = new string[]
                        {
                            "NAME",
                            "RECORD_LOCATOR"
                        }
                        },
                        PosCriteria = new Trip_SearchRQReadRequestsReservationReadRequestPosCriteria()
                        {
                            Pcc = agentpcc.ToArray()
                        }
                    }
                }
            };
        }

        private static MessageHeader CreateHeader(string pcc)
        {
            return new MessageHeader
            {
                version = Constants.TripSearchVersion,
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
                Action = "Trip_SearchRQ",
                CPAId = pcc,
                ConversationId = "Aeronology",
                Service = new Service()
                {
                    Value = "StatefulTrip_SearchRQ"
                },
                MessageData = new MessageData()
                {
                    MessageId = "Aeronology" + Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.GetSabreDatetime()
                }
            };
        }

        internal async Task<List<SabreManageFlightItem>> GetManageFlightDate(SearchPNRRequest request, string token, Models.Pcc pcc, List<string> agentpccs)
        {
            EnableTLS();

            TripSearchPortTypeClient client = null;

            try
            {
                client = new TripSearchPortTypeClient(GetBasicHttpBinding(), new EndpointAddress(url));

                //Attach client credentials
                client.ClientCredentials.UserName.UserName = pcc.Username;
                client.ClientCredentials.UserName.Password = pcc.Password;

                logger.LogInformation($"SearchPNR\\TripSearchRQAsync invoked.");
                var sw = Stopwatch.StartNew();

                client.Endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour(new LoggingMessageInspector()));

                var result = await client.
                                        TripSearchRQAsync(
                                            CreateHeader(pcc.PccCode),
                                            new Security { BinarySecurityToken = token },
                                            GetRequest(request, pcc.PccCode, agentpccs, true));

                logger.LogInformation($"SearchPNR\\TripSearchRQAsync completed in {sw.ElapsedMilliseconds} ms.");
                sw.Stop();

                if (result == null || result.Trip_SearchRS.Success != "Success")
                {
                    var errors = result.
                                    Trip_SearchRS.
                                    Errors;

                    var dbtimeout = errors.Where(w => w.ErrorMessage.Contains("DB search timeout"));
                    if (!dbtimeout.IsNullOrEmpty())
                    {
                        throw new AeronologyException("50000014", "Connection to Sabre is currently unavailable. Please try again later.");
                    }

                    errors.
                        Where(w => w.ErrorMessage.Contains("Too many results - specify more data;")).
                        ToList().
                        ForEach(s => s.ErrorMessage = "Too many records found! Please refine your search text by in-corporating first name or first initial and try again.");

                    throw new GDSException("20000252", errors.Select(s => s.ErrorMessage).FirstOrDefault());
                }

                await client.CloseAsync();

                return ((Reservations)
                            ((Trip_SearchRSReservationsList)result.Trip_SearchRS.Items.FirstOrDefault()).
                            Items.
                            First()).
                            Reservation?.
                            Select(s => new SabreManageFlightItem(s.Item)).
                            ToList();
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
                logger.LogError(ex, "Search PNR failed");
                client.Abort();
                throw;
            }
        }
    }

    public enum PNRSearchType
    {
        Locator,
        Name,
        PhoneNumber,
        Email
    }
}

