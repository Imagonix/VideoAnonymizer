using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using VideoAnonymizer.Web.Contracts;
using VideoAnonymizer.Web.Pages;

namespace VideoAnonymizer.Web.Tests.Pages
{
    [Binding]
    public class HomeStepDefinitions : BlazorTestBase<Home>
    {
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

        public HomeStepDefinitions(ScenarioContext scenarioContext)
            : base(scenarioContext)
        {
        }

        [Given("I open the homepage")]
        [When("I open the homepage")]
        public void WhenIOpenTheHomepage()
        {
            // redered in [BeforeScenario]
        }

        [Then("I see a upload video form")]
        public void ThenISeeAUploadVideoForm()
        {
            ComponentUnderTest.Find("input[type='file']").Should().NotBeNull("There should be a file uplod field!");

            var uploadButton = ComponentUnderTest.Find("button");
            uploadButton.TextContent.Should().Contain("Upload", "No upload button!", StringComparison.OrdinalIgnoreCase);
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

        [When("I press download")]
        public async Task WhenIPressDownload()
        {
            await ComponentUnderTest.WaitForAssertionAsync(() =>
            {
                ComponentUnderTest.Instance.IsAnonymized.Should().BeTrue();
            }, TimeSpan.FromSeconds(5));
            var downloadButton = ComponentUnderTest.Find("button:contains('Download')");
            downloadButton.Click();
        }

        [Then("I get an anonymized video back")]
        public void ThenIGetAnAnonymizedVideoBack()
        {
            var callCount = MockHttpMessageHandler.GetMatchCount(
                MockHttpMessageHandler.When(HttpMethod.Get, $"/anonymized/{CurrentAnonimizationJobId}")
                );
            callCount.Should().Be(1,
                $"The endpoint /anonymized/{CurrentAnonimizationJobId} should have been called exactly once.");
            MockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }


        protected override void SetupMockClient()
        {
            CurrentVideoId = Guid.NewGuid();
            CurrentAnonimizationJobId = Guid.NewGuid();

            MockHttpMessageHandler
                .When(HttpMethod.Post, "/analyze*")
                .Respond(req =>
                {
                    _ = Task.Delay(50).ContinueWith(async _ =>
                    {
                        // send message shortly after return to simulate the video analyzing finished
                        var message = new LongRunningJobFinishedMessage
                        {
                            JobId = CurrentVideoId ?? Guid.NewGuid(),
                            Status = "Completed",
                        };
                        await HubConnection.SendAsync("videoAnalyzed", message);
                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"{{\"videoId\": \"{CurrentVideoId}\" }}")
                    };
                });

            MockHttpMessageHandler
                .When(HttpMethod.Get, $"/analyzed/{CurrentVideoId}")
                .Respond("application/json", "{}");

            MockHttpMessageHandler
                .When(HttpMethod.Post, $"/anonymize/{CurrentVideoId}")
                .Respond(req =>
                {
                    _ = Task.Delay(50).ContinueWith(async _ =>
                    {
                        // send message shortly after return to simulate the video anonimization finished
                        var message = new LongRunningJobFinishedMessage
                        {
                            JobId = CurrentVideoId ?? Guid.NewGuid(),
                            Status = "Completed",
                        };
                        await HubConnection.SendAsync("videoAnnonymzed", message);
                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"{{\"videoId\": \"{CurrentAnonimizationJobId}\" }}")
                    };
                });

            MockHttpMessageHandler
                .When(HttpMethod.Get, $"/anonymized/{CurrentAnonimizationJobId}")
                .Respond("application/json", "{}");
        }

        [AfterScenario]
        public void Cleanup()
        {
            MockHttpMessageHandler.Clear();
        }
    }
}