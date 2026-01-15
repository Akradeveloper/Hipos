using FlaUI.Core.AutomationElements;
using Hipos.Framework.Config;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Base class for all Page Objects.
/// Provides common functionality for interacting with elements.
/// </summary>
public abstract class BasePage
{
    protected Window Window { get; }
    protected int DefaultTimeout { get; }

    protected BasePage(Window window)
    {
        Window = window ?? throw new ArgumentNullException(nameof(window));
        DefaultTimeout = ConfigManager.Instance.DefaultTimeout;
    }

    /// <summary>
    /// Finds an element by its AutomationId and wraps it in an ElementWrapper.
    /// </summary>
    /// <param name="automationId">AutomationId of the element</param>
    /// <returns>ElementWrapper of the found element</returns>
    protected ElementWrapper FindElement(string automationId)
    {
        Log.Debug("Looking for element on page: {AutomationId}", automationId);

        var element = WaitHelper.WaitForElement(Window, automationId, DefaultTimeout);
        
        if (element == null)
        {
            throw new InvalidOperationException(
                $"Element not found with AutomationId: {automationId}");
        }

        return new ElementWrapper(element, DefaultTimeout);
    }

    /// <summary>
    /// Verifies if an element exists by its AutomationId.
    /// </summary>
    /// <param name="automationId">AutomationId of the element</param>
    /// <returns>True if exists, false otherwise</returns>
    protected bool ElementExists(string automationId)
    {
        try
        {
            var element = Window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            return element != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Waits until an element is visible.
    /// </summary>
    /// <param name="automationId">AutomationId of the element</param>
    /// <param name="timeoutMs">Timeout in milliseconds (optional)</param>
    /// <returns>True if the element is visible, false if timeout</returns>
    protected bool WaitForElementVisible(string automationId, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeout;
        return WaitHelper.WaitUntil(
            () => ElementExists(automationId),
            timeout,
            conditionDescription: $"element '{automationId}' visible");
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
}
