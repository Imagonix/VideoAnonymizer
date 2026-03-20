using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var rabbitPassword = builder.AddParameter("rabbit-password", secret: true);
var rabbitUser = builder.AddParameter("rabbit-user", "rabbit");
var rabbit = builder.AddRabbitMQ("rabbit", rabbitUser, rabbitPassword).WithManagementPlugin();

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithEnvironment("POSTGRES_DB", "postgresdb")
    .WithVolume("video-anonomyzer-postgres-data", "/var/lib/postgresql/data");
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
