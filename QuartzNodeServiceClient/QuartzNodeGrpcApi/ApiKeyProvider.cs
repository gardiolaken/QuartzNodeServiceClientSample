namespace QuartzNodeService.QuartzNodeGrpcApi
{
    public class ApiKeyProvider
    {
        public string Key { get; }
        public string ServiceID { get; }

        public ApiKeyProvider(string key, string serviceID)
        {
            Key = key;
            ServiceID = serviceID;
        }
    }
}
