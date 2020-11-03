using Amazon.Runtime.Internal.Util;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Interface;
using System;

namespace SabreWebtopTicketingService.Services
{
    public class SabreGDS
    {
        private readonly string url;
        private readonly SessionCreateService _sessionCreateService;
        private readonly IgnoreTransactionService _ignoreTransactionService;
        private readonly ChangeContextService _changeContextService;
        private readonly DisplayTicketService _displayTicket;
        private readonly ConsolidatorPccDataSource _consolidatorPccDataSource;
        private readonly TicketingPccDataSource _ticketingPccDataSource;
        private readonly SabreCrypticCommandService _sabreCommandService;
        private readonly IGetTurnaroundPointDataSource _getTurnaroundPointDataSource;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly DbCache _dbCache;

        public SabreGDS(
            SessionCreateService sessionCreateService,
            Microsoft.Extensions.Logging.ILogger logger,
            DbCache dbCache,
            ConsolidatorPccDataSource consolidatorPccDataSource,
            TicketingPccDataSource ticketingPccDataSource,
            IgnoreTransactionService ignoreTransactionService,
            ChangeContextService changeContextService,
            DisplayTicketService displayTicket,
            SabreCrypticCommandService sabreCommandService,
            IGetTurnaroundPointDataSource getTurnaroundPointDataSource)
        {
            url = Constants.GetSoapUrl();
            _sessionCreateService = sessionCreateService;
            _logger = logger;
            _dbCache = dbCache;
            _consolidatorPccDataSource = consolidatorPccDataSource;
            _ticketingPccDataSource = ticketingPccDataSource;
            _ignoreTransactionService = ignoreTransactionService;
            _changeContextService = changeContextService;
            _displayTicket = displayTicket;
            _sabreCommandService = sabreCommandService;
            _getTurnaroundPointDataSource = getTurnaroundPointDataSource;
        }
    }
}
