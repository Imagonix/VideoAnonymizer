Feature: Persisting Vue editor changes through Blazor callbacks
  As a reviewer editing detected faces in the Vue editor
  I want each local change to tell Blazor exactly what changed and what the previous state was
  So that persistence, undo and redo can stay authoritative outside the editor.

  Scenario: Deselecting one face sends its previous state
    Given the editor is open with persisted frames
    When the reviewer deselects face "o5"
    Then Vue sends a "toggle" update for face "o5" with selected false and previous selected true

  Scenario: Deselecting a tracked row sends a bulk before state
    Given the editor is open with persisted frames
    When the reviewer deselects tracked row 1
    Then Vue sends a "toggle" bulk update for faces "o1,o3,o6" with selected false and previous selected true

  Scenario: Adding a face tells Blazor which object changed
    Given the editor is open with persisted frames
    When the reviewer draws a new face box at 100,110 with size 50x60
    Then Vue sends an add callback for the new face in frame "f1"

  Scenario: Deleting a newly added face tells Blazor which object changed
    Given the editor has sent an add callback for a new face
    When the reviewer deletes that new face
    Then Vue sends a delete callback for the same face in frame "f1"
    And the new face is no longer in the editor state

  Scenario: Blazor-pushed changes update the reactive editor state
    Given the editor is open with persisted frames
    When Blazor pushes an update for face "o1", removes face "o2", and adds face "added-from-blazor"
    Then face "o1" is deselected at x 77
    And face "o2" is no longer in the editor state
    And face "added-from-blazor" is present on track 9

  Scenario: Removing repeated tracking updates clears every timeline occurrence
    Given the editor is open with persisted frames
    When Blazor adds tracked face "streamed-face" twice and then removes it
    Then face "streamed-face" is no longer in the editor state
    And no timeline occurrence remains for face "streamed-face"

  Scenario: Concurrent tracking progress is shown independently
    Given the editor is tracking tracks 1 and 2
    When Blazor reports track 1 at 1000 ms and track 2 at 2000 ms
    Then track 1 has its moving tracking dot at 10 percent
    And track 2 has its moving tracking dot at 20 percent
    And the moving dots are the only timeline tracking indicators
