Feature: Inspector gap checkboxes, width, and numeric steps
  As a reviewer using the three-panel inspector
  I want clear gap checkboxes, readable panel width, and predictable spin steps
  So that segment buffers and overrides stay easy to control without truncated labels.

  Scenario: Desktop inspector group uses 320 px width for placement and clamping
    Given the editor is open with a selected tracked occurrence on a wide stage
    Then the inspector group width is 320 pixels
    And the automatic placement uses the 320 px width on the opposite side

  Scenario: Narrow stage shrinks the inspector width and keeps labels readable
    Given the editor is open with a selected tracked occurrence on a narrow stage
    Then the inspector group width fits inside the stage margins
    And inspector labels and gap captions do not use text-overflow ellipsis

  Scenario: Time buffer inputs use 100 ms steps and accept non-step values
    Given the editor is open with a continuous track and outer buffer controls
    Then the segment Before and After inputs use step 100
    And the track Time buffer control is absent
    When the reviewer types segment Before as 250
    Then the stored Before override is 250 without rounding to a step

  Scenario: Blur size inputs use 10 percent steps and accept non-step values
    Given the editor is open with a continuous track and outer buffer controls
    Then the occurrence and track blur inputs use step 10
    When the reviewer types occurrence blur as 125
    Then the stored occurrence blur override is 125 without rounding to a step

  Scenario: Before and after views of the same gap stay synchronized through checkboxes
    Given the editor is open with a track that has two segments separated by a real gap
    When the reviewer unchecks Interpolate gap after on the first segment
    Then the second segment shows Interpolate gap before unchecked
    When the reviewer checks Interpolate gap before on the second segment
    Then the first segment shows Interpolate gap after checked
    And the stored gap mode is null for default Interpolate
