@navigation_rail
Feature: Global left navigation rail
  As a reviewer
  I want one fixed compact left icon rail to switch between Import and Review/Export
  So that the shell stays compact, the CPU/GPU mode stays visible, and no title row consumes space.

  Scenario: The home page has no standalone title row
    Given the home page is open
    Then there is no standalone "Video Anonymizer" title row

  Scenario: Import and Review/Export are icon-only navigation actions
    Given the home page is open
    Then the navigation rail shows an Import icon action with an accessible name
    And the navigation rail shows a Review and Export icon action with an accessible name

  Scenario: The active navigation action is clearly indicated
    Given the home page is open
    Then the Import navigation action is the active action
    When the reviewer activates the Review and Export navigation action
    Then the Review and Export navigation action is the active action
    And the review view is shown

  Scenario: The content area keeps a stable layout when the active view changes
    Given the home page is open
    When the reviewer switches between the Import and Review and Export views
    Then the content area keeps its fixed rail and does not shift position

  Scenario: The CPU/GPU mode indicator is pinned at the bottom of the rail in both views
    Given the home page is open with GPU runtime available
    Then the runtime mode indicator is placed at the bottom of the navigation rail
    When the reviewer activates the Review and Export navigation action
    Then the runtime mode indicator remains visible at the bottom of the navigation rail
