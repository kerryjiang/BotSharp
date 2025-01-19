using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Core.Infrastructures;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using SciSharp.MySQL.Replication;

namespace BotSharp.Plugin.MySQLChanges;

internal class MySQLChangesAgentHook : AgentHookBase
{
    private readonly MySQLChangesPluginConfig _mySqlChangesConfig;

    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private readonly IEnumerable<IChangeDataCaptureHook> _changeDataCpatureHooks;

    public MySQLChangesAgentHook(IServiceProvider services, AgentSettings settings, IOptions<MySQLChangesPluginConfig> mySqlChangesConfigOptions, IHostApplicationLifetime hostApplicationLifetime, IEnumerable<IChangeDataCaptureHook> changeDataCpatureHooks)
        : base(services, settings)
    {
        _mySqlChangesConfig = mySqlChangesConfigOptions.Value;
        _hostApplicationLifetime = hostApplicationLifetime;
        _changeDataCpatureHooks = changeDataCpatureHooks;
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var tasks = _mySqlChangesConfig.MySQLServers.Select(mySQLServerConfig =>
        {
            return ConnectMySQLServerAsync(mySQLServerConfig, _hostApplicationLifetime.ApplicationStopping);
        });

        _ = Task.WhenAll(tasks).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                // Log the failure;
            }
        });
    }

    private async Task ConnectMySQLServerAsync(MySQLChangesPluginConfig.MySQLServerConfig mySQLServerConfig, CancellationToken cancellationToken)
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