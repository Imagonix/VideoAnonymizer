using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.VideoProcessor
{
    public static class IServiceCollectionExtensions
    {
        public static void AddSingletonAsHostedService<T>(this IServiceCollection services)
            where T : class, IHostedService
        {
            services.AddSingleton<T>();
            services.AddHostedService(sp => sp.GetRequiredService<T>());
        }
    }
}
