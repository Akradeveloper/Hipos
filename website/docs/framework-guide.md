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

`MsaaHelper` provides Microsoft Active Accessibility (MSAA) interactions for legacy controls or applications where UIA is not enough. **Important:** MSAA is accessed through FlaUI window handles - FlaUI is used to launch applications and obtain window handles, then MSAA is used for UI element interactions.

### How It Works

1. **FlaUI** launches the application and provides a `Window` object
2. The **native window handle** is extracted from the FlaUI `Window` object
3. **MSAA** uses this handle to interact with UI elements within that window

### Basic Usage

```csharp
// FlaUI provides the window
var mainWindow = AppLauncher.Instance.MainWindow;

// Get native handle from FlaUI window
var handle = mainWindow.Properties.NativeWindowHandle.Value;

// Use MSAA with the handle obtained from FlaUI
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

**Note:** MSAA selectors are now defined as static constants in PageObjects (see Page Objects section below), not in `appsettings.json`.

## WaitHelper

Utilities for explicit waits. ALWAYS use explicit waits, not `Thread.Sleep()`.

### WaitUntil (Fixed Polling)

Generic wait with fixed polling interval:

```csharp
// Wait for custom condition with fixed polling
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

### WaitUntilAdaptive (Adaptive Polling) ⭐ Recommended

Adaptive polling that starts fast and increases gradually if condition doesn't meet. Automatically records response times for adaptive timeouts.

```csharp
// Wait with adaptive polling (recommended)
bool success = WaitHelper.WaitUntilAdaptive(
    condition: () => ElementExistsByPath("Parent", "Button"),
    timeoutMs: 5000,
    conditionDescription: "button exists"
);
```

**How it works:**
- **First 2 seconds**: Fast polling (100ms) for quick conditions
- **2-5 seconds**: Medium polling (300ms)
- **After 5 seconds**: Slower polling (up to 1000ms) to save resources

**Benefits:**
- ✅ Faster detection of quick conditions
- ✅ More efficient than fixed polling
- ✅ Automatically records response times for adaptive timeouts

### Best Practices

✅ **DO:**

```csharp
// Use adaptive polling (recommended)
WaitHelper.WaitUntilAdaptive(
    () => ElementExistsByPath("Parent", "Button"),
    timeoutMs: 5000,
    conditionDescription: "button exists");
ClickElement("Parent", "Button");
```

✅ **Also valid (fixed polling):**

```csharp
// Explicit wait with fixed polling interval
WaitHelper.WaitUntil(
    () => ElementExistsByPath("Parent", "Button"),
    timeoutMs: 5000,
    pollingIntervalMs: 500,
    conditionDescription: "button exists");
ClickElement("Parent", "Button");
```

❌ **DON'T:**

```csharp
// Hardcoded sleep - NEVER use this
Thread.Sleep(2000);
ClickElement("Parent", "Button");
```

## BasePage MSAA Methods

`BasePage` provides MSAA-based methods for interacting with elements using name paths. The MSAA interactions are performed using the native window handle obtained from the FlaUI `Window` object passed to the PageObject constructor.

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

Base class for all Page Objects. Uses MSAA (via FlaUI window handles) for element interactions.

```csharp
public abstract class BasePage
{
    protected Window Window { get; }  // FlaUI Window (used to obtain native handle)
    protected IntPtr WindowHandle { get; }  // Native handle extracted from FlaUI Window (used for MSAA)
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
        // Uses adaptive timeouts if enabled in configuration
        // Uses WaitUntilAdaptive for efficient polling
    }
    
    protected static string[] ParseNamePath(string rawPath)
    {
        // Parses configuration path string into array
    }
    
    protected void EnsureWindowInForeground()
    {
        // Brings window to foreground using FlaUI
        // Uses WaitUntilAdaptive instead of Thread.Sleep
    }
}
```

### Create Your Page Object (MSAA)

MSAA selectors are now defined as static constants in PageObjects for better encapsulation and type safety:

```csharp
public class HiposLoginPage : BasePage
{
    // MSAA selectors as static constants
    private static readonly string[] EmployeePath = { "employee" };
    private static readonly string[] PasswordPath = { "password" };
    private static readonly string[] LoginButtonPath = { "login" };
    private static readonly string[] DataCtrlPath = { "datactrl" };

    public HiposLoginPage(Window window) : base(window)
    {
    }

    public void Login(string employee, string password)
    {
        EnsureWindowInForeground();
        SetElementText(employee, EmployeePath);
        SetElementText(password, PasswordPath);
        ClickElement(LoginButtonPath);
    }
    
    public bool WaitForDataCtrlToDisappear()
    {
        // Uses adaptive timeouts if enabled
        return WaitForElementToDisappear(DataCtrlPath);
    }
}
```

**Benefits of static constants:**
- ✅ Better encapsulation (selectors live with the PageObject)
- ✅ Type safety (compile-time checking)
- ✅ No configuration file needed for selectors
- ✅ Easier to maintain and refactor

### Conventions

1. **Selectors as static constants**: Define MSAA selectors as static readonly arrays in PageObjects
2. **Public methods only**: Don't expose elements directly
3. **Naming**: Verbs for actions (`Click`, `Enter`, `Select`)
4. **ExtentReports Logging**: Document important actions
5. **Return values**: Only for verifications, not elements
6. **MSAA for interactions**: Use BasePage MSAA methods for all element interactions
7. **Adaptive waits**: Use `WaitUntilAdaptive()` for better performance

## ConfigManager

Singleton to manage configuration.

### Usage

```csharp
var config = ConfigManager.Instance;

// Predefined properties
string appPath = config.AppPath;
int timeout = config.DefaultTimeout;

// Adaptive timeout properties
bool adaptiveEnabled = config.AdaptiveTimeoutsEnabled;
int initialTimeout = config.InitialTimeout;
int minTimeout = config.MinTimeout;
int maxTimeout = config.MaxTimeout;
int responseTimeWindow = config.ResponseTimeWindow;

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
  "Timeouts": {
    "Adaptive": true,
    "InitialTimeout": 5000,
    "MinTimeout": 2000,
    "MaxTimeout": 30000,
    "ResponseTimeWindow": 10
  },
  "Serilog": {
    "MinimumLevel": "Information"
  },
  "Reporting": {
    "CucumberJsonPath": "reports/cucumber.json",
    "IncludeScreenshots": true
  },
  "MyCustomKey": "MyValue",
  "MySection": {
    "SubKey": "SubValue"
  }
}
```

**Timeouts Configuration:**
- `Adaptive`: Enable/disable adaptive timeouts (default: false)
- `InitialTimeout`: Initial timeout in milliseconds (default: 5000)
- `MinTimeout`: Minimum timeout allowed (default: 2000)
- `MaxTimeout`: Maximum timeout allowed (default: 30000)
- `ResponseTimeWindow`: Number of response times to track (default: 10)

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

## AdaptiveTimeoutManager

`AdaptiveTimeoutManager` automatically adjusts timeouts based on measured application response times. This makes tests faster when the app is fast, and more robust when the app is slow.

### How It Works

1. **Records Response Times**: Automatically records how long conditions take to fulfill
2. **Calculates Adaptive Timeouts**: Uses percentile 95 of recent response times × safety factor
3. **Adjusts Automatically**: Timeouts adapt to app speed without manual configuration

### Usage

The framework uses `AdaptiveTimeoutManager` automatically when enabled in configuration:

```json
{
  "Timeouts": {
    "Adaptive": true,
    "InitialTimeout": 5000,
    "MinTimeout": 2000,
    "MaxTimeout": 30000,
    "ResponseTimeWindow": 10
  }
}
```

When enabled:
- `WaitUntilAdaptive()` automatically records response times
- `BasePage.WaitForElementToDisappear()` uses adaptive timeouts
- Timeouts adjust based on actual app performance

### Manual Usage

```csharp
var timeoutManager = AdaptiveTimeoutManager.Instance;

// Get adaptive timeout based on measured response times
int adaptiveTimeout = timeoutManager.GetAdaptiveTimeout(baseTimeout: 5000);

// Get statistics
var stats = timeoutManager.GetStats();
if (stats != null)
{
    Console.WriteLine($"Average: {stats.Average}ms");
    Console.WriteLine($"P95: {stats.Percentile95}ms");
}

// Reset history
timeoutManager.Reset();
```

### Benefits

- ✅ **Faster tests**: If app is fast, timeouts are shorter
- ✅ **More robust**: If app is slow, timeouts adjust automatically
- ✅ **No configuration needed**: Works automatically once enabled
- ✅ **Self-learning**: Adapts to app performance over time

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
        // Wait for result using adaptive polling
        WaitHelper.WaitUntilAdaptive(
            () => _page!.IsResultReady(),
            timeoutMs: 5000,
            conditionDescription: "result ready");
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
