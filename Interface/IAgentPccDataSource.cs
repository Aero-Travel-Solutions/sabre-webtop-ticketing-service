using SabreWebtopTicketingService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
  public interface IAgentPccDataSource
  {
        Task<AgentPccList> RetrieveAgentPccs(string agentId, string sessionID);
        Task<Agent> RetrieveAgentDetails(string consolidator_id, string agent_id, string sessionID);
        Task<List<Agent>> RetrieveAgents(string consolidator_id, string sessionID);
  }
}