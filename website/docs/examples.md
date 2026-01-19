---
sidebar_position: 6
---

# Test Examples

Examples focused on HIPOS login using MSAA.

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

## MSAA Configuration

Define the MSAA selectors in `src/Hipos.Tests/appsettings.json`:

```json
{
  "Msaa": {
    "SearchMaxDepth": 6,
    "Login": {
      "EmployeeNamePath": "employee",
      "PasswordNamePath": "password",
      "LoginButtonNamePath": "login",
      "DataCtrlNamePath": "datactrl"
    }
  }
}
```
