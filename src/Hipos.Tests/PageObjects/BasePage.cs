using FlaUI.Core.AutomationElements;
using Hipos.Framework.Config;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Base class for all Page Objects.
/// Provides common functionality for interacting with elements using MSAA.
/// </summary>
public abstract class BasePage
{
    protected Window Window { get; }
    protected IntPtr WindowHandle { get; }
    protected int DefaultTimeout { get; }

    protected BasePage(Window window)
    {
        Window = window ?? throw new ArgumentNullException(nameof(window));
        WindowHandle = GetWindowHandle(window);
        DefaultTimeout = ConfigManager.Instance.DefaultTimeout;
    }

    /// <summary>
    /// Finds an MSAA element by its name path.
    /// </summary>
    /// <param name="namePath">Path of element names to find</param>
    /// <returns>MSAA element found</returns>
    protected MsaaHelper.MsaaElement FindElementByPath(params string[] namePath)
    {
        try
        {
            return MsaaHelper.FindByNamePath(WindowHandle, namePath);
        }
        catch (InvalidOperationException ex)
        {
            var path = string.Join(" > ", namePath);
            throw new InvalidOperationException(
                $"No se encontró elemento MSAA con ruta '{path}'.", ex);
        }
    }

    /// <summary>
    /// Verifies if an MSAA element exists by its name path.
    /// </summary>
    /// <param name="namePath">Path of element names to check</param>
    /// <returns>True if exists, false otherwise</returns>
    protected bool ElementExistsByPath(params string[] namePath)
    {
        return MsaaHelper.ExistsByNamePath(WindowHandle, namePath);
    }

    /// <summary>
    /// Clicks on an MSAA element by its name path.
    /// </summary>
    /// <param name="namePath">Path of element names to click</param>
    protected void ClickElement(params string[] namePath)
    {
        var element = FindElementByPath(namePath);
        element.Click();
    }

    /// <summary>
    /// Sets text on an MSAA element by its name path.
    /// </summary>
    /// <param name="text">Text to set</param>
    /// <param name="namePath">Path of element names</param>
    protected void SetElementText(string text, params string[] namePath)
    {
        var element = FindElementByPath(namePath);
        element.SetText(text);
    }

    /// <summary>
    /// Waits until an MSAA element disappears.
    /// </summary>
    /// <param name="namePath">Path of element names</param>
    /// <param name="timeoutMs">Timeout in milliseconds (optional)</param>
    /// <returns>True if the element disappeared, false if timeout</returns>
    protected bool WaitForElementToDisappear(string[] namePath, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeout;
        var path = string.Join(" > ", namePath);
        return WaitHelper.WaitUntil(
            () => !ElementExistsByPath(namePath),
            timeout,
            conditionDescription: $"elemento MSAA '{path}' desaparezca");
    }

    /// <summary>
    /// Parses a name path string from configuration into an array.
    /// </summary>
    /// <param name="rawPath">Raw path string (e.g., "parent > child > element")</param>
    /// <returns>Array of path parts</returns>
    protected static string[] ParseNamePath(string rawPath)
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

    /// <summary>
    /// Brings the window to the foreground.
    /// Useful when you need to ensure the window is active before an interaction.
    /// </summary>
    protected void EnsureWindowInForeground()
    {
        try
        {
            Window.Focus();
            Thread.Sleep(300); // Small pause for the window to respond
            Log.Debug("Window brought to foreground in Page Object");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not bring window to foreground in Page Object");
        }
    }

    /// <summary>
    /// Gets the native window handle from a FlaUI Window.
    /// </summary>
    /// <param name="window">FlaUI Window</param>
    /// <returns>Native window handle</returns>
    private static IntPtr GetWindowHandle(Window window)
    {
        if (!window.Properties.NativeWindowHandle.IsSupported)
        {
            throw new InvalidOperationException("La ventana no expone un handle nativo.");
        }

        return window.Properties.NativeWindowHandle.Value;
    }
}
