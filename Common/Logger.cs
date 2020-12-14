using System;
using Serilog;
using Serilog.Formatting.Compact;

namespace SabreWebtopTicketingService.Common
{
	public interface ILogger
	{
		void LogInformation(string messageTemplate, params object[] arguments);
		void LogMaskInformation(string messageTemplate, params object[] arguments);
		void LogError(string messageTemplate, params object[] arguments);
		void LogError(Exception ex);
	}

	public class Logger : ILogger
	{
		private static Serilog.Core.Logger _logger;

		public Logger()
		{
			_logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Console(new CompactJsonFormatter())
				.CreateLogger();
		}

		public void LogInformation(string messageTemplate, params object[] arguments)
		{
			_logger.Information(messageTemplate, arguments);
		}

		public void LogMaskInformation(string messageTemplate, params object[] arguments)
        {
			_logger.Information(messageTemplate.Mask(), arguments);
		}

		public void LogError(string messageTemplate, params object[] arguments)
		{
			_logger.Error(messageTemplate, arguments);
		}

		public void LogError(Exception ex)
		{
			_logger.Error(ex, ex.Message + (ex.InnerException == null || string.IsNullOrEmpty(ex.InnerException.Message) ? "" : (Environment.NewLine + ex.InnerException.Message)));
		}
	}
}
