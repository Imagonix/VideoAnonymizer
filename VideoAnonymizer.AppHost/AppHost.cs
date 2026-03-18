var builder = DistributedApplication.CreateBuilder(args);

var rabbitPassword = builder.AddParameter("rabbit-password", secret: true);
var rabbitUser = builder.AddParameter("rabbit-user", "rabbit");
var rabbit = builder.AddRabbitMQ("rabbit", rabbitUser, rabbitPassword).WithManagementPlugin();

var faceDetection = builder.AddUvicornApp(
        name: "objectDetection",
        appDirectory: "../VideoAnonymizer.ObjectDetection",
        app: "main:app"
    )
    .WithExternalHttpEndpoints();

var apiService = builder.AddProject<Projects.VideoAnonymizer_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(rabbit)
    .WaitFor(rabbit);

builder.AddProject<Projects.VideoAnonymizer_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.VideoAnonymizer_VideoProcessor>("videoanonymizer-videoprocessor")
    .WithReference(faceDetection)
    .WithReference(rabbit)
    .WaitFor(rabbit);


builder.Build().Run();
