using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using VideoAnonymizer.Web;
using VideoAnonymizer.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using VideoAnonymizer.Web.Utils;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMudServices();
builder.Services.AddServiceDiscovery();

builder.Services.AddHttpClient("ApiService", client =>
{
    var baseUrl = builder.Configuration.GetApiServiceBaseUrl();
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<IJobHubClient, JobHubClient>();
builder.Services.AddScoped<IDownloadService, DownloadService>();

await builder.Build().RunAsync();
