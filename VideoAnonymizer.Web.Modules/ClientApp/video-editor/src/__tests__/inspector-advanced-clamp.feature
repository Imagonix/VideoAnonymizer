Feature: Clamp inspector after Advanced expands
  As a reviewer opening Advanced near a workspace edge
  I want the inspector to remeasure after paint and move only as needed
  So that every Advanced option stays reachable without resetting placement.

  Scenario: Opening Advanced near the bottom corrects position using post-render height
    Given the editor is open with the inspector near the bottom of a short stage
    When the reviewer opens Advanced
    Then the inspector height is remeasured after Advanced has rendered
    And the inspector is moved up only enough to keep the expanded group inside the stage
    And Advanced options remain reachable

  Scenario: Opening Advanced mid-stage does not flip or reset a manual position
    Given the editor is open with a manually placed inspector in the middle of a tall stage
    When the reviewer opens Advanced
    Then the inspector keeps its manual horizontal position
    And the inspector does not recompute opposite-side placement

  Scenario: When the expanded group exceeds the stage the group scrolls as one unit
    Given the editor is open with the inspector on a very short stage
    When the reviewer opens Advanced
    Then the inspector group uses whole-group vertical scrolling
    And Advanced options remain reachable

  Scenario: Closing Advanced keeps the corrected position
    Given the editor is open with the inspector near the bottom of a short stage
    When the reviewer opens Advanced
    And the inspector is corrected for the expanded height
    And the reviewer closes Advanced
    Then the inspector does not jump back to the pre-expansion coordinates
