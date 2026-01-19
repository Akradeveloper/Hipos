using FlaUI.Core.AutomationElements;
using Hipos.Framework.Config;
using Hipos.Framework.Utils;
using Hipos.Tests.PageObjects;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace Hipos.Tests.StepDefinitions;

[Binding]
public class HiposLoginStepDefinitions : BaseStepDefinitions
{
    private HiposLoginPage? _loginPage;

    [Given("the HIPOS login page is open")]
    public void GivenTheHiposLoginPageIsOpen()
    {
        LogInfo("Verificando que la ventana de login de HIPOS esté disponible");
        Assert.That(MainWindow, Is.Not.Null, "La ventana principal de HIPOS debe estar disponible");

        var hiposWindow = WaitForWindowByTitle("HIPOS", ConfigManager.Instance.DefaultTimeout);
        TestContextHelper.MainWindow = hiposWindow;
        hiposWindow.Focus();

        _loginPage = new HiposLoginPage(hiposWindow);
        LogPass("Login de HIPOS listo");
    }

    [When("I login with employee \"(.*)\" and password \"(.*)\"")]
    public void WhenILoginWithEmployeeAndPassword(string employee, string password)
    {
        LogInfo($"Login con employee: {employee}");
        _loginPage!.Login(employee, password);
    }

    [Then("the datactrl element should not exist")]
    public void ThenTheDataCtrlElementShouldNotExist()
    {
        var dataCtrlGone = _loginPage!.WaitForDataCtrlToDisappear();

        Assert.That(
            dataCtrlGone,
            Is.True,
            "El elemento 'datactrl' debería desaparecer después del login");

        LogPass("datactrl ya no existe después del login");
    }

    private Window WaitForWindowByTitle(string titlePart, int timeoutMs)
    {
        Window? foundWindow = null;

        var found = WaitHelper.WaitUntil(
            () =>
            {
                var automation = AppLauncher?.Automation;
                if (automation == null)
                {
                    return false;
                }

                var windows = automation.GetDesktop().FindAllChildren();
                foreach (var element in windows)
                {
                    var window = element.AsWindow();
                    if (window == null || window.IsOffscreen)
                    {
                        continue;
                    }

                    var title = window.Title ?? string.Empty;
                    if (title.Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                    {
                        foundWindow = window;
                        return true;
                    }
                }

                return false;
            },
            timeoutMs,
            conditionDescription: $"ventana con título '{titlePart}'");

        if (!found || foundWindow == null)
        {
            throw new TimeoutException($"No se encontró la ventana con título '{titlePart}' en {timeoutMs}ms.");
        }

        return foundWindow;
    }
}