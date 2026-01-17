namespace QuartzNodeService.QuartzNodeGrpcApi
{
    public class ApiKeyProvider
    {
        public string Key { get; }
        public string ServiceID { get; }

        public string ServerName { get; }

        public string ServiceName { get; }

        public ApiKeyProvider(string key, string serviceID, string serviceName, string serverName)
        {
            Key = key;
            ServiceID = serviceID;
            ServiceName = serviceName;
            ServerName = serverName;

		}
    }
}
