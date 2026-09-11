Feature: Collapsible selected-track-aware timeline
  As a reviewer
  I want a collapsed timeline by default that expands into a full track navigator
  So that the video stays dominant while track context remains available.

  Scenario: Timeline starts collapsed
    Given the editor is open in the video-first workspace
    Then the timeline is collapsed
    And the caret is labeled Expand timeline with aria-expanded false

  Scenario: Caret expands and collapses the timeline
    Given the editor is open in the video-first workspace
    When the reviewer clicks the timeline caret
    Then the timeline is expanded
    And the caret is labeled Collapse timeline with aria-expanded true
    When the reviewer clicks the timeline caret
    Then the timeline is collapsed

  Scenario: Collapsed bar shows overview when nothing is selected
    Given the editor is open in the video-first workspace
    Then the collapsed overview is visible
    And the collapsed track strip is not visible

  Scenario: Collapsed bar shows selected track strip
    Given the editor is open in the video-first workspace
    When the reviewer selects track 1
    Then the collapsed track strip is visible
    And the collapsed overview is not visible
    And the collapsed track strip shows the track label and inclusion control

  Scenario: Occurrence dots seek while retaining selection
    Given the editor is open in the video-first workspace
    When the reviewer selects track 1
    And the reviewer clicks the second occurrence dot in the collapsed track strip
    Then the video seeks to the second occurrence time of track 1
    And the selection remains track 1

  Scenario: Expanding scrolls and highlights the selected track
    Given the editor is open in the video-first workspace
    When the reviewer selects track 2
    And the reviewer expands the timeline
    Then the expanded timeline is visible
    And the selected track row is highlighted for track 2

  Scenario: Collapsing preserves the selected track
    Given the editor is open in the video-first workspace
    When the reviewer selects track 1
    And the reviewer expands the timeline
    And the reviewer collapses the timeline
    Then the timeline is collapsed
    And the selection remains track 1
    And the collapsed track strip is visible

  Scenario: Expanded row selection opens the inspector
    Given the editor is open in the video-first workspace
    When the reviewer expands the timeline
    And the reviewer clicks the expanded label for track 2
    Then the selection remains track 2
    And the object details panel is open
