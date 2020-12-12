using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net;

namespace SabreWebtopTicketingService.Common
{
    public class ApiInvoker
    {
        private readonly ILogger logger;

        private readonly IHttpClientFactory httpClientFactory;

        public ApiInvoker(ILogger logger, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<HttpResponseMessage> InvokeApi(
            string clientFacatoryName,
            HttpMethod method,
            string url,
            object payload = null,
            string region = null,
            string sessionUserConsolidatorId = null,
            string token = null,
            string sessionId = null)
        {
            var client = httpClientFactory.CreateClient(clientFacatoryName);
            var apiUrl = $"{client.BaseAddress.OriginalString.TrimEnd('/')}{url}";
            logger.LogInformation($"Invoking API {apiUrl} with method {method.Method}");
            var rq = new HttpRequestMessage(method, apiUrl);
            var regionVal = !string.IsNullOrWhiteSpace(region) ? region : "ap-southeast-2";
            rq.Headers.Add("x-region", regionVal);

            if (!string.IsNullOrWhiteSpace(sessionUserConsolidatorId))
            {
                rq.Headers.Add("x-consolidator-id", sessionUserConsolidatorId);
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                rq.Headers.Add("x-session-id", sessionId);
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                var val = token.StartsWith("Bearer ", StringComparison.InvariantCulture) ? token : $"Bearer {token}";
                rq.Headers.Add("Authorization", val);
            }

            if (payload != null)
            {
                var data = JsonSerializer.Serialize(
                    payload,                    
                    new JsonSerializerOptions
                    {
                        IgnoreNullValues = true                        
                    });

                rq.Content = new StringContent(
                    data,
                    Encoding.UTF8,
                    "application/json");

                rq.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }

            var response = await client.SendAsync(rq);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{response.StatusCode} : {response.ReasonPhrase}");
            }

            logger.LogInformation($"{method.Method} {apiUrl} {response.StatusCode}");
            return response;
        }
    }
}
