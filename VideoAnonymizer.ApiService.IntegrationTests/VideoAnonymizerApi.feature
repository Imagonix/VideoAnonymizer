Feature: VideoAnonymizerApi

Scenario: Upload Video
	Given I upload a video containing sensitive data
	When the video has been analyzed
	Then I get a notification
	And the response contains a list of detected objects per frame

Scenario: Create Video
	Given I upload a video containing sensitive data
	And the video has been analyzed
	When I upload my selection of objects to blur
	Then I get video with blurred sensitive data