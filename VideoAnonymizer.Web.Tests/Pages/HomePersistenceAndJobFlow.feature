@home_persistence
Feature: Reopening persisted videos from the home page
  As a reviewer returning to local work
  I want saved videos and job completions to open the right review state
  So that I only continue the video I actually selected or exported.

  Scenario: Opening an existing video resumes review with persisted frames and settings
    Given the saved video list contains "persisted-video.mp4" with blur size 180 percent and time buffer 650 ms
    And the saved video has one analyzed frame with one face
    When the reviewer opens the saved video from the library
    Then the review tab opens with the persisted frame, face, blur size 180 percent and time buffer 650 ms
    And the upload form does not mark the saved video as a freshly selected file

  Scenario: Opening an existing video uses the current interpolation setting
    Given the saved video list contains "persisted-video.mp4" with blur size 180 percent and time buffer 650 ms
    And the saved video has one analyzed frame with one face
    And app state disables object interpolation
    When the reviewer opens the saved video from the library
    Then the review tab opens with interpolation disabled

  Scenario: Unrelated analysis completion messages are ignored
    Given the reviewer starts object detection for "new-video.mp4"
    When an unrelated analysis completion message arrives
    Then the reviewer is not moved to review

  Scenario: Current analysis completion opens review
    Given the reviewer starts object detection for "new-video.mp4"
    When the current analysis completion message arrives
    Then the reviewer is moved to review

  Scenario: Unrelated export completion messages are ignored
    Given the reviewer is reviewing analyzed results for "new-video.mp4"
    When the reviewer starts anonymization and an unrelated export completion message arrives
    Then no anonymized video is downloaded

  Scenario: Current export completion downloads the anonymized video
    Given the reviewer has started anonymization for analyzed results in "new-video.mp4"
    When the current export completion message arrives
    Then the anonymized video is downloaded for the current video
