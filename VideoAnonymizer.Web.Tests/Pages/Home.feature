Feature: Home

Scenario: Open
	When I open the homepage
	Then I see a upload video form

Scenario: AnonomyzeVideo
	Given I open the homepage
	And I uploaded a video
	And I press anonymize
	When the video has been anonymized
	Then I get an anonymized video back