@mvp_surrounding_flow
Feature: Imported video status and working-copy deletion
  As a reviewer managing imported videos
  I want compact status and a safe Delete working copy action
  So that I can manage working copies without storage-location claims.

  Scenario: Imported video table shows derived statuses
    Given the home page lists videos in imported analyzed and exported states
    Then the listed video statuses are "Imported", "Ready to review", and "Exported"

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

  Scenario: Reviewer can delete a working copy from the table
    Given the home page lists an imported video "library-clip.mp4" ready to review
    When the reviewer deletes the working copy for the listed video
    Then the working copy delete request is sent
    And the listed video is no longer shown
