@HIPOS @Smoke
Feature: HIPOS login
  As a user
  I want to login to HIPOS
  So that I can access the main screen

  Scenario Outline: Successful login hides datactrl
    Given the HIPOS login page is open
    When I login with employee "<employee>" and password "<password>"
    Then the datactrl element should not exist
    When I select the last available day in the calendar
    Then the date_picker element should not exist
    When I click Yes on the confirmation messagebox
    When I click OK on the counting button
    When I click OK on the preview doc confirmation modal
    Then the main menu page should display all required elements

    Examples:
      | employee | password |
      | 1        | 000      |
      | -1       | 000000   |
