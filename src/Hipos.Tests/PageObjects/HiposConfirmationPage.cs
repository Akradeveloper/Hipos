using FlaUI.Core.AutomationElements;
using Hipos.Framework.Config;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object for the HIPOS confirmation messagebox.
/// Handles interactions with the messagebox dialog that appears during tests.
/// </summary>
public class HiposConfirmationPage : BasePage
{
    private static readonly string[] MessageBoxPath = { "messagebox" };
    private static readonly string[] YesButtonPath = { "messagebox", "Yes" };

    public HiposConfirmationPage(Window window) : base(window)
    {
    }

    /// <summary>
    /// Waits until the messagebox element appears.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (optional)</param>
    /// <returns>True if the messagebox appeared, false if timeout</returns>
    public bool WaitForMessageBoxToAppear(int? timeoutMs = null)
    {
        var config = ConfigManager.Instance;
        var baseTimeout = timeoutMs ?? DefaultTimeout;
        
        // Usar timeout adaptativo si está habilitado
        var timeout = config.AdaptiveTimeoutsEnabled
            ? AdaptiveTimeoutManager.Instance.GetAdaptiveTimeout(baseTimeout)
            : baseTimeout;
        
        var path = string.Join(" > ", MessageBoxPath);
        Log.Information("Esperando a que aparezca el messagebox (timeout: {Timeout}ms)", timeout);
        
        return WaitHelper.WaitUntilAdaptive(
            () => ElementExistsByPath(MessageBoxPath),
            timeout,
            conditionDescription: $"messagebox '{path}' aparezca");
    }

    /// <summary>
    /// Clicks on the "Yes" button inside the messagebox.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the messagebox or Yes button does not exist</exception>
    public void ClickYesButton()
    {
        EnsureWindowInForeground();
        Log.Information("Haciendo clic en el botón 'Yes' del messagebox");

        // Verificar que el messagebox existe
        if (!ElementExistsByPath(MessageBoxPath))
        {
            throw new InvalidOperationException(
                "El elemento 'messagebox' no existe. Asegúrate de que el cuadro de mensaje esté visible.");
        }

        // Hacer clic en el botón Yes
        ClickElement(YesButtonPath);
        Log.Information("Clic en el botón 'Yes' realizado exitosamente");
    }

    /// <summary>
    /// Waits until the messagebox element disappears.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (optional)</param>
    /// <returns>True if the messagebox disappeared, false if timeout</returns>
    public bool WaitForMessageBoxToDisappear(int? timeoutMs = null)
    {
        return WaitForElementToDisappear(MessageBoxPath, timeoutMs);
    }
}