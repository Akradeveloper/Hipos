---
sidebar_position: 2
---

# Getting Started

This guide will take you from zero to running your first test in **less than 5 minutes**.

## Prerequisites

Before starting, make sure you have installed:

### Required Software

| Software | Minimum Version | Download |
|----------|----------------|----------|
| **Windows** | 10 or higher | - |
| **.NET SDK** | 8.0 | [Download](https://dotnet.microsoft.com/download) |
| **Git** | Any | [Download](https://git-scm.com/) |

### Recommended Software

- **Visual Studio 2022** or **JetBrains Rider** for development
- **Windows SDK** with UI Automation Tools (includes Inspect.exe)

### Verify Installation

```bash
# Verify .NET
dotnet --version
# Should show: 8.0.x or higher

# Verify Git
git --version
```

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/Hipos.git
cd Hipos
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Project

```bash
dotnet build
```

If everything is correct, you should see:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Your First Test

### 1. Run All Tests

```bash
dotnet test
```

You should see something like:

```
✅ Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22
⏱️  Duration: ~90s
```

### 2. View the Reports

Reports are generated automatically in multiple formats:

```bash
# ExtentReports HTML
src/Hipos.Tests/bin/Debug/net8.0-windows/reports/extent-report.html

# Cucumber JSON (for Jira/Xray)
src/Hipos.Tests/bin/Debug/net8.0-windows/reports/cucumber.json

# Logs
src/Hipos.Tests/bin/Debug/net8.0-windows/logs/
```

## Open HTML Report

### Windows

```bash
# Open ExtentReports in browser
start src\Hipos.Tests\bin\Debug\net8.0-windows\reports\extent-report.html
```

### PowerShell

```powershell
Invoke-Item src\Hipos.Tests\bin\Debug\net8.0-windows\reports\extent-report.html
```

The report includes:
- ✅ Status of each test (passed/failed)
- 📊 Charts and statistics
- 📸 Screenshots of failures
- 📄 Detailed logs
- 🏷️ Tags and categories
- 🌙 Dark theme

## Project Structure

```
Hipos/
├── src/
│   ├── Hipos.Framework/        # Framework core
│   │   ├── Core/               # AppLauncher, ScreenshotHelper
│   │   ├── Utils/              # WaitHelper, MsaaHelper
│   │   │                       # ExtentReportManager, CucumberJsonReportGenerator
│   │   └── Config/             # ConfigManager
│   └── Hipos.Tests/            # Tests and Page Objects
│       ├── PageObjects/        # HiposLoginPage, BasePage
│       ├── StepDefinitions/    # SpecFlow step definitions
│       ├── Features/           # Gherkin feature files
│       ├── Tests/              # NUnit tests (si aplica)
│       ├── Hooks/              # SpecFlow hooks (TestHooks.cs)
│       └── appsettings.json    # Configuration
├── website/                    # Docusaurus documentation
└── .github/workflows/          # CI/CD (ui-tests.yml, docs.yml)
```

**Note:** Tests run against the configured HIPOS executable.

## Configuration

### appsettings.json

Configure the application to test in `src/Hipos.Tests/appsettings.json`:

```json
{
  "AppPath": "C:\\hiposAut.exe",
  "DefaultTimeout": 15000,
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
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/test-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

**Important Parameters:**
- `AppPath`: Path to executable (absolute, relative, or in PATH)
- `C:\\hiposAut.exe` - HIPOS executable
- `C:\\MyApp\\App.exe` - Your custom application
  - `C:\MyApp\App.exe` - Your custom application
- `DefaultTimeout`: Timeout in milliseconds (15s recommended for UWP apps)
- `Reporting.CucumberJsonPath`: Path for Jira/Xray compatible JSON
- `Reporting.IncludeScreenshots`: Include screenshots in JSON (base64)
- `Msaa.*`: MSAA name paths and search depth for HIPOS login

**Supported Applications:**
- ✅ Classic Win32 (Notepad, Paint, legacy apps)
- ✅ Modern UWP (Windows Store apps)
- ✅ WPF/WinForms (your custom applications)

### Environment Variables

You can also use environment variables (they override appsettings.json):

```bash
# Windows
set AppPath=C:\path\to\your\app.exe
set DefaultTimeout=10000

# PowerShell
$env:AppPath = "C:\path\to\your\app.exe"
$env:DefaultTimeout = "10000"
```

## Run Tests from IDE

### Visual Studio

1. Open `Hipos.sln`
2. Go to **Test Explorer** (Ctrl+E, T)
3. Right-click → Run/Debug tests

### Rider

1. Open `Hipos.sln`
2. Go to **Unit Tests** (Alt+8)
3. Right-click → Run/Debug tests

## Integration with Jira/Xray

After running tests, the `cucumber.json` file is ready to import:

```bash
# File location
src/Hipos.Tests/bin/Debug/net8.0-windows/reports/cucumber.json

# Upload to Xray Cloud via API
curl -H "Content-Type: application/json" \
     -X POST \
     -H "Authorization: Bearer YOUR_TOKEN" \
     --data @cucumber.json \
     https://xray.cloud.getxray.app/api/v2/import/execution/cucumber
```

See [Reporting & Logging](./reporting-logging.md) for detailed Xray integration guide.

## Next Steps

Now that you have the framework running:

1. **[Architecture](./architecture.md)** - Understand how it's organized
2. **[Framework Guide](./framework-guide.md)** - Learn to create your own tests
3. **[Reporting & Logging](./reporting-logging.md)** - Customize reports and Xray integration
4. **[Examples](./examples.md)** - See complete code examples
5. **[CI/CD](./ci-cd.md)** - Integrate with your pipeline

## Quick Troubleshooting

### Error: "Executable not found"

**For system apps** (notepad, etc.): Use only the executable name:
```json
"AppPath": "notepad.exe"  // ✅ Correct
```

**For custom apps**: Use absolute or relative path:
```json
"AppPath": "C:\\MyApp\\bin\\Debug\\App.exe"  // ✅ Correct
```

### Tests hang or timeout

**UWP Apps:**
- Increase `DefaultTimeout` to 15000 or more
- Framework uses hybrid search (first 5s strict, then relaxed)
- Check logs in `logs/test-*.log` to see which search mode was used

**Classic Win32 Apps:**
- `DefaultTimeout` of 5000-10000 is usually sufficient
- Verify the app doesn't require admin permissions
- Check logs in `src/Hipos.Tests/bin/Debug/net8.0-windows/logs/`

### Screenshots not generated

- Verify FlaUI can capture the window
- Check write permissions in `reports/` directory
- Verify `ScreenshotHelper.TakeScreenshot()` is being called

### Report not generated

- Verify `reports/` directory exists
- Check `ExtentReportManager.InitializeReport()` was called
- Check `ExtentReportManager.FlushReport()` was called
- Look for errors in logs

For more help, see [Troubleshooting](./troubleshooting.md).
