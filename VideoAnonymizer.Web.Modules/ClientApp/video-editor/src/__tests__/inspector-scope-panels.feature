Feature: Three-scope floating inspector panels
  As a reviewer working in the video-first workspace
  I want the floating inspector to be one movable group of three scope panels
  So that each control edits exactly the occurrence, segment, or track scope it claims.

  Scenario: The inspector renders three scope panels in order
    Given the editor is open with a selected tracked occurrence
    Then the inspector shows Current occurrence, Current segment, and Entire track panels in order
    And each panel has its own header and surface

  Scenario: The panels stay separate surfaces with transparent gaps
    Given the editor is open with a selected tracked occurrence
    Then the scope panels are separate sibling elements with transparent gaps between them

  Scenario: The whole group moves together and stays inside the stage
    Given the editor is open with a selected tracked occurrence on a wide stage
    When the reviewer drags the inspector group handle far beyond the stage edges
    Then all three panels remain attached and the group stays fully inside the stage

  Scenario: The group keeps its manual position when the selected occurrence changes
    Given the editor is open with a two-occurrence track and the group dragged to a custom position
    When the reviewer navigates to the next occurrence
    Then the group keeps the custom position

  Scenario: A group taller than the stage scrolls as one unit
    Given the editor is open with a selected tracked occurrence on a very short stage
    Then the inspector group stays attached and scrolls as one unit inside the stage

  Scenario: An interior occurrence shows the containing segment boundary values
    Given the editor is open with a segment whose first occurrence holds a pre override
    When the reviewer selects the middle occurrence of that segment
    Then the Current segment panel shows the segment pre override and the global post value
    And selecting the interior occurrence does not store pre or post on it

  Scenario: Every control is scoped to its panel
    Given the editor is open with a selected tracked occurrence
    Then the Current occurrence panel contains only occurrence-level controls
    And the Current segment panel contains only the Before and After controls
    And the Entire track panel contains shape, track blur, and advanced controls
    And the Entire track panel has no Time buffer control

  Scenario: The inspector does not duplicate track-wide inclusion
    Given the editor is open with a selected tracked occurrence
    Then the inspector has no track-wide inclusion control

  Scenario: Occurrence blur inheritance uses exact provenance badges
    Given the editor is open with a track without any blur overrides
    When the reviewer selects the box
    Then the occurrence blur badge reads "Inherited: Video"
    Given the editor is open with a track whose blur size is overridden
    When the reviewer selects the box
    Then the occurrence blur badge reads "Inherited: Track"
    Given the editor is open with a track whose occurrence blur is overridden
    When the reviewer selects the box
    Then the occurrence blur badge reads "Occurrence override"

  Scenario: Track and segment badges use exact provenance labels
    Given the editor is open with a track without any blur overrides
    When the reviewer selects the box
    Then the track blur badge reads "Inherited: Video"
    And the segment Before and After badges read "Inherited: Video"
    Given the editor is open with a track whose blur size is overridden
    When the reviewer selects the box
    Then the track blur badge reads "Track override"
    Given the editor is open with a segment whose first occurrence holds a pre override
    When the reviewer selects the middle occurrence of that segment
    Then the segment Before badge reads "Segment override"
    And the segment After badge reads "Inherited: Video"

  Scenario: Reset controls use inherited and video destinations
    Given the editor is open with a track whose occurrence blur is overridden
    When the reviewer selects the box
    Then the occurrence blur reset control is labeled Reset to inherited value
    And the track blur and segment buffer reset controls are labeled Reset to video value

  Scenario: Resetting occurrence blur falls back to the track or video value
    Given the editor is open with a track whose occurrence blur is overridden
    When the reviewer selects the box and resets the occurrence blur size
    Then the occurrence stores a null occurrence blur override
