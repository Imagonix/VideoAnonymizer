Feature: Track settings and consecutive-segment boundary overrides
  As a reviewer fine-tuning anonymization
  I want track-wide shape and blur-size settings plus pre/post overrides for the segment containing the selected occurrence
  So that blur boundaries are applied to exactly the right occurrences.

  Scenario: Segment lookup spans adjacent analyzed frames only
    Given the editor is open with consecutive track segments
    When the reviewer selects the first occurrence of track 1
    Then the selected segment contains all three occurrences of track 1
    And a gap in analyzed frames splits track 2 into two one-object segments

  Scenario: Inherited segment values display the global buffer while storage stays null
    Given the editor is open with consecutive track segments
    When the reviewer selects an occurrence of a track without boundary overrides
    Then the pre value shows the global buffer and is marked Global
    And the post value shows the global buffer and is marked Global
    And the first and last occurrences keep null boundary overrides

  Scenario: Editing pre writes only the segment first occurrence
    Given the editor is open with consecutive track segments
    When the reviewer selects the last occurrence of track 1 and sets pre to 450
    Then only the first occurrence of track 1 stores a pre override of 450
    And Vue sends a "pre-buffer" update for the first occurrence with the previous null state

  Scenario: Editing post writes only the segment last occurrence
    Given the editor is open with consecutive track segments
    When the reviewer selects the first occurrence of track 1 and sets post to 500
    Then only the last occurrence of track 1 stores a post override of 500
    And Vue sends a "post-buffer" update for the last occurrence with the previous null state

  Scenario: A one-object segment stores pre and post on the same occurrence
    Given the editor is open with consecutive track segments
    When the reviewer selects an untracked occurrence and sets pre to 450 and post to 500
    Then the untracked occurrence stores both the pre override 450 and post override 500

  Scenario: Reset clears a boundary override
    Given the editor is open with a pre override on the first occurrence of track 1
    When the reviewer resets the pre override on the first occurrence
    Then the first occurrence stores a null pre override
    And Vue sends a "pre-buffer" update with the previous override state

  Scenario: Editing a boundary field to the global value normalizes storage to null
    Given the editor is open with consecutive track segments
    When the reviewer sets the segment pre to the global buffer value 300
    Then the first occurrence stores a null pre override

  Scenario: A later global change leaves an existing override intact
    Given the editor is open with a custom pre override on the first occurrence of track 1
    When the global time buffer changes from 300 to 500
    Then the custom pre override remains stored
    And the displayed pre value stays 450

  Scenario: Adding a box that extends a run transfers the boundary override
    Given the editor is open with a track whose last occurrence holds a post override
    When the reviewer adds a box on the next frame assigned to track 1
    Then the new box stores the previous last occurrence post override 500
    And the previous last occurrence clears its post override
    And Vue sends an add callback and a "track-settings" update for the cleared boundary

  Scenario: Track shape and blur size bulk-update every occurrence
    Given the editor is open with consecutive track segments
    When the reviewer sets track 1 shape to rectangle and blur size to 150
    Then every track 1 occurrence stores shape rectangle and blur size override 150
    And Vue sends a "track-settings" bulk update with cloned before state

  Scenario: Blur size equal to the global value normalizes storage to null
    Given the editor is open with consecutive track segments
    When the reviewer sets track 1 blur size to the global value 200
    Then every track 1 occurrence stores a null blur size override

  Scenario: New boxes default to the Other class
    Given the AddBoxDialog is open
    When the reviewer inspects the default class selection
    Then the default class is Other and Face remains available

  Scenario: A new box assigned to an existing track inherits shape and blur size
    Given the editor is open with a track that carries shape and blur size
    When the reviewer adds a box on the next frame assigned to that track
    Then the new box inherits the track shape and blur size override

  Scenario: Splitting normalizes the resulting runs
    Given the editor is open with boundary overrides on track 1
    When the reviewer splits out the middle occurrence of track 1
    Then the split occurrence moves to a new track
    And each resulting run keeps boundary overrides only on its outer occurrences

  Scenario: Merging uses the first selected track and normalizes boundaries
    Given the editor is open with two mergeable tracks
    When the reviewer merges track 2 into track 1
    Then the merged track adopts track 1 shape and blur size
    And boundary overrides survive only on the merged run outer occurrences

  Scenario: Occurrence blur overrides the track blur and resolves above it
    Given the editor is open with a track that carries shape and blur size
    When the reviewer sets an occurrence blur size to 180 on the first occurrence
    Then the first occurrence stores an occurrence blur override of 180
    And the first occurrence resolves to 180 while the other occurrences resolve to 150

  Scenario: Resetting an occurrence blur falls back to the track value
    Given the editor is open with a track that carries shape and blur size
    When the reviewer sets an occurrence blur size to 180 on the first occurrence and resets it
    Then the first occurrence stores a null occurrence blur override
    And the first occurrence resolves to the track blur size 150

  Scenario: A later track blur change preserves an existing occurrence blur
    Given the editor is open with a track that carries shape and blur size
    When the reviewer sets an occurrence blur size to 180 on the first occurrence
    And the reviewer sets track 1 blur size to 170
    Then the first occurrence keeps its occurrence blur override of 180
