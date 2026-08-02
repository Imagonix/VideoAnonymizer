@object_interpolation
Feature: Predicting object positions between analyzed frames
  As a reviewer anonymizing a video with sampled detections
  I want tracked objects to be predicted between analyzed frames
  So that moving license plates and faces stay covered while the export renders every frame.

  Scenario: A tracked object's box is interpolated halfway between analyzed frames
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | y  | width | height | blurShape |
      | 0.0         | 7       | 10  | 20 | 30    | 40     | rectangle |
      | 1.0         | 7       | 100 | 60 | 50    | 20     | rectangle |
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x  | y  | width | height | blurShape |
      | 7       | 55 | 40 | 40    | 30     | rectangle |

  Scenario: An analyzed frame keeps its exact detected box
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
    When the processor predicts objects at 1.0 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x   |
      | 7       | 100 |

  Scenario: Interpolation can be disabled while buffer coverage still applies
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
    And object interpolation is disabled
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x  |
      | 7       | 10 |

  Scenario: A track without a next sample stays visible during its own post-buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 1       | 10  |
      | 1.0         | 2       | 100 |
    When the processor predicts objects at 0.15 seconds with a 0.25 second buffer
    Then the predicted tracks are 1

  Scenario: A track without a next sample expires after its own post-buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 1       | 10  |
      | 1.0         | 2       | 100 |
    When the processor predicts objects at 1.15 seconds with a 0.25 second buffer
    Then the predicted tracks are 2

  Scenario: An upcoming track is used shortly before its first analyzed sample
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | blurShape |
      | 1.0         | 1       | 100 | rectangle |
    When the processor predicts objects at 0.85 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x   | blurShape |
      | 1       | 100 | rectangle |

  Scenario: A tracked object keeps moving before its first analyzed sample during the buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 1.0         | 1       | 100 |
      | 2.0         | 1       | 190 |
    When the processor predicts objects at 0.8 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x  |
      | 1       | 82 |

  Scenario: A tracked object keeps moving after its last analyzed sample during the buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
    When the processor predicts objects at 1.2 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x   |
      | 7       | 118 |

  Scenario: A tracked object is not visible after its post-buffer elapses
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
    When the processor predicts objects at 1.4 seconds with a 0.25 second buffer
    Then no predicted objects are returned

  Scenario: A tracked object can move partly outside the frame during extrapolation
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 20  |
      | 1.0         | 7       | -10 |
    When the processor predicts objects at 1.2 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x   | width |
      | 7       | -16 | 30    |

  Scenario: An upcoming track is not used outside the time buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 1.0         | 1       | 100 |
    When the processor predicts objects at 0.70 seconds with a 0.25 second buffer
    Then no predicted objects are returned

  Scenario: An upcoming track is not used when the time buffer is disabled
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 1.0         | 1       | 100 |
    When the processor predicts objects at 0.85 seconds with a 0.0 second buffer
    Then no predicted objects are returned

  Scenario: Untracked detections are separate one-object segments
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         |         | 10  |
      | 1.0         |         | 100 |
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
    Then no predicted objects are returned

  Scenario: An untracked occurrence uses the inherited global pre-buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 1.0         |         | 100 |
    When the processor predicts objects at 0.85 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x   |
      |         | 100 |

  Scenario: An untracked occurrence uses the inherited global post-buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 1.0         |         | 100 |
    When the processor predicts objects at 1.15 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x   |
      |         | 100 |

  Scenario: A consecutive segment inherits the global pre-buffer before its first occurrence
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
    When the processor predicts objects at -0.15 seconds with a 0.25 second buffer
    Then the predicted tracks are 7

  Scenario: A consecutive segment inherits the global post-buffer after its last occurrence
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
    When the processor predicts objects at 1.15 seconds with a 0.25 second buffer
    Then the predicted tracks are 7

  Scenario: A first-occurrence pre override replaces the global pre-buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | preOverrideMs |
      | 0.0         | 7       | 10  | 100           |
      | 1.0         | 7       | 100 |               |
    When the processor predicts objects at -0.05 seconds with a 0.25 second buffer
    Then the predicted tracks are 7

  Scenario: A segment is not covered before its first-occurrence pre override
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | preOverrideMs |
      | 0.0         | 7       | 10  | 100           |
      | 1.0         | 7       | 100 |               |
    When the processor predicts objects at -0.20 seconds with a 0.25 second buffer
    Then no predicted objects are returned

  Scenario: A last-occurrence post override replaces the global post-buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | postOverrideMs |
      | 0.0         | 7       | 10  |                |
      | 1.0         | 7       | 100 | 400            |
    When the processor predicts objects at 1.3 seconds with a 0.25 second buffer
    Then the predicted tracks are 7

  Scenario: A segment is not covered after its last-occurrence post override
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | postOverrideMs |
      | 0.0         | 7       | 10  |                |
      | 1.0         | 7       | 100 | 400            |
    When the processor predicts objects at 1.5 seconds with a 0.25 second buffer
    Then no predicted objects are returned

  Scenario: Interpolation stops at a missing analyzed frame
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex |
      | 0.0         | 7       | 10  | 0          |
      | 1.0         | 7       | 100 | 1          |
      | 3.0         | 7       | 200 | 3          |
    When the processor predicts objects at 2.0 seconds with a 0.0 second buffer
    Then no predicted objects are returned

  Scenario: Interpolated boxes copy override metadata and blur shape
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | blurShape | blurSizePercentOverride | preOverrideMs | postOverrideMs |
      | 0.0         | 7       | 10  | rectangle | 150                     | 100            |                |
      | 1.0         | 7       | 100 | rectangle | 150                     |                | 400            |
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x  | blurShape | blurSizePercentOverride | postOverrideMs |
      | 7       | 55 | rectangle | 150                     | 400            |
