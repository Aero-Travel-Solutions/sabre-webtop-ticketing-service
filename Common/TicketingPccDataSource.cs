using SabreWebtopTicketingService.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class TicketingPccDataSource
    {
        private readonly LambdaHelper lambda;

        private readonly SessionDataSource session;

        private readonly string PFX_ADB;

        public TicketingPccDataSource(LambdaHelper lambda, SessionDataSource session)
        {
            this.lambda = lambda;
            this.session = session;
            PFX_ADB = $"agent-database-{System.Environment.GetEnvironmentVariable("ENVIRONMENT")??"dev"}";
        }


        public async Task<TicketingPcc> GetDefaultTicketingPccByGdsCode(string gdsCode, string sessionID)
        {
            var list = await RetrieveTicketingPccs(sessionID);
            if (list == null || list.PccList == null)
            {
                return null;
            }
            return
                list.PccList.FirstOrDefault(p => p.IsDefault && p.GdsCode == gdsCode)
                ?? list.PccList.FirstOrDefault(p => p.GdsCode == gdsCode);
        }

        public async Task<TicketingPccList> RetrieveTicketingPccs(string sessionID)
        {
            var consolidator_id = (await session.GetSessionUser(sessionID))?.ConsolidatorId;
            var result = await lambda.Invoke<TicketingPcc[]>($"{PFX_ADB}-ticketing-pcc-list", new { consolidator_id }, sessionID);
            return new TicketingPccList { PccList = result };
        }
    }
}
