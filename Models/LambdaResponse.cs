using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class LambdaResponse
    {
        public int statusCode { get; set; }
        public Headers headers { get; set; }
        public string body { get; set; }
        public bool isBase64Encoded { get; set; }
    }

    public class Headers
    {
        public string contentType { get; set; }
    }

    public class WebtopError
    {
        public string code { get; set; }
        public string message { get; set; }
        public string stack { get; set; }
    }

    public class WebtopWarning
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    public class SearchPNRLambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public SearchPNRResponse data { get; set; }
        public List<WebtopError> error { get; set; }
    }

    public class GetQuoteLambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public List<Quote> data { get; set; }
        public List<WebtopError> error { get; set; }
    }

    public class IssueTicketLambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public IssueExpressTicketRS data { get; set; }
        public List<WebtopError> error { get; set; }
    }

    public class ValidateCommissionLambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public List<WebtopWarning> data { get; set; }
        public List<WebtopError> error { get; set; }
    }

    public class GetROELambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public GetROEResponse data { get; set; }
        public List<WebtopError> error { get; set; }
    }

    public class DisplayTicketLambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public string data { get; set; }
        public List<WebtopError> error { get; set; }
    }

    public class GetPNRTextLambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public string data { get; set; }
        public List<WebtopError> error { get; set; }
    }

    public class GetQuoteTextLambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public GetQuoteTextResponse data { get; set; }
        public List<WebtopError> error { get; set; }
    }
 
    public class MerchantLambdaResponse
    {
        public string ApprovalCode { get; set; }
        public string CardNumber { get; set; }
        public string CardType { get; set; }
        public string CardExpiry { get; set; }
        public bool Success { get; set; }
    }

    public class CurrencyConvertLambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public ConvertCurrencyResponse data { get; set; }
        public List<WebtopError> error { get; set; }
    }
}
