using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
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

            App = await appHost.BuildAsync();
            await App.StartAsync();
        }

        [Given("I upload a video containing sensitive data")]
        public async Task GivenIUploadAVideoContainingSensitiveData()
        {
            var currentDirectory = Environment.CurrentDirectory;
            var vodeoPath = Path.Combine(currentDirectory, "Samples/grok-video-a33c5aac-1cb9-4b9b-bcfb-8dcb1dfcd15e.mp4");
            var content = new StringContent(vodeoPath, Encoding.UTF8, MediaTypeNames.Text.Plain);
            using var httpclient = CreteApiServiceHttpClient();
            await httpclient.PostAsync("/analyze", content);
        }

        [When("the video has been analyzed")]
        public async Task WhenTheVideoHasBeenAnalyzed()
        {
            throw new PendingStepException();
        }

        [Then("I get a notification")]
        public void ThenIGetANotification()
        {
            throw new PendingStepException();
        }

        [Then("the response contains a list of detected objects per frame")]
        public void ThenTheResponseContainsAListOfDetectedObjectsPerFrame()
        {
            throw new PendingStepException();
        }

        [Given("the video has been analyzed")]
        public async Task GivenTheVideoHasBeenAnalyzed()
        {
            await WhenTheVideoHasBeenAnalyzed();
        }

        [When("I upload my selection of objects to blur")]
        public void WhenIUploadMySelectionOfObjectsToBlur()
        {
            throw new PendingStepException();
        }

        [Then("I get video with blurred sensitive data")]
        public void ThenIGetVideoWithBlurredSensitiveData()
        {
            throw new PendingStepException();
        }

    }
}
