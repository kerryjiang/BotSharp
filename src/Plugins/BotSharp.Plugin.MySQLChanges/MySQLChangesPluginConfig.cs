namespace BotSharp.Plugin.MySQLChanges;

public class MySQLChangesPluginConfig
{
    public MySQLServerConfig[] MySQLServers { get; set; }
    
    public class MySQLServerConfig
    {
        public string HostName { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }
    }
}