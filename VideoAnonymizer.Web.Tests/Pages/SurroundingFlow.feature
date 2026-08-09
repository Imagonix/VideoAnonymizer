@surrounding_flow
Feature: Imported video status and working-copy deletion
  As a reviewer managing imported videos
  I want compact status and a safe Delete working copy action
  So that I can manage working copies without storage-location claims.

  Scenario: Imported video table shows derived statuses
    Given the home page lists videos in imported analyzed and exported states
    Then the listed video statuses are "Exported", "Ready to review", and "Imported"

  Scenario: Existing video rows and delete action are keyboard accessible
    Given the home page lists an imported video "library-clip.mp4" ready to review
    Then the existing video row is keyboard focusable
    And the delete working copy action is keyboard accessible
    When the reviewer activates the existing video row with the keyboard
    Then the review workspace opens for the listed video

  Scenario: Delete working copy uses deployment-neutral wording
    Given the home page lists an imported video "library-clip.mp4" ready to review
    Then the delete action is labeled "Delete working copy"
    And the imported videos UI does not claim local or cloud-only storage

  Scenario: Reviewer confirms deleting a working copy from the table
    Given the home page lists an imported video "library-clip.mp4" ready to review
    When the reviewer opens the delete confirmation for the listed video
    Then a delete confirmation dialog names the listed video
    And the working copy delete request is not sent yet
    When the reviewer confirms the working copy deletion
    Then the working copy delete request is sent
    And the listed video is no longer shown

  Scenario: Cancelling the working copy confirmation does not delete
    Given the home page lists an imported video "library-clip.mp4" ready to review
    When the reviewer cancels the delete confirmation for the listed video
    Then the working copy delete request is not sent
    And the listed video is still shown

  Scenario: Deleting a working copy does not open the video
    Given the home page lists an imported video "library-clip.mp4" ready to review
    When the reviewer opens the delete confirmation for the listed video
    Then the delete confirmation names the listed video
    And the review workspace does not open

  Scenario: The delete confirmation explains the working copy and uses red affordances
    Given the home page lists an imported video "library-clip.mp4" ready to review
    When the reviewer opens the delete confirmation for the listed video
    Then the delete confirmation explains that source, export, analysis, and records will be deleted
    And the delete icon and confirmation action use the destructive red color

  Scenario: The imported video table shows upload time in local time
    Given the home page lists imported videos with known upload times
    Then the table shows each listed upload time in local time

  Scenario: Imported videos list defaults to newest upload first
    Given the home page lists imported videos with known upload times
    Then the imported videos are listed newest upload first

  Scenario: Filename column sort toggles direction with accessible indication
    Given the home page lists imported videos with known upload times
    When the reviewer activates the Filename sort once
    Then the imported videos are listed by filename ascending
    And the active Filename sort is indicated ascending
    When the reviewer activates the Filename sort again
    Then the imported videos are listed by filename descending
    And the active Filename sort is indicated descending

  Scenario: Uploaded column sort toggles direction with accessible indication
    Given the home page lists imported videos with known upload times
    When the reviewer activates the Uploaded sort once
    Then the imported videos are listed by upload time ascending
    And the active Uploaded sort is indicated ascending
    When the reviewer activates the Uploaded sort again
    Then the imported videos are listed by upload time descending
    And the active Uploaded sort is indicated descending
