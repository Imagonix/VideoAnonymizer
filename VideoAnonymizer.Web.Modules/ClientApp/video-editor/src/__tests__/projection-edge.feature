Feature: Projected region edge exit without snapback
  As a reviewer watching a tracked box leave the video
  I want the preview to keep moving with the projection model
  So that boxes never jump back to an earlier stored position before disappearing.

  Scenario: Post-buffer extrapolation continues without historical snapback
    Given a consecutive track segment moving right across two analyzed frames
    When the preview projects during the post-buffer
    Then the box keeps moving past the last stored position
    And the box never reappears at the last stored position after the buffer ends

  Scenario: Partial exit keeps raw size while the visible intersection shrinks
    Given a consecutive track segment exiting each video edge
    When the preview projects a partially outside box with known video dimensions
    Then the raw boxes retain equal width or height and continue center velocity
    And their visible intersections are smaller partial clips

  Scenario: Fully outside projected boxes disappear
    Given a consecutive track segment that fully leaves the left edge during post-buffer
    When the preview projects after the box is completely outside
    Then no preview region is returned for that track

  Scenario: Buffer ending before a complete exit disappears at the final projected position
    Given a consecutive track segment that stays partly inside through its post-buffer
    When the preview projects at the buffer end and just after
    Then the final in-buffer region is the continued projection
    And no region is returned after the buffer ends

  Scenario: Default Interpolate bridges a missing analyzed-frame gap
    Given a track with a missing analyzed frame between two segments
    When the preview projects inside the gap with no buffer
    Then exactly one interpolated gap region is returned for that track

  Scenario: Non-unit FrameIndex steps form one segment without a history trail
    Given an uninterrupted track sampled at FrameIndex 0, 15, and 30
    When the preview projects at a time between those samples
    Then exactly one preview region is returned for that track

  Scenario: A missing track at FrameIndex 15 is still bridged by default Interpolate
    Given a track present at FrameIndex 0 and 30 but missing at 15
    When the preview projects at the middle empty analyzed frame with no buffer
    Then exactly one interpolated gap region is returned for that track

  Scenario: Explicit UseBuffers leaves the gap uncovered when buffers are short
    Given a track present at FrameIndex 0 and 30 but missing at 15 with UseBuffers gap mode
    When the preview projects at the middle empty analyzed frame with no buffer
    Then no preview region is returned for that track
