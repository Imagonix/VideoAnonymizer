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

  Scenario: Object loss completes with a reacquisition summary
    Given a reviewed video has trackable analyzed frames
    And the Python forward tracker reports reacquisition and lost timeout
    When the reviewer tracks the seed face forward
    Then the response includes the reacquisition summary

  Scenario: Python tracking errors are surfaced
    Given the Python tracking stream reports an error
    When the tracking stream is read
    Then the tracking stream fails with the Python error

  Scenario: Completed tracking streams are accepted
    Given the Python tracking stream completes normally
    When the tracking stream is read
    Then the tracking stream returns its completion metadata

  Scenario: Truncated tracking streams are rejected
    Given the Python tracking stream ends without completion
    When the tracking stream is read
    Then the tracking stream fails because completion is missing

  Scenario: Duplicate tracking completion is rejected
    Given the Python tracking stream completes twice
    When the tracking stream is read
    Then the tracking stream fails because completion is duplicated
