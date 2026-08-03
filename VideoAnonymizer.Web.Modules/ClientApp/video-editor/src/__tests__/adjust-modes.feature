Feature: Inline adjust and add modes on the main video
  As a reviewer
  I want to adjust and add detections directly on the main video
  So that I can fix bounding boxes without leaving the review workspace.

  Scenario: Selecting a box never moves it
    Given the editor is open in the review workspace
    When the reviewer selects a box and drags on it
    Then the selected box geometry is unchanged

  Scenario: Adjust detection enters adjust mode and pauses playback
    Given the editor is open in the review workspace
    When the reviewer clicks Adjust detection for the selected occurrence
    Then the editor is in adjust mode

  Scenario: Dragging inside the selected box moves only the current occurrence
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode and drags the selected box by 50 px
    Then the current occurrence is moved by 50 px
    And no other occurrence of the track moves

  Scenario: Resizing uses the visible handles for the current occurrence
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode and resizes the selected box from the east handle
    Then the current occurrence width changes

  Scenario: Reset restores the box before dispatch
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode, moves the box, and clicks Reset
    Then the selected box is back at its original geometry
    And the editor is still in adjust mode

  Scenario: Done dispatches one adjust action and returns to select
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode, moves the box, and clicks Done
    Then Vue sends one "adjust" update with the previous geometry
    And the editor returns to select mode

  Scenario: Escape restores the box and exits adjust without saving
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode, moves the box, and presses Escape
    Then the selected box is back at its original geometry
    And Vue sends no update
    And the editor returns to select mode

  Scenario: Add Object draws on the main video and defaults to Other
    Given the editor is open in the review workspace
    When the reviewer clicks Add Object, draws a box, and confirms the default class
    Then a new object is added with class other on the current frame
    And the editor returns to select mode

  Scenario: Track forward is activated from the Advanced menu
    Given the editor is open in the review workspace
    When the reviewer opens the Advanced menu and clicks Track forward
    Then Vue is asked to track the selected occurrence forward

  Scenario: Action modes are mutually exclusive
    Given the editor is open in the review workspace
    When the reviewer activates adjust and then activates add
    Then the editor is in add mode only
