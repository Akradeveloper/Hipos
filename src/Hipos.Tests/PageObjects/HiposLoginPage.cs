using FlaUI.Core.AutomationElements;
using Hipos.Framework.Config;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

public class HiposLoginPage : BasePage
{
    private readonly IntPtr _windowHandle;
    private readonly string[] _employeePath;
    private readonly string[] _passwordPath;
    private readonly string[] _loginButtonPath;
    private readonly string[] _dataCtrlPath;

    public HiposLoginPage(Window window) : base(window)
    {
        _windowHandle = GetWindowHandle(window);

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

        SetText(_employeePath, employee);
        SetText(_passwordPath, password);
        Click(_loginButtonPath);
    }

    public bool WaitForDataCtrlToDisappear()
    {
        return WaitHelper.WaitUntil(
            () => !Exists(_dataCtrlPath),
            DefaultTimeout,
            conditionDescription: "elemento MSAA 'datactrl' desaparezca");
    }

    private void SetText(string[] namePath, string text)
    {
        var element = FindElementByPath(namePath);
        element.SetText(text);
    }

    private void Click(string[] namePath)
    {
        var element = FindElementByPath(namePath);
        element.Click();
    }

    private bool Exists(string[] namePath)
    {
        return MsaaHelper.ExistsByNamePath(_windowHandle, namePath);
    }

    private MsaaHelper.MsaaElement FindElementByPath(string[] namePath)
    {
        try
        {
            return MsaaHelper.FindByNamePath(_windowHandle, namePath);
        }
        catch (InvalidOperationException ex)
        {
            var path = string.Join(" > ", namePath);
            throw new InvalidOperationException(
                $"No se encontró elemento MSAA con ruta '{path}'.", ex);
        }
    }

    private static IntPtr GetWindowHandle(Window window)
    {
        if (!window.Properties.NativeWindowHandle.IsSupported)
        {
            throw new InvalidOperationException("La ventana no expone un handle nativo.");
        }

        return window.Properties.NativeWindowHandle.Value;
    }

    private static string[] ParseNamePath(string rawPath)
    {
        var parts = rawPath
            .Split('>')
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrEmpty(part))
            .ToArray();

        if (parts.Length == 0)
        {
            throw new InvalidOperationException("MSAA name path inválido en configuración.");
        }

        return parts;
    }
}