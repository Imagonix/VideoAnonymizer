using VideoAnonymizer.ApiService;
using VideoAnonymizer.ApiService.Notifications;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Database.Extensions;
using VideoAnonymizer.Database.SQLite.Extensions;
using VideoAnonymizer.VideoProcessor;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.StandaloneHost;

var builder = WebApplication.CreateBuilder(args);

var standaloneUrl = builder.Configuration["Standalone:Url"] ?? "http://127.0.0.1:5117";
builder.WebHost.UseUrls(standaloneUrl);

builder.Services.AddSingleton<IMessagePublisher, DirectMessagePublisher>();
builder.Services.AddVideoProcessorWorkers();
builder.Services.AddVideoProcessorMessageHandlers();

builder.AddSqliteVideoAnonymizerDbContextFactory();
builder.AddVideoAnonymizerApiServices();

builder.Services.AddHostedService<ObjectDetectionProcessHostedService>();
builder.Services.AddHostedService<BrowserLauncherHostedService>();

var app = builder.Build();

await app.Services.ApplyDatabaseMigrationsAsync(app.Configuration);

app.UseExceptionHandler();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<LongRunningJobsHub>(SharedConstants.SignalR.JobHubUrl);
app.MapFallbackToFile("index.html");

await app.RunAsync();
