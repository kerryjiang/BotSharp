using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BotSharp.Core.Infrastructures;
using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.MySQLChanges;
using BotSharp.Abstraction.Plugins;

namespace UnitTest
{
    [TestClass]
    public class MySqlChangesPluginTests
    {
        [TestMethod]
        public async Task TestMySqlChangesAgentHook()
        {
            using var host = await Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHostedService<AgentHostService>();
                })
                .Build();

            await host.StartAsync();

            var services = new ServiceCollection();

            var hooks = Enumerable.Range(0, 3).Select(_ =>  new TestChangeDataCaptureHook()).ToArray();

            foreach (var hook in hooks)
            {
                services.AddSingleton<IChangeDataCaptureHook>(hook);
            }

            services.AddSingleton<IBotSharpPlugin, MySQLChangesPlugin>();

            await host.StopAsync();
        }

        class TestChangeDataCaptureHook : IChangeDataCaptureHook
        {
            private List<ChangeRecord> _changeRecords = new List<ChangeRecord>();

            public IReadOnlyList<ChangeRecord> ChangeRecords => _changeRecords;

            public TestChangeDataCaptureHook()
            {
            }

            public Task<bool> OnChangeCaptured(ChangeRecord record)
            {
                _changeRecords.Add(record);
                return Task.FromResult(true);
            }
        }
    }
}