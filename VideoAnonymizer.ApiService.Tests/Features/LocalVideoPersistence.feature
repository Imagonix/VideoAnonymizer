@api_persistence
Feature: Local video persistence
  As a reviewer working in the local application
  I want uploaded videos, settings, reviewed objects and file paths to be persisted
  So that I can leave and return without losing review work.

  Scenario: Uploaded video appears in the saved video list
    Given a reviewer uploads "street-interview.mp4" for object detection every 250 ms
    When the reviewer opens the saved videos list
    Then the uploaded video is listed with the default anonymization settings
    And an analysis job is published for the stored video

  Scenario: Review settings are remembered for an imported video
    Given an imported video named "persisted-settings.mp4"
    When the reviewer changes the blur size to 180 percent and the time buffer to 650 ms
    Then the saved videos list shows blur size 180 percent and time buffer 650 ms
    And the database stores blur size 180 percent and time buffer 650 ms

  Scenario: Object edits are saved and reloaded with the analyzed video
    Given a reviewed video has one detected face
    When the reviewer adds another face, moves it, deselects it, and deletes the original face
    Then reopening the analyzed video shows only the edited face

  Scenario: Bulk object edits reject faces from another video
    Given a reviewed video and another video both have detected faces
    When a bulk edit includes a face from the other video
    Then no face in the reviewed video is partially changed

  Scenario: Bulk object edits save all reviewed video faces together
    Given a reviewed video and another video both have detected faces
    When the reviewer bulk edits only faces from the reviewed video
    Then all reviewed video faces are saved together

  Scenario: Object endpoints protect route identity
    Given a reviewed video has one detected face
    When object changes use mismatched route and body identifiers
    Then the object changes are rejected

  Scenario: Persisted original and anonymized files are streamed back
    Given a saved video has original and anonymized file paths
    When the viewer requests the original and anonymized files
    Then the API streams both persisted video files

  Scenario: Local SQLite data survives service restart
    Given a file-backed local database contains a reviewed video
    When the API services are recreated
    Then the saved video, settings, frame and face are still available
