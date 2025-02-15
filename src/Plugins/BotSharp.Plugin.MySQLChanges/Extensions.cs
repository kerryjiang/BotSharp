namespace BotSharp.Plugin.MySQLChanges;

public static class Extensions
{
    public static IServiceCollection UseMySQLChangesPlugin(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IBotSharpPlugin, MySQLChangesPlugin>();
        services.AddSingleton<IAgentHook, MySQLChangesAgentHook>();
        services.Configure<MySQLChangesPluginConfig>(config.GetSection(MySQLChangesPlugin.Name));
        return services;
    }
}