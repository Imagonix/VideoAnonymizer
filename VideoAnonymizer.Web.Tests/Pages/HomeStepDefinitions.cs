using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using VideoAnonymizer.Web.Pages;
using VideoAnonymizer.Web.Services;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;
using VideoAnonymizer.Web.Tests.FakeServices;
using VideoAnonymizer.Web.Tests.TestDoubles;

namespace VideoAnonymizer.Web.Tests.Pages
{
    [Binding]
    public class HomeStepDefinitions : BlazorTestBase<Home>
    {
        private Guid _currentVideoIdDefaultValue = Guid.NewGuid();
        private Guid _currentAnonimizationJobIdDefaultValue = Guid.NewGuid();

        private Guid? CurrentVideoId
        {
            get => _scenarioContext.Get<Guid>(nameof(CurrentVideoId));
            set => _scenarioContext.Set(value, nameof(CurrentVideoId));
        }

        private Guid? CurrentAnonimizationJobId
        {
            get => _scenarioContext.Get<Guid>(nameof(CurrentAnonimizationJobId));
            set => _scenarioContext.Set(value, nameof(CurrentAnonimizationJobId));
        }

        private MockHttpMessageHandler MockHttpMessageHandler
        {
            get => _scenarioContext.Get<MockHttpMessageHandler>(nameof(MockHttpMessageHandler));
            set => _scenarioContext.Set(value, nameof(MockHttpMessageHandler));
        }

        protected FakeJobHubClient JobHubClient
        {
            get => _scenarioContext.Get<FakeJobHubClient>(nameof(JobHubClient));
            set => _scenarioContext.Set(value, nameof(JobHubClient));
        }

        protected FakeDownloadService DownloadService
        {
            get => _scenarioContext.Get<FakeDownloadService>(nameof(DownloadService));
            set => _scenarioContext.Set(value, nameof(DownloadService));
        }

        public HomeStepDefinitions(ScenarioContext scenarioContext)
            : base(scenarioContext)
        {
        }

        [Given("I open the homepage")]
        [When("I open the homepage")]
        public void WhenIOpenTheHomepage()
        {
            // rendered in [BeforeScenario]
        }

        [Then("I see a upload video form")]
        public void ThenISeeAUploadVideoForm()
        {
            ComponentUnderTest.Find("input[type='file']")
                .Should().NotBeNull("There should be a file upload field!");

            var mudButtons = ComponentUnderTest.FindAll("label");
            var uploadButtons = mudButtons.Where(x => x.InnerHtml.Contains("Upload"));
            uploadButtons.Count().Should().Be(1, "There should be exactly one upload button");
        }

        [Given("I uploaded a video")]
        public async Task WhenIUploadAVideo()
        {
            var mudFileUpload = ComponentUnderTest.FindComponent<MudFileUpload<IBrowserFile>>();

            var dummyVideo = InputFileContent.CreateFromBinary(
                new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 },
                "test-video.mp4",
                contentType: "video/mp4");

            mudFileUpload.FindComponent<InputFile>()
                .UploadFiles([dummyVideo]);
        }

        [Given("I press anonymize")]
        public async Task GivenIPressAnonymize()
        {
            await ComponentUnderTest.WaitForAssertionAsync(() =>
            {
                var anonymizeButton = ComponentUnderTest
                    .FindComponents<MudButton>()
                    .Single(x => x.Markup.Contains("Anonymize", StringComparison.OrdinalIgnoreCase));

                anonymizeButton.Instance.Disabled.Should().BeFalse(
                    "the anonymize button should be enabled after the video was analyzed");
            }, TimeSpan.FromSeconds(60));

            await ComponentUnderTest.InvokeAsync(async () =>
            {
                var anonymizeButton = ComponentUnderTest
                    .FindComponents<MudButton>()
                    .Single(x => x.Markup.Contains("Anonymize", StringComparison.OrdinalIgnoreCase));

                var buttonElement = anonymizeButton.Find("button");
                await buttonElement.ClickAsync();
            });
        }


        [When("I press download")]
        public async Task WhenIPressDownload()
        {
            await Task.Delay(60);
            await ComponentUnderTest.WaitForAssertionAsync(() =>
            {
                ComponentUnderTest.Instance.IsAnonymized.Should().BeTrue();
            }, TimeSpan.FromSeconds(60));
            //var downloadButton = ComponentUnderTest.Find("button:contains('Download')");
            //downloadButton.Click();
            await ComponentUnderTest.InvokeAsync(async () =>
            {
                var downloadButton = ComponentUnderTest
                .FindComponents<MudButton>()
                .Single(x => x.Markup.Contains("Download", StringComparison.OrdinalIgnoreCase));
                var buttonElement = downloadButton.Find("button");
                await buttonElement.ClickAsync();
            });
        }

        [Then("I get an anonymized video back")]
        public void ThenIGetAnAnonymizedVideoBack()
        {
            DownloadService.DownloadCallCount.Should().Be(1, "Download should have been triggered once");
            DownloadService.LastDownloadedUrl.Should().Contain(SharedConstants.Paths.Anonymized);
            DownloadService.LastDownloadedUrl.Should().Contain($"{CurrentVideoId}");
        }

        protected override void SetupServices()
        {
            DownloadService = new FakeDownloadService();
            JobHubClient = new FakeJobHubClient();
            SetupMockClient();
            Services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(MockHttpMessageHandler));
            Services.AddSingleton<IJobHubClient>(JobHubClient);
            Services.AddSingleton<IDownloadService>(DownloadService);
            SetupMockClient();
        }

        protected void SetupMockClient()
        {
            MockHttpMessageHandler = new MockHttpMessageHandler();
            MockHttpMessageHandler
                .When(HttpMethod.Post, $"/{SharedConstants.Paths.Analyze}*")
                .Respond(req =>
                {
                    CurrentVideoId = _currentVideoIdDefaultValue;
                    _ = Task.Delay(150).ContinueWith(async _ =>
                    {
                        // send message shortly after return to simulate the video analyzing finished
                        var message = new LongRunningJobFinishedMessage
                        {
                            JobId = CurrentVideoId ?? Guid.NewGuid(),
                            Status = "Completed",
                        };
                        await JobHubClient.RaiseVideoAnalyzedAsync(message);
                    });

                    string response = System.Text.Json.JsonSerializer.Serialize(new ApiResponse<Guid>() { Payload = CurrentVideoId.Value, IsSuccess = true });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(response)
                    };
                });

            MockHttpMessageHandler
                .When(HttpMethod.Get, $"/{SharedConstants.Paths.Analyzed}/{_currentVideoIdDefaultValue}")
                .Respond("application/json", "{}");

            MockHttpMessageHandler
                .When(HttpMethod.Post, $"/{SharedConstants.Paths.Anonymize}/{_currentVideoIdDefaultValue}")
                .Respond(req =>
                {
                    CurrentAnonimizationJobId = _currentAnonimizationJobIdDefaultValue;
                    _ = Task.Delay(150).ContinueWith(async _ =>
                    {
                        // send message shortly after return to simulate the video anonimization finished
                        var message = new LongRunningJobFinishedMessage
                        {
                            JobId = CurrentAnonimizationJobId ?? Guid.NewGuid(),
                            Status = "Completed",
                        };

                        await JobHubClient.RaiseVideoAnonymizedAsync(message);
                    });

                    string response = System.Text.Json.JsonSerializer.Serialize(new ApiResponse<Guid>() { Payload = CurrentAnonimizationJobId.Value, IsSuccess = true });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(response)
                    };
                });

            MockHttpMessageHandler
                .When(HttpMethod.Get, $"/{SharedConstants.Paths.Anonymized}/{_currentVideoIdDefaultValue}")
                .Respond("application/json", "{}");
        }

        [AfterScenario]
        public void Cleanup()
        {
            MockHttpMessageHandler.Clear();
        }

        public new void Dispose()
        {
            MockHttpMessageHandler.Dispose();
            base.Dispose();
        }
    }
}