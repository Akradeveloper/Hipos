using FlaUI.Core.AutomationElements;
using Hipos.Tests.PageObjects;
using TechTalk.SpecFlow;
using NUnit.Framework;
using Serilog;

namespace Hipos.Tests.StepDefinitions;

/// <summary>
/// Step definitions for Calculator scenarios.
/// </summary>
[Binding]
public class CalculatorStepDefinitions : BaseStepDefinitions
{
    private CalculatorPage? _calculatorPage;
    private string? _displayValue;
    private string? _intermediateResult;
    private string? _finalResult;
    private List<int>? _missingButtons;

    [Given("the calculator is open")]
    public void GivenTheCalculatorIsOpen()
    {
        LogInfo("Verifying that the calculator is open");
        
        Assert.That(MainWindow, Is.Not.Null, "Calculator window should be available");
        
        // Ensure window is in foreground
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        _calculatorPage = new CalculatorPage(MainWindow!);
        LogPass("Calculator opened and ready to use");
    }

    [When("I verify the window title")]
    public void WhenIVerifyTheWindowTitle()
    {
        LogInfo("Verifying window title");
    }

    [Then("the title should contain \"(.*)\" or \"(.*)\"")]
    public void ThenTheTitleShouldContainOr(string option1, string option2)
    {
        Assert.That(MainWindow, Is.Not.Null, "Window should exist");
        Assert.That(MainWindow!.Title, Does.Contain(option1).Or.Contains(option2),
            $"Title should contain '{option1}' or '{option2}'");
        
        LogPass($"Title verified: {MainWindow.Title}");
    }

    [When("I verify the window visibility")]
    public void WhenIVerifyTheWindowVisibility()
    {
        LogInfo("Verifying window visibility");
    }

    [Then("the window should be visible and enabled")]
    public void ThenTheWindowShouldBeVisibleAndEnabled()
    {
        Assert.That(MainWindow, Is.Not.Null, "Window should exist");
        Assert.That(MainWindow!.IsOffscreen, Is.False, "Window should not be offscreen");
        Assert.That(MainWindow.IsEnabled, Is.True, "Window should be enabled");
        
        LogPass("Window visible and accessible");
    }

    [When("I verify the interface elements")]
    public void WhenIVerifyTheInterfaceElements()
    {
        LogInfo("Verifying UI elements");
    }

    [Then("there should be available UI elements")]
    public void ThenThereShouldBeAvailableUIElements()
    {
        var children = MainWindow!.FindAllChildren();
        var childCount = children.Length;
        
        LogInfo($"UI elements found: {childCount}");
        
        Assert.That(childCount, Is.GreaterThan(0), "Calculator should have UI elements");
        Assert.That(MainWindow.IsEnabled, Is.True, "Window should be enabled for interaction");
        
        LogPass($"Calculator has {childCount} UI elements");
    }

    [When("I get the window information")]
    public void WhenIGetTheWindowInformation()
    {
        LogInfo("Getting window information");
    }

    [Then("it should display the title, class, process ID and dimensions")]
    public void ThenItShouldDisplayTheTitleClassProcessIDAndDimensions()
    {
        var title = MainWindow!.Title;
        var className = MainWindow.ClassName;
        var processId = MainWindow.Properties.ProcessId;
        var bounds = MainWindow.BoundingRectangle;
        
        LogInfo($"Title: {title}");
        LogInfo($"Class: {className}");
        LogInfo($"Process ID: {processId}");
        LogInfo($"Dimensions: {bounds.Width}x{bounds.Height}");
        
        LogPass("Calculator information captured correctly");
    }

    [When("I clear the calculator")]
    public void WhenIClearTheCalculator()
    {
        // Ensure window is in foreground before interacting
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        LogInfo("Clearing the calculator");
        _calculatorPage!.ClickClear();
        Thread.Sleep(200);
    }

    [When("I perform the operation \"(.*)\"")]
    public void WhenIPerformTheOperation(string operation)
    {
        // Ensure window is in foreground before interacting
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(300);
        
        // Parse operation: "2 + 3", "10 - 4", etc.
        var parts = operation.Split(' ');
        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid operation format: {operation}");
        }
        
        var num1 = int.Parse(parts[0]);
        var operatorSymbol = parts[1];
        var num2 = int.Parse(parts[2]);
        
        LogInfo($"Performing operation: {num1} {operatorSymbol} {num2}");
        _calculatorPage!.PerformOperation(num1, operatorSymbol, num2);
        
        // Wait longer for calculator to process and display result
        Thread.Sleep(1000);
        
        // Ensure window is in foreground again before reading result
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
    }

    [Then("the result should be \"(.*)\"")]
    public void ThenTheResultShouldBe(string expectedResult)
    {
        // Ensure window is in foreground before reading
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        // Try to read display multiple times with waits
        _displayValue = null;
        for (int i = 0; i < 3; i++)
        {
            _displayValue = _calculatorPage!.GetDisplayValue();
            
            // Extract number from text if it contains "La pantalla muestra X" or "Display is X"
            if (!string.IsNullOrEmpty(_displayValue))
            {
                var numberMatch = System.Text.RegularExpressions.Regex.Match(_displayValue, @"\d+");
                if (numberMatch.Success)
                {
                    _displayValue = numberMatch.Value;
                }
                
                if (_displayValue.Contains(expectedResult))
                {
                    break;
                }
            }
            Thread.Sleep(300);
        }
        
        LogInfo($"Result obtained: {_displayValue}");
        
        Assert.That(_displayValue, Is.Not.Null, "Could not get display value");
        Assert.That(_displayValue, Does.Contain(expectedResult),
            $"Result should contain '{expectedResult}', but got: '{_displayValue}'");
        
        LogPass($"Correct operation: result = {expectedResult}");
    }

    [When("I enter the number \"(.*)\"")]
    public void WhenIEnterTheNumber(string number)
    {
        // Ensure window is in foreground
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        var num = int.Parse(number);
        LogInfo($"Entering number: {num}");
        _calculatorPage!.ClickNumber(num);
    }

    [When("I press the add button")]
    public void WhenIPressTheAddButton()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo("Pressing add button");
        _calculatorPage!.ClickPlus();
    }

    [When("I press the multiply button")]
    public void WhenIPressTheMultiplyButton()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo("Pressing multiply button");
        _calculatorPage!.ClickMultiply();
    }

    [When("I press the equals button")]
    public void WhenIPressTheEqualsButton()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo("Pressing equals button");
        _calculatorPage!.ClickEquals();
        Thread.Sleep(800); // Wait for result calculation
    }

    [Then("the intermediate result should be \"(.*)\"")]
    public void ThenTheIntermediateResultShouldBe(string expectedResult)
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        _intermediateResult = _calculatorPage!.GetDisplayValue();
        
        // Extract number from text if necessary
        if (!string.IsNullOrEmpty(_intermediateResult))
        {
            var numberMatch = System.Text.RegularExpressions.Regex.Match(_intermediateResult, @"\d+");
            if (numberMatch.Success)
            {
                _intermediateResult = numberMatch.Value;
            }
        }
        
        LogInfo($"Intermediate result obtained: {_intermediateResult}");
        
        Assert.That(_intermediateResult, Does.Contain(expectedResult),
            $"Intermediate result should contain '{expectedResult}', but got: '{_intermediateResult}'");
        
        LogPass($"Correct intermediate result: {expectedResult}");
    }

    [Then("the final result should be \"(.*)\"")]
    public void ThenTheFinalResultShouldBe(string expectedResult)
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        _finalResult = _calculatorPage!.GetDisplayValue();
        
        // Extract number from text if necessary
        if (!string.IsNullOrEmpty(_finalResult))
        {
            var numberMatch = System.Text.RegularExpressions.Regex.Match(_finalResult, @"\d+");
            if (numberMatch.Success)
            {
                _finalResult = numberMatch.Value;
            }
        }
        
        LogInfo($"Final result obtained: {_finalResult}");
        
        Assert.That(_finalResult, Does.Contain(expectedResult),
            $"Final result should contain '{expectedResult}', but got: '{_finalResult}'");
        
        LogPass($"Correct final result: {expectedResult}");
    }

    [When("I verify the availability of numeric buttons from (\\d+) to (\\d+)")]
    public void WhenIVerifyTheAvailabilityOfNumericButtonsFromTo(int from, int to)
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(200);
        
        LogInfo($"Verifying availability of numeric buttons from {from} to {to}");
        _missingButtons = new List<int>();
        
        for (int i = from; i <= to; i++)
        {
            try
            {
                _calculatorPage!.ClickNumber(i);
                Log.Debug("✓ Button {Number} available", i);
            }
            catch (Exception ex)
            {
                LogWarning($"Button {i} not available: {ex.Message}");
                _missingButtons.Add(i);
            }
        }
        
        // Clear after verification
        _calculatorPage!.ClickClear();
    }

    [Then("all numeric buttons should be available")]
    public void ThenAllNumericButtonsShouldBeAvailable()
    {
        Assert.That(_missingButtons, Is.Not.Null, "Button verification should have been executed");
        Assert.That(_missingButtons!, Is.Empty,
            $"The following buttons are not available: {string.Join(", ", _missingButtons)}");
        
        LogPass("All numeric buttons (0-9) are available");
    }

    [When("I enter the numbers \"(.*)\"")]
    public void WhenIEnterTheNumbers(string numbers)
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo($"Entering numbers: {numbers}");
        foreach (var digit in numbers)
        {
            if (char.IsDigit(digit))
            {
                _calculatorPage!.ClickNumber(int.Parse(digit.ToString()));
            }
        }
        Thread.Sleep(300);
    }

    [When("I verify the display value")]
    public void WhenIVerifyTheDisplayValue()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        _displayValue = _calculatorPage!.GetDisplayValue();
        
        // Extract number from text if necessary
        if (!string.IsNullOrEmpty(_displayValue))
        {
            var numberMatch = System.Text.RegularExpressions.Regex.Match(_displayValue, @"\d+");
            if (numberMatch.Success)
            {
                _displayValue = numberMatch.Value;
            }
        }
        
        LogInfo($"Display value: {_displayValue}");
    }

    [Then("the display should contain \"(.*)\"")]
    public void ThenTheDisplayShouldContain(string expectedValue)
    {
        Assert.That(_displayValue, Is.Not.Null, "Display value should have been obtained");
        Assert.That(_displayValue, Does.Contain(expectedValue),
            $"Display should contain '{expectedValue}', but shows: '{_displayValue}'");
        
        LogPass($"Display contains '{expectedValue}'");
    }

    [When("I press the Clear button")]
    public void WhenIPressTheClearButton()
    {
        AppLauncher?.EnsureWindowIsInForeground();
        Thread.Sleep(100);
        
        LogInfo("Pressing Clear button");
        _calculatorPage!.ClickClear();
        Thread.Sleep(300);
    }

    [Then("the display should show \"(.*)\"")]
    public void ThenTheDisplayShouldShow(string expectedValue)
    {
        Assert.That(_displayValue, Is.Not.Null, "Display value should have been obtained");
        Assert.That(_displayValue, Does.Contain(expectedValue),
            $"Display should show '{expectedValue}', but shows: '{_displayValue}'");
        
        LogPass($"Display shows '{expectedValue}'");
    }
}
