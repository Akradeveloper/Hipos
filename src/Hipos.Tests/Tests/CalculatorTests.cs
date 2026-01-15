using NUnit.Framework;
using Hipos.Framework.Core;
using Hipos.Framework.Utils;
using Hipos.Tests.PageObjects;
using Serilog;

namespace Hipos.Tests.Tests;

/// <summary>
/// Demo tests for Windows Calculator.
/// These tests demonstrate the framework usage with a real Windows application.
/// </summary>
[TestFixture]
[Category("Demo")]
[Description("Calculator Demo Tests")]
public class CalculatorTests : BaseTest
{
    private CalculatorPage? _calculatorPage;

    [SetUp]
    public void TestSetup()
    {
        _calculatorPage = new CalculatorPage(MainWindow!);
        ExtentReportManager.LogInfo($"Starting test: {TestContext.CurrentContext.Test.Name}");
    }

    [TearDown]
    public void TestTearDown()
    {
        var outcome = TestContext.CurrentContext.Result.Outcome.Status;
        var testName = TestContext.CurrentContext.Test.Name;
        
        if (outcome == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            ExtentReportManager.LogFail($"Test failed: {testName}");
        }
        else if (outcome == NUnit.Framework.Interfaces.TestStatus.Passed)
        {
            ExtentReportManager.LogPass($"Test passed: {testName}");
        }
    }

    [Test]
    [Category("Smoke")]
    [Description("Verifies that Calculator opens correctly")]
    public void VerifyCalculatorOpens()
    {
        Log.Information("Verifying that Calculator is open");
        ExtentReportManager.LogInfo("Verifying that Calculator is open");
        
        // Assert
        Assert.That(MainWindow, Is.Not.Null, "Calculator window should be available");
        Assert.That(MainWindow!.Title, Does.Contain("Calculadora").Or.Contains("Calculator"), 
            "Title should contain 'Calculadora' or 'Calculator'");
        
        Log.Information("✓ Calculator opened successfully - Title: {Title}", MainWindow.Title);
        ExtentReportManager.LogPass($"Calculator opened - Title: {MainWindow.Title}");
    }

    [Test]
    [Description("Verifies that Calculator window is visible and accessible")]
    public void VerifyCalculatorWindowVisible()
    {
        Log.Information("Verifying Calculator window visibility");
        ExtentReportManager.LogInfo("Verifying window visibility");
        
        // Verify window is accessible
        Assert.That(MainWindow, Is.Not.Null, "Window should exist");
        Assert.That(MainWindow!.IsOffscreen, Is.False, "Window should not be offscreen");
        Assert.That(MainWindow.IsEnabled, Is.True, "Window should be enabled");
        
        Log.Information("✓ Calculator window visible and accessible");
        ExtentReportManager.LogPass("Window visible and accessible");
    }

    [Test]
    [Description("Verifies that Calculator interface has interactive elements")]
    public void VerifyCalculatorUIElements()
    {
        Log.Information("Verifying UI elements in Calculator");
        ExtentReportManager.LogInfo("Verifying UI elements");
        
        // Verify window has child elements (buttons, display, etc.)
        var children = MainWindow!.FindAllChildren();
        var childCount = children.Length;
        
        Log.Information("UI elements found: {Count}", childCount);
        ExtentReportManager.LogInfo($"UI elements found: {childCount}");
        
        Assert.That(childCount, Is.GreaterThan(0), "Calculator should have UI elements");
        Assert.That(MainWindow.IsEnabled, Is.True, "Window should be enabled for interaction");
        
        Log.Information("✓ Calculator has {Count} UI elements", childCount);
        ExtentReportManager.LogPass($"Calculator has {childCount} UI elements");
    }

    [Test]
    [Description("Displays information about Calculator window")]
    public void DisplayCalculatorInfo()
    {
        Log.Information("Getting Calculator window information");
        ExtentReportManager.LogInfo("Getting window information");
        
        if (MainWindow != null)
        {
            var title = MainWindow.Title;
            var className = MainWindow.ClassName;
            var processId = MainWindow.Properties.ProcessId;
            var isEnabled = MainWindow.IsEnabled;
            var bounds = MainWindow.BoundingRectangle;
            
            Log.Information("🧮 Title: {Title}", title);
            Log.Information("🏷️ Class: {ClassName}", className);
            Log.Information("🔢 Process ID: {ProcessId}", processId);
            Log.Information("✓ Enabled: {IsEnabled}", isEnabled);
            Log.Information("📐 Position: X={X}, Y={Y}, Width={Width}, Height={Height}", 
                bounds.X, bounds.Y, bounds.Width, bounds.Height);
            
            ExtentReportManager.LogInfo($"Title: {title}");
            ExtentReportManager.LogInfo($"Class: {className}");
            ExtentReportManager.LogInfo($"Process ID: {processId}");
            ExtentReportManager.LogInfo($"Dimensions: {bounds.Width}x{bounds.Height}");
            
            TestContext.Out.WriteLine($"Calculator - {title}");
            TestContext.Out.WriteLine($"Class: {className}");
            TestContext.Out.WriteLine($"Process ID: {processId}");
            TestContext.Out.WriteLine($"Dimensions: {bounds.Width}x{bounds.Height}");
            
            Assert.Pass($"Calculator information captured correctly");
        }
    }

    // ============================================================
    // COMPLEX TESTS - Real Calculator interactions
    // ============================================================

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

    [Test]
    [Category("Complex")]
    [Description("Performs subtraction: 10 - 4 = 6")]
    public void PerformSubtraction()
    {
        Log.Information("Test: Subtraction 10 - 4");
        ExtentReportManager.LogInfo("Performing subtraction: 10 - 4");
        
        // Arrange
        _calculatorPage!.ClickClear();
        
        // Act
        _calculatorPage.PerformOperation(10, "-", 4);
        Thread.Sleep(500);
        
        var display = _calculatorPage.GetDisplayValue();
        Log.Information("Result obtained: {Display}", display);
        ExtentReportManager.LogInfo($"Result: {display}");
        
        // Assert
        Assert.That(display, Does.Contain("6"), 
            $"Result should contain '6', but got: '{display}'");
        
        Log.Information("✓ Correct subtraction: 10 - 4 = 6");
        ExtentReportManager.LogPass("Correct subtraction: 10 - 4 = 6");
    }

    [Test]
    [Category("Complex")]
    [Description("Performs multiplication: 7 * 8 = 56")]
    public void PerformMultiplication()
    {
        Log.Information("Test: Multiplication 7 * 8");
        ExtentReportManager.LogInfo("Performing multiplication: 7 * 8");
        
        // Arrange
        _calculatorPage!.ClickClear();
        
        // Act
        _calculatorPage.PerformOperation(7, "*", 8);
        Thread.Sleep(500);
        
        var display = _calculatorPage.GetDisplayValue();
        Log.Information("Result obtained: {Display}", display);
        ExtentReportManager.LogInfo($"Result: {display}");
        
        // Assert
        Assert.That(display, Does.Contain("56"), 
            $"Result should contain '56', but got: '{display}'");
        
        Log.Information("✓ Correct multiplication: 7 * 8 = 56");
        ExtentReportManager.LogPass("Correct multiplication: 7 * 8 = 56");
    }

    [Test]
    [Category("Complex")]
    [Description("Performs division: 20 / 4 = 5")]
    public void PerformDivision()
    {
        Log.Information("Test: Division 20 / 4");
        ExtentReportManager.LogInfo("Performing division: 20 / 4");
        
        // Arrange
        _calculatorPage!.ClickClear();
        
        // Act
        _calculatorPage.PerformOperation(20, "/", 4);
        Thread.Sleep(500);
        
        var display = _calculatorPage.GetDisplayValue();
        Log.Information("Result obtained: {Display}", display);
        ExtentReportManager.LogInfo($"Result: {display}");
        
        // Assert
        Assert.That(display, Does.Contain("5"), 
            $"Result should contain '5', but got: '{display}'");
        
        Log.Information("✓ Correct division: 20 / 4 = 5");
        ExtentReportManager.LogPass("Correct division: 20 / 4 = 5");
    }

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
        Assert.That(intermediateResult, Does.Contain("8"), "Intermediate result should be 8");
        Assert.That(finalResult, Does.Contain("16"), "Final result should be 16");
        
        Log.Information("✓ Correct sequential operations: (5 + 3) * 2 = 16");
        ExtentReportManager.LogPass("Correct sequential operations: (5 + 3) * 2 = 16");
    }

    [Test]
    [Category("Complex")]
    [Description("Verifies that all numeric buttons (0-9) are available")]
    public void VerifyAllNumericButtonsAvailable()
    {
        Log.Information("Test: Verifying availability of numeric buttons 0-9");
        ExtentReportManager.LogInfo("Verifying availability of buttons 0-9");
        
        var missingButtons = new List<int>();
        
        for (int i = 0; i <= 9; i++)
        {
            try
            {
                _calculatorPage!.ClickNumber(i);
                Log.Debug("✓ Button {Number} available", i);
            }
            catch (Exception ex)
            {
                Log.Warning("✗ Button {Number} not available: {Error}", i, ex.Message);
                ExtentReportManager.LogWarning($"Button {i} not available: {ex.Message}");
                missingButtons.Add(i);
            }
        }
        
        // Clear
        _calculatorPage!.ClickClear();
        
        // Assert
        Assert.That(missingButtons, Is.Empty, 
            $"The following buttons are not available: {string.Join(", ", missingButtons)}");
        
        Log.Information("✓ All numeric buttons (0-9) are available");
        ExtentReportManager.LogPass("All numeric buttons (0-9) are available");
    }

    [Test]
    [Category("Complex")]
    [Description("Verifies that Clear (C) button clears the display correctly")]
    public void VerifyClearButtonFunctionality()
    {
        Log.Information("Test: Clear button functionality");
        ExtentReportManager.LogInfo("Verifying Clear button functionality");
        
        // Arrange - Enter some numbers
        _calculatorPage!.ClickNumber(1);
        _calculatorPage.ClickNumber(2);
        _calculatorPage.ClickNumber(3);
        Thread.Sleep(300);
        
        var beforeClear = _calculatorPage.GetDisplayValue();
        Log.Information("Display before Clear: {Display}", beforeClear);
        ExtentReportManager.LogInfo($"Display before Clear: {beforeClear}");
        
        // Act - Press Clear
        _calculatorPage.ClickClear();
        Thread.Sleep(300);
        
        var afterClear = _calculatorPage.GetDisplayValue();
        Log.Information("Display after Clear: {Display}", afterClear);
        ExtentReportManager.LogInfo($"Display after Clear: {afterClear}");
        
        // Assert
        Assert.That(beforeClear, Does.Contain("123").Or.Contains("1"), 
            "Should have numbers before Clear");
        Assert.That(afterClear, Does.Contain("0"), 
            "Display should show '0' after Clear");
        
        Log.Information("✓ Clear button works correctly");
        ExtentReportManager.LogPass("Clear button works correctly");
    }
}
