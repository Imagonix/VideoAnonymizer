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
        private Guid? _currentVideoId;

        public HomeStepDefinitions(ScenarioContext scenarioContext)
            : base(scenarioContext)
        {
        }

        [Given("I open the homepage")]
        public void GivenIOpenTheHomepage()
        {
            SetupAnalyzeMock();
        }

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

        [When("I upload a video")]
        public async Task WhenIUploadAVideo()
        {
            var mudFileUpload = ComponentUnderTest.FindComponent<MudFileUpload<IBrowserFile>>();

            var dummyVideo = InputFileContent.CreateFromBinary(
                new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 },
                "test-video.mp4",
                contentType: "video/mp4");

            mudFileUpload.FindComponent<InputFile>()
                       .UploadFiles([dummyVideo]);

            await Task.Delay(20); 
            var message = new LongRunningJobFinishedMessage
            {
                JobId = _currentVideoId ?? Guid.NewGuid(),
                Status = "Completed",
            };
            HubConnection.SendAsync("videoAnalyzed", message);
        }

        [Then("I get an anonymized video back")]
        public void ThenIGetAnAnonymizedVideoBack()
        {
            // TODO download should start

            MockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }

        private void SetupAnalyzeMock()
        {
            _currentVideoId = Guid.NewGuid();

            MockHttpMessageHandler
                .When(HttpMethod.Post, "/analyze*")
                .Respond(req =>
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"{{\"videoId\": \"{_currentVideoId}\" }}")
                    };
                });

            MockHttpMessageHandler
                .When(HttpMethod.Get, $"/analyzed/{_currentVideoId}")
                .Respond("application/json", "{}");
        }

        [AfterScenario]
        public void Cleanup()
        {
            MockHttpMessageHandler.Clear();
        }
    }
}