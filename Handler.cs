using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Models;
using SabreWebtopTicketingService.Services;
using Amazon.Lambda.Core;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SabreWebtopTicketingService.CustomException;

[assembly:LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace SabreWebtopTicketingService
{
    public class Handler
    {
        private readonly SabreGDS sabreGDS;
        private readonly ILogger logger;
        
        public Handler()
        {
            //Set up Dependency Injection
            var serviceProvider = new Startup().ConfigureServices();

            sabreGDS = serviceProvider.GetService<SabreGDS>();
            logger = serviceProvider.GetService<ILogger>();
        }

        public async Task<LambdaResponse> SearchPNR(SearchPNRRequest rq)
        {
            logger.LogInformation("*****SearchPNR invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || string.IsNullOrEmpty(rq.SearchText))
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new SearchPNRLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.SearchText}-{rq.SessionID}-{Guid.NewGuid()}";
                SearchPNRResponse result = await sabreGDS.SearchPNR(rq, contextid);
                lambdaResponse.statusCode = result.Errors.IsNullOrEmpty() ? 200 : 500;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new SearchPNRLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = result.Errors.IsNullOrEmpty() ? new List<WebtopError>() : result.Errors,
                                                    data = result.Errors.IsNullOrEmpty() ? result : null
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> GetPublishQuote(GetQuoteRQ rq)
        {
            logger.LogInformation("*****GetPublishQuote invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || rq.SelectedPassengers.IsNullOrEmpty() || rq.SelectedSectors.IsNullOrEmpty())
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.Locator}-{rq.SessionID}-{Guid.NewGuid()}";
                List<Quote> result = await sabreGDS.GetQuote(rq, contextid);
                lambdaResponse.statusCode = 200;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>(),
                                                    data = result
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> PriceOverride(GetQuoteRQ rq)
        {
            logger.LogInformation("*****PriceOveride invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || rq.SelectedPassengers.IsNullOrEmpty() || rq.SelectedSectors.IsNullOrEmpty())
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.Locator}-{rq.SessionID}-{Guid.NewGuid()}";
                List<Quote> result = await sabreGDS.GetQuote(rq, contextid, true);
                lambdaResponse.statusCode = 200;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>(),
                                                    data = result
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> BestBuy(GetQuoteRQ rq)
        {
            logger.LogInformation("*****BestBuy invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || rq.SelectedPassengers.IsNullOrEmpty() || rq.SelectedSectors.IsNullOrEmpty())
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.Locator}-{rq.SessionID}-{Guid.NewGuid()}";
                List<Quote> result = await sabreGDS.BestBuy(rq, contextid);
                lambdaResponse.statusCode = 200;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>(),
                                                    data = result
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> ForceFarebasis(ForceFBQuoteRQ rq)
        {
            logger.LogInformation("*****ForceFarebasis invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || rq.SelectedPassengers.IsNullOrEmpty() || rq.SelectedSectors.IsNullOrEmpty())
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.Locator}-{rq.SessionID}-{Guid.NewGuid()}";
                List<Quote> result = await sabreGDS.ForceFBQuote(rq, contextid);
                lambdaResponse.statusCode = 200;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>(),
                                                    data = result
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> GetHistoricalQuote(GetQuoteRQ rq)
        {
            logger.LogInformation("*****GetPastDateQuote invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || rq.SelectedPassengers.IsNullOrEmpty() || rq.SelectedSectors.IsNullOrEmpty())
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.Locator}-{rq.SessionID}-{Guid.NewGuid()}";
                List<Quote> result = await sabreGDS.GetQuote(rq, contextid);
                lambdaResponse.statusCode = 200;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>(),
                                                    data = result
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> ManualBuildAndIssue(IssueExpressTicketRQ rq)
        {
            logger.LogInformation("*****ManualBuildIssue invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || (rq.IssueTicketQuoteKeys.IsNullOrEmpty() && rq.Quotes.IsNullOrEmpty()))
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new SearchPNRLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.Locator}-{rq.SessionID}-{Guid.NewGuid()}";
                IssueExpressTicketRS result = await sabreGDS.IssueExpressTicket(rq, contextid);
                lambdaResponse.statusCode = result.Tickets.IsNullOrEmpty() ? 500 : 200;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new IssueTicketLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = result.Errors.Select(s=> s.Error.message).ToList(),
                                                    data = result.Tickets.IsNullOrEmpty() ? null : result
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> GetPNRText(SearchPNRRequest rq)
        {
            logger.LogInformation("*****GetPNRText invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || string.IsNullOrEmpty(rq.SearchText) || string.IsNullOrEmpty(rq.AgentID))
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetPNRTextLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.SearchText}-{rq.SessionID}-{Guid.NewGuid()}";
                try
                {
                    string result = await sabreGDS.GetPNRText(rq, contextid);
                    lambdaResponse.statusCode = 200;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new GetPNRTextLambdaResponseBody()
                                                    {
                                                        context_id = contextid,
                                                        session_id = rq.SessionID,
                                                        error = new List<WebtopError>(),
                                                        data = result
                                                    },
                                                    new JsonSerializerSettings()
                                                    {
                                                        ContractResolver = new DefaultContractResolver()
                                                        {
                                                            NamingStrategy = new SnakeCaseNamingStrategy()
                                                            {
                                                                OverrideSpecifiedNames = false
                                                            }
                                                        }
                                                    }
                                                );
                }
                catch (Exception ex)
                {
                    lambdaResponse.statusCode = 500;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new GetPNRTextLambdaResponseBody()
                                                    {
                                                        context_id = contextid,
                                                        session_id = rq.SessionID,
                                                        error = new List<WebtopError>()
                                                        {
                                                            new WebtopError()
                                                            {
                                                                message = ex.Message,
                                                                code = "UNKNOWN_ERROR",
                                                                stack = ex.StackTrace.ToString()
                                                            }
                                                        },
                                                        data = null
                                                    },
                                                    new JsonSerializerSettings()
                                                    {
                                                        ContractResolver = new DefaultContractResolver()
                                                        {
                                                            NamingStrategy = new SnakeCaseNamingStrategy()
                                                            {
                                                                OverrideSpecifiedNames = false
                                                            }
                                                        }
                                                    }
                                                );
                }
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> GetQuoteText(GetQuoteTextRequest rq)
        {
            logger.LogInformation("*****GetQuoteText invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || string.IsNullOrEmpty(rq.Locator))
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetPNRTextLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.Locator}-{rq.SessionID}-{Guid.NewGuid()}";
                try
                {
                    GetQuoteTextResponse result = await sabreGDS.GetQuoteText(rq, contextid);
                    lambdaResponse.statusCode = 200;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new GetQuoteTextLambdaResponseBody()
                                                    {
                                                        context_id = contextid,
                                                        session_id = rq.SessionID,
                                                        error = new List<WebtopError>(),
                                                        data = result
                                                    },
                                                    new JsonSerializerSettings()
                                                    {
                                                        ContractResolver = new DefaultContractResolver()
                                                        {
                                                            NamingStrategy = new SnakeCaseNamingStrategy()
                                                            {
                                                                OverrideSpecifiedNames = false
                                                            }
                                                        }
                                                    }
                                                );
                }
                catch (Exception ex)
                {
                    lambdaResponse.statusCode = 500;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new GetQuoteTextLambdaResponseBody()
                                                    {
                                                        context_id = contextid,
                                                        session_id = rq.SessionID,
                                                        error = new List<WebtopError>()
                                                        {
                                                            new WebtopError()
                                                            {
                                                                message = ex.Message,
                                                                code = "UNKNOWN_ERROR",
                                                                stack = ex.StackTrace.ToString()
                                                            }
                                                        },
                                                        data = null
                                                    },
                                                    new JsonSerializerSettings()
                                                    {
                                                        ContractResolver = new DefaultContractResolver()
                                                        {
                                                            NamingStrategy = new SnakeCaseNamingStrategy()
                                                            {
                                                                OverrideSpecifiedNames = false
                                                            }
                                                        }
                                                    }
                                                );
                }
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> ValidateCommission(ValidateCommissionRQ rq)
        {
            logger.LogInformation("*****ValidateCommission invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || rq.Quotes.IsNullOrEmpty() || rq.Sectors.IsNullOrEmpty())
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new ValidateCommissionLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-{rq.Locator}-{rq.SessionID}-{Guid.NewGuid()}";
                try
                {
                    List<WebtopWarning> result = await sabreGDS.ValidateCommission(rq, contextid);
                    lambdaResponse.statusCode = 200;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new ValidateCommissionLambdaResponseBody()
                                                    {
                                                        context_id = contextid,
                                                        session_id = rq.SessionID,
                                                        error = new List<WebtopError>(),
                                                        data = result
                                                    },
                                                    new JsonSerializerSettings()
                                                    {
                                                        ContractResolver = new DefaultContractResolver()
                                                        {
                                                            NamingStrategy = new SnakeCaseNamingStrategy()
                                                            {
                                                                OverrideSpecifiedNames = false
                                                            }
                                                        }
                                                    }
                                                );
                }
                catch (AeronologyException ex)
                {
                    lambdaResponse.statusCode = 500;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new ValidateCommissionLambdaResponseBody()
                                                    {
                                                        context_id = contextid,
                                                        session_id = rq.SessionID,
                                                        error = new List<WebtopError>()
                                                        {
                                                            new WebtopError()
                                                            {
                                                                message = ex.Message,
                                                                code = ex.ErrorCode,
                                                                stack = ex.StackTrace.ToString()
                                                            }
                                                        },
                                                        data = null
                                                    },
                                                    new JsonSerializerSettings()
                                                    {
                                                        ContractResolver = new DefaultContractResolver()
                                                        {
                                                            NamingStrategy = new SnakeCaseNamingStrategy()
                                                            {
                                                                OverrideSpecifiedNames = false
                                                            }
                                                        }
                                                    }
                                                );
                }
                catch (Exception ex)
                {
                    lambdaResponse.statusCode = 500;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new ValidateCommissionLambdaResponseBody()
                                                    {
                                                        context_id = contextid,
                                                        session_id = rq.SessionID,
                                                        error = new List<WebtopError>()
                                                        {
                                                            new WebtopError()
                                                            {
                                                                message = ex.Message,
                                                                code = "UNKNOWN_ERROR",
                                                                stack = ex.StackTrace.ToString()
                                                            }
                                                        },
                                                        data = null
                                                    },
                                                    new JsonSerializerSettings()
                                                    {
                                                        ContractResolver = new DefaultContractResolver()
                                                        {
                                                            NamingStrategy = new SnakeCaseNamingStrategy()
                                                            {
                                                                OverrideSpecifiedNames = false
                                                            }
                                                        }
                                                    }
                                                );
                }
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> CurrencyConvert(ConvertCurrencyRequest rq)
        {
            logger.LogInformation("*****CurrencyConvert invoked *****");
            logger.LogInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.FromCurrency) || string.IsNullOrEmpty(rq.ToCurrency) || rq.Amount == int.MinValue)
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new ValidateCommissionLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = new List<WebtopError>()
                                                    {
                                                        new WebtopError
                                                        {
                                                            code = "INVALID_REQUEST",
                                                            message = "Mandatory request elements missing."
                                                        }
                                                    }
                                                },
                                                new JsonSerializerSettings()
                                                {
                                                    ContractResolver = new DefaultContractResolver()
                                                    {
                                                        NamingStrategy = new SnakeCaseNamingStrategy()
                                                        {
                                                            OverrideSpecifiedNames = false
                                                        }
                                                    }
                                                }
                                            );
            }
            else
            {
                contextid = $"1W-ConvertCurrency-{rq.SessionID}-{Guid.NewGuid()}";
                try
                {
                    ConvertCurrencyResponse result = await sabreGDS.CurrencyConvert(rq, contextid);
                    lambdaResponse.statusCode = 200;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new CurrencyConvertLambdaResponseBody()
                                                    {
                                                        context_id = contextid,
                                                        session_id = rq.SessionID,
                                                        error = new List<WebtopError>(),
                                                        data = result
                                                    },
                                                    new JsonSerializerSettings()
                                                    {
                                                        ContractResolver = new DefaultContractResolver()
                                                        {
                                                            NamingStrategy = new SnakeCaseNamingStrategy()
                                                            {
                                                                OverrideSpecifiedNames = false
                                                            }
                                                        }
                                                    }
                                                );
                }
                catch (Exception ex)
                {
                    lambdaResponse.statusCode = 500;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new CurrencyConvertLambdaResponseBody()
                                                    {
                                                        context_id = contextid,
                                                        session_id = rq.SessionID,
                                                        error = new List<WebtopError>()
                                                        {
                                                            new WebtopError()
                                                            {
                                                                message = ex.Message,
                                                                code = "UNKNOWN_ERROR",
                                                                stack = ex.StackTrace.ToString()
                                                            }
                                                        },
                                                        data = null
                                                    },
                                                    new JsonSerializerSettings()
                                                    {
                                                        ContractResolver = new DefaultContractResolver()
                                                        {
                                                            NamingStrategy = new SnakeCaseNamingStrategy()
                                                            {
                                                                OverrideSpecifiedNames = false
                                                            }
                                                        }
                                                    }
                                                );
                }
            }

            logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }
    }
}
