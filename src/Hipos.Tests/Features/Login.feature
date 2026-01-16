# language: en
@HIPOS @Smoke
Feature: HIPOS login
  As a user
  I want to login to HIPOS
  So that I can access the main screen

  Scenario: Successful login hides datactrl
    Given the HIPOS login page is open
    When I login with employee "-1" and password "000000"
    Then the datactrl element should not exist
