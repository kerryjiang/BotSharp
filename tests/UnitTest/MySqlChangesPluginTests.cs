using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.MySQLChanges;
using BotSharp.Abstraction.Plugins;
using MySql.Data.MySqlClient;
using System.Threading.Tasks.Sources;

namespace UnitTest
{
    [TestClass]
    public class MySqlChangesPluginTests
    {
        private const string _host = "localhost";
        private const string _username = "root";
        private const string _password = "root";

        [TestMethod]
        public async Task TestMySqlChangesAgentHook()
        {
            var hooks = Enumerable.Range(0, 3)
                .Select(_ => new TestChangeDataCaptureHook())
                .ToArray();

            using var host = Host.CreateDefaultBuilder()
                .ConfigureServices((services, context) =>
                {
                    services.AddHostedService<AgentHostService>();
                    services.UseMySQLChangesPlugin(context.Configuration);
                    services.AddSingleton<IBotSharpPlugin, MySQLChangesPlugin>();

                    foreach (var hook in hooks)
                    {
                        services.AddSingleton<IChangeDataCaptureHook>(hook);
                    }
                })
                .Build();

            await host.StartAsync();

            using var mysqlConn = new MySqlConnection($"Server={_host};Database=garden;Uid={_username};Pwd={_password};");

            await mysqlConn.OpenAsync();

            // insert
            var cmd = mysqlConn.CreateCommand();
            cmd.CommandText = "INSERT INTO pet (name, owner, species, sex, birth, death) values ('Rokie', 'Kerry', 'abc', 'F', '1982-04-20', '3000-01-01'); SELECT LAST_INSERT_ID();";
            var id = (UInt64)(await cmd.ExecuteScalarAsync())!;

            // update
            cmd = mysqlConn.CreateCommand();
            cmd.CommandText = "update pet set owner='Linda' where `id`=" + id;
            await cmd.ExecuteNonQueryAsync();

            // delete
            cmd = mysqlConn.CreateCommand();
            cmd.CommandText = "delete from pet where `id`= " + id;
            await cmd.ExecuteNonQueryAsync();   

            var tasks = hooks.Select(async hook => 
            {
                var records = new List<ChangeRecord>();

                for (var i = 0; i < 3; i++)
                {
                    var record = await hook.GetChangeRecordAsync();
                    records.Add(record);
                }

                return records;

            }).ToList();

            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(30));

            Assert.IsTrue(tasks.All(t => t.IsCompletedSuccessfully));
            Assert.IsTrue(tasks.All(t => t.Result.Count == 3));

            await host.StopAsync();
        }

        class TestChangeDataCaptureHook : IChangeDataCaptureHook, IValueTaskSource<ChangeRecord>
        {
            private List<ChangeRecord> _changeRecords = new List<ChangeRecord>();

            public IReadOnlyList<ChangeRecord> ChangeRecords => _changeRecords;

            private ManualResetValueTaskSourceCore<ChangeRecord> _valueTaskSourceCore  = new ManualResetValueTaskSourceCore<ChangeRecord>();

            public TestChangeDataCaptureHook()
            {
            }

            public Task<bool> OnChangeCaptured(ChangeRecord record)
            {
                _changeRecords.Add(record);
                _valueTaskSourceCore.SetResult(record);
                return Task.FromResult(true);
            }

            public ValueTask<ChangeRecord> GetChangeRecordAsync()
            {
                return new ValueTask<ChangeRecord>(this, _valueTaskSourceCore.Version);
            }

            public ChangeRecord GetResult(short token)
            {
                return _valueTaskSourceCore.GetResult(token);
            }

            public ValueTaskSourceStatus GetStatus(short token)
            {
                return _valueTaskSourceCore.GetStatus(token);
            }

            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                _valueTaskSourceCore.OnCompleted(continuation, state, token, flags);
            }
        }
    }
}