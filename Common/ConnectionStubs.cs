using System.Net;
using System.ServiceModel;

namespace SabreWebtopTicketingService.Common
{
    public class ConnectionStubs
    {
        //Enable TLS
        public void EnableTLS()
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.CheckCertificateRevocationList = false;
        }

        //Basic HTTP Binding
        public BasicHttpBinding GetBasicHttpBinding()
        {
            //GET PROXY configurtations
            BasicHttpBinding bHTTPBinding = new BasicHttpBinding();
            bHTTPBinding.Name = "AeronologyBinding";
            bHTTPBinding.UseDefaultWebProxy = true;
            bHTTPBinding.Security.Mode = BasicHttpSecurityMode.Transport;
            bHTTPBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            bHTTPBinding.MaxReceivedMessageSize = 2147483647;
            return bHTTPBinding;
        }
    }
}
