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

  Scenario: Track-level overrides round-trip and stay nullable
    Given a reviewed video has two detected faces
    When the reviewer saves the first face with custom track settings
    Then the first face keeps its custom track settings when reopening the video
    And the second face still has no track overrides

  Scenario: Consecutive segments group adjacent same-track occurrences
    Given a reviewed video has a track with occurrences in the first, second, and fourth frames and an untracked occurrence in the fifth frame
    When the reviewer resolves the segment of each occurrence
    Then the segment of the first occurrence spans the second occurrence
    And the segment of the second occurrence is unchanged by the missing third frame
    And the fourth occurrence forms a single-occurrence segment after the gap
    And the untracked occurrence forms a single-occurrence segment

  Scenario: Joining runs keeps only the outer boundary overrides
    Given a segment now spans previously separate runs
    When the reviewer normalizes the segment boundaries
    Then only the first occurrence stores a pre-buffer override
    And only the last occurrence stores a post-buffer override

  Scenario: Effective blur size uses the occurrence override with the global default
    Given a reviewer's video has a global blur size of 120 percent
    And one face overrides its blur size while another face has no override
    When the effective blur sizes are resolved
    Then the overriding face resolves to 150 percent and the other face resolves to 120 percent

  Scenario: Saved video list includes derived statuses
    Given videos exist in imported analyzed and exported states
    When the reviewer opens the saved videos list
    Then the listed videos show statuses "Imported", "Ready to review", and "Exported"

  Scenario: Working copy deletion removes database and standalone files
    Given a saved video has original and anonymized file paths under standalone storage
    And the video has editor action history
    When the reviewer deletes the working copy
    Then the video is removed from the database
    And the standalone source and anonymized files are deleted
    And the delete result reports no file warnings

  Scenario: Working copy deletion removes files under hosted storage layout
    Given a saved video has original and anonymized file paths under hosted storage
    When the reviewer deletes the working copy
    Then the video is removed from the database
    And the hosted source and anonymized files are deleted

  Scenario: Working copy deletion skips unsafe paths and reports warnings
    Given a saved video points its source path outside managed storage
    When the reviewer deletes the working copy
    Then the video is removed from the database
    And the delete result reports a skipped source file warning

  Scenario: Upload records the server UTC upload time
    Given a reviewer uploads "timestamped.mp4" for object detection every 250 ms
    When the reviewer opens the saved videos list
    Then the listed video carries a server UTC upload time close to now

  Scenario: Saved videos list orders newest upload first with stable tie-breaking
    Given imported videos exist with known upload times
    When the reviewer opens the saved videos list
    Then the listed videos are ordered newest upload first
    And upload time ties are broken by file name deterministically
