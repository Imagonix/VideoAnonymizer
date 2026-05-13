@frame_coverage
Feature: Applying persisted detections between analyzed frames
  As a reviewer who sampled detections at intervals
  I want the processor to reuse persisted detections between analyzed frames
  So that selected faces stay blurred continuously in the exported video.

  Scenario: Latest analyzed frame is used before the next analyzed frame
    Given analyzed detections
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
    When the processor asks for objects at 0.5 seconds with a 0.0 second buffer
    Then the relevant objects are
      | trackId | x  |
      | 7       | 10 |

  Scenario: Next analyzed frame is used after it becomes current
    Given analyzed detections
      | timeSeconds | trackId | x   |
      | 0.0         | 7       | 10  |
      | 1.0         | 7       | 100 |
    When the processor asks for objects at 1.25 seconds with a 0.0 second buffer
    Then the relevant objects are
      | trackId | x   |
      | 7       | 100 |

  Scenario: Time buffer keeps the previous track visible briefly after the next sample
    Given analyzed detections
      | timeSeconds | trackId | x   |
      | 0.0         | 1       | 10  |
      | 1.0         | 2       | 100 |
    When the processor asks for objects at 1.15 seconds with a 0.25 second buffer
    Then the relevant tracks are 1, 2

  Scenario: Previous track expires after the configured time buffer
    Given analyzed detections
      | timeSeconds | trackId | x   |
      | 0.0         | 1       | 10  |
      | 1.0         | 2       | 100 |
    When the processor asks for objects at 1.30 seconds with a 0.25 second buffer
    Then the relevant tracks are 2

  Scenario: No detections are projected before the first analyzed frame
    Given analyzed detections
      | timeSeconds | trackId | x   |
      | 0.0         | 1       | 10  |
      | 1.0         | 2       | 100 |
    When the processor asks for objects at -0.10 seconds with a 0.25 second buffer
    Then no relevant objects are returned

  Scenario: Newer same-track detections win over older buffered positions
    Given analyzed detections
      | timeSeconds | trackId | x   |
      | 0.0         | 5       | 10  |
      | 0.5         | 5       | 50  |
      | 1.0         | 5       | 100 |
    When the processor asks for objects at 0.75 seconds with a 0.75 second buffer
    Then the relevant objects are
      | trackId | x  |
      | 5       | 50 |
