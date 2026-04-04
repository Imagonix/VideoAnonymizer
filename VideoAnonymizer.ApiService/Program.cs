using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using VideoAnonymizer.ApiService;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();
var mvcBuilder = builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddModelValidationLogging();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AsyncNotificationService>();
    x.ConfigureRabbitMq(builder);
});
builder.Services.AddSingleton<LongRunningJobsHub>();
builder.Services.AddScoped<VideoDataService>();

builder.AddVideoAnonymizerDbContextFactory();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024;
    options.MultipartHeadersLengthLimit = 32 * 1024;
    options.MultipartBoundaryLengthLimit = 256;
    options.MemoryBufferThreshold = 128 * 1024;
    options.ValueLengthLimit = int.MaxValue;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("AllowAll");
}
app.MapDefaultEndpoints();
app.MapControllers();
app.MapHub<LongRunningJobsHub>(SharedConstants.SignalR.JobHubUrl);

app.Run();