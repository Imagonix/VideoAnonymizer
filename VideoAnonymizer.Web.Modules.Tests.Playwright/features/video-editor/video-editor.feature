Feature: Video editor for analyzed videos

  As a user
  I want to edit the result of an analyzed video
  So that I can inspect detected objects in the video preview and on the timeline

Scenario: Editor shows video preview, current-frame object list, and timeline
	Given the editor is opened for an analyzed video
	Then I should see a video preview
	And I should see a list of detected objects for the current frame next to the video preview
	And every detected object in the current frame should have a checkbox
	And I should see a timeline below the video preview
	And I should see the detected objects listed on the left side of the timeline
	And each object on the timeline should have a color
	And each object on the timeline should have a checkbox

Scenario: Timeline highlights the times where an object is detected
	Given the editor is opened for an analyzed video
	When an object is detected in one or more analyzed frames
	Then the timeline should highlight that object in its assigned color at the corresponding times

Scenario: Neighboring detections are rendered as one continuous colored line
	Given the editor is opened for an analyzed video
	And an object is detected in neighboring analyzed frames
	When the timeline is rendered
	Then the object should be shown as one continuous colored line across the corresponding time range


Scenario: Bounding box border color matches the timeline object color
	Given the editor is opened for an analyzed video
	When an object is shown as a bounding box in the video preview
	Then the border of the bounding box should use the same color as the object on the timeline

Scenario: Current-frame object can be disabled from the object list
	Given the editor is opened for an analyzed video
	And the current frame contains a detected object
	When I uncheck that object in the current-frame object list
	Then the object should no longer be active in this frame
	And the object should not be displayed in the overlay on this frame
	And the point for the object in this frame should turn gray in the timeline
	And the other points for the object on the timeline should not be effected
    
Scenario: Current-frame object can be enabled from the object list
	Given the editor is opened for an analyzed video
	And the current frame contains a detected object
	And one object is disabled in the object list
	When I check that object in the current-frame object list
	Then the object should be active in this frame
	And the object should be displayed in the overlay on this frame
	And the point for the object in this frame should turn to its color in the timeline
	And the other points for the object on the timeline should not be effected

Scenario: Timeline object can be disabled from the timeline list
	Given the editor is opened for an analyzed video
	And the timeline contains a detected object
	When I uncheck that object in the timeline list
	Then the object should no longer be selected in any frame
	And the line or points in the timeline for the object should turn gray

Scenario: Timeline object can be enabled from the timeline list
	Given the editor is opened for an analyzed video
	And the timeline contains a detected object
	And one object has been disabled on the timeline
	When I check that object in the timeline list
	Then the object should be selected in all frames
	And the line or points in the timeline for the object should turn to its color

Scenario: Playback indicator moves with the current video time
	Given the editor is opened for an analyzed video
	When the video is playing
	Then a vertical playback indicator should move on the timeline with the current video time
	And the current timestamp should be shown at the indicator

Scenario: Clicking on the timeline updates the video current time
	Given the editor is opened for an analyzed video
	And the video is paused
	When I click on a position on the timeline at 3 seconds
	Then the video current time should be set to 3 seconds
	And the video frame displayed should correspond to 3 seconds
	And the playback indicator should move to the clicked position on the timeline

#  Scenario: Timeline can zoom in by 25 percent
#    Given the editor is opened for an analyzed video
#    And the timeline shows a current visible time range
#    When I zoom in
#    Then the visible timeline range should decrease by 25 percent
#
#  Scenario: Timeline can zoom out by 25 percent
#    Given the editor is opened for an analyzed video
#    And the timeline shows a current visible time range
#    When I zoom out
#    Then the visible timeline range should increase by 25 percent
#

#
#  Scenario: Timeline stays fixed until the playback indicator reaches the middle
#    Given the editor is opened for an analyzed video
#    And the video is playing
#    When the playback indicator is left of the timeline center
#    Then the timeline should stay fixed
#    When the playback indicator reaches the timeline center
#    Then the current time should be shown at the center of the timeline
#
#  Scenario: Timeline scrolls after the playback indicator reaches the middle
#    Given the editor is opened for an analyzed video
#    And the video is playing
#    And the playback indicator has reached the timeline center
#    When the current video time increases further
#    Then the timeline should scroll
#    And the playback indicator should remain at the center
#    And this should continue until the end area of the timeline is reached
#
#  Scenario: Playback indicator moves to the right near the end of the video
#    Given the editor is opened for an analyzed video
#    And the video is playing near the end of the video
#    When the end area of the timeline is reached
#    Then the timeline should stop scrolling
#    And the playback indicator should move to the right together with the current video time
#    And the playback indicator should stop at the end of the timeline
#
#  Scenario: At 5.0 seconds the correct current-frame object is shown from the fixture data
#    Given the editor is opened with the provided analyzed fixture data
#    When the current video time is set to 5.0 seconds
#    Then the current-frame object list should contain exactly 1 detected object
#    And that detected object should be "track:1"
#
#  Scenario: At 5.0 seconds the correct bounding box is shown from the fixture data
#    Given the editor is opened with the provided analyzed fixture data
#    When the current video time is set to 5.0 seconds
#    Then a bounding box for "track:1" should be visible in the video preview
#    And the bounding box should correspond to x 176
#    And the bounding box should correspond to y 91
#    And the bounding box should correspond to width 45
#    And the bounding box should correspond to height 44
#
#  Scenario: At 5.0 seconds the timeline and bounding box colors stay consistent
#    Given the editor is opened with the provided analyzed fixture data
#    When the current video time is set to 5.0 seconds
#    Then the timeline entry for "track:1" should have an assigned color
#    And the bounding box border for "track:1" should use the same color