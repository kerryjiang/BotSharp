using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
using Microsoft.Extensions.Hosting;

namespace UnitTest
{
    public class AgentHostService : IHostedService
    {
        private readonly IEnumerable<IAgentHook> _agentHooks;
        private readonly Agent _agent;

        public AgentHostService(IEnumerable<IAgentHook> agentHooks)
        {
            _agentHooks = agentHooks;
            _agent = new Agent();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}