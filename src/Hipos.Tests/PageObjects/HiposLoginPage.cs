using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object para el login inicial de HIPOS.
/// </summary>
public class HiposLoginPage : BasePage
{
    private const string EmployeePaneId = "employee";
    private const string EmployeeInputId = "input";
    private const string PasswordId = "password";
    private const string OkButtonId = "ok";
    private const string DataCtrlId = "datactrl";

    public HiposLoginPage(Window window) : base(window)
    {
        Log.Information("Inicializando HiposLoginPage");
    }

    public void EnterEmployeeAndTab(string employee)
    {
        Log.Information("Ingresando employee: {Employee}", employee);
        ExtentReportManager.LogInfo($"Ingresando employee: {employee}");

        var employeeInput = FindEmployeeInput();
        employeeInput.SetText(employee);

        Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.TAB);

        WaitHelper.WaitUntil(
            () => ElementExists(PasswordId),
            DefaultTimeout,
            conditionDescription: "campo password disponible");
    }

    public void EnterPassword(string password)
    {
        Log.Information("Ingresando password");
        ExtentReportManager.LogInfo("Ingresando password");

        var passwordInput = FindElement(PasswordId);
        passwordInput.SetText(password);
    }

    public void ClickOk()
    {
        Log.Information("Click en botón OK");
        ExtentReportManager.LogInfo("Click en botón OK");

        var okButton = FindElement(OkButtonId);
        okButton.Click();
    }

    public void Login(string employee, string password)
    {
        EnsureWindowInForeground();
        EnterEmployeeAndTab(employee);
        EnterPassword(password);
        ClickOk();
    }

    public bool WaitForDataCtrlToDisappear(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeout;
        return WaitHelper.WaitUntil(
            () => !ElementExists(DataCtrlId),
            timeout,
            conditionDescription: "datactrl desaparezca");
    }

    public bool IsDataCtrlPresent()
    {
        return ElementExists(DataCtrlId);
    }

    private ElementWrapper FindEmployeeInput()
    {
        var employeePane = WaitHelper.WaitForElement(Window, EmployeePaneId, DefaultTimeout);
        if (employeePane == null)
        {
            throw new InvalidOperationException(
                $"No se encontró el contenedor de employee con AutomationId: {EmployeePaneId}");
        }

        var input = WaitHelper.WaitForElement(employeePane, EmployeeInputId, DefaultTimeout);
        if (input == null)
        {
            throw new InvalidOperationException(
                $"No se encontró el input de employee con AutomationId: {EmployeeInputId}");
        }

        return new ElementWrapper(input, DefaultTimeout);
    }
}
