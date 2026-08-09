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

  Scenario: Post-buffer extrapolation continues past the last stored box
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
      | 3.0         | 9       | 200 |
    When the processor predicts objects at 1.2 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x   |
      | 7       | 118 |

  Scenario: Post-buffer never snaps back to a historical stored box after the buffer ends
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
      | 3.0         | 9       | 200 |
    When the processor predicts objects at 1.4 seconds with a 0.25 second buffer
    Then no predicted objects are returned

  Scenario: Close boundary frames keep continuous post-buffer motion just after the last sample
    Given analyzed detections for prediction
      | timeSeconds | trackId | x  |
      | 0.0         | 7       | 10 |
      | 1.0         | 7       | 20 |
    When the processor predicts objects at 1.05 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x  |
      | 7       | 20 |

  Scenario: Close boundary frames keep continuous post-buffer motion later in the buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x  |
      | 0.0         | 7       | 10 |
      | 1.0         | 7       | 20 |
    When the processor predicts objects at 1.2 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x  |
      | 7       | 22 |

  Scenario: Partial left-edge exit keeps raw width and center velocity
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | y  | width | height |
      | 0.0         | 7       | 20  | 20 | 30    | 40     |
      | 1.0         | 7       | -10 | 20 | 30    | 40     |
    And the video frame size is 200 by 100
    When the processor predicts objects at 1.2 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x   | y  | width | height |
      | 7       | -16 | 20 | 30    | 40     |

  Scenario: Partial right-edge exit keeps raw width and center velocity
    Given analyzed detections for prediction
      | timeSeconds | trackId | x  | y  | width | height |
      | 0.0         | 7       | 50 | 20 | 40    | 30     |
      | 1.0         | 7       | 80 | 20 | 40    | 30     |
    And the video frame size is 100 by 100
    When the processor predicts objects at 1.2 seconds with a 0.5 second buffer
    Then the predicted objects are
      | trackId | x  | y  | width | height |
      | 7       | 86 | 20 | 40    | 30     |

  Scenario: Partial top-edge exit keeps raw height and center velocity
    Given analyzed detections for prediction
      | timeSeconds | trackId | x  | y   | width | height |
      | 0.0         | 7       | 20 | 20  | 30    | 40     |
      | 1.0         | 7       | 20 | -10 | 30    | 40     |
    And the video frame size is 200 by 100
    When the processor predicts objects at 1.2 seconds with a 0.5 second buffer
    Then the predicted objects are
      | trackId | x  | y  | width | height |
      | 7       | 20 | -16 | 30    | 40     |

  Scenario: Partial bottom-edge exit keeps raw height and center velocity
    Given analyzed detections for prediction
      | timeSeconds | trackId | x  | y  | width | height |
      | 0.0         | 7       | 20 | 40 | 30    | 40     |
      | 1.0         | 7       | 20 | 80 | 30    | 40     |
    And the video frame size is 200 by 100
    When the processor predicts objects at 1.2 seconds with a 0.5 second buffer
    Then the predicted objects are
      | trackId | x  | y  | width | height |
      | 7       | 20 | 88 | 30    | 40     |

  Scenario: A fully outside projected box yields no export region
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | y  | width | height |
      | 0.0         | 7       | 10  | 20 | 30    | 40     |
      | 1.0         | 7       | -20 | 20 | 30    | 40     |
    And the video frame size is 100 by 100
    When the processor predicts objects at 1.5 seconds with a 1.0 second buffer
    Then no predicted objects are returned

  Scenario: Buffer ending before complete exit keeps the final projected position
    Given analyzed detections for prediction
      | timeSeconds | trackId | x  | y  | width | height |
      | 0.0         | 7       | 40 | 20 | 30    | 40     |
      | 1.0         | 7       | 50 | 20 | 30    | 40     |
    And the video frame size is 200 by 100
    When the processor predicts objects at 1.25 seconds with a 0.25 second buffer
    Then the predicted objects are
      | trackId | x  | y  | width | height |
      | 7       | 52 | 20 | 31    | 40     |

  Scenario: After buffer end the projected box disappears without historical fallback
    Given analyzed detections for prediction
      | timeSeconds | trackId | x  | y  | width | height |
      | 0.0         | 7       | 40 | 20 | 30    | 40     |
      | 1.0         | 7       | 50 | 20 | 30    | 40     |
    And the video frame size is 200 by 100
    When the processor predicts objects at 1.26 seconds with a 0.25 second buffer
    Then no predicted objects are returned

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

  Scenario: Default Interpolate bridges a real missing-track gap
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex |
      | 0.0         | 7       | 10  | 0          |
      | 1.0         | 7       | 100 | 1          |
      | 3.0         | 7       | 200 | 3          |
    And an empty analyzed frame at 2.0 seconds with frame index 2
    When the processor predicts objects at 2.0 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x   |
      | 7       | 150 |
    And exactly one predicted region is returned for track 7

  Scenario: Non-unit FrameIndex steps still form one segment for an uninterrupted track
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex |
      | 0.0         | 7       | 10  | 0          |
      | 0.5         | 7       | 55  | 15         |
      | 1.0         | 7       | 100 | 30         |
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x  |
      | 7       | 55 |
    And exactly one predicted region is returned for track 7

  Scenario: Default Interpolate bridges non-unit FrameIndex samples split by a missing middle track
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex |
      | 0.0         | 7       | 10  | 0          |
      | 1.0         | 7       | 100 | 30         |
    And an empty analyzed frame at 0.5 seconds with frame index 15
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x  |
      | 7       | 55 |
    And exactly one predicted region is returned for track 7

  Scenario: An uninterrupted track does not render a multi-box history trail
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex |
      | 0.0         | 7       | 10  | 0          |
      | 0.5         | 7       | 55  | 15         |
      | 1.0         | 7       | 100 | 30         |
      | 1.5         | 7       | 145 | 45         |
    When the processor predicts objects at 0.75 seconds with a 0.25 second buffer
    Then exactly one predicted region is returned for track 7

  Scenario: UseBuffers lets a post-buffer cover alone before the next segment pre-buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex | postOverrideMs | preOverrideMs | nextGapHandlingMode |
      | 0.0         | 7       | 10  | 0          | 400            |               | UseBuffers          |
      | 1.0         | 7       | 100 | 30         |                | 400           |                     |
    And an empty analyzed frame at 0.5 seconds with frame index 15
    When the processor predicts objects at 0.3 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x  |
      | 7       | 10 |
    And exactly one predicted region is returned for track 7

  Scenario: UseBuffers lets a pre-buffer cover alone after the previous segment post-buffer
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex | postOverrideMs | preOverrideMs | nextGapHandlingMode |
      | 0.0         | 7       | 10  | 0          | 400            |               | UseBuffers          |
      | 1.0         | 7       | 100 | 30         |                | 400           |                     |
    And an empty analyzed frame at 0.5 seconds with frame index 15
    When the processor predicts objects at 0.7 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x   |
      | 7       | 100 |
    And exactly one predicted region is returned for track 7

  Scenario: UseBuffers allows overlapping pre and post regions across a gap
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex | postOverrideMs | preOverrideMs | nextGapHandlingMode |
      | 0.0         | 7       | 10  | 0          | 700            |               | UseBuffers          |
      | 1.0         | 7       | 100 | 30         |                | 700           |                     |
    And an empty analyzed frame at 0.5 seconds with frame index 15
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
    Then exactly two predicted regions are returned for track 7

  Scenario: Interpolate ignores stored boundary buffers across a real gap
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex | postOverrideMs | preOverrideMs | nextGapHandlingMode |
      | 0.0         | 7       | 10  | 0          | 400            |               |                     |
      | 1.0         | 7       | 100 | 30         |                | 400           |                     |
    And an empty analyzed frame at 0.5 seconds with frame index 15
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x  |
      | 7       | 55 |
    And exactly one predicted region is returned for track 7

  Scenario: Excluded gap endpoints are not used as included interpolate endpoints
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | frameIndex | selected |
      | 0.0         | 7       | 10  | 0          | true     |
      | 1.0         | 7       | 100 | 30         | false    |
    And an empty analyzed frame at 0.5 seconds with frame index 15
    When the processor predicts objects at 0.5 seconds with a 0.0 second buffer
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

  Scenario: Interpolated boxes copy occurrence blur metadata
    Given analyzed detections for prediction
      | timeSeconds | trackId | x   | occurrenceBlurSizePercentOverride |
      | 0.0         | 7       | 10  | 180                              |
      | 1.0         | 7       | 100 |                                  |
    When the processor predicts objects at 0.4 seconds with a 0.0 second buffer
    Then the predicted objects are
      | trackId | x  | occurrenceBlurSizePercentOverride |
      | 7       | 46 | 180                               |
