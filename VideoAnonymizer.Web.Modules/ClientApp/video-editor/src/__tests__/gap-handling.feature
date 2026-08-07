Feature: Per-gap Interpolate versus Use Before/After buffers
  As a reviewer controlling anonymization across detection gaps
  I want each real same-track gap to choose Interpolate or Use buffers
  So that I can bridge missing detections or keep independent segment buffers.

  Scenario: Gap controls appear only for real previous and following gaps
    Given the editor is open with a track that has two segments separated by a real gap
    When the reviewer selects an occurrence in the first segment
    Then Gap after is available and Gap before is hidden
    When the reviewer selects an occurrence in the second segment
    Then Gap before is available and Gap after is hidden

  Scenario: New gaps default to Interpolate
    Given the editor is open with a track that has two segments separated by a real gap
    When the reviewer selects an occurrence in the first segment
    Then Gap after shows Interpolate
    And the last occurrence before the gap stores a null nextGapHandlingMode

  Scenario: Editing After automatically selects UseBuffers for the following gap
    Given the editor is open with a track that has two segments separated by a real gap
    When the reviewer selects the first segment and sets After to 450
    Then Gap after becomes UseBuffers
    And the last occurrence before the gap stores UseBuffers
    And Vue sends one authoritative update for that boundary

  Scenario: Editing Before automatically selects UseBuffers for the preceding gap
    Given the editor is open with a track that has two segments separated by a real gap
    When the reviewer selects the second segment and sets Before to 450
    Then Gap before becomes UseBuffers
    And the previous segment last occurrence stores UseBuffers

  Scenario: Resetting Before or After leaves the gap mode unchanged
    Given the editor is open with a UseBuffers gap and custom After on the first segment
    When the reviewer resets After
    Then the After override is cleared
    And Gap after remains UseBuffers

  Scenario: Explicit Interpolate keeps inactive buffer overrides stored
    Given the editor is open with a UseBuffers gap and custom After on the first segment
    When the reviewer sets Gap after to Interpolate
    Then Gap after shows Interpolate
    And the custom After override remains stored
    And the inspector explains that After is inactive while Interpolate is selected

  Scenario: Track-wide Time buffer apply switches internal gaps to UseBuffers
    Given the editor is open with a track that has two segments separated by a real gap
    When the reviewer applies Time buffer 500 to the entire track
    Then every current segment boundary stores 500
    And every internal gap is UseBuffers

  Scenario: Track-wide Time buffer reset clears boundaries without changing gap modes
    Given the editor is open with custom segment boundaries and UseBuffers gaps
    When the reviewer resets the track Time buffer
    Then every segment boundary override is cleared
    And every internal gap remains UseBuffers

  Scenario: Closing a gap clears the obsolete nextGapHandlingMode
    Given the editor is open with a UseBuffers gap between two segments
    When the reviewer adds an occurrence that closes the gap
    Then the resulting single segment has no nextGapHandlingMode
