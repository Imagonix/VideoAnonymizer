Feature: Video-first selection workspace
  As a reviewer
  I want to select boxes on the main video and inspect their track
  So that I can configure and navigate the selected occurrence without leaving the video.

  Scenario: Clicking a box selects its track
    Given the editor is open in the video-first workspace
    When the reviewer clicks a box of track 1
    Then the selection is keyed to track 1
    And the object details panel is open

  Scenario: Clicking another box changes the selection
    Given the editor is open in the video-first workspace
    When the reviewer clicks a box of track 2 after selecting track 1
    Then the selection is keyed to track 2

  Scenario: Clicking an untracked box selects the object itself
    Given the editor is open in the video-first workspace
    When the reviewer clicks the untracked box
    Then the selection is keyed to that object id

  Scenario: Clicking empty video space clears the selection
    Given the editor is open in the video-first workspace
    When the reviewer clicks empty video space after selecting a box
    Then the selection is cleared

  Scenario: Escape clears the selection
    Given the editor is open in the video-first workspace
    When the reviewer presses Escape after selecting a box
    Then the selection is cleared

  Scenario: Escape does not clear while an action mode is active
    Given the editor is open in the video-first workspace
    When the reviewer selects a box and activates an action mode and presses Escape
    Then the selection is still set

  Scenario: The inspector opens on the side opposite the selected box
    Given the editor is open in the video-first workspace
    When the reviewer selects a box on the left side of the stage
    Then the inspector is placed on the right side of the stage

  Scenario: The inspector prefers letterbox space over the video
    Given the editor is open with a portrait video in a wide stage
    When the reviewer selects the leftmost box
    Then the inspector is placed in the horizontal letterbox margin

  Scenario: Next occurrence seeks to the next analyzed detection
    Given the editor is open in the video-first workspace
    When the reviewer selects the first occurrence of track 1 and clicks next occurrence
    Then the video seeks to the second occurrence time of track 1

  Scenario: Previous occurrence seeks to the previous analyzed detection
    Given the editor is open in the video-first workspace
    When the reviewer selects the second occurrence of track 1 and clicks previous occurrence
    Then the video seeks to the first occurrence time of track 1

  Scenario: Occurrence buttons disable at the track boundaries
    Given the editor is open in the video-first workspace
    When the reviewer selects the last occurrence of track 1
    Then the next occurrence button is disabled and previous is enabled

  Scenario: Selection stays on the track while the playback time moves
    Given the editor is open in the video-first workspace
    When the reviewer selects track 1 and the playback time moves to its third occurrence
    Then the selection is still track 1 and the inspector context is the third occurrence

  Scenario: Bounding boxes are keyboard selectable
    Given the editor is open in the video-first workspace
    When the reviewer focuses a bounding box and presses Enter
    Then the selection is keyed to that box's track
