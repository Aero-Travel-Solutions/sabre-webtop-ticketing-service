using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Models;
using SabreWebtopTicketingService.Services;
using Amazon.Lambda.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
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

            //AWSSDKHandler.RegisterXRayForAllServices();
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

            if (rq == null || string.IsNullOrEmpty(rq.SessionID) || string.IsNullOrEmpty(rq.GDSCode) || rq.SelectedPassengers.IsNullOrEmpty() || rq.SelectedSectors.IsNullOrEmpty())
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
                List<Quote> result = await sabreGDS.GetQuote(rq, contextid);
                lambdaResponse.statusCode = result.All(a=> a.Errors.IsNullOrEmpty()) ? 200 : 500;
                lambdaResponse.body = JsonConvert.
                                            SerializeObject
                                            (
                                                new GetQuoteLambdaResponseBody()
                                                {
                                                    context_id = contextid,
                                                    session_id = rq.SessionID,
                                                    error = result.All(a => a.Errors.IsNullOrEmpty()) ? 
                                                                new List<WebtopError>() : 
                                                                result.
                                                                    SelectMany(s=> s.Errors).
                                                                    DistinctBy(d=> d.message).
                                                                    ToList(),
                                                    data = result.All(a => a.Errors.IsNullOrEmpty()) ? result : null
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

    }    
}
