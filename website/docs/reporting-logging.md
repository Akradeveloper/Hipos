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
- 🎥 **Videos**: Test execution recordings (if video recording is enabled)
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

### Video Recording

Hipos supports optional video recording of test execution, providing visual evidence of both successful and failed tests.

#### Configuration

Configure video recording in `appsettings.json`:

```json
{
  "VideoRecording": {
    "Enabled": true,
    "Mode": "Always",
    "VideoDirectory": "reports/videos",
    "FrameRate": 10,
    "Quality": "medium"
  }
}
```

**Configuration Options:**

- `Enabled`: Enable/disable video recording (default: `false`)
- `Mode`: When to record videos:
  - `"Always"`: Record all tests (successful and failed)
  - `"OnFailure"`: Only record when tests fail
  - `"OnSuccess"`: Only record when tests pass
  - `"Disabled"`: Disable video recording
- `VideoDirectory`: Directory to save videos (default: `"reports/videos"`)
- `FrameRate`: Frames per second (default: `10`, recommended: 5-15)
- `Quality`: Video quality preset:
  - `"low"`: Smaller file size, lower quality (faster encoding)
  - `"medium"`: Balanced quality and file size (recommended)
  - `"high"`: Best quality, larger file size (slower encoding)

#### Requirements

Video recording requires **FFmpeg** to be installed and available in the system PATH, or placed in the project directory.

**Installing FFmpeg:**

1. Download from [ffmpeg.org](https://ffmpeg.org/download.html)
2. Extract and add to PATH, or place `ffmpeg.exe` in:
   - Project root directory
   - `tools/ffmpeg.exe`
   - `bin/ffmpeg.exe`
   - `ffmpeg/ffmpeg.exe`

**Verifying FFmpeg:**

```bash
ffmpeg -version
```

If FFmpeg is not found, video recording will be automatically disabled and a warning will be logged.

#### How It Works

1. **BeforeScenario**: If recording is enabled and mode requires it, `VideoRecorder.StartRecording()` begins capturing the screen
2. **During Test**: Video is recorded in the background
3. **AfterScenario**: 
   - Recording stops
   - Based on mode and test result, video is either saved or deleted
   - Saved videos are automatically attached to ExtentReports

#### Video Files

Videos are saved with descriptive names:

```
reports/videos/Successful_login_hides_datactrl_20240115_143022.mp4
```

#### Performance Considerations

- **Frame Rate**: Lower frame rates (5-10 fps) reduce file size and CPU usage
- **Quality**: Use `"low"` for faster tests, `"medium"` for balance, `"high"` for detailed analysis
- **Disk Space**: Videos consume more space than screenshots. Consider cleanup policies for CI/CD

#### Example Usage

```csharp
// Video recording is automatic based on configuration
// No code changes needed in your tests!

// Configuration example for CI/CD (only record failures):
{
  "VideoRecording": {
    "Enabled": true,
    "Mode": "OnFailure",
    "FrameRate": 8,
    "Quality": "low"
  }
}

// Configuration for local development (record all):
{
  "VideoRecording": {
    "Enabled": true,
    "Mode": "Always",
    "FrameRate": 10,
    "Quality": "medium"
  }
}
```

### Test Categories

SpecFlow tags are automatically converted to categories:

```gherkin
@Hipos @Login
Feature: HIPOS login

  @Smoke
  Scenario: Successful login hides datactrl
    Given the HIPOS login page is open
    When I login with employee "-1" and password "000000"
    Then the datactrl element should not exist
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
    "id": "hipos-login",
    "name": "HIPOS login",
    "description": "As a user...",
    "keyword": "Feature",
    "uri": "Features/Login.feature",
    "tags": [
      {"name": "@Hipos"},
      {"name": "@Login"}
    ],
    "elements": [
      {
        "id": "hipos-login;successful-login",
        "name": "Successful login hides datactrl",
        "keyword": "Scenario",
        "type": "scenario",
        "tags": [{"name": "@Smoke"}],
        "steps": [
          {
            "name": "the HIPOS login page is open",
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
@HIPOS-123 @regression
Feature: HIPOS login
  
  @HIPOS-124 @smoke
  Scenario: Successful login hides datactrl
    Given the HIPOS login page is open
    When I login with employee "-1" and password "000000"
    Then the datactrl element should not exist
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

public class HiposLoginPage : BasePage
{
    public void Login(string employee, string password)
    {
        Log.Information("Login con employee: {Employee}", employee);
        Log.Debug("Buscando elementos MSAA para login");
        
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
