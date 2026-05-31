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

  Scenario: The previous track stays visible briefly when no matching next sample exists
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 1       | 10  |
      | 1.0         | 2       | 100 |
    When the processor predicts objects at 1.15 seconds with a 0.25 second buffer
    Then the predicted tracks are 1, 2

  Scenario: An upcoming track is used shortly before its first analyzed sample
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | blurShape |
      | 1.0         | 1       | 100 | rectangle |
    When the processor predicts objects at 0.85 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x   | blurShape |
      | 1       | 100 | rectangle |

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

  Scenario: Untracked detections keep the latest analyzed box instead of interpolating
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         |         | 10  |
      | 1.0         |         | 100 |
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x  |
      |         | 10 |
