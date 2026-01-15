@Calculator @Demo
Feature: Windows Calculator
  As a user
  I want to use the Windows Calculator
  To perform mathematical operations

  @Smoke
  Scenario: Verify that the Calculator opens correctly
    Given the calculator is open
    When I verify the window title
    Then the title should contain "Calculadora" or "Calculator"

  Scenario: Verify that the Calculator window is visible and accessible
    Given the calculator is open
    When I verify the window visibility
    Then the window should be visible and enabled

  Scenario: Verify that the Calculator interface has interactive elements
    Given the calculator is open
    When I verify the interface elements
    Then there should be available UI elements

  Scenario: Display information about the Calculator window
    Given the calculator is open
    When I get the window information
    Then it should display the title, class, process ID and dimensions

  @Complex
  Scenario: Perform a simple addition
    Given the calculator is open
    When I clear the calculator
    And I perform the operation "2 + 3"
    Then the result should be "5"

  @Complex
  Scenario: Perform a subtraction
    Given the calculator is open
    When I clear the calculator
    And I perform the operation "10 - 4"
    Then the result should be "6"

  @Complex
  Scenario: Perform a multiplication
    Given the calculator is open
    When I clear the calculator
    And I perform the operation "7 * 8"
    Then the result should be "56"

  @Complex
  Scenario: Perform a division
    Given the calculator is open
    When I clear the calculator
    And I perform the operation "20 / 4"
    Then the result should be "5"

  @Complex
  Scenario: Perform sequential operations
    Given the calculator is open
    When I clear the calculator
    And I enter the number "5"
    And I press the add button
    And I enter the number "3"
    And I press the equals button
    Then the intermediate result should be "8"
    When I press the multiply button
    And I enter the number "2"
    And I press the equals button
    Then the final result should be "16"

  @Complex
  Scenario: Verify that all numeric buttons are available
    Given the calculator is open
    When I verify the availability of numeric buttons from 0 to 9
    Then all numeric buttons should be available

  @Complex
  Scenario: Verify that the Clear button clears the display correctly
    Given the calculator is open
    When I clear the calculator
    And I enter the numbers "123"
    And I verify the display value
    Then the display should contain "123"
    When I press the Clear button
    And I verify the display value
    Then the display should show "0"
