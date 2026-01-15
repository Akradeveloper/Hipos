---
sidebar_position: 8
---

# Troubleshooting

Solutions to common problems in Windows UI automation.

## Application Launch Issues

### TimeoutException: "Could not get main window"

**Symptoms:**
```
System.TimeoutException: Could not get main window after 15000ms (PID: 38092)
```

**Cause:** The framework cannot find the application window.

#### For UWP Apps (Calculator, Windows Store Apps)

✅ **Solution 1:** Increase timeout to 15-20 seconds

```json
{
  "AppPath": "calc.exe",
  "DefaultTimeout": 20000  // 20 seconds
}
```

✅ **Solution 2:** Check logs to see which search mode was used

```
# Search in logs/test-*.log
[00:05.000] ⚠️ Switching to relaxed search mode (by window title)
[00:05.500] ✓ Window found: 'Calculadora' (PID: 38124, Mode: Relaxed)
```

If you see `Mode: Relaxed`, it means the window is in a **child process** (normal for UWP).

✅ **Solution 3:** Verify app doesn't require admin permissions

```bash
# Run as normal user (NOT as admin)
dotnet test
```

#### For Classic Win32 Apps (Notepad, Paint, legacy apps)

✅ **Solution:** Timeout of 5-10 seconds is sufficient

```json
{
  "AppPath": "notepad.exe",
  "DefaultTimeout": 5000  // 5 seconds
}
```

If it fails, verify the app isn't blocked or requires manual interaction.

### Cursor/VS Code Closes When Running Tests

**Symptom:** When running `dotnet test`, the IDE closes unexpectedly.

**Cause:** Old framework version that searched for windows without filtering by PID.

✅ **Solution:** Update to latest version which includes:
- Strict search by ProcessId in first 5 seconds
- Exclusion list for IDEs (Cursor, VS Code, Visual Studio)

**Verify you have the latest version:**
```csharp
// In AppLauncher.cs should have:
var excludedTitles = new[] { 
    "Barra de tareas", "Taskbar", "Program Manager", 
    "Cursor", "Visual Studio", "Visual Studio Code"
};
```

## Element Issues

### Element Not Found / TimeoutException

**Symptoms:**
```
FlaUI.Core.Exceptions.ElementNotAvailableException: 
Element with AutomationId 'ButtonId' not found
```

**Causes and Solutions:**

#### 1. Incorrect AutomationId

✅ **Solution:** Use Inspect.exe to verify

```bash
# Location (Windows SDK)
C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\inspect.exe
```

**Steps:**
1. Open Inspect.exe
2. Open your application
3. Hover over the element
4. Verify "AutomationId" property in right panel
5. Copy exact value (case-sensitive)

#### 2. Element Not Ready

✅ **Solution:** Increase timeout or add specific wait

```csharp
// Increase timeout
var element = WaitHelper.WaitForElement(window, "ButtonId", timeoutMs: 10000);

// Or wait for specific condition
WaitHelper.WaitUntil(
    () => element != null && element.IsEnabled,
    timeoutMs: 5000,
    conditionDescription: "button enabled"
);
```

#### 3. Element in Popup/Modal

✅ **Solution:** Wait for popup to appear first

```csharp
// Wait for popup
WaitHelper.WaitForWindowTitle("Settings", 5000);

// Search in all windows
var allWindows = automation.GetDesktop().FindAllChildren();
var popup = allWindows.FirstOrDefault(w => w.Name == "Settings");

// Search element in popup
var element = WaitHelper.WaitForElement(popup, "OkButton", 5000);
```

#### 4. Application Uses Non-Standard UI Framework

✅ **Solution:** Switch to UIA2 (if UIA3 doesn't work)

```csharp
// Instead of UIA3Automation
var automation = new UIA2Automation();
```

### Flaky Tests (Intermittent)

**Symptoms:** Tests pass sometimes but fail other times.

**Common Causes:**

#### 1. Insufficient Waits

❌ **Problem:**
```csharp
button.Click();
Thread.Sleep(500);  // Hardcoded sleep
var result = GetResult();
```

✅ **Solution:**
```csharp
button.Click();
WaitHelper.WaitForElement(window, "ResultLabel", 5000);
var result = GetResult();
```

#### 2. Race Conditions

❌ **Problem:**
```csharp
EnterText("user");
EnterPassword("pass");
ClickLogin();  // Click before text is fully entered
```

✅ **Solution:**
```csharp
EnterText("user");
Thread.Sleep(100);  // Small delay to process
EnterPassword("pass");
Thread.Sleep(100);
WaitHelper.WaitForElementEnabled(loginButton, 5000);
ClickLogin();
```

#### 3. Residual State from Previous Test

❌ **Problem:** Test depends on state left by previous test.

✅ **Solution:**
```csharp
[SetUp]
public void TestSetUp()
{
    // BaseTest.SetUp already launched clean app
    // Navigate to known initial state
    ResetToDefaultState();
}
```

#### 4. Animation Timing

✅ **Solution:** Wait for animation to finish

```csharp
// Wait for element to be visible AND settled
WaitHelper.WaitUntil(
    () => element.IsVisible && element.BoundingRectangle.IsEmpty == false,
    5000,
    conditionDescription: "element visible and rendered"
);

// Additional small delay for animations
Thread.Sleep(300);
```

## Permission Issues

### Application Requires Admin Permissions

**Symptoms:**
- App doesn't launch
- "Access denied"
- UAC prompt appears but tests can't interact

✅ **Solutions:**

#### Option 1: Run Test Runner as Admin

```bash
# Open terminal as Admin
dotnet test
```

In IDE:
- Visual Studio: Run VS as Admin
- Rider: Run Rider as Admin

#### Option 2: Disable UAC for the App

Create manifest for the app specifying required level:

```xml
<!-- app.manifest -->
<trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
  <security>
    <requestedPrivileges>
      <requestedExecutionLevel level="asInvoker" uiAccess="false" />
    </requestedPrivileges>
  </security>
</trustInfo>
```

#### Option 3: Modify UAC Settings (NOT recommended for production)

Only for local testing environment:
1. Win + R → `UserAccountControlSettings`
2. Lower slider to "Never notify"
3. Restart

## Session Issues

### Tests Fail in CI (Session Lock)

**Symptoms:**
- Tests pass locally
- Fail in CI with timeout
- Error: "Window not available"

✅ **Solution:** See [CI/CD Guide](./ci-cd.md) - "Interactive Session" section

Summary:
- Use self-hosted runner
- Configure auto-login
- Run runner in interactive session (not as service)

### Lock Screen Interrupts Tests

✅ **Solution:** Disable lock screen

```powershell
# PowerShell as Admin
New-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" `
  -Name "InactivityTimeoutSecs" -Value 0 -PropertyType DWORD -Force

# Disable screensaver
Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name ScreenSaveActive -Value 0
```

## Resolution/DPI Issues

### Tests Fail with Different Resolution

**Symptoms:**
- Click on incorrect coordinates
- Screenshots show cut-off elements
- BoundingRectangle doesn't match

✅ **Solution 1:** Don't use coordinates, use AutomationIds

❌ **Don't do this:**
```csharp
Mouse.Click(new Point(100, 200));  // Absolute coordinates
```

✅ **Do this:**
```csharp
var button = FindElement("ButtonId");
button.Click();  // Click relative to element
```

✅ **Solution 2:** Configure fixed resolution in CI

```powershell
# PowerShell on runner
Set-DisplayResolution -Width 1920 -Height 1080 -Force
```

✅ **Solution 3:** DPI Awareness

If your app is DPI-aware, ensure tests are too:

```xml
<!-- app.manifest -->
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true</dpiAware>
  </windowsSettings>
</application>
```

## FlaUI Issues

### UIA3 vs UIA2

**When to use UIA2 instead of UIA3?**

Use UIA2 if:
- ❌ Legacy app (Win32, old WinForms)
- ❌ UIA3 doesn't find elements
- ❌ App uses custom/third-party controls

```csharp
// Change in AppLauncher.cs
// From:
_automation = new UIA3Automation();

// To:
_automation = new UIA2Automation();
```

### Important Differences

| Aspect | UIA3 | UIA2 |
|---------|------|------|
| Performance | ✅ Faster | ❌ Slower |
| Compatibility | ✅ Modern apps | ✅ Legacy apps |
| Maintenance | ✅ Active | ⚠️ Legacy |
| Features | ✅ More complete | ❌ Limited |

## Performance Issues

### Tests Very Slow

**Causes:**

#### 1. Timeouts Too Long

✅ **Solution:** Adjust timeouts appropriately

```json
// appsettings.json
{
  "DefaultTimeout": 3000  // Reduce from 5000 to 3000 if app is fast
}
```

#### 2. Unnecessary Waits

❌ **Problem:**
```csharp
Thread.Sleep(5000);  // Always waits 5 seconds
```

✅ **Solution:**
```csharp
WaitHelper.WaitForElement(window, "ButtonId", 5000);
// Returns immediately when element is found
```

#### 3. App Launches Slowly

✅ **Solution:** Increase launch timeout

```json
{
  "DefaultTimeout": 10000
}
```

```csharp
var window = launcher.LaunchApp(appPath, timeoutMs: 15000);
```

#### 4. Excessive Logging

✅ **Solution:** Reduce log level

```json
{
  "Serilog": {
    "MinimumLevel": "Information"  // Change from Debug to Information
  }
}
```

## Reporting Issues

### ExtentReports Not Generated

**Symptoms:**
- Report file doesn't exist
- Report is empty

✅ **Solutions:**

#### 1. Verify reports directory exists

```bash
ls src/Hipos.Tests/bin/Debug/net8.0-windows/reports/
# Should have: extent-report.html, cucumber.json
```

If empty:
- Verify ExtentReports is initialized in `TestHooks` or `BaseTest`
- Verify tests ran completely
- Check for exceptions during `AfterTestRun`/`OneTimeTearDown`

#### 2. Verify report is flushed

Check that `ExtentReportManager.Flush()` is called in:
- `TestHooks.AfterTestRun` (for SpecFlow)
- `BaseTest.OneTimeTearDown` (for NUnit)

### Screenshots Not Appearing in Report

✅ **Solution:**

Verify screenshots were saved:
```bash
ls src/Hipos.Tests/bin/Debug/net8.0-windows/reports/screenshots/
```

If empty:
- Test didn't fail (screenshots only on failures)
- Verify write permissions
- Verify `TestHooks.AfterScenario` or `BaseTest.TearDown` executes

### Cucumber JSON Not Generated

✅ **Solution:**

Verify `CucumberJsonReportGenerator` is initialized and flushed:

```csharp
// In TestHooks.cs
[BeforeTestRun]
public static void BeforeTestRun()
{
    // ...
    var cucumberJsonPath = ConfigManager.Instance.GetValue("Reporting:CucumberJsonPath", "reports/cucumber.json");
    CucumberJsonReportGenerator.Initialize(cucumberJsonPath, includeScreenshots: true);
}

[AfterTestRun]
public static void AfterTestRun()
{
    // ...
    CucumberJsonReportGenerator.GenerateReport();
}
```

## Configuration Issues

### appsettings.json Not Read

**Symptoms:**
- `AppPath` is null or empty
- Configuration uses defaults

✅ **Solutions:**

#### 1. Verify file exists in output

```bash
ls src/Hipos.Tests/bin/Debug/net8.0-windows/appsettings.json
```

If it doesn't exist, verify .csproj:
```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

#### 2. Verify valid JSON format

Use online JSON validator or:
```bash
# PowerShell
Get-Content appsettings.json | ConvertFrom-Json
```

### Environment Variables Don't Override

```bash
# Verify correct syntax
# Windows CMD
set AppPath=C:\path\to\app.exe
echo %AppPath%

# PowerShell
$env:AppPath = "C:\path\to\app.exe"
$env:AppPath

# Bash (Git Bash on Windows)
export AppPath="C:/path/to/app.exe"
echo $AppPath
```

## FAQ / Limitations

### Why are my tests flaky?

**Answer:** 99% of the time it's due to **insufficient or incorrect waits**.

- ❌ Don't use hardcoded `Thread.Sleep()`
- ✅ Use `WaitHelper.WaitForElement()` and variants
- ✅ Wait for specific conditions, not arbitrary times

### Can I run tests in parallel?

**Answer:** NOT recommended for UI tests.

**Reasons:**
- Only one app can have focus at a time
- Elements in background windows may not be accessible
- Race conditions between tests

If you want to parallelize:
- Use multiple runners/physical VMs (not on same machine)
- Or run each test in isolated VM/container

### Does it work with Remote Desktop applications?

**Answer:** NOT directly.

UI Automation needs direct access to Windows session. RDP creates a separate session.

**Alternatives:**
- Run tests ON the remote server (self-hosted runner)
- Use virtualization technologies (Hyper-V, VMware) instead of RDP

### Can I test 32-bit apps from 64-bit tests?

**Answer:** YES, FlaUI handles both architectures.

Ensure build configuration is correct:
```xml
<PropertyGroup>
  <PlatformTarget>AnyCPU</PlatformTarget>
</PropertyGroup>
```

### Does it work with Electron/Chromium applications?

**Answer:** PARTIALLY.

- Standard elements (buttons, textboxes) work
- Custom controls may not be accessible
- Consider Selenium/Playwright for web-based apps

## Debug Tools

### Inspect.exe (Windows SDK)
- View UI structure
- Identify AutomationIds
- Verify element properties

### FlaUI Inspect
```bash
# Install
dotnet tool install -g FlaUI.Inspect

# Run
flaui-inspect
```

### Spy++ (Visual Studio)
- Analyze window hierarchy
- View Windows messages
- Debug events

## Getting Help

If your problem isn't listed:

1. **Review logs:** `src/Hipos.Tests/bin/Debug/net8.0-windows/logs/`
2. **Manual screenshot capture:** During debug, see what's happening
3. **Use Inspect.exe:** Verify elements are accessible
4. **Simplify:** Create minimal test that reproduces the problem
5. **Search GitHub Issues:** [FlaUI Issues](https://github.com/FlaUI/FlaUI/issues)

## Next Steps

- **[Contributing](./contributing.md)** - Contribute to the project
- **[Framework Guide](./framework-guide.md)** - Back to guides
