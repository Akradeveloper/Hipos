---
sidebar_position: 5
---

# Reporting & Logging

Hipos includes comprehensive reporting with ExtentReports 5 and Cucumber JSON for Jira/Xray integration, plus structured logging with Serilog.

## ExtentReports 5

ExtentReports generates beautiful, interactive HTML reports with a modern dark theme.

### Automatic Generation

Reports are generated automatically after running tests:

```bash
# 1. Run tests (generates extent-report.html)
dotnet test

# 2. Open report in browser (Windows)
start src\Hipos.Tests\bin\Debug\net8.0-windows\reports\extent-report.html
```

### Report Location

```
src/Hipos.Tests/bin/Debug/net8.0-windows/reports/extent-report.html
```

### Report Content

The ExtentReports report includes:

- 📊 **Dashboard**: General statistics with visual charts
- 📋 **Tests**: Organized by TestFixture and Feature
- 📈 **Categories**: Filtering by tags (Smoke, Complex, Demo)
- ⏱️ **Timeline**: Execution time per test
- 📸 **Screenshots**: Automatic capture on failures
- 📄 **Logs**: Step-by-step test execution logs
- 🌙 **Dark Theme**: Better readability
- 💻 **System Info**: Framework, environment, automation tool details

### Configuration

ExtentReports is initialized in `TestHooks.cs`:

```csharp
var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "reports", "extent-report.html");
ExtentReportManager.InitializeReport(reportPath);
```

### Using ExtentReports in Code

The `ExtentReportManager` provides methods to log test steps:

```csharp
using Hipos.Framework.Utils;

public class MyStepDefinitions : BaseStepDefinitions
{
    [When("I perform some action")]
    public void WhenIPerformSomeAction()
    {
        ExtentReportManager.LogInfo("Performing action");
        
        try
        {
            // your code
            ExtentReportManager.LogPass("Action completed successfully");
        }
        catch (Exception ex)
        {
            ExtentReportManager.LogFail("Action failed", ex);
            throw;
        }
    }
}
```

### Log Methods

```csharp
// Information log
ExtentReportManager.LogInfo("Test step description");

// Pass log (green)
ExtentReportManager.LogPass("Verification passed");

// Fail log (red)
ExtentReportManager.LogFail("Expected X but got Y");
ExtentReportManager.LogFail("Error message", exception);

// Warning log (yellow)
ExtentReportManager.LogWarning("Retry attempt");

// Skip log
ExtentReportManager.LogSkip("Test skipped due to...");
```

### Automatic Screenshots

`TestHooks` automatically captures screenshots when a scenario fails:

1. Scenario fails with exception
2. `AfterScenario` detects failure
3. `ScreenshotHelper.TakeScreenshot()` captures screen
4. Screenshot is attached to ExtentReports
5. Available in report under the failed test

### Test Categories

Use NUnit categories to organize tests:

```csharp
[TestFixture]
[Category("Demo")]
public class CalculatorTests : BaseTest
{
    [Test]
    [Category("Smoke")]
    [Description("Verifies calculator opens correctly")]
    public void VerifyCalculatorOpens()
    {
        // test code
    }
}
```

SpecFlow tags are automatically converted to categories:

```gherkin
@Calculator @Demo
Feature: Windows Calculator

  @Smoke
  Scenario: Verify calculator opens
    Given the calculator is open
    When I verify the window title
    Then the title should contain "Calculator"
```

## Cucumber JSON for Jira/Xray

Hipos generates **Cucumber JSON** format reports compatible with **Jira Xray** for test management integration.

### Automatic Generation

The `cucumber.json` file is generated automatically alongside ExtentReports:

```bash
# Run tests
dotnet test

# Generated files:
# - reports/extent-report.html (HTML report)
# - reports/cucumber.json (Xray-compatible JSON)
```

### Configuration

Configure in `appsettings.json`:

```json
{
  "Reporting": {
    "CucumberJsonPath": "reports/cucumber.json",
    "IncludeScreenshots": true
  }
}
```

**Options:**
- `CucumberJsonPath`: Path where cucumber.json will be saved
- `IncludeScreenshots`: Include screenshots as base64 in JSON (for failures)

### JSON Structure

The cucumber.json file contains:

```json
[
  {
    "id": "windows-calculator",
    "name": "Windows Calculator",
    "description": "As a user...",
    "keyword": "Feature",
    "uri": "Features/Calculator.feature",
    "tags": [
      {"name": "@Calculator"},
      {"name": "@Demo"}
    ],
    "elements": [
      {
        "id": "windows-calculator;perform-addition",
        "name": "Perform a simple addition",
        "keyword": "Scenario",
        "type": "scenario",
        "tags": [{"name": "@Complex"}],
        "steps": [
          {
            "name": "the calculator is open",
            "keyword": "Given ",
            "result": {
              "status": "passed",
              "duration": 762000000
            }
          }
        ]
      }
    ]
  }
]
```

### Importing to Xray

#### Option 1: Xray Web Interface

1. Go to your project in Jira
2. Navigate to **Xray** → **Import Execution Results**
3. Select format: **Cucumber JSON**
4. Upload `cucumber.json` file
5. Configure import options (create new tests, update existing, etc.)

#### Option 2: Xray REST API

**Xray Cloud:**
```bash
curl -H "Content-Type: application/json" \
     -X POST \
     -H "Authorization: Bearer YOUR_TOKEN" \
     --data @reports/cucumber.json \
     https://xray.cloud.getxray.app/api/v2/import/execution/cucumber
```

**Xray Server/DC:**
```bash
curl -H "Content-Type: application/json" \
     -X POST \
     -u username:password \
     --data @reports/cucumber.json \
     https://your-jira-instance.com/rest/raven/2.0/import/execution/cucumber
```

#### Option 3: CI/CD Integration

Example for GitHub Actions:

```yaml
- name: Run Tests
  run: dotnet test

- name: Upload results to Xray
  if: always()
  run: |
    curl -H "Content-Type: application/json" \
         -X POST \
         -H "Authorization: Bearer ${{ secrets.XRAY_TOKEN }}" \
         --data @src/Hipos.Tests/bin/Debug/net8.0-windows/reports/cucumber.json \
         https://xray.cloud.getxray.app/api/v2/import/execution/cucumber
```

### Tag Mapping for Xray

Use tags in your SpecFlow features to link with Xray test cases:

```gherkin
@CALC-123 @regression
Feature: Calculator Operations
  
  @CALC-124 @smoke
  Scenario: Simple addition
    Given the calculator is open
    When I perform the operation "2 + 3"
    Then the result should be "5"
```

Tags like `@CALC-123` and `@CALC-124` will be imported to Xray and automatically link to corresponding Test Cases.

### Benefits

- 📊 **Complete traceability** between requirements, tests and executions
- 🔄 **Automatic synchronization** of results on each execution
- 📈 **Centralized metrics** and reports in Jira
- 👥 **Visibility** for the entire team (QA, Dev, PM)
- 🎯 **Test case management** directly from Jira

## Serilog Logging

Hipos uses Serilog for structured logging.

### Configuration

In `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/test-.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### Log Levels

| Level | Usage | Example |
|-------|-------|---------|
| `Verbose` | Very granular details | Each element interaction |
| `Debug` | Debug information | Element search, waits |
| `Information` | General flow | "Test started", "App launched" |
| `Warning` | Abnormal but recoverable situations | Retries, timeouts |
| `Error` | Errors requiring attention | Unexpected failures |
| `Fatal` | Critical errors | App crash |

### Using Serilog in Your Code

```csharp
using Serilog;

public class CalculatorPage : BasePage
{
    public void ClickNumber(int number)
    {
        Log.Information("Clicking on number: {Number}", number);
        Log.Debug("Looking for element with ID: {AutomationId}", $"num{number}Button");
        
        try
        {
            // code
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error clicking number");
            throw;
        }
    }
}
```

### Structured Logging

Serilog allows structured logging with properties:

```csharp
Log.Information("User {Username} attempted login from {IpAddress}", username, ipAddress);
// Output: User john attempted login from 192.168.1.1
```

### Log Location

By default, logs are saved in:
```
src/Hipos.Tests/bin/Debug/net8.0-windows/logs/test-YYYYMMDD.log
```

Daily rolling means a new file is created each day.

## Artifacts in CI

### GitHub Actions

The `ui-tests.yml` workflow automatically uploads artifacts:

```yaml
- name: Upload Test Reports
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: test-reports
    path: |
      src/Hipos.Tests/bin/Debug/net8.0-windows/reports/
      src/Hipos.Tests/bin/Debug/net8.0-windows/logs/
    retention-days: 30
```

### Available Artifacts

1. **extent-report.html**: Visual HTML report
2. **cucumber.json**: Xray-compatible JSON
3. **test-logs**: Serilog log files
4. **screenshots**: Failure screenshots

### Download Artifacts

On GitHub:
1. Go to "Actions" tab
2. Click on workflow run
3. Scroll down to "Artifacts"
4. Click to download ZIP

## Advanced Customization

### Multiple Serilog Sinks

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/test-.log",
          "rollingInterval": "Day"
        }
      },
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}"
        }
      }
    ]
  }
}
```

### Custom Report Path

```csharp
// In TestHooks.cs
var reportPath = ConfigManager.Instance.GetValue("Reporting:ExtentReportPath", "reports/extent-report.html");
var fullReportPath = Path.Combine(Directory.GetCurrentDirectory(), reportPath);
ExtentReportManager.InitializeReport(fullReportPath);
```

## Best Practices

### Logging

✅ **DO:**
- Log at appropriate level (Information for flow, Debug for details)
- Use structured logging with properties
- Log before and after critical actions
- Log exceptions with `Log.Error(ex, "message")`

❌ **DON'T:**
- Don't log sensitive information (passwords, tokens)
- Don't use `Console.WriteLine()` (use Serilog)
- Don't fill logs with irrelevant information

### Reporting

✅ **DO:**
- Let TestHooks capture automatically on failures
- Use descriptive test names and descriptions
- Use tags to categorize tests
- Include screenshots for visual verification

❌ **DON'T:**
- Don't capture screenshot on every step (generates too many files)
- Don't duplicate information
- Don't attach huge files (> 10MB)

### Xray Integration

✅ **DO:**
- Use consistent tag naming (@PROJECT-123)
- Map tags to Jira test cases
- Automate upload in CI/CD pipeline
- Include meaningful test descriptions

❌ **DON'T:**
- Don't manually upload every time (automate it)
- Don't forget to configure authentication
- Don't use generic tags without Jira mapping

## Troubleshooting

### "ExtentReports not generated"

Verify:
1. `reports/` directory exists
2. `ExtentReportManager.InitializeReport()` was called
3. `ExtentReportManager.FlushReport()` was called in AfterTestRun
4. No file permission issues

### "cucumber.json is empty"

Verify:
1. `CucumberJsonReportGenerator` is initialized in BeforeTestRun
2. Scenarios are being captured (BeforeScenario, AfterStep, AfterScenario hooks)
3. `GenerateReport()` is called in AfterTestRun
4. Path in appsettings.json is correct

### "Screenshots don't appear in report"

Verify:
1. `ScreenshotHelper.TakeScreenshot()` is being called
2. Screenshot path is correct
3. File exists at the path
4. `ExtentReportManager.AttachScreenshot()` is called

### "Logs are empty"

Verify:
1. Log level in appsettings.json (use Debug for more details)
2. Write permissions in `logs/` directory
3. Serilog initialized in BeforeTestRun
4. Using `Log.Information()` not `Console.WriteLine()`

## Next Steps

- **[CI/CD](./ci-cd.md)** - Integrate with pipelines
- **[Troubleshooting](./troubleshooting.md)** - Solve common issues
- **[Examples](./examples.md)** - See complete examples
