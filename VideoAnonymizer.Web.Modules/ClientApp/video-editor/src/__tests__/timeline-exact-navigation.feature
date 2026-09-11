Feature: Exact occurrence navigation and compact collapsed timeline
  As a reviewer
  I want a single fixed-height collapsed timeline row and exact Previous/Next occurrence navigation
  So that selecting a track never reflows the timeline and navigation lands on exact stored occurrences.

  Scenario: The collapsed timeline stays one fixed-height row with a selected track
    Given the editor is open in the video-first workspace
    Then the collapsed timeline shows one compact row without track metadata
    When the reviewer selects track 1
    Then the track metadata sits inline in the same single row
    And the collapsed timeline keeps its fixed height

  Scenario: Compact selected-track metadata fits in the single row
    Given the editor is open in the video-first workspace
    When the reviewer selects track 1
    Then the collapsed track strip shows a small thumbnail, inclusion checkbox, color marker, and an ellipsized non-wrapping label

  Scenario: Ruler and playback indicator persist with a selected track
    Given the editor is open with a timed video
    When the reviewer selects track 1
    Then the collapsed seek surface shows multiple time labels
    And the vertical playback indicator remains visible in the collapsed seek surface
    And the collapsed track metadata is inline beside the seek surface

  Scenario: Arbitrary-position seeking works across the selected-track strip
    Given the editor is open with a timed video
    When the reviewer selects track 1
    And the reviewer clicks the collapsed seek surface at 60 percent
    Then the video seeks near 6 seconds
    And the selection remains track 1

  Scenario: The track label click does not seek
    Given the editor is open with a timed video
    When the reviewer seeks to 2.5 seconds
    And the reviewer selects track 1
    And the reviewer clicks the collapsed track label
    Then the video remains at 2.5 seconds

  Scenario: Next occurrence navigates to the adjacent stored occurrence
    Given the editor is open with consecutive track occurrences at 0, 1, and 2 seconds
    When the reviewer selects the first occurrence of track 1
    And the reviewer navigates to the next occurrence
    Then the video time matches the adjacent stored occurrence at 1 second
    And the selection remains track 1
    When the reviewer navigates to the next occurrence
    Then the video time matches the adjacent stored occurrence at 2 seconds

  Scenario: Previous occurrence navigates back exactly
    Given the editor is open with consecutive track occurrences at 0, 1, and 2 seconds
    When the reviewer navigates to the last occurrence of track 1
    And the reviewer navigates to the previous occurrence
    Then the video time matches the adjacent stored occurrence at 1 second

  Scenario: One navigation spans a gap over a missing analyzed frame
    Given the editor is open with track occurrences at 0 and 2 seconds and a missing frame between
    When the reviewer selects the first occurrence of track 1
    And the reviewer navigates to the next occurrence
    Then the video time matches the adjacent stored occurrence at 2 seconds

  Scenario: Navigation works between close stored timestamps
    Given the editor is open with track occurrences at 0 and 0.04 seconds
    When the reviewer selects the first occurrence of track 1
    And the reviewer navigates to the next occurrence
    Then the video time matches the adjacent stored occurrence at 0.04 seconds

  Scenario: Previous and Next are disabled at the track ends
    Given the editor is open with consecutive track occurrences at 0, 1, and 2 seconds
    When the reviewer selects the first occurrence of track 1
    Then Previous is disabled and Next is enabled
    When the reviewer navigates to the last occurrence of track 1
    Then Previous is enabled and Next is disabled

  Scenario: Navigating keeps selection and inspector context synchronized
    Given the editor is open with consecutive track occurrences at 0, 1, and 2 seconds
    When the reviewer selects the first occurrence of track 1
    And the reviewer navigates to the next occurrence
    Then the selection remains track 1
    And the inspector occurrence context matches the stored occurrence at 1 second
