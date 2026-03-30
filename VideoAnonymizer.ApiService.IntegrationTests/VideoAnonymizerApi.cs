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
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Contracts;
using VideoAnonymizer.Web.Contracts.DTO;

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

        private ApiResponse<List<AnalyzedFrameDto>>? AnalyzedVideoResponse
        {
            get => (ApiResponse<List<AnalyzedFrameDto>>?)_scenarioContext[nameof(AnalyzedVideoResponse)];
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
            client.Timeout = TimeSpan.FromMinutes(1);
            await client.GetAsync("/");

            TaskCompletionSourceLongRunningJobFinishedMessage = new TaskCompletionSource<LongRunningJobFinishedMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            using var httpclient = CreteApiServiceHttpClient();
            var hubUrl = $"{httpclient.BaseAddress!.AbsoluteUri.TrimEnd('/')}/hubs/jobs";

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            connection.On<LongRunningJobFinishedMessage>("videoAnalyzed", message =>
            {
                TaskCompletionSourceLongRunningJobFinishedMessage.TrySetResult(message);
            });
            connection.On<LongRunningJobFinishedMessage>("videoAnonymized", message =>
            {
                TaskCompletionSourceLongRunningJobFinishedMessage.TrySetResult(message);
            });
            await connection.StartAsync();
        }

        [Given("I upload a video containing sensitive data")]
        public async Task GivenIUploadAVideoContainingSensitiveData()
        {
            var currentDirectory = Environment.CurrentDirectory;
            var videoPath = Path.Combine(currentDirectory, "Samples/grok-video-a33c5aac-1cb9-4b9b-bcfb-8dcb1dfcd15e.mp4");
            using var httpClient = CreteApiServiceHttpClient();

            using var form = new MultipartFormDataContent();
            await using var fileStream = File.OpenRead(videoPath);
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            form.Add(fileContent, "video", Path.GetFileName(videoPath));

            var response = await httpClient.PostAsync("/analyze", form);
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
            VideoFinishedMessage.Status.Should().Be("completed");
        }

        [Then("the response contains a list of detected objects per frame")]
        public async Task ThenTheResponseContainsAListOfDetectedObjectsPerFrame()
        {
            PostVideoResponse.Should().NotBeNull();
            using var httpClient = CreteApiServiceHttpClient();
            AnalyzedVideoResponse = await httpClient.GetFromJsonAsync<ApiResponse<List<AnalyzedFrameDto>>>($"analyzed/{PostVideoResponse.Payload}");
            AnalyzedVideoResponse.Should().NotBeNull();
            AnalyzedVideoResponse.IsSuccess.Should().BeTrue();
            AnalyzedVideoResponse.Payload.Should().NotBeNull();
            AnalyzedVideoResponse.Payload.Should().NotBeNull();
            AnalyzedVideoResponse.Payload.Count.Should().BeGreaterThan(0);
            AnalyzedVideoResponse.Payload.SelectMany(x => x.DetectedObjects).Count().Should().BeGreaterThan(0);
        }

        [Given("the video has been analyzed")]
        public async Task GivenTheVideoHasBeenAnalyzed()
        {
            await WhenTheVideoHasBeenAnalyzed();
            AnalyzedVideoResponse = await CreteApiServiceHttpClient().GetFromJsonAsync<ApiResponse<List<AnalyzedFrameDto>>>($"analyzed/{PostVideoResponse.Payload}");
        }

        [When("I upload my selection of objects to blur")]
        public async Task WhenIUploadMySelectionOfObjectsToBlur()
        {
            TaskCompletionSourceLongRunningJobFinishedMessage = new TaskCompletionSource<LongRunningJobFinishedMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var selectedObjects = new List<AnalyzedFrame>();
            using var httpClient = CreteApiServiceHttpClient();
            var firstFrame = AnalyzedVideoResponse.Payload.First();
            var response = await httpClient.PostAsJsonAsync($"anonymize/{firstFrame.VideoId}", AnalyzedVideoResponse.Payload);
            AnonymizeVideoResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        }

        [Then("I get video with blurred sensitive data")]
        public async Task ThenIGetVideoWithBlurredSensitiveData()
        {
            AnonymizeVideoResponse.Should().NotBeNull();
            VideoFinishedMessage = await TaskCompletionSourceLongRunningJobFinishedMessage.Task.WaitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);
            using var httpClient = CreteApiServiceHttpClient();
            var response = await httpClient.GetAsync($"anonymized/{AnalyzedVideoResponse.Payload.First().VideoId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.Should().NotBeNull();
            response.Content.Headers.ContentType!.MediaType
                .Should().StartWith("video/");
            var bytes = await response.Content.ReadAsByteArrayAsync();

            bytes.Should().NotBeNullOrEmpty();
            bytes.Length.Should().BeGreaterThan(1000);
        }

    }
}
