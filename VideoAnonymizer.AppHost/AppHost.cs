using Microsoft.Extensions.Hosting;
using VideoAnonymizer.Contracts.Extensions;

var builder = DistributedApplication.CreateBuilder(args);


var rabbitPassword = DefaultLoadSecret(builder, "rabbit-password");
var rabbitUser = builder.AddParameter("rabbit-user", "rabbit");
var rabbit = builder.AddRabbitMQ("rabbit", rabbitUser, rabbitPassword).WithManagementPlugin();

var postgresPassword = DefaultLoadSecret(builder, "postgres-password");
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithEnvironment("POSTGRES_DB", "postgresdb");
//.WithVolume("video-anonomyzer-postgres-data", "/var/lib/postgresql/data");
if (builder.Environment.IsDevelopment())
{
    postgres.WithPgAdmin();
}

var postgresdb = postgres.AddDatabase("videoAnonymizerDb");

var migrationService = builder.AddProject<Projects.VideoAnonymizer_Database_MigrationService>("videoanonymizer-database-migrationservice")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

var objectDetection = builder.AddUvicornApp(
        name: "objectDetection",
        appDirectory: "../VideoAnonymizer.ObjectDetection",
        app: "main:app");

if (builder.Environment.IsDevelopment())
{
    objectDetection.WithEnvironment("PYDEVD_WARN_SLOW_RESOLVE_TIMEOUT", "0")
        .WithEnvironment("PYTHON_ENV", "Development")
        .WithEnvironment("DEBUGPY", "1")
        .WithEnvironment("DEBUGPY_PORT", "5678")
        // when setting DEBUGPY_WAIT to 1 FAST API wont start up, until a debugger attaches
        .WithEnvironment("DEBUGPY_WAIT", "0")
        .WithEnvironment("PYDEVD_WARN_SLOW_RESOLVE_TIMEOUT", "10.0");
}
if (builder.Environment.IsTest())
{
    objectDetection
        .WithEnvironment("UVICORN_RELOAD", "false")
        .WithEnvironment("PYTHON_ENV", "Test");
}

var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "data");

var apiService = builder.AddProject<Projects.VideoAnonymizer_ApiService>("apiservice")
    .WithHttpEndpoint(port: 5001, name: "apiservice-http")
    .WithHttpsEndpoint(port: 5002, name: "apiservice-https")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithReference(rabbit)
    .WaitFor(rabbit);

if (!builder.Environment.IsTest()) { 
    builder.AddProject<Projects.VideoAnonymizer_Web>("webfrontend")
        .WithExternalHttpEndpoints()
        .WithHttpHealthCheck("/health")
        .WithReference(apiService)
        .WaitFor(apiService);
}

var videoProcessor = builder.AddProject<Projects.VideoAnonymizer_VideoProcessor>("videoanonymizer-videoprocessor")
    .WithReference(objectDetection)
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithReference(rabbit)
    .WaitFor(rabbit);

if (builder.Environment.IsTest())
{
    SetEnvironmentTest(migrationService);
    SetEnvironmentTest(apiService);
    SetEnvironmentTest(videoProcessor);
}

builder.Build().Run();

static IResourceBuilder<ParameterResource> DefaultLoadSecret(IDistributedApplicationBuilder builder, string key)
{
    return LoadSecret(builder, key, $"{key}-test");
}

static IResourceBuilder<ParameterResource> LoadSecret(IDistributedApplicationBuilder builder, string key, string testValue)
{
    return builder.Environment.IsTest()
        ? builder.AddParameter(key, testValue)
        : builder.AddParameter(key, secret: true);
}

static void SetEnvironmentTest(IResourceBuilder<ProjectResource> apiService)
{
    apiService
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", HostEnvironmentExtensions.ENVIRONMENT_TEST)
        .WithEnvironment("DOTNET_ENVIRONMENT", HostEnvironmentExtensions.ENVIRONMENT_TEST);
}