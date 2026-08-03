@review_export_mvp
Feature: Review export progressive disclosure and compact recovery
  As a reviewer finishing anonymization
  I want gated export, clear save status, and a compact Download control
  So that I can export safely and recover a blocked download without leaving the editor.

  Scenario: Save state shows Saved when idle without errors
    Given the review editor is open with mixed track inclusion
    Then the save state label is "Saved"
    And the save state label is not "Saved" when errors are present

  Scenario: Export is disabled while editor actions are pending
    Given the review editor is open with mixed track inclusion
    When editor actions are pending
    Then export is disabled because "Wait for editor changes to finish saving."

  Scenario: Export is disabled while undo redo is applying
    Given the review editor is open with mixed track inclusion
    When undo redo is applying
    Then export is disabled because "Wait for undo/redo to finish."

  Scenario: Export is disabled while track forward is active
    Given the review editor is open with mixed track inclusion
    When track forward is active
    Then export is disabled because "Wait for track-forward to finish."

  Scenario: Export is disabled when persistence has errors
    Given the review editor is open with mixed track inclusion
    When persistence has errors
    Then export is disabled because "Fix save errors before export."
    And the save state label is "Save failed - Retry"

  Scenario: Export is disabled while anonymization is running
    Given the review editor is open with mixed track inclusion
    When anonymization is running
    Then export is disabled because "Export is already running."

  Scenario: Export starts immediately without confirmation
    Given the review editor is open with mixed track inclusion
    When the reviewer clicks Export anonymized video
    Then anonymization is started with the current frames
    And no export confirmation is shown

  Scenario: Download is hidden until a successful anonymized result exists
    Given the review editor is open with mixed track inclusion
    Then the Download button is not shown

  Scenario: Automatic download leaves a compact Download control beside Export
    Given the review editor finished export with automatic download
    Then the Download button is shown beside Export anonymized video
    And the Download button is enabled
    And the editor remains available

  Scenario: Blocked automatic download still offers Download recovery
    Given the review editor finished export with a failed automatic download
    Then the Download button is shown beside Export anonymized video
    And the Download button is enabled
    And the editor remains available

  Scenario: Manual Download requests the latest result again
    Given the review editor finished export with automatic download
    When the reviewer clicks Download
    Then another download is requested

  Scenario: Download is disabled during a later export
    Given the review editor finished export with automatic download
    When anonymization is running
    Then the Download button is shown beside Export anonymized video
    And the Download button is disabled because "Wait for the current export to finish."
    And export is disabled because "Export is already running."

  Scenario: After a later export succeeds Download targets the newest result
    Given the review editor finished export with automatic download
    When anonymization is running
    And anonymization succeeds again
    Then the Download button is enabled
    When the reviewer clicks Download
    Then another download is requested
