Feature: Inspector placement and effective blur preview
  As a reviewer working in the video-first workspace
  I want the floating inspector to start on a side of the stage and the blur preview to use each track's effective blur size
  So that the inspector never blocks the video vertically and the preview matches export.

  Scenario: Initial placement is vertically centered on the side opposite the box
    Given the editor is open with a box on the left of a wide stage
    When the reviewer selects that box
    Then the inspector is vertically centered on the right side of the stage
    And the inspector is not placed above or below the stage center

  Scenario: A box on the right places the inspector on the left
    Given the editor is open with a box on the right of a wide stage
    When the reviewer selects that box
    Then the inspector is vertically centered on the left side of the stage

  Scenario: Dragging the inspector clamps it inside the stage
    Given the editor is open with an open inspector
    When the reviewer drags the inspector handle far beyond the stage edges
    Then the inspector stays fully inside the stage

  Scenario: The inspector keeps its dragged position across occurrence changes
    Given the editor is open with an open inspector on track 1
    When the reviewer drags the inspector to a custom position and navigates to the next occurrence
    Then the inspector keeps the custom position

  Scenario: Reopening the inspector keeps the last retained position
    Given the editor is open with an open inspector on track 1
    When the reviewer drags the inspector to a custom position, deselects, and reselects the box
    Then the inspector keeps the custom position after hide and show

  Scenario: Automatic placement runs only once per mounted session
    Given the editor is open with left and right boxes on a wide stage
    When the reviewer selects the left box
    Then the inspector is vertically centered on the right side of the stage
    When the reviewer selects the right box without dragging
    Then the inspector stays on the right side without jumping

  Scenario: Workspace resize only re-clamps the retained position
    Given the editor is open with an open inspector on track 1
    When the reviewer drags the inspector to a custom position
    And the workspace shrinks so the inspector would leave the stage
    Then the inspector is re-clamped inside the stage without flipping sides

  Scenario: Dragging the inspector does not change the selection
    Given the editor is open with an open inspector
    When the reviewer presses and moves the inspector drag handle
    Then the current selection is unchanged

  Scenario: The blur preview falls back to the global blur size
    Given the editor is open with a track without a blur override
    When the reviewer inspects the blur preview
    Then the preview blur area is two times the box size

  Scenario: The blur preview uses the track blur override
    Given the editor is open with a track without a blur override
    When the reviewer sets the track blur size to 150
    Then the preview blur area is one and a half times the box size

  Scenario: Reset returns the preview to the global blur size
    Given the editor is open with a track whose blur size is overridden
    When the reviewer resets the track blur size
    Then the preview blur area is two times the box size

  Scenario: Undo updates the preview without a persistence round-trip
    Given the editor is open with a track whose blur size is overridden
    When the reviewer receives an undo change that clears the override
    Then the preview blur area is two times the box size

  Scenario: Blazor-pushed changes update the preview immediately
    Given the editor is open with a track without a blur override
    When the reviewer receives a pushed detection change carrying an override
    Then the preview blur area is one and a half times the box size
