---
sidebar_position: 3
---

# Architecture

This page explains the Hipos framework architecture, its main components, and how they interact.

## High-Level Diagram

```mermaid
graph TB
    subgraph Tests[Test Layer]
        UT[Unit Tests - NUnit<br/>11 traditional tests]
        BDD[BDD Tests - SpecFlow<br/>11 Gherkin scenarios]
    end
    
    subgraph PageObjects[Page Object Layer]
        CP[CalculatorPage<br/>Click nums operators]
        BP[BasePage<br/>FindElement waits]
    end
    
    subgraph Framework[Framework Layer]
        AL[AppLauncher<br/>Hybrid Search]
        BT[BaseTest<br/>OneTimeSetUp TearDown]
        TH[TestHooks<br/>SpecFlow Lifecycle]
        WH[WaitHelper<br/>Explicit waits]
        EW[ElementWrapper<br/>Simplified API]
        SH[ScreenshotHelper<br/>Auto capture]
        CM[ConfigManager<br/>appsettings env]
        RP[RetryPolicy<br/>Transient errors]
        CJ[CucumberJsonReportGenerator<br/>Xray Integration]
    end
    
    subgraph External[External Libraries]
        FlaUI[FlaUI UIA3<br/>v4.0]
        NUnit[NUnit<br/>v4.2]
        SpecFlow[SpecFlow<br/>v4.0]
        ExtentReports[ExtentReports<br/>v5.0]
        Serilog[Serilog<br/>v3.1]
    end
    
    subgraph AUT[Applications Under Test]
        CalcApp[Windows Calculator<br/>calc.exe]
        NotepadApp[Notepad<br/>notepad.exe]
        CustomApp[Your App<br/>customizable]
    end
    
    UT --> CP
    BDD --> CP
    CP --> BP
    BP --> WH
    BP --> EW
    UT --> BT
    BDD --> TH
    BT --> AL
    TH --> AL
    BT --> SH
    BT --> CM
    TH --> CM
    TH --> CJ
    AL -->|"Strict (5s)"| FlaUI
    AL -->|"Relaxed (10s)"| FlaUI
    WH --> FlaUI
    EW --> FlaUI
    BT --> NUnit
    TH --> SpecFlow
    TH --> ExtentReports
    TH --> Serilog
    EW --> RP
    AL --> CalcApp
    AL --> NotepadApp
    AL --> CustomApp
```

## Framework Layers

### 1. Test Layer

**Responsibility:** Define test cases and assertions.

**Components:**
- `CalculatorTests.cs` - 11 traditional NUnit tests against Windows Calculator
  - 4 basic tests (`Category=Demo`): Open and UI verification
  - 7 complex tests (`Category=Complex`): Real mathematical operations
- `Calculadora.feature` - 11 BDD scenarios in Gherkin format
  - Uses SpecFlow step definitions
  - Generates Cucumber JSON for Jira/Xray

**Features:**
- Inherits from `BaseTest` or uses `BaseStepDefinitions` for automatic hooks
- Uses `CalculatorPage` to interact with buttons and display
- Includes ExtentReports logging
- Implements Arrange-Act-Assert (AAA) pattern
- Complex tests perform real operations: addition, subtraction, multiplication, division, sequential operations

**Example (NUnit):**

```csharp
[Test]
[Category("Smoke")]
[Description("Verifies calculator opens correctly")]
public void VerifyCalculatorOpens()
{
    // Arrange
    var calculatorPage = new CalculatorPage(MainWindow!);
    
    // Act & Assert
    Assert.That(MainWindow.Title, Does.Contain("Calculator").Or.Contains("Calculadora"));
    ExtentReportManager.LogPass("Calculator opened successfully");
}
```

**Example (SpecFlow):**

```gherkin
@Calculator @Smoke
Scenario: Verify that the Calculator opens correctly
  Given the calculator is open
  When I verify the window title
  Then the title should contain "Calculator" or "Calculadora"
```

### 2. Page Object Layer

**Responsibility:** Encapsulate UI elements and actions for each page/window.

**Components:**
- `BasePage.cs` - Base class with common functionality
- `CalculatorPage.cs` - Page for calculator operations

**Page Object Pattern:**

```mermaid
classDiagram
    class BasePage {
        #Window Window
        #int DefaultTimeout
        +FindElement(automationId) ElementWrapper
        +ElementExists(automationId) bool
        +WaitForElementVisible(automationId) bool
    }
    
    class CalculatorPage {
        +ClickNumber(num) void
        +ClickPlus() void
        +ClickMinus() void
        +ClickMultiply() void
        +ClickDivide() void
        +ClickEquals() void
        +ClickClear() void
        +GetDisplayValue() string
        +PerformOperation(num1, op, num2) void
    }
    
    BasePage <|-- CalculatorPage
```

**Advantages:**
- ✅ Reduces code duplication
- ✅ Facilitates maintenance (UI changes only affect Page Object)
- ✅ Improves test readability
- ✅ Allows reuse of common actions

### 3. Framework Layer (Core)

**Responsibility:** Provide base functionality for automation.

#### AppLauncher (Singleton)
- Launches and closes applications
- Maintains reference to main window
- Handles timeouts and startup errors

```mermaid
sequenceDiagram
    participant Test
    participant AppLauncher
    participant FlaUI
    participant App
    
    Test->>AppLauncher: LaunchApp(exePath)
    AppLauncher->>FlaUI: Application.Launch()
    FlaUI->>App: Start process
    App-->>FlaUI: Process started
    AppLauncher->>FlaUI: GetMainWindow()
    FlaUI-->>AppLauncher: Window reference
    AppLauncher-->>Test: Window ready
```

#### BaseTest
- Provides SetUp/TearDown hooks for NUnit tests
- Configures Serilog and ExtentReports
- Captures screenshots on failures
- Attaches evidence to report

#### TestHooks
- Provides lifecycle hooks for SpecFlow scenarios
- Configures Serilog, ExtentReports, and CucumberJsonReportGenerator
- Captures screenshots on scenario failures
- Tracks feature/scenario/step execution
- Generates Cucumber JSON for Jira/Xray

**Execution Flow:**

```mermaid
sequenceDiagram
    participant NUnit/SpecFlow
    participant BaseTest/TestHooks
    participant AppLauncher
    participant Test
    participant ExtentReports
    participant CucumberJson
    
    NUnit/SpecFlow->>BaseTest/TestHooks: [BeforeTestRun/OneTimeSetUp]
    BaseTest/TestHooks->>AppLauncher: LaunchApp()
    AppLauncher-->>BaseTest/TestHooks: MainWindow
    BaseTest/TestHooks->>Test: Execute test
    alt Test Failed
        Test-->>BaseTest/TestHooks: Exception
        BaseTest/TestHooks->>BaseTest/TestHooks: TakeScreenshot()
        BaseTest/TestHooks->>ExtentReports: AttachScreenshot()
        BaseTest/TestHooks->>CucumberJson: RecordFailure()
    end
    BaseTest/TestHooks->>AppLauncher: CloseApp()
    BaseTest/TestHooks->>ExtentReports: Flush()
    BaseTest/TestHooks->>CucumberJson: GenerateReport()
    BaseTest/TestHooks->>NUnit/SpecFlow: [AfterTestRun/OneTimeTearDown]
```

#### WaitHelper
- Explicit waits with retry
- Configurable polling
- Attempt logging
- Customizable conditions

**Main Methods:**
- `WaitUntil(condition, timeout)` - Generic wait
- `WaitForElement(parent, automationId, timeout)` - Wait for element
- `WaitForWindowTitle(title, timeout)` - Wait for window
- `WaitForElementEnabled(element, timeout)` - Wait for enable

#### ElementWrapper
- Simplified API over AutomationElement
- Implicit waits before actions
- Automatic interaction logging
- Robust error handling

**Click Flow:**

```mermaid
sequenceDiagram
    participant Test
    participant Wrapper
    participant WaitHelper
    participant FlaUI
    
    Test->>Wrapper: Click()
    Wrapper->>WaitHelper: WaitForElementClickable()
    loop Every 500ms
        WaitHelper->>FlaUI: IsEnabled && !IsOffscreen?
        alt Ready
            FlaUI-->>WaitHelper: true
        else Not Ready
            FlaUI-->>WaitHelper: false
        end
    end
    WaitHelper-->>Wrapper: Element ready
    Wrapper->>FlaUI: Click()
    FlaUI-->>Wrapper: Clicked
    Wrapper-->>Test: Success
```

#### ScreenshotHelper
- Screenshot capture with FlaUI
- Automatic save to reports/screenshots/
- Fallback to full screen if no window
- Filename sanitization

#### ConfigManager (Singleton)
- Reads appsettings.json
- Support for multiple environments (Development, Production)
- Environment variables override values
- Typed properties for easy access

#### CucumberJsonReportGenerator
- Converts SpecFlow results to Cucumber JSON format
- Compatible with Jira/Xray import
- Tracks features, scenarios, steps
- Records execution status, duration, errors
- Optional screenshot embedding

### 4. External Libraries

#### FlaUI (UIA3)
- **UI Automation 3.0** - Latest version of Microsoft UI Automation
- Support for Win32, WPF, WinForms, UWP
- Better performance than UIA2
- Modern fluent API

#### NUnit
- Mature testing framework
- Attributes for categorization
- Test fixtures and setup/teardown
- Expressive asserts

#### SpecFlow
- BDD framework for .NET
- Gherkin syntax support
- Given-When-Then scenarios
- Integration with NUnit

#### ExtentReports
- Interactive HTML reports
- Screenshots and attachments
- Categorization with tags
- Dark theme

#### Serilog
- Structured logging
- Multiple sinks (file, console, etc.)
- Configurable levels
- Optimal performance

## Design Patterns

### Singleton Pattern
- `AppLauncher` - Single instance for entire suite
- `ConfigManager` - Centralized configuration

### Page Object Pattern
- Encapsulation of elements and actions
- Separation of concerns
- Maintainability

### Factory Pattern (implicit)
- `AppLauncher.LaunchApp()` acts as factory for Window

### Wrapper Pattern
- `ElementWrapper` wraps `AutomationElement`
- Adds functionality without modifying original class

## Complete Test Flow

```mermaid
sequenceDiagram
    autonumber
    participant NUnit
    participant BaseTest
    participant Test
    participant PageObject
    participant Framework
    participant FlaUI
    participant App
    
    NUnit->>BaseTest: [OneTimeSetUp]
    BaseTest->>BaseTest: Initialize Serilog
    
    NUnit->>BaseTest: [SetUp]
    BaseTest->>Framework: ConfigManager.GetAppPath()
    BaseTest->>Framework: AppLauncher.LaunchApp()
    Framework->>FlaUI: Application.Launch()
    FlaUI->>App: Start
    App-->>FlaUI: Running
    FlaUI-->>Framework: MainWindow
    Framework-->>BaseTest: Window ready
    
    NUnit->>Test: Execute test
    Test->>PageObject: ClickNumber(5)
    PageObject->>Framework: FindElement("Number5")
    Framework->>FlaUI: FindFirstDescendant()
    FlaUI-->>Framework: Element
    Framework-->>PageObject: ElementWrapper
    PageObject->>Framework: Click()
    Framework->>FlaUI: AsButton().Click()
    
    Test->>Test: Assert.That(...)
    
    alt Test Failed
        Test-->>BaseTest: Exception
        BaseTest->>Framework: ScreenshotHelper.TakeScreenshot()
        Framework->>FlaUI: Capture.Window()
        FlaUI-->>Framework: Image
        Framework-->>BaseTest: Screenshot path
        BaseTest->>BaseTest: ExtentReports.AttachScreenshot()
    end
    
    NUnit->>BaseTest: [TearDown]
    BaseTest->>Framework: AppLauncher.CloseApp()
    Framework->>App: Close()
    
    NUnit->>BaseTest: [OneTimeTearDown]
    BaseTest->>BaseTest: Log.CloseAndFlush()
```

## Design Principles

### SOLID

- **Single Responsibility**: Each class has a single responsibility
- **Open/Closed**: Extensible through inheritance (BaseTest, BasePage)
- **Liskov Substitution**: Page Objects are interchangeable
- **Interface Segregation**: Small and specific interfaces
- **Dependency Inversion**: Dependency on abstractions (IConfiguration)

### DRY (Don't Repeat Yourself)
- Helpers and wrappers avoid duplicate code
- BaseTest and BasePage centralize common logic

### KISS (Keep It Simple)
- Clear and easy-to-use API
- Convention over configuration
- Sensible defaults

### Separation of Concerns
- Tests don't know FlaUI details
- Page Objects don't know ExtentReports details
- Framework provides abstractions

## Extensibility

The framework is designed to be easily extensible:

### Add New Page Object

```csharp
public class NewPage : BasePage
{
    public NewPage(Window window) : base(window) { }
    
    // Your logic here
}
```

### Add New Helper

```csharp
public static class CustomHelper
{
    public static void DoSomething() { }
}
```

### Custom Configuration

```json
{
  "CustomSetting": "value"
}
```

```csharp
var customValue = ConfigManager.Instance.GetValue("CustomSetting", "default");
```

## Next Steps

- **[Framework Guide](./framework-guide.md)** - Detailed usage of each component
- **[Reporting & Logging](./reporting-logging.md)** - Report configuration
- **[CI/CD](./ci-cd.md)** - Continuous integration
