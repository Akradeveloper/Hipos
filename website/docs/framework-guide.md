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
var mainWindow = launcher.LaunchApp("calc.exe", timeoutMs: 15000);

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

A key feature that supports modern UWP apps (Calculator) and classic Win32 apps (Notepad):

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
if (window.Title.Contains("Calculadora") || 
    window.Title.Contains("Calculator")) {
    // ✅ Found
}
```

**Advantages:**
- ✅ Detects UWP apps whose window is in child process
- ✅ Excludes system windows (Taskbar, Program Manager)
- ✅ Logs mode used for debugging

#### Log Example

```
[00:00.000] Launching application: calc.exe
[00:00.100] Process launched with PID: 38092
[00:05.000] ⚠️ Switching to relaxed search mode (by window title)
[00:05.500] ✓ Window found: 'Calculadora' (PID: 38124, Mode: Relaxed)
```

### Configuration

The application path is configured in `appsettings.json`:

```json
{
  "AppPath": "calc.exe",           // System apps: name only
  "DefaultTimeout": 15000          // 15s for UWP apps
}
```

**AppPath examples:**

```json
// Windows Calculator (UWP)
"AppPath": "calc.exe"

// Notepad (Win32)
"AppPath": "notepad.exe"

// Your custom application
"AppPath": "C:\\MyApp\\bin\\Debug\\MyApp.exe"
```

## BaseTest

Base class for all your tests. Provides automatic hooks.

### Usage

```csharp
public class MyTests : BaseTest
{
    [Test]
    public void MyTest()
    {
        // MainWindow is already available
        var page = new CalculatorPage(MainWindow!);
        
        // Your test logic here
    }
}
```

### Automatic Hooks

#### `[OneTimeSetUp]` - Once per fixture
1. Configures Serilog
2. Initializes ConfigManager
3. **Launches application ONCE**
4. Provides `MainWindow` for all tests

**Advantage:** App does NOT open/close between tests → **much faster tests**

#### `[SetUp]` - Before each test
1. Log test start
2. Create Page Object instance if needed

#### `[TearDown]` - After each test
1. If test failed:
   - Automatically capture screenshot
   - Attach to ExtentReports report
2. Log test completion

#### `[OneTimeTearDown]` - Once at the end
1. **Close application**
2. Attach full logs
3. Close Serilog

### Migration from old SetUp/TearDown

```csharp
// ❌ Old (app opens/closes each test)
[SetUp]
public void SetUp() {
    AppLauncher.Instance.LaunchApp(...);
}

[TearDown]
public void TearDown() {
    AppLauncher.Instance.CloseApp();
}

// ✅ New (app opens ONCE)
// BaseTest already handles this automatically with OneTimeSetUp/TearDown
```

### Protected Property

```csharp
protected Window? MainWindow { get; private set; }
```

Use `MainWindow` in your tests to create Page Objects.

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

### WaitForElement

```csharp
// Wait for element to appear
var element = WaitHelper.WaitForElement(
    parent: MainWindow,
    automationId: "SubmitButton",
    timeoutMs: 5000
);

if (element == null)
{
    // Element not found
}
```

### WaitForWindowTitle

```csharp
// Wait for window with specific title
bool found = WaitHelper.WaitForWindowTitle(
    title: "Settings",
    timeoutMs: 3000
);
```

### WaitForElementEnabled

```csharp
// Wait for element to be enabled
bool enabled = WaitHelper.WaitForElementEnabled(
    element: button,
    timeoutMs: 5000
);
```

### WaitForElementClickable

```csharp
// Wait for element to be clickable (enabled + visible)
bool clickable = WaitHelper.WaitForElementClickable(
    element: button,
    timeoutMs: 5000
);
```

### Best Practices

✅ **DO:**

```csharp
// Explicit wait
WaitHelper.WaitForElement(window, "ButtonId", 5000);
button.Click();
```

❌ **DON'T:**

```csharp
// Hardcoded sleep
Thread.Sleep(2000);
button.Click();
```

## ElementWrapper

Wrapper over `AutomationElement` that adds implicit waits and logging.

### Create Wrapper

```csharp
var element = WaitHelper.WaitForElement(window, "InputTextBox", 5000);
var wrapper = new ElementWrapper(element, defaultTimeout: 5000);
```

### Methods

#### Click()

```csharp
wrapper.Click();
// Automatic wait until clickable
// Automatic action logging
```

#### SetText()

```csharp
wrapper.SetText("Hello World");
// Clears existing text (Ctrl+A, Delete)
// Sets new text
// Automatic logging
```

#### GetText()

```csharp
string text = wrapper.GetText();
// Attempts multiple ways to get text:
// 1. Text Pattern
// 2. Value Pattern
// 3. Name property
```

#### IsEnabled()

```csharp
bool enabled = wrapper.IsEnabled();
```

#### IsVisible()

```csharp
bool visible = wrapper.IsVisible();
// Verifies it's not offscreen
```

#### WaitUntilExists()

```csharp
bool exists = wrapper.WaitUntilExists(timeoutMs: 3000);
```

### Access to Original Element

```csharp
AutomationElement original = wrapper.Element;
// For advanced cases where you need FlaUI's full API
```

## Page Objects

### BasePage

Base class for all Page Objects.

```csharp
public abstract class BasePage
{
    protected Window Window { get; }
    protected int DefaultTimeout { get; }
    
    protected ElementWrapper FindElement(string automationId)
    {
        // Searches for element with automatic wait
        // Throws exception if not found
    }
    
    protected bool ElementExists(string automationId)
    {
        // Verifies existence without throwing exception
    }
    
    protected bool WaitForElementVisible(string automationId, int? timeoutMs = null)
    {
        // Waits until element is visible
    }
}
```

### Create Your Page Object

```csharp
public class LoginPage : BasePage
{
    // AutomationIds (constants for easy maintenance)
    private const string UsernameTextBoxId = "UsernameTextBox";
    private const string PasswordTextBoxId = "PasswordTextBox";
    private const string LoginButtonId = "LoginButton";
    private const string ErrorMessageId = "ErrorMessage";
    
    public LoginPage(Window window) : base(window)
    {
        Log.Information("Navigating to LoginPage");
        ExtentReportManager.LogInfo("Navigating to LoginPage");
    }
    
    // Atomic actions
    public void EnterUsername(string username)
    {
        Log.Information("Entering username: {Username}", username);
        ExtentReportManager.LogInfo($"Entering username: {username}");
        var textBox = FindElement(UsernameTextBoxId);
        textBox.SetText(username);
    }
    
    public void EnterPassword(string password)
    {
        Log.Information("Entering password");
        ExtentReportManager.LogInfo("Entering password");
        var textBox = FindElement(PasswordTextBoxId);
        textBox.SetText(password);
    }
    
    public void ClickLogin()
    {
        Log.Information("Clicking Login");
        ExtentReportManager.LogInfo("Clicking Login");
        var button = FindElement(LoginButtonId);
        button.Click();
    }
    
    // Composed actions (fluent)
    public void Login(string username, string password)
    {
        Log.Information("Login with user: {Username}", username);
        ExtentReportManager.LogInfo($"Login with user: {username}");
        EnterUsername(username);
        EnterPassword(password);
        ClickLogin();
    }
    
    // Verifications
    public string GetErrorMessage()
    {
        var label = FindElement(ErrorMessageId);
        return label.GetText();
    }
    
    public bool IsLoginButtonEnabled()
    {
        var button = FindElement(LoginButtonId);
        return button.IsEnabled();
    }
}
```

### Conventions

1. **AutomationIds as constants**: Easy to find and change
2. **Public methods only**: Don't expose elements directly
3. **Naming**: Verbs for actions (`Click`, `Enter`, `Select`)
4. **ExtentReports Logging**: Document important actions
5. **Return values**: Only for verifications, not elements

## ConfigManager

Singleton to manage configuration.

### Usage

```csharp
var config = ConfigManager.Instance;

// Predefined properties
string appPath = config.AppPath;
int timeout = config.DefaultTimeout;
int retries = config.RetryCount;
string logLevel = config.LogLevel;

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
  "RetryCount": 3,
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

`BaseTest` automatically captures screenshots when a test fails, you don't need to call it manually.

## RetryPolicy

Retry policy for transient operations.

### Usage

```csharp
// Action without return
RetryPolicy.Execute(
    action: () => button.Click(),
    maxRetries: 3,
    delayMs: 1000
);

// Function with return
var result = RetryPolicy.Execute(
    func: () => element.GetText(),
    maxRetries: 3,
    delayMs: 1000
);
```

### Transient Errors (retryable)

- `ElementNotAvailableException`
- `TimeoutException`
- `InvalidOperationException` with specific messages

### Non-Transient Errors (NOT retryable)

- **AssertionException**: Assert failures never retry
- Other non-transient errors

### Example

```csharp
// This will retry if element is temporarily unavailable
RetryPolicy.Execute(() =>
{
    var element = FindElement("DynamicButton");
    element.Click();
}, maxRetries: 3);

// This will NOT retry (assert failure)
RetryPolicy.Execute(() =>
{
    var text = GetResult();
    Assert.That(text, Is.EqualTo("Expected")); // If it fails, throws immediately
});
```

## Naming Conventions

### AutomationIds
- PascalCase
- Descriptive: `SubmitButton`, `UsernameTextBox`, `ErrorMessage`

### Page Objects
- Suffix `Page`: `LoginPage`, `CalculatorPage`
- PascalCase

### Tests
- Descriptive and specific
- Format: `Verify[What]_[Condition]_[Result]`
- Example: `VerifyLogin_WithInvalidCredentials_ShowsError`

### Methods in Page Objects
- Action verbs: `Click`, `Enter`, `Select`, `Get`, `Is`, `Wait`
- PascalCase

## Complete Example

```csharp
[TestFixture]
[Category("Smoke")]
[Description("Tests for login functionality")]
public class LoginTests : BaseTest
{
    private LoginPage _loginPage = null!;
    
    [SetUp]
    public void TestSetUp()
    {
        // BaseTest.SetUp already launched the app
        _loginPage = new LoginPage(MainWindow!);
        ExtentReportManager.LogInfo($"Starting test: {TestContext.CurrentContext.Test.Name}");
    }
    
    [Test]
    [Category("Positive")]
    [Description("Verifies successful login with valid credentials")]
    public void VerifyLogin_WithValidCredentials_Success()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";
        
        // Act
        ExtentReportManager.LogInfo("Attempting to login");
        _loginPage.Login(username, password);
        
        // Wait for dashboard
        WaitHelper.WaitForWindowTitle("Dashboard", 5000);
        
        // Assert
        Assert.That(MainWindow.Title, Does.Contain("Dashboard"));
        ExtentReportManager.LogPass("Login successful - Dashboard displayed");
    }
    
    [Test]
    [Category("Negative")]
    [Description("Verifies error message with invalid credentials")]
    public void VerifyLogin_WithInvalidCredentials_ShowsError()
    {
        // Arrange
        var username = "invalid";
        var password = "wrong";
        
        // Act
        ExtentReportManager.LogInfo("Attempting to login with invalid credentials");
        _loginPage.Login(username, password);
        
        // Wait for error
        Thread.Sleep(500); // Or better: WaitForElementVisible("ErrorMessage")
        
        var errorMessage = _loginPage.GetErrorMessage();
        ExtentReportManager.LogInfo($"Error message displayed: {errorMessage}");
        
        // Assert
        Assert.That(errorMessage, Does.Contain("Invalid credentials"));
        ExtentReportManager.LogPass("Error message verified successfully");
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
