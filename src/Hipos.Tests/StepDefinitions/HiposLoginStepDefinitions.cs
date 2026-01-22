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
    private HiposConfirmationOpenFundsPage? _confirmationOpenFundsPage;
    private HiposMainMenuPage? _mainMenuPage;

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

    [When("I click OK on the preview doc confirmation modal")]
    public void WhenIClickOkOnThePreviewDocConfirmationModal()
    {
        LogInfo("Esperando a que aparezca la modal de confirmación preview_doc");

        // Asegurarse de que tenemos la ventana principal
        Assert.That(MainWindow, Is.Not.Null, "La ventana principal de HIPOS debe estar disponible");

        // Crear o reutilizar la instancia del confirmation open funds page
        if (_confirmationOpenFundsPage == null)
        {
            _confirmationOpenFundsPage = new HiposConfirmationOpenFundsPage(MainWindow!);
        }

        try
        {
            // Esperar a que aparezca la modal
            var previewDocAppeared = _confirmationOpenFundsPage.WaitForPreviewDocToAppear();
            
            Assert.That(
                previewDocAppeared,
                Is.True,
                "La modal preview_doc debería aparecer");

            LogPass("Modal preview_doc apareció correctamente");

            // Hacer clic en el botón OK
            _confirmationOpenFundsPage.ClickOkButton();
            LogPass("Clic en el botón 'OK' de preview_doc realizado exitosamente");

            // Opcionalmente, esperar a que desaparezca la modal
            var previewDocDisappeared = _confirmationOpenFundsPage.WaitForPreviewDocToDisappear();
            
            if (previewDocDisappeared)
            {
                LogPass("Modal preview_doc desapareció después de hacer clic en 'OK'");
            }
            else
            {
                LogWarning("La modal preview_doc no desapareció después del timeout esperado");
            }
        }
        catch (InvalidOperationException ex)
        {
            LogFail($"Error al interactuar con la modal preview_doc: {ex.Message}", ex);
            throw;
        }
    }

    [Then("the main menu page should display all required elements")]
    public void ThenTheMainMenuPageShouldDisplayAllRequiredElements()
    {
        LogInfo("Verificando que todos los elementos requeridos del menú principal estén presentes");

        // Asegurarse de que tenemos la ventana principal
        Assert.That(MainWindow, Is.Not.Null, "La ventana principal de HIPOS debe estar disponible");

        // Crear o reutilizar la instancia del main menu page
        if (_mainMenuPage == null)
        {
            _mainMenuPage = new HiposMainMenuPage(MainWindow!);
        }

        try
        {
            // Verificar que todos los elementos existen
            var allElementsExist = _mainMenuPage.VerifyAllElementsExist();

            Assert.That(
                allElementsExist,
                Is.True,
                "Todos los elementos requeridos del menú principal (receipt, receiptinfo, article_info, main_data, keypad) deben estar presentes");

            // Verificación individual para logging detallado
            var receiptExists = _mainMenuPage.VerifyReceiptExists();
            var receiptInfoExists = _mainMenuPage.VerifyReceiptInfoExists();
            var articleInfoExists = _mainMenuPage.VerifyArticleInfoExists();
            var mainDataExists = _mainMenuPage.VerifyMainDataExists();
            var keypadExists = _mainMenuPage.VerifyKeypadExists();

            Assert.That(receiptExists, Is.True, "El elemento 'receipt' debe existir en el menú principal");
            Assert.That(receiptInfoExists, Is.True, "El elemento 'receiptinfo' debe existir en el menú principal");
            Assert.That(articleInfoExists, Is.True, "El elemento 'article_info' debe existir en el menú principal");
            Assert.That(mainDataExists, Is.True, "El elemento 'main_data' debe existir en el menú principal");
            Assert.That(keypadExists, Is.True, "El elemento 'keypad' debe existir en el menú principal");

            LogPass("Todos los elementos requeridos del menú principal están presentes: receipt, receiptinfo, article_info, main_data, keypad");
        }
        catch (InvalidOperationException ex)
        {
            LogFail($"Error al verificar los elementos del menú principal: {ex.Message}", ex);
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
