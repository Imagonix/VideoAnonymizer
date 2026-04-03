namespace VideoAnonymizer.Web
{
    public static class ConfigurationExtensions
    {
        public static string GetApiServiceBaseUrl(this IConfiguration configuration)
        {
            return configuration["ApiService:BaseUrl"]
                    ?? throw new InvalidOperationException("ApiService:BaseUrl not found in appsettings");
        }
    }
}
