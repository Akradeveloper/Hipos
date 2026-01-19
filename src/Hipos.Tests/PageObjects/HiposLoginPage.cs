using FlaUI.Core.AutomationElements;
using Hipos.Framework.Config;
using Serilog;

namespace Hipos.Tests.PageObjects;

public class HiposLoginPage : BasePage
{
    private readonly string[] _employeePath;
    private readonly string[] _passwordPath;
    private readonly string[] _loginButtonPath;
    private readonly string[] _dataCtrlPath;

    public HiposLoginPage(Window window) : base(window)
    {
        var config = ConfigManager.Instance;
        _employeePath = ParseNamePath(config.GetValue("Msaa:Login:EmployeeNamePath", "employee"));
        _passwordPath = ParseNamePath(config.GetValue("Msaa:Login:PasswordNamePath", "password"));
        _loginButtonPath = ParseNamePath(config.GetValue("Msaa:Login:LoginButtonNamePath", "login"));
        _dataCtrlPath = ParseNamePath(config.GetValue("Msaa:Login:DataCtrlNamePath", "datactrl"));
    }

    public void Login(string employee, string password)
    {
        EnsureWindowInForeground();
        Log.Information("MSAA login con employee: {Employee}", employee);

        SetElementText(employee, _employeePath);
        SetElementText(password, _passwordPath);
        ClickElement(_loginButtonPath);
    }

    public bool WaitForDataCtrlToDisappear()
    {
        return WaitForElementToDisappear(_dataCtrlPath);
    }
}