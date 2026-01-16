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
        AppLauncher?.EnsureWindowIsInForeground();
        _loginPage = new HiposLoginPage(MainWindow!);
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
}
