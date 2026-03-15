namespace VideoAnonymizer.ApiService.IntegrationTests
{
    [Binding]
    public sealed class VideoAnonymizerApi
    {
        [Given("I upload a video containing sensitive data")]
        public void GivenIUploadAVideoContainingSensitiveData()
        {
            string vodeoPath = "TODO";
            throw new PendingStepException();
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
