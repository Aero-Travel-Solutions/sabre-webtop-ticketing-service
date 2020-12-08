using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
  public interface IAgentPccDataSource
  {
        Task<AgentPccList> RetrieveAgentPccs(string agentId, string sessionID);
        Task<ADBAgent> RetrieveAgentDetails(string consolidator_id, string agent_id, string sessionID);
        Task<List<DataAgent>> RetrieveAgents(string consolidator_id, string sessionID);
  }
}