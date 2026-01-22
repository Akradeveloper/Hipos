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
    private HiposCalendarPage? _calendarPage;
    private HiposConfirmationPage? _confirmationPage;
    private HiposOpenFundsPage? _openFundsPage;

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

    [When("I select the last available day in the calendar")]
    public void WhenISelectTheLastAvailableDayInTheCalendar()
    {
        LogInfo("Seleccionando el último día disponible del calendario");

        // Asegurarse de que tenemos la ventana principal
        Assert.That(MainWindow, Is.Not.Null, "La ventana principal de HIPOS debe estar disponible");

        // Crear o reutilizar la instancia del calendar page
        if (_calendarPage == null)
        {
            _calendarPage = new HiposCalendarPage(MainWindow!);
        }

        try
        {
            _calendarPage.SelectLastAvailableDay();
            LogPass("Último día disponible seleccionado exitosamente");
        }
        catch (InvalidOperationException ex)
        {
            LogFail($"Error al seleccionar el último día disponible: {ex.Message}", ex);
            throw;
        }
    }

    [Then("the date_picker element should not exist")]
    public void ThenTheDatePickerElementShouldNotExist()
    {
        LogInfo("Verificando que el elemento 'date_picker' ya no existe");

        // Asegurarse de que tenemos la ventana principal
        Assert.That(MainWindow, Is.Not.Null, "La ventana principal de HIPOS debe estar disponible");

        // Crear o reutilizar la instancia del calendar page
        if (_calendarPage == null)
        {
            _calendarPage = new HiposCalendarPage(MainWindow!);
        }

        var datePickerGone = _calendarPage.WaitForDatePickerToDisappear();

        Assert.That(
            datePickerGone,
            Is.True,
            "El elemento 'date_picker' debería desaparecer después de seleccionar un día");

        LogPass("date_picker ya no existe después de seleccionar el día");
    }

    [When("I click Yes on the confirmation messagebox")]
    public void WhenIClickYesOnTheConfirmationMessagebox()
    {
        LogInfo("Esperando a que aparezca el messagebox de confirmación");

        // Asegurarse de que tenemos la ventana principal
        Assert.That(MainWindow, Is.Not.Null, "La ventana principal de HIPOS debe estar disponible");

        // Crear o reutilizar la instancia del confirmation page
        if (_confirmationPage == null)
        {
            _confirmationPage = new HiposConfirmationPage(MainWindow!);
        }

        try
        {
            // Esperar a que aparezca el messagebox
            var messageBoxAppeared = _confirmationPage.WaitForMessageBoxToAppear();
            
            Assert.That(
                messageBoxAppeared,
                Is.True,
                "El messagebox de confirmación debería aparecer");

            LogPass("Messagebox de confirmación apareció correctamente");

            // Hacer clic en el botón Yes
            _confirmationPage.ClickYesButton();
            LogPass("Clic en el botón 'Yes' realizado exitosamente");

            // Opcionalmente, esperar a que desaparezca el messagebox
            var messageBoxDisappeared = _confirmationPage.WaitForMessageBoxToDisappear();
            
            if (messageBoxDisappeared)
            {
                LogPass("Messagebox desapareció después de hacer clic en 'Yes'");
            }
            else
            {
                LogWarning("El messagebox no desapareció después del timeout esperado");
            }
        }
        catch (InvalidOperationException ex)
        {
            LogFail($"Error al interactuar con el messagebox de confirmación: {ex.Message}", ex);
            throw;
        }
    }

    [When("I click OK on the counting button")]
    public void WhenIClickOkOnTheCountingButton()
    {
        LogInfo("Haciendo clic en el botón OK del counting button");

        // Asegurarse de que tenemos la ventana principal
        Assert.That(MainWindow, Is.Not.Null, "La ventana principal de HIPOS debe estar disponible");

        // Crear o reutilizar la instancia del open funds page
        if (_openFundsPage == null)
        {
            _openFundsPage = new HiposOpenFundsPage(MainWindow!);
        }

        try
        {
            _openFundsPage.ClickOkButton();
            LogPass("Clic en el botón 'OK' del counting button realizado exitosamente");
        }
        catch (InvalidOperationException ex)
        {
            LogFail($"Error al hacer clic en el botón OK del counting button: {ex.Message}", ex);
            throw;
        }
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
