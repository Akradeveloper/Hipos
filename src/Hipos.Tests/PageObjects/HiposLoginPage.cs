using FlaUI.Core.AutomationElements;
using Serilog;

namespace Hipos.Tests.PageObjects;

public class HiposLoginPage : BasePage
{
    // Selectores MSAA como constantes estáticas
    // Nota: en HIPOS el input de "employee" no cuelga directo del root; requiere ruta completa.
    // Si en tu app los nombres cambian, ajusta estos segmentos según Inspect/AccExplorer.
    private static readonly string[] EmployeePath = { "signon", "datactrl", "employee", "input" };
    private static readonly string[] PasswordPath = { "signon", "datactrl","password" };
    private static readonly string[] LoginButtonPath = { "signon", "datactrl","ok" };
    private static readonly string[] DataCtrlPath = { "signon", "datactrl" };

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