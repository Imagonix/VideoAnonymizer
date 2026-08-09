Feature: Persistent timeline playback and seeking affordances
  As a reviewer
  I want Play/Pause, time labels, and a playback indicator in both timeline states
  So that I can navigate the video without losing transport controls when selecting tracks.

  Scenario: Play/Pause sits left of the timeline when collapsed with no selection
    Given the editor is open with a timed video
    Then the timeline is collapsed
    And the Play/Pause control is in the fixed left timeline slot
    And the collapsed left slot does not show current-time text
    When the reviewer clicks Play/Pause
    Then video playback is toggled

  Scenario: Play/Pause remains available with a selected track while collapsed
    Given the editor is open with a timed video
    When the reviewer selects track 1
    Then the Play/Pause control is in the fixed left timeline slot
    And the collapsed track strip is visible
    When the reviewer clicks Play/Pause
    Then video playback is toggled

  Scenario: Play/Pause remains available when the timeline is expanded
    Given the editor is open with a timed video
    When the reviewer expands the timeline
    Then the Play/Pause control is in the fixed left timeline slot
    And the expanded timeline is visible
    When the reviewer clicks Play/Pause
    Then video playback is toggled

  Scenario: Collapsed timeline shows multiple time labels and a vertical indicator
    Given the editor is open with a timed video
    Then the collapsed seek surface shows multiple time labels
    And the vertical playback indicator is visible in the collapsed seek surface
    When the reviewer selects track 1
    Then the collapsed seek surface still shows multiple time labels
    And the vertical playback indicator remains visible in the collapsed seek surface

  Scenario: Expanded timeline shows multiple time labels and a vertical indicator
    Given the editor is open with a timed video
    When the reviewer expands the timeline
    Then the expanded timeline shows multiple time labels
    And the vertical playback indicator is visible in the expanded timeline
    When the reviewer selects track 1
    Then the vertical playback indicator remains visible in the expanded timeline

  Scenario: Arbitrary-position seeking works in the collapsed overview
    Given the editor is open with a timed video
    When the reviewer clicks the collapsed seek surface at 40 percent
    Then the video seeks near 4 seconds

  Scenario: Arbitrary-position seeking works on the collapsed selected-track strip
    Given the editor is open with a timed video
    When the reviewer selects track 1
    And the reviewer clicks the collapsed seek surface at 60 percent
    Then the video seeks near 6 seconds
    And the selection remains track 1

  Scenario: Exact occurrence dots seek and keep selection
    Given the editor is open with a timed video
    When the reviewer selects track 1
    And the reviewer clicks the second occurrence dot in the collapsed track strip
    Then the video seeks to 1 second
    And the selection remains track 1

  Scenario: Non-seek controls do not change playback time
    Given the editor is open with a timed video
    When the reviewer seeks to 2.5 seconds
    And the reviewer clicks the timeline caret
    Then the video remains at 2.5 seconds
    When the reviewer collapses the timeline
    And the reviewer selects track 1
    And the reviewer seeks to 2.5 seconds
    And the reviewer clicks the track inclusion checkbox
    Then the video remains at 2.5 seconds

  Scenario: Collapse and expand preserve selection, ruler, and indicator
    Given the editor is open with a timed video
    When the reviewer selects track 1
    And the reviewer expands the timeline
    Then the selection remains track 1
    And the vertical playback indicator is visible in the expanded timeline
    And the expanded timeline shows multiple time labels
    When the reviewer collapses the timeline
    Then the selection remains track 1
    And the vertical playback indicator remains visible in the collapsed seek surface
    And the collapsed seek surface still shows multiple time labels

  Scenario: Compact ruler row remains short but labeled
    Given the editor is open with a timed video
    Then the collapsed time-label row is compact
    When the reviewer expands the timeline
    Then the expanded time-label row is compact
