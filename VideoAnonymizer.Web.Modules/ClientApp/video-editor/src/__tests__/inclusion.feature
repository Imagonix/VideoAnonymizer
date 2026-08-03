Feature: Occurrence and track inclusion semantics
  As a reviewer deciding what to anonymize
  I want the inspector checkbox to control only the selected occurrence
  And the timeline checkbox to control the whole track
  So that partial inclusion is possible while excluded detections stay visible and selectable.

  Scenario: Inspector exclude updates only the selected occurrence
    Given the editor is open with a multi-occurrence track
    When the reviewer selects occurrence "o1" and excludes it from the inspector
    Then only occurrence "o1" is excluded
    And occurrences "o2" and "o3" remain included
    And Vue sends a single "toggle" update for "o1" with selected false
    And the track selection and inspector remain open with the occurrence unchecked

  Scenario: Timeline track checkbox bulk-excludes every occurrence
    Given the editor is open with a multi-occurrence track
    When the reviewer bulk-excludes track 1 from the timeline
    Then every occurrence of track 1 is excluded
    And Vue sends a "toggle" bulk update for "o1,o2,o3" with selected false

  Scenario: Timeline track checkbox bulk-includes every occurrence
    Given the editor is open with a mixed-inclusion track
    When the reviewer bulk-includes track 1 from the timeline
    Then every occurrence of track 1 is included
    And Vue sends a "toggle" bulk update for "o1,o2,o3" with selected true

  Scenario: Mixed track shows an indeterminate timeline checkbox
    Given the editor is open with a multi-occurrence track
    When the reviewer selects occurrence "o2" and excludes it from the inspector
    Then the collapsed track inclusion control is indeterminate
    And the timeline checkbox for track 1 is indeterminate

  Scenario: Clicking an indeterminate track checkbox includes the whole track
    Given the editor is open with a mixed-inclusion track
    When the reviewer clicks the indeterminate timeline checkbox for track 1
    Then every occurrence of track 1 is included

  Scenario: Excluded occurrence renders as a selectable ghost without blur fill
    Given the editor is open with a multi-occurrence track
    When the reviewer selects occurrence "o1" and excludes it from the inspector
    Then occurrence "o1" is rendered as an excluded ghost box
    And occurrence "o1" has no blur fill outline
    And the excluded ghost box remains pointer-selectable

  Scenario: Keyboard reselection of an excluded ghost reopens the inspector
    Given the editor is open with occurrence "o1" already excluded
    When the reviewer keyboard-selects the excluded ghost box for "o1"
    Then the track selection is keyed to track 1
    And the object details panel is open with occurrence "o1" unchecked

  Scenario: Re-including an excluded occurrence from the inspector
    Given the editor is open with occurrence "o1" already excluded
    When the reviewer selects occurrence "o1" and includes it from the inspector
    Then occurrence "o1" is included
    And Vue sends a single "toggle" update for "o1" with selected true
    And occurrence "o1" is no longer rendered as an excluded ghost

  Scenario: Occurrence inclusion change keeps undoable before-state
    Given the editor is open with a multi-occurrence track
    When the reviewer selects occurrence "o1" and excludes it from the inspector
    Then the single toggle before-state has "o1" selected true
    When Blazor pushes undo restoring occurrence "o1" to selected true
    Then occurrence "o1" is included
    And the other track occurrences remain included

  Scenario: Track inclusion change keeps undoable before-state
    Given the editor is open with a multi-occurrence track
    When the reviewer bulk-excludes track 1 from the timeline
    Then the bulk toggle before-state has "o1,o2,o3" selected true
    When Blazor pushes undo restoring "o1,o2,o3" to selected true
    Then every occurrence of track 1 is included
