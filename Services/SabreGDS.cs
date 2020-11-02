using Amazon.Runtime.Internal.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Services
{
    public class SabreGDS
    {
        public SabreGDS(
            SessionCreateService sessionCreateService,
            ILogger logger,
            DbCache dbCache,
            ConsolidatorPccDataSource consolidatorPccDataSource,
            TicketingPccDataSource ticketingPccDataSource,
            IgnoreTransactionService ignoreTransactionService,
            ChangeContextService changeContextService,
            DisplayTicketService displayTicket,
            SabreCrypticCommandService sabreCommandService,
            IRefundControlDataSource refundControlDataSource,
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
            _refundControlDataSource = refundControlDataSource;
            _getTurnaroundPointDataSource = getTurnaroundPointDataSource;
        }
    }
}
