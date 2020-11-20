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
        public object stack { get; set; }
    }

    public class LambdaResponseBody
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public SearchPNRResponse data { get; set; }
        public List<WebtopError> error { get; set; }
    }
}
