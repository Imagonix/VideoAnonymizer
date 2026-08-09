Feature: Right editor toolbar and square editing geometry
  As a reviewer editing detections on the main video
  I want a fixed right-side icon toolbar with contextual Confirm/Discard and square manipulation geometry
  So that editing stays predictable and the manipulation frame never rounds off.

  Scenario: The toolbar is a fixed vertical control on the right side
    Given the editor is open in the review workspace
    Then the toolbar is a fixed vertical control on the right side

  Scenario: Add Object enters add mode and pauses playback
    Given the editor is open in the review workspace
    When the reviewer clicks the Add Object toolbar button
    Then the editor is in add mode
    And playback is paused

  Scenario: Add mode shows a disabled Confirm until a box is drawn
    Given the editor is open in the review workspace
    When the reviewer clicks the Add Object toolbar button
    Then the toolbar shows a disabled Confirm and an enabled Discard
    When the reviewer draws a box on the main video
    Then the Confirm becomes enabled

  Scenario: Confirm applies the pending add with the default class
    Given the editor is open in the review workspace
    When the reviewer clicks the Add Object toolbar button, draws a box, and clicks Confirm
    Then a new object is added with class other on the current frame
    And the editor returns to select mode

  Scenario: Discard cancels add without persisting
    Given the editor is open in the review workspace
    When the reviewer clicks the Add Object toolbar button, draws a box, and clicks Discard
    Then no object is added
    And the editor returns to select mode

  Scenario: Discard restores adjust changes without persisting
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode, moves the box, and clicks Discard
    Then the box is back at its original geometry
    And Vue sends no update
    And the editor returns to select mode

  Scenario: Enter confirms the adjust operation
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode, moves the box, and presses Enter
    Then Vue sends one "adjust" update with the previous geometry
    And the editor returns to select mode

  Scenario: Escape discards the adjust operation
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode, moves the box, and presses Escape
    Then the box is back at its original geometry
    And Vue sends no update
    And the editor returns to select mode

  Scenario: Cycling modes does not shift the toolbar or video frame
    Given the editor is open in the review workspace
    When the reviewer cycles through select, add, and adjust modes
    Then the video frame size is unchanged
    And the toolbar stays in its fixed position

  Scenario: Toolbar clicks do not leak into the workspace
    Given the editor is open in the review workspace
    When the reviewer clicks the Add Object toolbar button while a box is selected
    Then the selection is unchanged and the workspace click handler does not fire

  Scenario: The manipulation frame uses square corners and square handles
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode on a rectangle-shaped box
    Then the adjust box has square corners and square handles
    And the blur preview region is rectangular

  Scenario: An elliptical anonymization region is edited with a rectangular frame
    Given the editor is open in the review workspace
    When the reviewer enters adjust mode on an ellipse-shaped box and resizes from the east handle
    Then the manipulation frame stays rectangular
    And the blur preview region is elliptical
    And the stored geometry changes as a rectangle
