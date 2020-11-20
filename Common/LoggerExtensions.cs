using Microsoft.Extensions.Logging;

namespace SabreWebtopTicketingService.Common
{
    /// <summary>
    /// Extensions for <see cref="Microsoft.Extensions.Logging.ILogger"/>
    /// </summary>
    public static class LoggerExtensions
    {
        public static void LogMaskInformation<T>(this ILogger<T> logger, string message)
        {
            logger.LogInformation(message.MaskLog());
        }
    }
}
