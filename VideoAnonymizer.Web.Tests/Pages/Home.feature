Feature: Home

Scenario: Open
	When I open the homepage
	Then I see a upload video form

Scenario: AnonomyzeVideo
	Given I open the homepage
	When I upload a video
	Then I get an anonomyzed video back