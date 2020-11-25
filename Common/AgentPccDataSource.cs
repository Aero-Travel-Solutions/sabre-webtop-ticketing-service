using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class AgentPccDataSource : IAgentPccDataSource
    {
        private readonly LambdaHelper lambda;

        private readonly SessionDataSource session;

        private readonly string PFX_ADB;

        public AgentPccDataSource(LambdaHelper lambda, SessionDataSource session)
        {
            this.lambda = lambda;
            this.session = session;
            PFX_ADB = $"agent-database-{System.Environment.GetEnvironmentVariable("ENVIRONMENT")}";
        }

        public async Task<AgentPccList> RetrieveAgentPccs(string agent_id, string sessionID)
        {
            var consolidator_id = (await session.GetSessionUser(sessionID))?.ConsolidatorId;
            var result = await lambda.Invoke<AgentPcc[]>($"{PFX_ADB}-agent-pcc-list", new { consolidator_id, agent_id }, sessionID);
            return new AgentPccList { PccList = result };
        }

        public async Task<Agent> RetrieveAgentDetails(string consolidator_id, string agent_id, string sessionID)
        {           
            var result = await lambda.Invoke<Agent>($"{PFX_ADB}-agent-retrieve", new { consolidator_id, agent_id }, sessionID);
            return result;
        }

        public async Task<List<Agent>> RetrieveAgents(string consolidator_id, string sessionID)
        {
            var result = await lambda.Invoke<List<Agent>>($"{PFX_ADB}-agent-pcc-list-by-consolidator", new { consolidator_id }, sessionID);
            return result;
        }
    }
}
