@appearance_tracking
Feature: Assigning track ids by appearance
  As a reviewer checking detected faces in a video
  I want visually similar faces to keep the same track id
  So that one person is not split into many separate objects.

  Scenario: A similar face keeps the same track
    Given an appearance tracker with default matching settings
    And the tracker has already seen "first face" as "person A"
    When the next frame contains "later face" as "person A"
    Then "later face" has the same track id as "first face"

  Scenario: A different face starts a new track
    Given an appearance tracker with default matching settings
    And the tracker has already seen "first face" as "person A"
    When the next frame contains "other face" as "person B"
    Then "other face" has a different track id than "first face"

  Scenario: Only one face can continue an existing track in the same frame
    Given an appearance tracker with default matching settings
    And the tracker has already seen "first face" as "person A"
    When the next frame contains these faces
      | name             | appearance |
      | first candidate  | person A   |
      | second candidate | person A   |
    Then exactly one of these faces has the same track id as "first face"
      | name             |
      | first candidate  |
      | second candidate |
    And these faces have different track ids
      | name             |
      | first candidate  |
      | second candidate |
