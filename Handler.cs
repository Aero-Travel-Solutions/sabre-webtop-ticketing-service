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
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****SearchPNR invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> GetPublishQuote(GetQuoteRQ rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****GetPublishQuote invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> PriceOverride(GetQuoteRQ rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****PriceOveride invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> BestBuy(GetQuoteRQ rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****BestBuy invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> ForceFarebasis(ForceFBQuoteRQ rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****ForceFarebasis invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> GetHistoricalQuote(GetQuoteRQ rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****GetPastDateQuote invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> ManualBuildAndIssue(IssueExpressTicketRQ rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****ManualBuildIssue invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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
                lambdaResponse.statusCode = result.Tickets.IsNullOrEmpty() && result.ValidateCommissionWarnings.IsNullOrEmpty() ? 500 : 200;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new IssueTicketLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = result.Tickets.IsNullOrEmpty() ? result.Errors.Select(s => s.Error).ToList() : null,
                                                    data = result.Tickets.IsNullOrEmpty() && result.ValidateCommissionWarnings.IsNullOrEmpty() ? null : result
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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> GetPNRText(SearchPNRRequest rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****GetPNRText invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> GetQuoteText(GetQuoteTextRequest rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****GetQuoteText invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> ValidateCommission(ValidateCommissionRQ rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****ValidateCommission invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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
                    List<ValidateCommissionWarning> validateCommissionWarnings = await sabreGDS.ValidateCommission(rq, contextid);
                    List<WebtopWarning> result = validateCommissionWarnings.
                                                        SelectMany(m => m.Warnings).
                                                        DistinctBy(d => d.message).
                                                        ToList();
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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> CurrencyConvert(ConvertCurrencyRequest rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****CurrencyConvert invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> GetROE(GetROERequest rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****GetROE invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.CurrencyCode))
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetROELambdaResponseBody()
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
                contextid = $"1W-GetROE-{rq.SessionID}-{Guid.NewGuid()}";
                try
                {
                    GetROEResponse result = await sabreGDS.GetROE(rq, contextid);
                    lambdaResponse.statusCode = 200;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new GetROELambdaResponseBody()
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
                                                    new GetROELambdaResponseBody()
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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> DisplayGDSTicketImage(DisplayGDSTicketImageRequest rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****DisplayGDSTicketImage invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.DocumentNumber) || string.IsNullOrEmpty(rq.DocumentType) || string.IsNullOrEmpty(rq.TicketingPcc))
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new DisplayTicketLambdaResponseBody()
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
                contextid = $"1W-DisplayGDSTicket-{rq.SessionID}-{Guid.NewGuid()}";
                try
                {
                    string result = await sabreGDS.DisplayTicketImage(rq, contextid);
                    lambdaResponse.statusCode = 200;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new DisplayTicketLambdaResponseBody()
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
                                                    new DisplayTicketLambdaResponseBody()
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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }

        public async Task<LambdaResponse> VoidTicket(VoidTicketRequest rq)
        {
            if (!string.IsNullOrEmpty(rq.warmer))
            {
                return new LambdaResponse()
                {
                    statusCode = 200,
                    headers = new Headers()
                    {
                        contentType = "application/json"
                    },
                    body = ""
                };
            }

            logger.LogInformation("*****Void Ticket invoked *****");
            logger.LogMaskInformation($"#Request: {JsonConvert.SerializeObject(rq)}");

            LambdaResponse lambdaResponse = new LambdaResponse()
            {
                headers = new Headers()
                {
                    contentType = "application/json"
                }
            };

            string contextid = "";

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || rq.Tickets.IsNullOrEmpty())
            {
                lambdaResponse.statusCode = 400;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new VoidTicketLambdaResponseBody()
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
                contextid = $"1W-VoidTicket-{rq.SessionID}-{Guid.NewGuid()}";
                try
                {
                    List<VoidTicketResponse> result = await sabreGDS.VoidTicket(rq, contextid);
                    lambdaResponse.statusCode = 200;
                    lambdaResponse.body = JsonConvert.
                                                SerializeObject
                                                (
                                                    new VoidTicketLambdaResponseBody()
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
                                                    new VoidTicketLambdaResponseBody()
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

            logger.LogMaskInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

            return lambdaResponse;
        }
    }
}
