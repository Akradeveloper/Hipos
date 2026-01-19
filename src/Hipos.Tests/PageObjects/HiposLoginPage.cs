using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object para el login inicial de HIPOS usando MSAA.
/// </summary>
public class HiposLoginPage : BasePage
{
    private const string SignonPaneId = "signon";
    private const string DataCtrlId = "datactrl";
    private const string EmployeePaneId = "employee";
    private const string EmployeeInputId = "input";
    private const string PasswordId = "password";
    private const string OkButtonId = "ok";

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

        var passwordInput = FindPasswordInput();
        passwordInput.SetText(password);
    }

    public void ClickOk()
    {
        Log.Information("Click en botón OK");
        ExtentReportManager.LogInfo("Click en botón OK");

        var okButton = FindOkButton();
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
        var windowHandle = GetWindowHandle();
        return WaitHelper.WaitUntil(
            () => !MsaaHelper.ExistsByNamePath(windowHandle, SignonPaneId, DataCtrlId),
            timeout,
            conditionDescription: "datactrl desaparezca");
    }

    public bool IsDataCtrlPresent()
    {
        var windowHandle = GetWindowHandle();
        return MsaaHelper.ExistsByNamePath(windowHandle, SignonPaneId, DataCtrlId);
    }

    private MsaaHelper.MsaaElement FindEmployeeInput()
    {
        var windowHandle = GetWindowHandle();
        return MsaaHelper.FindByNamePath(
            windowHandle,
            SignonPaneId,
            DataCtrlId,
            EmployeePaneId,
            EmployeeInputId);
    }

    private MsaaHelper.MsaaElement FindPasswordInput()
    {
        var windowHandle = GetWindowHandle();
        return MsaaHelper.FindByNamePath(windowHandle, SignonPaneId, DataCtrlId, PasswordId);
    }

    private MsaaHelper.MsaaElement FindOkButton()
    {
        var windowHandle = GetWindowHandle();
        return MsaaHelper.FindByNamePath(windowHandle, SignonPaneId, DataCtrlId, OkButtonId);
    }

    private IntPtr GetWindowHandle()
    {
        if (!Window.Properties.NativeWindowHandle.IsSupported)
        {
            throw new InvalidOperationException("El handle nativo de la ventana no está disponible.");
        }

        return Window.Properties.NativeWindowHandle.Value;
    }
}