using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using Microsoft.Extensions.Options;
using SciSharp.MySQL.Replication;

namespace BotSharp.Plugin.MySQLChanges;

internal class MySQLChangesAgentHook : AgentHookBase
{
    private readonly MySQLChangesPluginConfig _mySqlChangesConfig;

    private readonly IEnumerable<IChangeDataCaptureHook> _changeDataCpatureHooks;

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public MySQLChangesAgentHook(IServiceProvider services, AgentSettings settings, IOptions<MySQLChangesPluginConfig> mySqlChangesConfigOptions, IEnumerable<IChangeDataCaptureHook> changeDataCpatureHooks)
        : base(services, settings)
    {
        _mySqlChangesConfig = mySqlChangesConfig;
        _changeDataCpatureHooks = changeDataCpatureHooks;
    }

    public override void OnAgentLoaded(Agent agent)
    {
        _mySqlChangesConfig.Value.MySQLServers.ForEach(async mySQLServerConfig =>
        {
            await ConnectMySQLServerAsync(mySQLServerConfig, _cancellationTokenSource.Token);
        });
    }

    public override void OnAgentUnLoaded(Agent agent)
    {
        _cancellationTokenSource.Cancel();
    }

    private async Task ConnectMySQLServerAsync(MySQLServerConfig mySQLServerConfig, CancellationToken cancellationToken)
    {
        var client = new ReplicationClient();

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await client.ConnectAsync(
                mySQLServerConfig.HostName,
                mySQLServerConfig.UserName,
                mySQLServerConfig.Password,
                mySQLServerConfig.ServerId);

            if (result.Result)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var changeLog = await client.ReceiveAsync();
            var chanegRecord = GetChangeRecord(changeLog);

            foreach (var hook in _changeDataCpatureHooks)
            {
                await hook.OnChangeCaptured(chanegRecord);
            }
        }

        await client.CloseAsync();
    }

    private ChangeRecord GetChangeRecord(LogEvent logEvent)
    {
        throw new NotImplementedException();
    }
}