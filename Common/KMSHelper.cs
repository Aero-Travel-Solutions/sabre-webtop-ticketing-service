using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public interface IKMSHelper
    {
        Task<string> Encrypt(string textToEncrypt);
        Task<string> Decrypt(string encryptedText);
    }
    public class KMSHelper : IKMSHelper
    {
        private readonly IAmazonKeyManagementService _client;
        private readonly ILogger _logger;
        private readonly string _keyID;

        public KMSHelper(IAmazonKeyManagementService client, ILogger logger)
        {
            _client = client;
            _logger = logger;
            _keyID = Environment.GetEnvironmentVariable("WEBTOP_SERVICE_KEY") ?? string.Empty;
        }
        public async Task<string> Encrypt(string textToEncrypt)
        {
            if (string.IsNullOrWhiteSpace(textToEncrypt))
            {
                return "";
            }
            try
            {
                var textBytes = Encoding.UTF8.GetBytes(textToEncrypt);
                var encryptRequest = new EncryptRequest
                {
                    KeyId = _keyID,
                    Plaintext = new MemoryStream(textBytes, 0, textBytes.Length)
                };
                var response = await _client.EncryptAsync(encryptRequest);
                if (response != null)
                {
                    return Convert.ToBase64String(response.CiphertextBlob.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
            return "";
        }
        public async Task<string> Decrypt(string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                return "";
            }
            try
            {
                var fromBase64Bytes = Convert.FromBase64String(encryptedText);
                var decryptRequest = new DecryptRequest
                {
                    CiphertextBlob = new MemoryStream(fromBase64Bytes, 0, fromBase64Bytes.Length)
                };
                var response = await _client.DecryptAsync(decryptRequest);
                if (response != null)
                {
                    return Encoding.UTF8.GetString(response.Plaintext.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
            return "";
        }
    }
}
