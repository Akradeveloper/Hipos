---
sidebar_position: 6
---

# Test Examples

Real automated test examples against Windows Calculator.

## Basic Test - Open Verification

```csharp
[Test]
[Category("Demo")]
[Description("Verifies calculator opens correctly")]
public void VerifyCalculatorOpens()
{
    Log.Information("Verifying that Calculator is open");
    
    // Assert
    Assert.That(MainWindow, Is.Not.Null, 
        "Calculator window should be available");
    Assert.That(MainWindow!.Title, 
        Does.Contain("Calculadora").Or.Contains("Calculator"), 
        "Title should contain 'Calculadora' or 'Calculator'");
    
    Log.Information("✓ Calculator opened successfully - Title: {Title}", 
        MainWindow.Title);
}
```

## Complex Test - Simple Addition

```csharp
[Test]
[Category("Complex")]
[Description("Performs simple addition: 2 + 3 = 5")]
public void PerformSimpleAddition()
{
    Log.Information("Test: Simple addition 2 + 3");
    ExtentReportManager.LogInfo("Performing addition: 2 + 3");
    
    // Arrange
    _calculatorPage!.ClickClear();
    
    // Act
    _calculatorPage.PerformOperation(2, "+", 3);
    Thread.Sleep(500);
    
    var display = _calculatorPage.GetDisplayValue();
    Log.Information("Result obtained: {Display}", display);
    ExtentReportManager.LogInfo($"Result: {display}");
    
    // Assert
    Assert.That(display, Does.Contain("5"), 
        $"Result should contain '5', but got: '{display}'");
    
    Log.Information("✓ Correct addition: 2 + 3 = 5");
    ExtentReportManager.LogPass("Correct addition: 2 + 3 = 5");
}
```

## Advanced Test - Sequential Operations

```csharp
[Test]
[Category("Complex")]
[Description("Performs sequential operations: (5 + 3) * 2")]
public void PerformSequentialOperations()
{
    Log.Information("Test: Sequential operations (5 + 3) * 2");
    ExtentReportManager.LogInfo("Performing sequential operations: (5 + 3) * 2");
    
    // Arrange
    _calculatorPage!.ClickClear();
    
    // Act - First operation: 5 + 3 = 8
    _calculatorPage.ClickNumber(5);
    _calculatorPage.ClickPlus();
    _calculatorPage.ClickNumber(3);
    _calculatorPage.ClickEquals();
    Thread.Sleep(500);
    
    var intermediateResult = _calculatorPage.GetDisplayValue();
    Log.Information("Intermediate result (5 + 3): {Result}", intermediateResult);
    ExtentReportManager.LogInfo($"Intermediate result: {intermediateResult}");
    
    // Second operation: * 2 = 16
    _calculatorPage.ClickMultiply();
    _calculatorPage.ClickNumber(2);
    _calculatorPage.ClickEquals();
    Thread.Sleep(500);
    
    var finalResult = _calculatorPage.GetDisplayValue();
    Log.Information("Final result (* 2): {Result}", finalResult);
    ExtentReportManager.LogInfo($"Final result: {finalResult}");
    
    // Assert
    Assert.That(intermediateResult, Does.Contain("8"), 
        "Intermediate result should be 8");
    Assert.That(finalResult, Does.Contain("16"), 
        "Final result should be 16");
    
    Log.Information("✓ Correct sequential operations: (5 + 3) * 2 = 16");
    ExtentReportManager.LogPass("Correct sequential operations: (5 + 3) * 2 = 16");
}
```

## SpecFlow BDD Example

```gherkin
@Calculator @Demo
Feature: Windows Calculator
  As a user
  I want to use the Windows Calculator
  To perform mathematical operations

  @Complex
  Scenario: Perform a simple addition
    Given the calculator is open
    When I clear the calculator
    And I perform the operation "2 + 3"
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
```

### Step Definitions

```csharp
[Binding]
public class CalculatorStepDefinitions : BaseStepDefinitions
{
    private CalculatorPage? _calculatorPage;

    [Given("the calculator is open")]
    public void GivenTheCalculatorIsOpen()
    {
        LogInfo("Verifying that the calculator is open");
        Assert.That(MainWindow, Is.Not.Null, "Calculator window should be available");
        _calculatorPage = new CalculatorPage(MainWindow!);
        LogPass("Calculator opened and ready to use");
    }

    [When("I perform the operation \"(.*)\"")]
    public void WhenIPerformTheOperation(string operation)
    {
        var parts = operation.Split(' ');
        var num1 = int.Parse(parts[0]);
        var operatorSymbol = parts[1];
        var num2 = int.Parse(parts[2]);
        
        LogInfo($"Performing operation: {num1} {operatorSymbol} {num2}");
        _calculatorPage!.PerformOperation(num1, operatorSymbol, num2);
        Thread.Sleep(1000);
    }

    [Then("the result should be \"(.*)\"")]
    public void ThenTheResultShouldBe(string expectedResult)
    {
        var displayValue = _calculatorPage!.GetDisplayValue();
        LogInfo($"Result obtained: {displayValue}");
        
        Assert.That(displayValue, Does.Contain(expectedResult),
            $"Result should contain '{expectedResult}', but got: '{displayValue}'");
        
        LogPass($"Correct operation: result = {expectedResult}");
    }
}
```

## Page Object - CalculatorPage

```csharp
public class CalculatorPage : BasePage
{
    public CalculatorPage(Window window) : base(window)
    {
        Log.Information("Initializing CalculatorPage for Windows Calculator");
    }

    /// <summary>
    /// Clicks a numeric button (0-9).
    /// </summary>
    public void ClickNumber(int number)
    {
        if (number < 0 || number > 9)
            throw new ArgumentException("Number must be between 0 and 9");

        Log.Information("Clicking on number: {Number}", number);
        
        // Spanish and English names
        var numberNames = new Dictionary<int, string[]>
        {
            {0, new[] {"Cero", "Zero"}},
            {1, new[] {"Uno", "One"}},
            {2, new[] {"Dos", "Two"}},
            // ... rest of numbers
        };

        var button = FindButtonByNames(numberNames[number]);
        button?.Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Performs a complete operation: num1 operator num2 = result.
    /// </summary>
    public void PerformOperation(int num1, string operation, int num2)
    {
        Log.Information("Performing operation: {Num1} {Op} {Num2}", 
            num1, operation, num2);
        
        ClickClear();
        EnterNumber(num1);
        
        switch (operation)
        {
            case "+": ClickPlus(); break;
            case "-": ClickMinus(); break;
            case "*": ClickMultiply(); break;
            case "/": ClickDivide(); break;
        }
        
        EnterNumber(num2);
        ClickEquals();
    }
}
```

## Arrange-Act-Assert Pattern

```csharp
[Test]
public void ExampleTest()
{
    // Arrange (Setup)
    // - Configure initial state
    // - Create Page Objects
    // - Prepare test data
    _calculatorPage.ClickClear();
    var expectedResult = "10";
    
    // Act (Execute)
    // - Execute the action to test
    // - Interact with the UI
    _calculatorPage.PerformOperation(5, "+", 5);
    var actualResult = _calculatorPage.GetDisplayValue();
    
    // Assert (Verify)
    // - Verify expected result
    // - Compare values
    Assert.That(actualResult, Does.Contain(expectedResult));
}
```

## Configuration for Different Apps

### Windows Calculator

```json
{
  "AppPath": "calc.exe",
  "DefaultTimeout": 15000,
  "Reporting": {
    "CucumberJsonPath": "reports/cucumber.json",
    "IncludeScreenshots": true
  }
}
```

### Notepad

```json
{
  "AppPath": "notepad.exe",
  "DefaultTimeout": 5000,
  "Reporting": {
    "CucumberJsonPath": "reports/cucumber.json",
    "IncludeScreenshots": false
  }
}
```

### Custom Application

```json
{
  "AppPath": "C:\\MyProject\\bin\\Debug\\MyApp.exe",
  "DefaultTimeout": 10000,
  "Reporting": {
    "CucumberJsonPath": "reports/cucumber.json",
    "IncludeScreenshots": true
  }
}
```

## Test Execution

```bash
# All tests
dotnet test

# Only basic tests
dotnet test --filter "Category=Demo"

# Only complex tests
dotnet test --filter "Category=Complex"

# Specific test
dotnet test --filter "FullyQualifiedName~PerformSimpleAddition"

# With detailed logging
dotnet test --logger "console;verbosity=detailed"

# Only SpecFlow tests
dotnet test --filter "TestCategory=Calculator"
```

## View Results

```bash
# Open ExtentReports HTML
start src\Hipos.Tests\bin\Debug\net8.0-windows\reports\extent-report.html

# View logs
cat src\Hipos.Tests\bin\Debug\net8.0-windows\logs\test-*.log

# Check Cucumber JSON for Xray
cat src\Hipos.Tests\bin\Debug\net8.0-windows\reports\cucumber.json
```

## Project Metrics

**Current Status:**
- ✅ 22 tests implemented (11 NUnit + 11 SpecFlow)
- ✅ 100% success rate
- ✅ ~90s execution time
- ✅ Both traditional and BDD approaches

**Supported Mathematical Operations:**
- Addition: `2 + 3 = 5`
- Subtraction: `10 - 4 = 6`
- Multiplication: `7 * 8 = 56`
- Division: `20 / 4 = 5`
- Sequential: `(5 + 3) * 2 = 16`

## Report Integration

### ExtentReports

Automatically generated HTML report with:
- Test results with pass/fail status
- Step-by-step logs
- Screenshots on failures
- Execution timeline
- Dark theme

### Cucumber JSON / Jira Xray

Compatible JSON for test management:
- Feature and scenario details
- Step execution results
- Tag mapping for Jira
- Duration tracking
- Error messages

Upload to Xray:
```bash
curl -H "Content-Type: application/json" \
     -X POST \
     -H "Authorization: Bearer YOUR_TOKEN" \
     --data @reports/cucumber.json \
     https://xray.cloud.getxray.app/api/v2/import/execution/cucumber
```

## Next Steps

1. Add tests with decimals
2. Implement scientific function tests
3. Validate memory operations (M+, M-, MR)
4. Validation tests (division by zero)
5. Performance tests
6. Cross-language UI tests (Spanish/English Calculator)

For more information, see other documentation sections:
- [Framework Guide](./framework-guide.md) - Create your own tests
- [Reporting & Logging](./reporting-logging.md) - Configure reports
- [CI/CD](./ci-cd.md) - Automate in pipelines
