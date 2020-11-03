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
        private readonly SabreGDS _sabreGDS;
        private readonly ILogger _logger;
        
        public Handler()
        {
            //Set up Dependency Injection
            var serviceProvider = new Startup().ConfigureServices();

            _sabreGDS = serviceProvider.GetService<SabreGDS>();
            _logger = serviceProvider.GetService<ILogger>();

            //AWSSDKHandler.RegisterXRayForAllServices();
        }

        //public async Task<LambdaResponse> AssessRefund(AutoRefundQuoteRequest request)
        //{
        //    _logger.LogInformation("***** AssessRefund invoked *****");
        //    _logger.LogInformation($"#Request: {JsonConvert.SerializeObject(request)}");

        //    LambdaResponse lambdaResponse = new LambdaResponse()
        //    {
        //        headers = new Headers()
        //        {
        //            contentType = "application/json"
        //        }
        //    };

        //    string contextid = $"1W-{string.Join(",", request.RefundTickets.Select(s=> s.DocumentNumber))}-{Guid.NewGuid()}";

        //    if (request == null || request.RefundTickets.IsNullOrEmpty() || string.IsNullOrEmpty(request.SessionID))
        //    {
        //        lambdaResponse.statusCode = 400;
        //        lambdaResponse.body = JsonConvert.
        //                                    SerializeObject
        //                                    (
        //                                        new LambdaQuoteResponseBody()
        //                                        {
        //                                            context_id = contextid,
        //                                            session_id = request.SessionID,
        //                                            error = new List<RefundError>()
        //                                            {
        //                                                new RefundError
        //                                                {
        //                                                    code = "INVALID_REQUEST",
        //                                                    message = "Mandatory request elements missing."
        //                                                }
        //                                            }
        //                                        },
        //                                        new JsonSerializerSettings()
        //                                        {
        //                                            ContractResolver = new DefaultContractResolver()
        //                                            {
        //                                                NamingStrategy = new SnakeCaseNamingStrategy()
        //                                                {
        //                                                    OverrideSpecifiedNames = false
        //                                                }
        //                                            }
        //                                        }
        //                                    );
        //    }
        //    else
        //    {
        //        List<AutoRefundQuoteResponse> serviceresponse = _sabreAutoRefundService.AssessRefund(request, request.SessionID, contextid);
        //        lambdaResponse.statusCode = serviceresponse.All(a=> !a.Errors.IsNullOrEmpty()) ? 500 : 200;
        //        lambdaResponse.body = JsonConvert.
        //                                    SerializeObject
        //                                    (                          
        //                                        new LambdaQuoteResponseBody()
        //                                        {
        //                                            context_id = contextid,
        //                                            session_id = request.SessionID,
        //                                            error = serviceresponse.All(a => !a.Errors.IsNullOrEmpty()) ?
        //                                                        serviceresponse.
        //                                                            SelectMany(err => err.Errors).
        //                                                            Select(err =>
        //                                                                        new RefundError
        //                                                                        {
        //                                                                            code = err.code,
        //                                                                            message = err.message,
        //                                                                            stack = err.stack
        //                                                                        }).
        //                                                                        ToList():
        //                                                        new List<RefundError>(),
        //                                            data = serviceresponse.All(a => !a.Errors.IsNullOrEmpty()) ? null: serviceresponse
        //                                        },
        //                                        new JsonSerializerSettings()
        //                                        {
        //                                            ContractResolver = new DefaultContractResolver()
        //                                            {
        //                                                NamingStrategy = new SnakeCaseNamingStrategy()
        //                                                {
        //                                                    OverrideSpecifiedNames = false
        //                                                }
        //                                            }
        //                                        }
        //                                    );
        //    }

        //    _logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

        //    return lambdaResponse;
        //}

        //public async Task<LambdaResponse> ProcessRefund(AutoRefundProcessRequest request)
        //{
        //    _logger.LogInformation("***** ProcessRefund invoked *****");
        //    _logger.LogInformation($"#Request: {JsonConvert.SerializeObject(request)}");

        //    LambdaResponse lambdaResponse = new LambdaResponse()
        //    {
        //        headers = new Headers()
        //        {
        //            contentType = "application/json"
        //        }
        //    };
        //    string contextid = $"1W-{string.Join(",", request.RefundTickets.Select(s => s.DocumentNumber))}-{Guid.NewGuid()}";

        //    if (request == null || request.RefundTickets.IsNullOrEmpty() || string.IsNullOrEmpty(request.SessionID))
        //    {
        //        lambdaResponse.statusCode = 400;
        //        lambdaResponse.body = JsonConvert.
        //                                    SerializeObject
        //                                    (
        //                                        new LambdaProcessRefundResponseBody()
        //                                        {
        //                                            context_id = contextid,
        //                                            session_id = request.SessionID,
        //                                            error = new List<RefundError>()
        //                                            {
        //                                                new RefundError
        //                                                {
        //                                                    code = "INVALID_REQUEST",
        //                                                    message = "Mandatory request elements missing."
        //                                                }
        //                                            }
        //                                        },
        //                                        new JsonSerializerSettings()
        //                                        {
        //                                            ContractResolver = new DefaultContractResolver()
        //                                            {
        //                                                NamingStrategy = new SnakeCaseNamingStrategy()
        //                                                {
        //                                                    OverrideSpecifiedNames = false
        //                                                }
        //                                            }
        //                                        }
        //                                    );
        //    }
        //    else
        //    {
        //        List<AutoRefundProcessResponse> serviceresponse = _sabreAutoRefundService.ProcessRefund(request, request.SessionID, contextid);
        //        lambdaResponse.statusCode = serviceresponse.All(a => !a.Errors.IsNullOrEmpty()) ? 500 : 200;
        //        lambdaResponse.body = JsonConvert.
        //                                    SerializeObject
        //                                    (
        //                                        new LambdaProcessRefundResponseBody()
        //                                        {
        //                                            context_id = contextid,
        //                                            session_id = request.SessionID,
        //                                            error = serviceresponse.All(a => !a.Errors.IsNullOrEmpty()) ?
        //                                                        serviceresponse.
        //                                                            SelectMany(err => err.Errors).
        //                                                            Select(err =>
        //                                                                        new RefundError
        //                                                                        {
        //                                                                            code = err.code,
        //                                                                            message = err.message,
        //                                                                            stack = err.stack
        //                                                                        }).
        //                                                                        ToList() :
        //                                                        new List<RefundError>(),
        //                                            data = serviceresponse.All(a => !a.Errors.IsNullOrEmpty()) ? null : serviceresponse
        //                                        },
        //                                        new JsonSerializerSettings()
        //                                        {
        //                                            ContractResolver = new DefaultContractResolver()
        //                                            {
        //                                                NamingStrategy = new SnakeCaseNamingStrategy()
        //                                                {
        //                                                    OverrideSpecifiedNames = false
        //                                                }
        //                                            }
        //                                        }
        //                                    );
        //    }

        //    _logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

        //    return lambdaResponse;
        //}

        //public async Task<LambdaResponse> ValidateTickets(ValidateTicketRequest request)
        //{
        //    _logger.LogInformation("***** ValidateTickets invoked *****");
        //    _logger.LogInformation($"#Request: {string.Join(",", request)}");

        //    string contextid = $"1W-{string.Join(",", request)}-{Guid.NewGuid()}";
        //    LambdaResponse lambdaResponse = new LambdaResponse()
        //    {
        //        headers = new Headers()
        //        {
        //            contentType = "application/json"
        //        }
        //    };

        //    if (request == null || request.Tickets.IsNullOrEmpty() || string.IsNullOrEmpty(request.SessionID))
        //    {
        //        lambdaResponse.statusCode = 400;
        //        lambdaResponse.body = JsonConvert.
        //                                    SerializeObject
        //                                    (
        //                                        new ValidateTicketResponseBody()
        //                                        {
        //                                            context_id = contextid,
        //                                            session_id = request.SessionID,
        //                                            error = new List<RefundError>()
        //                                            {
        //                                                new RefundError
        //                                                {
        //                                                    code = "INVALID_REQUEST",
        //                                                    message = "Mandatory request elements missing."
        //                                                }
        //                                            }
        //                                        },
        //                                        new JsonSerializerSettings()
        //                                        {
        //                                            ContractResolver = new DefaultContractResolver()
        //                                            {
        //                                                NamingStrategy = new SnakeCaseNamingStrategy()
        //                                                {
        //                                                    OverrideSpecifiedNames = false
        //                                                }
        //                                            }
        //                                        }
        //                                    );
        //    }
        //    else if (request.Tickets.Any(a => a.Trim().Length != 13))
        //    {
        //        lambdaResponse.statusCode = 400;
        //        lambdaResponse.body = JsonConvert.
        //                                    SerializeObject
        //                                    (
        //                                        new ValidateTicketResponseBody()
        //                                        {
        //                                            context_id = contextid,
        //                                            session_id = request.SessionID,
        //                                            error = new List<RefundError>()
        //                                            {
        //                                                new RefundError
        //                                                {
        //                                                    code = "INVALID_TKT_NO",
        //                                                    message = "Invalid ticket number found."
        //                                                }
        //                                            }
        //                                        },
        //                                        new JsonSerializerSettings()
        //                                        {
        //                                            ContractResolver = new DefaultContractResolver()
        //                                            {
        //                                                NamingStrategy = new SnakeCaseNamingStrategy()
        //                                                {
        //                                                    OverrideSpecifiedNames = false
        //                                                }
        //                                            }
        //                                        }
        //                                    );
        //    }
        //    else
        //    {
        //        var serviceresponse = _sabreAutoRefundService.ValidateTickets(request, contextid);
        //        if (serviceresponse)
        //        {
        //            lambdaResponse.statusCode = 200;
        //            lambdaResponse.body = JsonConvert.
        //                                        SerializeObject
        //                                        (
        //                                            new ValidateTicketResponseBody()
        //                                            {
        //                                                context_id = contextid,
        //                                                session_id = request.SessionID,
        //                                                data = serviceresponse
        //                                            },
        //                                            new JsonSerializerSettings()
        //                                            {
        //                                                ContractResolver = new DefaultContractResolver()
        //                                                {
        //                                                    NamingStrategy = new SnakeCaseNamingStrategy()
        //                                                    {
        //                                                        OverrideSpecifiedNames = false
        //                                                    }
        //                                                }
        //                                            }
        //                                        );
        //        }
        //        else
        //        {
        //            lambdaResponse.statusCode = 405;
        //            lambdaResponse.body = JsonConvert.
        //                                        SerializeObject
        //                                        (
        //                                            new ValidateTicketResponseBody()
        //                                            {
        //                                                context_id = contextid,
        //                                                session_id = request.SessionID,
        //                                                error = new List<RefundError>()
        //                                                {
        //                                                new RefundError
        //                                                {
        //                                                    code = "TKT_NOT_QUALIFY_FOR_REFUND",
        //                                                    message = "Tickets from different carriers or for routes not permitted via online refund."
        //                                                }
        //                                                }
        //                                            },
        //                                            new JsonSerializerSettings()
        //                                            {
        //                                                ContractResolver = new DefaultContractResolver()
        //                                                {
        //                                                    NamingStrategy = new SnakeCaseNamingStrategy()
        //                                                    {
        //                                                        OverrideSpecifiedNames = false
        //                                                    }
        //                                                }
        //                                            }
        //                                        );
        //        }
        //    }

        //    _logger.LogInformation($"Response: {JsonConvert.SerializeObject(lambdaResponse)}");

        //    return lambdaResponse;
        //}
    }    
}
