@forward_tracking
Feature: Forward-only single-object tracking
  As a reviewer correcting one object track
  I want generated forward detections to be saved safely
  So that the editor can continue with normal reviewed detections.

  Scenario: Generated detections keep the seed track and use analyzed frame samples
    Given a reviewed video has trackable analyzed frames
    And the Python forward tracker returns boxes on analyzed frames and a decoded-only frame
    When the reviewer tracks the seed face forward
    Then generated faces use the seed track id
    And the Python tracker is asked to persist only analyzed frames

  Scenario: Conflicting generated detections are skipped
    Given a reviewed video has trackable analyzed frames
    And a later analyzed frame already has an overlapping face from another track
    And the Python forward tracker returns boxes on analyzed frames
    When the reviewer tracks the seed face forward
    Then only the non-conflicting generated face is saved

  Scenario: Unsupported conflict modes are rejected
    Given a reviewed video has trackable analyzed frames
    When track forward is requested with replace conflicts
    Then the track forward request is rejected

  Scenario: Reacquisition summary is returned
    Given a reviewed video has trackable analyzed frames
    And the Python forward tracker reports reacquisition and lost timeout
    When the reviewer tracks the seed face forward
    Then the response includes the reacquisition summary
