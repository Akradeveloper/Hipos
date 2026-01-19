---
sidebar_position: 4
---

# Framework Guide

Detailed guide for using the Hipos framework components.

## AppLauncher

`AppLauncher` is a singleton that manages launching and closing the application under test.

### Basic Usage

```csharp
// Get instance
var launcher = AppLauncher.Instance;

// Launch application
var mainWindow = launcher.LaunchApp("C:\\hiposAut.exe", timeoutMs: 15000);

// Get main window at any time
var window = launcher.MainWindow;

// Close application
launcher.CloseApp();
```

### Key Features

- **Singleton**: Single instance for entire test suite
- **Hybrid Search**: Intelligent window detection system (see below)
- **Configurable Timeout**: Configurable wait for window to appear
- **Robust Handling**: Attempts graceful close, force-kill if it fails
- **Extensive Logging**: Logs all operations and found windows

### Hybrid Window Search ⭐

A key feature that supports modern UWP apps and classic Win32 apps:

#### Phase 1: Strict Mode (first 5 seconds)

```csharp
// Search for windows ONLY from exact ProcessId
if (windowProcessId != processId) {
    continue; // Skip
}
```

**Advantages:**
- ✅ Safe: doesn't capture windows from other apps (Cursor, VS Code, etc.)
- ✅ Fast: finds classic Win32 apps immediately
- ✅ Accurate: avoids false positives

#### Phase 2: Relaxed Mode (next 10 seconds)

```csharp
// Search by window title if strict mode failed
if (window.Title.Contains("HIPOS")) {
    // ✅ Found
}
```

**Advantages:**
- ✅ Detects UWP apps whose window is in child process
- ✅ Excludes system windows (Taskbar, Program Manager)
- ✅ Logs mode used for debugging

#### Log Example

```
[00:00.000] Launching application: C:\hiposAut.exe
[00:00.100] Process launched with PID: 38092
[00:05.000] ⚠️ Switching to relaxed search mode (by window title)
[00:05.500] ✓ Window found: 'HIPOS' (PID: 38124, Mode: Relaxed)
```

### Configuration

The application path is configured in `appsettings.json`:

```json
{
  "AppPath": "C:\\hiposAut.exe",
  "DefaultTimeout": 15000          // 15s for UWP apps
}
```

**AppPath examples:**

```json
// HIPOS (recommended)
"AppPath": "C:\\hiposAut.exe"

// Notepad (Win32)
"AppPath": "notepad.exe"

// Your custom application
"AppPath": "C:\\MyApp\\bin\\Debug\\MyApp.exe"
```

## MSAA Helper

`MsaaHelper` provides Microsoft Active Accessibility (MSAA) interactions for legacy controls or applications where UIA is not enough.

### Basic Usage

```csharp
var handle = mainWindow.Properties.NativeWindowHandle.Value;
var employee = MsaaHelper.FindByName(handle, "employee");
employee?.SetText("user123");

var loginButton = MsaaHelper.FindByName(handle, "login");
loginButton?.Click();
```

### Name Paths

You can search by name path to traverse nested containers:

```csharp
var element = MsaaHelper.FindByNamePath(handle, "LoginPanel", "employee");
```

### Configuration for MSAA

Define name paths in `appsettings.json` to keep selectors centralized:

```json
{
  "Msaa": {
    "SearchMaxDepth": 6,
    "Login": {
      "EmployeeNamePath": "LoginPanel > employee",
      "PasswordNamePath": "LoginPanel > password",
      "LoginButtonNamePath": "LoginPanel > login",
      "DataCtrlNamePath": "datactrl"
    }
  }
}
```

## WaitHelper

Utilities for explicit waits. ALWAYS use explicit waits, not `Thread.Sleep()`.

### WaitUntil (Generic)

```csharp
// Wait for custom condition
bool success = WaitHelper.WaitUntil(
    condition: () => someElement.IsVisible,
    timeoutMs: 5000,
    pollingIntervalMs: 500,
    conditionDescription: "element visible"
);

if (!success)
{
    // Timeout reached
}
```

### Best Practices

✅ **DO:**

```csharp
// Explicit wait with custom condition
WaitHelper.WaitUntil(
    () => ElementExistsByPath("Parent", "Button"),
    timeoutMs: 5000,
    conditionDescription: "button exists");
ClickElement("Parent", "Button");
```

❌ **DON'T:**

```csharp
// Hardcoded sleep
Thread.Sleep(2000);
ClickElement("Parent", "Button");
```

## BasePage MSAA Methods

`BasePage` provides MSAA-based methods for interacting with elements using name paths.

### Finding Elements

```csharp
// Find element by name path
var element = FindElementByPath("Parent", "Child", "Button");

// Check if element exists
bool exists = ElementExistsByPath("Parent", "Button");
```

### Interacting with Elements

```csharp
// Click on element
ClickElement("Parent", "Child", "Button");

// Set text on element
SetElementText("Hello World", "Parent", "TextBox");

// Wait for element to disappear
bool disappeared = WaitForElementToDisappear(
    new[] { "Parent", "LoadingIndicator" }, 
    timeoutMs: 5000);
```

### Parsing Name Paths

```csharp
// Parse configuration path string
string[] path = ParseNamePath("Parent > Child > Button");
// Returns: ["Parent", "Child", "Button"]
```

## Page Objects

### BasePage

Base class for all Page Objects. Uses MSAA for element interactions.

```csharp
public abstract class BasePage
{
    protected Window Window { get; }  // FlaUI Window for handle and focus
    protected IntPtr WindowHandle { get; }  // Native handle for MSAA
    protected int DefaultTimeout { get; }
    
    // MSAA methods
    protected MsaaHelper.MsaaElement FindElementByPath(params string[] namePath)
    {
        // Finds MSAA element by name path
        // Throws exception if not found
    }
    
    protected bool ElementExistsByPath(params string[] namePath)
    {
        // Verifies MSAA element existence without throwing exception
    }
    
    protected void ClickElement(params string[] namePath)
    {
        // Clicks on MSAA element by name path
    }
    
    protected void SetElementText(string text, params string[] namePath)
    {
        // Sets text on MSAA element by name path
    }
    
    protected bool WaitForElementToDisappear(string[] namePath, int? timeoutMs = null)
    {
        // Waits until MSAA element disappears
    }
    
    protected static string[] ParseNamePath(string rawPath)
    {
        // Parses configuration path string into array
    }
    
    protected void EnsureWindowInForeground()
    {
        // Brings window to foreground using FlaUI
    }
}
```

### Create Your Page Object (MSAA)

```csharp
public class HiposLoginPage : BasePage
{
    private readonly string[] _employeePath;
    private readonly string[] _passwordPath;
    private readonly string[] _loginButtonPath;
    private readonly string[] _dataCtrlPath;

    public HiposLoginPage(Window window) : base(window)
    {
        var config = ConfigManager.Instance;
        _employeePath = ParseNamePath(config.GetValue("Msaa:Login:EmployeeNamePath", "employee"));
        _passwordPath = ParseNamePath(config.GetValue("Msaa:Login:PasswordNamePath", "password"));
        _loginButtonPath = ParseNamePath(config.GetValue("Msaa:Login:LoginButtonNamePath", "login"));
        _dataCtrlPath = ParseNamePath(config.GetValue("Msaa:Login:DataCtrlNamePath", "datactrl"));
    }

    public void Login(string employee, string password)
    {
        EnsureWindowInForeground();
        SetElementText(employee, _employeePath);
        SetElementText(password, _passwordPath);
        ClickElement(_loginButtonPath);
    }
    
    public bool WaitForDataCtrlToDisappear()
    {
        return WaitForElementToDisappear(_dataCtrlPath);
    }
}
```

### Conventions

1. **Name paths in configuration**: Centralize selectors in appsettings.json
2. **Public methods only**: Don't expose elements directly
3. **Naming**: Verbs for actions (`Click`, `Enter`, `Select`)
4. **ExtentReports Logging**: Document important actions
5. **Return values**: Only for verifications, not elements
6. **MSAA for interactions**: Use BasePage MSAA methods for all element interactions

## ConfigManager

Singleton to manage configuration.

### Usage

```csharp
var config = ConfigManager.Instance;

// Predefined properties
string appPath = config.AppPath;
int timeout = config.DefaultTimeout;

// Custom values
string customValue = config.GetValue("MyCustomKey", "defaultValue");

// Complete section
IConfigurationSection section = config.GetSection("MySection");
```

### appsettings.json

```json
{
  "AppPath": "path/to/app.exe",
  "DefaultTimeout": 5000,
  "Serilog": {
    "MinimumLevel": "Information"
  },
  "Reporting": {
    "CucumberJsonPath": "reports/cucumber.json",
    "IncludeScreenshots": true
  },
  "Msaa": {
    "SearchMaxDepth": 6,
    "Login": {
      "EmployeeNamePath": "employee",
      "PasswordNamePath": "password",
      "LoginButtonNamePath": "login",
      "DataCtrlNamePath": "datactrl"
    }
  },
  "MyCustomKey": "MyValue",
  "MySection": {
    "SubKey": "SubValue"
  }
}
```

### Environment Variables

Override appsettings.json values:

```bash
# Windows CMD
set AppPath=C:\other\app.exe

# PowerShell
$env:AppPath = "C:\other\app.exe"

# GitHub Actions / CI
env:
  AppPath: "C:\ci\app.exe"
```

## ScreenshotHelper

Static helper to capture screenshots.

### Usage

```csharp
string? path = ScreenshotHelper.TakeScreenshot("test-name");

if (path != null && File.Exists(path))
{
    // Screenshot saved successfully
    ExtentReportManager.AttachScreenshot(path);
}
```

### Features

- Captures main window by default
- Fallback to full screen if no window
- Saves to `reports/screenshots/`
- Sanitized name (no invalid characters)
- Automatic timestamp

### Used Automatically

`TestHooks` captures screenshots when a scenario fails, you don't need to call it manually.

## Naming Conventions

### AutomationIds
- PascalCase
- Descriptive: `SubmitButton`, `UsernameTextBox`, `ErrorMessage`

### Page Objects
- Suffix `Page`: `HiposLoginPage`
- PascalCase

### Tests
- Descriptive and specific
- Format: `Verify[What]_[Condition]_[Result]`
- Example: `VerifyLogin_WithInvalidCredentials_ShowsError`

### Methods in Page Objects
- Action verbs: `Click`, `Enter`, `Select`, `Get`, `Is`, `Wait`
- PascalCase

## Complete Example (SpecFlow)

```csharp
[Binding]
public class HiposLoginStepDefinitions : BaseStepDefinitions
{
    private HiposLoginPage? _loginPage;

    [Given("the HIPOS login page is open")]
    public void GivenTheHiposLoginPageIsOpen()
    {
        Assert.That(MainWindow, Is.Not.Null, "HIPOS window should be available");
        _loginPage = new HiposLoginPage(MainWindow!);
    }

    [When("I login with employee \"(.*)\" and password \"(.*)\"")]
    public void WhenILoginWithEmployeeAndPassword(string employee, string password)
    {
        _loginPage!.Login(employee, password);
    }

    [Then("the datactrl element should not exist")]
    public void ThenTheDataCtrlElementShouldNotExist()
    {
        Assert.That(_loginPage!.WaitForDataCtrlToDisappear(), Is.True);
    }
}
```

## SpecFlow BDD Support

### TestHooks

The `TestHooks` class manages SpecFlow lifecycle events.

#### BeforeTestRun

Initializes:
- Serilog logging
- ConfigManager
- ExtentReports
- CucumberJsonReportGenerator
- Application launch

#### AfterTestRun

Finalizes:
- Closes application
- Flushes ExtentReports
- Generates Cucumber JSON
- Closes Serilog

#### BeforeScenario / AfterScenario

Handles:
- Scenario logging
- Screenshot capture on failure
- Report updates (ExtentReports + Cucumber JSON)

#### BeforeStep / AfterStep

Tracks:
- Step execution
- Step status (passed/failed)
- Duration

### BaseStepDefinitions

Base class for step definition files.

```csharp
[Binding]
public class MyStepDefinitions : BaseStepDefinitions
{
    private MyPage? _page;

    [Given("the page is open")]
    public void GivenThePageIsOpen()
    {
        LogInfo("Verifying that the page is open");
        Assert.That(MainWindow, Is.Not.Null, "Window should be available");
        _page = new MyPage(MainWindow!);
        LogPass("Page opened successfully");
    }

    [When("I click the button")]
    public void WhenIClickTheButton()
    {
        LogInfo("Clicking button");
        _page!.ClickButton();
        Thread.Sleep(500);
    }

    [Then("the result should be \"(.*)\"")]
    public void ThenTheResultShouldBe(string expectedResult)
    {
        var actualResult = _page!.GetResult();
        LogInfo($"Result obtained: {actualResult}");
        
        Assert.That(actualResult, Does.Contain(expectedResult),
            $"Result should contain '{expectedResult}', but got: '{actualResult}'");
        
        LogPass($"Correct result: {expectedResult}");
    }
}
```

### Helper Methods

Available in `BaseStepDefinitions`:

- `LogInfo(string message)` - Log informational message
- `LogPass(string message)` - Log successful step
- `LogFail(string message)` - Log failed step
- `LogWarning(string message)` - Log warning

## Next Steps

- **[Reporting & Logging](./reporting-logging.md)** - Customize reports
- **[CI/CD](./ci-cd.md)** - Integrate with pipelines
- **[Troubleshooting](./troubleshooting.md)** - Solve common issues
