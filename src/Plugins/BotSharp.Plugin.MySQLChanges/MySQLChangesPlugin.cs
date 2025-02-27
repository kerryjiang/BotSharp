namespace BotSharp.Plugin.MySQLChanges;

using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class MySQLChangesPlugin : IBotSharpPlugin
{
    public string Id => "7984163e-2dd4-485a-b6cd-149eef47d13d";

    public static string Name => nameof(MySQLChangesPlugin);

    public string Description => "A plugin to listen the changes of your MySQL databases.";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IBotSharpPlugin, MySQLChangesPlugin>();
        services.AddSingleton<IAgentHook, MySQLChangesAgentHook>();
        services.AddOptions<MySQLChangesPluginConfig>().Bind(config.GetSection(Name));
    }
}