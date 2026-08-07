Feature: Expanded timeline label and occurrence row alignment
  As a reviewer navigating tracks in the expanded timeline
  I want each label row to line up vertically with its occurrence row
  So that checkbox, preview, Track ID, and dots clearly belong to the same track.

  Scenario: The video editor mounts and expands without errors
    Given the editor is open with multiple tracks in the expanded timeline
    Then the video editor shell is visible
    And the expanded timeline shows matching label and occurrence rows

  Scenario: Each label row matches its occurrence row top and height
    Given the editor is open with multiple tracks in the expanded timeline
    Then each timeline label row shares the same top and height as its occurrence row

  Scenario: Checkbox preview and Track ID are vertically centered in the label row
    Given the editor is open with multiple tracks in the expanded timeline
    Then each label row vertically centers its checkbox preview and Track ID
