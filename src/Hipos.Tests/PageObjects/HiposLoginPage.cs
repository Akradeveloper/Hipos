using FlaUI.Core.AutomationElements;
using Serilog;

namespace Hipos.Tests.PageObjects;

public class HiposLoginPage : BasePage
{
    // Selectores MSAA como constantes estáticas
    private static readonly string[] EmployeePath = { "employee" };
    private static readonly string[] PasswordPath = { "password" };
    private static readonly string[] LoginButtonPath = { "login" };
    private static readonly string[] DataCtrlPath = { "datactrl" };

    public HiposLoginPage(Window window) : base(window)
    {
    }

    public void Login(string employee, string password)
    {
        EnsureWindowInForeground();
        Log.Information("MSAA login con employee: {Employee}", employee);

        SetElementText(employee, EmployeePath);
        SetElementText(password, PasswordPath);
        ClickElement(LoginButtonPath);
    }

    public bool WaitForDataCtrlToDisappear()
    {
        return WaitForElementToDisappear(DataCtrlPath);
    }
}