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


        public async Task<TicketingPcc> GetDefaultTicketingPccByGdsCode(string gdsCode, string sessionID, string consolidatorid)
        {
            var list = await RetrieveTicketingPccs(sessionID, consolidatorid);
            if (list == null || list.PccList == null)
            {
                return null;
            }
            return
                list.PccList.FirstOrDefault(p => p.IsDefault && p.GdsCode == gdsCode)
                ?? list.PccList.FirstOrDefault(p => p.GdsCode == gdsCode);
        }

        public async Task<TicketingPccList> RetrieveTicketingPccs(string sessionID, string consolidatorid)
        {
            var consolidator_id = consolidatorid;
            var result = await lambda.Invoke<TicketingPcc[]>($"{PFX_ADB}-ticketing-pcc-list", new { consolidator_id }, sessionID);
            return new TicketingPccList { PccList = result };
        }

        public async Task<PlateRuleTicketingPccResponse> GetTicketingPccFromRules(PlateRuleTicketingPccRequest request, string sessionID)
        {
            return await lambda.Invoke<PlateRuleTicketingPccResponse>($"{PFX_ADB}-plate-rule-retrieve-ticketing-pcc", request, sessionID);
        }
    }
}
