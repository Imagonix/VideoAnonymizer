namespace VideoAnonymizer.Web.Utils
{
    public static class ConfigurationExtensions
    {
        public static string GetApiServiceBaseUrl(this IConfiguration configuration, string fallbackBaseUrl)
        {
            return configuration["ApiService:BaseUrl"]
                    ?? fallbackBaseUrl;
        }
    }
}
