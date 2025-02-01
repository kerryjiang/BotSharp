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
            var chanegRecords = GetChangeRecords(changeLog);

            foreach (var chanegRecord in chanegRecords)
            {
                foreach (var hook in _changeDataCpatureHooks)
                {
                    await hook.OnChangeCaptured(chanegRecord);
                }
            }
        }

        await client.CloseAsync();
    }

    private IEnumerable<ChangeRecord> GetChangeRecords(LogEvent logEvent)
    {
        var changeEventType = GetChangeEventType(logEvent.EventType);

        if (changeEventType == null || !(logEvent is RowsEvent rowsEvent))
        {
            yield break;
        }

        foreach (var row in rowsEvent.RowSet.Rows)
        {
            var fields = new Dictionary<string, object>();

            for (var i = 0; i < row.Length; i++)
            {
                fields.Add(rowsEvent.RowSet.ColumnNames[i], row[i]);
            }

            yield return new ChangeRecord
            {
                EventType = changeEventType.Value,
                Fields = fields
            };
        }
    }

    private ChangeEventType? GetChangeEventType(LogEventType logEventType)
    {
        switch (logEventType)
        {
            case LogEventType.WRITE_ROWS_EVENT:
            case LogEventType.WRITE_ROWS_EVENT_V0:
            case LogEventType.WRITE_ROWS_EVENT_V1:
                return ChangeEventType.Added;
            case LogEventType.UPDATE_ROWS_EVENT:
            case LogEventType.UPDATE_ROWS_EVENT_V0:
            case LogEventType.UPDATE_ROWS_EVENT_V1:
                return ChangeEventType.Updated;
            case LogEventType.DELETE_ROWS_EVENT:
            case LogEventType.DELETE_ROWS_EVENT_V0:
            case LogEventType.DELETE_ROWS_EVENT_V1:
                return ChangeEventType.Deleted;
            default:
                return null;
        }
    }
}