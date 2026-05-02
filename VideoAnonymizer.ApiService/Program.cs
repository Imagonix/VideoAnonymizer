using VideoAnonymizer.ApiService;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.ApiService.Notifications;
using VideoAnonymizer.Contracts.Extensions;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database.Extensions;
using VideoAnonymizer.Database.Postgres.Extensions;
using VideoAnonymizer.ObjectDetectionClient;
using VideoAnonymizer.Web.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var mvcBuilder = builder.AddVideoAnonymizerApiServices();

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

builder.ConfigureRabbitMQConnection();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddRabbitMqVideoAnonymizerNotificationConsumers();


builder.AddPostgresVideoAnonymizerDbContextFactory();

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
