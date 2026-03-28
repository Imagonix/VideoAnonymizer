using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var rabbitPassword = builder.AddParameter("rabbit-password", secret: true);
var rabbitUser = builder.AddParameter("rabbit-user", "rabbit");
var rabbit = builder.AddRabbitMQ("rabbit", rabbitUser, rabbitPassword).WithManagementPlugin();

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithEnvironment("POSTGRES_DB", "postgresdb");
    //.WithVolume("video-anonomyzer-postgres-data", "/var/lib/postgresql/data");
if (builder.Environment.IsDevelopment())
{
    //postgres.WithPgAdmin();
}
var postgresdb = postgres.AddDatabase("videoAnonymizerDb");

var migrationService = builder.AddProject<Projects.VideoAnonymizer_Database_MigrationService>("videoanonymizer-database-migrationservice")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

var objectDetection = builder.AddUvicornApp(
        name: "objectDetection",
        appDirectory: "../VideoAnonymizer.ObjectDetection",
        app: "main:app"
    )
    .WithExternalHttpEndpoints();

if (builder.Environment.IsDevelopment())
{
    objectDetection.WithEnvironment("PYDEVD_WARN_SLOW_RESOLVE_TIMEOUT", "0")
      .WithEnvironment("PYTHON_ENV", "Development")
      .WithEnvironment("DEBUGPY", "1")
      .WithEnvironment("DEBUGPY_PORT", "5678")
      // when setting DEBUGPY_WAIT to 1 FAST API wont start up, until a debugger attaches
      .WithEnvironment("DEBUGPY_WAIT", "0")
      .WithEnvironment("PYDEVD_WARN_SLOW_RESOLVE_TIMEOUT", "10.0"); ;
}

var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "data");

var apiService = builder.AddProject<Projects.VideoAnonymizer_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

//builder.AddProject<Projects.VideoAnonymizer_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithHttpHealthCheck("/health")
//    .WithReference(apiService)
//    .WaitFor(apiService);

builder.AddProject<Projects.VideoAnonymizer_VideoProcessor>("videoanonymizer-videoprocessor")
    .WithReference(objectDetection)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithReference(postgresdb)
    .WaitFor(postgresdb);



builder.Build().Run();
