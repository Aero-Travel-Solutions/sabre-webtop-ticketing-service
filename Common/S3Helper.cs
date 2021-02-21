using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class S3Helper
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3Helper> _logger;
        public S3Helper(IAmazonS3 s3Client, ILogger<S3Helper> logger)
        {
            _s3Client = s3Client;
            _logger = logger;
        }
        public async Task<T> Read<T>(string bucketName, string key)
        {
            try
            {
                var getObjectRequest = new GetObjectRequest()
                {
                    BucketName = bucketName,
                    Key = key
                };

                var getObjectResponse = await _s3Client.GetObjectAsync(getObjectRequest);

                _logger.LogInformation($"S3-{bucketName}/{key}");
                _logger.LogInformation($"RS: { JsonSerializer.DeserializeAsync<T>(getObjectResponse.ResponseStream)}");

                using (var stream = getObjectResponse.ResponseStream)
                {
                    return await JsonSerializer.DeserializeAsync<T>(stream);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }            
        }

        public async Task<bool> Write(string bucketName, string key, string json)
        {
            try
            {
                byte[] byteArray = Encoding.ASCII.GetBytes(json);
                MemoryStream stream = new MemoryStream(byteArray);
                stream.Position = 0;

                PutObjectRequest request = new PutObjectRequest()
                {
                    InputStream = stream,
                    BucketName = bucketName,
                    Key = key,
                    ContentType = "application/json; charset=utf-8",
                };

                var putObjectResponse = await _s3Client.PutObjectAsync(request);

                return putObjectResponse.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
