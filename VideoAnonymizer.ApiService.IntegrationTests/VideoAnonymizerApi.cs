using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Reqnroll.Assist;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;

namespace VideoAnonymizer.ApiService.IntegrationTests
{
    [Binding]
    public sealed class VideoAnonymizerApi
    {
        private ScenarioContext _scenarioContext;

        private DistributedApplication App
        {
            get => (DistributedApplication)_scenarioContext[nameof(App)];
            set => _scenarioContext[nameof(App)] = value;
        }

        private TaskCompletionSource<LongRunningJobFinishedMessage> TaskCompletionSourceLongRunningJobFinishedMessage
        {
            get => (TaskCompletionSource<LongRunningJobFinishedMessage>)_scenarioContext[nameof(TaskCompletionSourceLongRunningJobFinishedMessage)];
            set => _scenarioContext[nameof(TaskCompletionSourceLongRunningJobFinishedMessage)] = value;
        }

        private LongRunningJobFinishedMessage VideoFinishedMessage
        {
            get => (LongRunningJobFinishedMessage)_scenarioContext[nameof(VideoFinishedMessage)];
            set => _scenarioContext[nameof(VideoFinishedMessage)] = value;
        }

        private ApiResponse<Guid>? PostVideoResponse
        {
            get => (ApiResponse<Guid>?)_scenarioContext[nameof(PostVideoResponse)];
            set => _scenarioContext[nameof(PostVideoResponse)] = value;
        }

        private ApiResponse<VideoAnalysis>? AnalyzedVideoResponse
        {
            get => (ApiResponse<VideoAnalysis>?)_scenarioContext[nameof(AnalyzedVideoResponse)];
            set => _scenarioContext[nameof(AnalyzedVideoResponse)] = value;
        }

        private ApiResponse<Guid>? AnonymizeVideoResponse
        {
            get => (ApiResponse<Guid>?)_scenarioContext[nameof(AnonymizeVideoResponse)];
            set => _scenarioContext[nameof(AnonymizeVideoResponse)] = value;
        }

        private const string AspireApiServiceKey = "apiservice";


        public VideoAnonymizerApi(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        private HttpClient CreteApiServiceHttpClient()
        {
                var client = App.CreateHttpClient(AspireApiServiceKey, "https");
                return client;
        }

        [BeforeScenario]
        public async Task SetupEnvironment()
        {
            var appHost =
            await DistributedApplicationTestingBuilder
                    .CreateAsync<Projects.VideoAnonymizer_AppHost>();
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });
            //appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            //{
            //    clientBuilder.AddStandardResilienceHandler(options =>
            //     {
            //         options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
            //         options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
            //     });
            // });

            App = await appHost.BuildAsync();
            await App.StartAsync();
            var client = CreteApiServiceHttpClient();
            //client.Timeout = TimeSpan.FromMinutes(3);
            await client.GetAsync("/");

            TaskCompletionSourceLongRunningJobFinishedMessage = new TaskCompletionSource<LongRunningJobFinishedMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            using var httpclient = CreteApiServiceHttpClient();
            var hubUrl = $"{httpclient.BaseAddress!.AbsoluteUri.TrimEnd('/')}/hubs/jobs";

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            connection.On<LongRunningJobFinishedMessage>("JobCompleted", message =>
            {
                TaskCompletionSourceLongRunningJobFinishedMessage.TrySetResult(message);
            });
            await connection.StartAsync();
        }

        [Given("I upload a video containing sensitive data")]
        public async Task GivenIUploadAVideoContainingSensitiveData()
        {
            var currentDirectory = Environment.CurrentDirectory;
            var vodeoPath = Path.Combine(currentDirectory, "Samples/grok-video-a33c5aac-1cb9-4b9b-bcfb-8dcb1dfcd15e.mp4");
            using var httpclient = CreteApiServiceHttpClient();
            var response = await httpclient.PostAsJsonAsync("/analyze", "vodeoPath");
            PostVideoResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        }

        [When("the video has been analyzed")]
        public async Task WhenTheVideoHasBeenAnalyzed()
        {
            VideoFinishedMessage = await TaskCompletionSourceLongRunningJobFinishedMessage.Task.WaitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);
        }

        [Then("I get a notification")]
        public void ThenIGetANotification()
        {
            VideoFinishedMessage.Should().NotBeNull();
            VideoFinishedMessage.Status.Should().Be("Completed");
        }

        [Then("the response contains a list of detected objects per frame")]
        public async Task ThenTheResponseContainsAListOfDetectedObjectsPerFrame()
        {
            PostVideoResponse.Should().NotBeNull();
            using var httpClient = CreteApiServiceHttpClient();
            AnalyzedVideoResponse = await httpClient.GetFromJsonAsync<ApiResponse<VideoAnalysis>>($"analyzed/{PostVideoResponse.Payload}");
        }

        [Given("the video has been analyzed")]
        public async Task GivenTheVideoHasBeenAnalyzed()
        {
            await WhenTheVideoHasBeenAnalyzed();
        }

        [When("I upload my selection of objects to blur")]
        public async Task WhenIUploadMySelectionOfObjectsToBlur()
        {
            var selectedObjects = new List<VideoAnalysis>();
            using var httpClient = CreteApiServiceHttpClient();
            var response = await httpClient.PostAsJsonAsync("anonymize", selectedObjects);
            AnonymizeVideoResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            VideoFinishedMessage = await TaskCompletionSourceLongRunningJobFinishedMessage.Task.WaitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);
        }

        [Then("I get video with blurred sensitive data")]
        public async Task ThenIGetVideoWithBlurredSensitiveData()
        {
            using var httpClient = CreteApiServiceHttpClient();
            var response = await httpClient.GetAsync($"anonymized/{VideoFinishedMessage.JobId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

    }
}
