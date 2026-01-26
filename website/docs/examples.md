---
sidebar_position: 6
---

# Test Examples

Examples focused on HIPOS login using MSAA (accessed through FlaUI window handles).

## SpecFlow Feature

```gherkin
Feature: HIPOS login
  Scenario: Successful login hides datactrl
    Given the HIPOS login page is open
    When I login with employee "-1" and password "000000"
    Then the datactrl element should not exist
```

## Step Definitions

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

## Page Object Implementation

MSAA selectors are defined as static constants in the PageObject. MSAA interactions use window handles obtained from FlaUI Window objects:

```csharp
public class HiposLoginPage : BasePage
{
    // MSAA selectors as static constants
    private static readonly string[] EmployeePath = { "employee" };
    private static readonly string[] PasswordPath = { "password" };
    private static readonly string[] LoginButtonPath = { "login" };
    private static readonly string[] DataCtrlPath = { "datactrl" };

    public HiposLoginPage(Window window) : base(window) { }

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

**Benefits:**
- Selectors are encapsulated with the PageObject
- Type-safe (compile-time checking)
- No configuration file needed
- Easier to maintain and refactor
