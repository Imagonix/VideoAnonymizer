@review_editor_persistence
Feature: Persisting review editor actions
  As a reviewer correcting detections before export
  I want each edit, undo and redo to use the persistence endpoints
  So that the saved review state stays in sync with what I see.

  Scenario: Added face is persisted
    Given the review editor is open for a persisted video with an empty frame
    When the reviewer adds a face
    Then the new face is posted to persistence

  Scenario: Undoing an added face removes it from persistence
    Given the review editor has saved a newly added face
    When the reviewer undoes the last review action
    Then the new face is deleted from persistence

  Scenario: Redoing an undone added face persists it again
    Given the review editor has undone a newly added face
    When the reviewer redoes the review action
    Then the new face is posted to persistence again

  Scenario: Undoing restored tracking removes its detected faces
    Given the review editor is reopened with a completed tracking action
    When the reviewer undoes the last review action
    Then the restored tracked face is removed from persistence

  Scenario: Moving a face is persisted
    Given the review editor is open for a persisted video with one face at x 10
    When the reviewer moves the face to x 90
    Then the face is saved at x 90

  Scenario: Undoing a moved face restores the saved before state
    Given the review editor has saved a face moved from x 10 to x 90
    When the reviewer undoes the last review action
    Then the face is saved at x 10

  Scenario: Redoing an undone move saves the moved state again
    Given the review editor has undone a face move from x 10 to x 90
    When the reviewer redoes the review action
    Then the face is saved at x 90

  Scenario: Blur settings are persisted
    Given the review editor is open with blur size 120 percent and time buffer 300 ms
    When the reviewer changes the blur size to 180 percent
    Then the settings are saved with blur size 180 percent and time buffer 300 ms

  Scenario: Undoing blur settings restores the previous settings
    Given the review editor has saved blur size 180 percent from 120 percent with time buffer 300 ms
    When the reviewer undoes the last review action
    Then the settings are saved with blur size 120 percent and time buffer 300 ms

  Scenario: Redoing blur settings saves the changed settings again
    Given the review editor has undone blur size 180 percent back to 120 percent with time buffer 300 ms
    When the reviewer redoes the review action
    Then the settings are saved with blur size 180 percent and time buffer 300 ms

  Scenario: New edits clear redo history
    Given the review editor has moved a face, undone the move, and added another face
    When the reviewer tries to redo
    Then no extra persistence request is sent

  @tracking_failure_ui
  Scenario: Failed tracking retains streamed faces
    Given tracking has streamed a new face into the review editor
    When tracking fails after retaining the streamed face
    Then the streamed face remains in the review editor
    And a warning says tracking can continue from the last occurrence
    And the partial tracking action is persisted
