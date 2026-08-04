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
    And the Entire track panel contains shape, track blur, time buffer, and advanced controls

  Scenario: The inspector does not duplicate track-wide inclusion
    Given the editor is open with a selected tracked occurrence
    Then the inspector has no track-wide inclusion control

  Scenario: Occurrence blur inheritance is labeled by its scope
    Given the editor is open with a track without any blur overrides
    When the reviewer selects the box
    Then the occurrence blur badge reads Global
    Given the editor is open with a track whose blur size is overridden
    When the reviewer selects the box
    Then the occurrence blur badge reads Track
    Given the editor is open with a track whose occurrence blur is overridden
    When the reviewer selects the box
    Then the occurrence blur badge reads Custom

  Scenario: The Apply Time buffer to track control reports a common value or Mixed
    Given the editor is open with a track whose segment boundaries share one value
    When the reviewer selects the box
    Then the time buffer control reports that common value
    Given the editor is open with a track whose segments carry different boundary values
    When the reviewer selects the box
    Then the time buffer control reports Mixed

  Scenario: Applying a time buffer to the track writes every current segment boundary
    Given the editor is open with a track whose segments carry different boundary values
    When the reviewer selects the box
    And applies a time buffer of 450 to the track
    Then every current segment stores the new pre and post boundary values
    And Vue sends one bulk update with cloned before-state

  Scenario: Resetting the time buffer clears all current segment boundaries to global
    Given the editor is open with a track whose segments carry custom boundary values
    When the reviewer selects the box
    And resets the track time buffer
    Then every current segment boundary falls back to global and stores null

  Scenario: Resetting occurrence blur falls back to the track or global value
    Given the editor is open with a track whose occurrence blur is overridden
    When the reviewer selects the box and resets the occurrence blur size
    Then the occurrence stores a null occurrence blur override
