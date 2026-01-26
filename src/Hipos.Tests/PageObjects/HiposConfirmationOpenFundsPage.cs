using FlaUI.Core.AutomationElements;
using Hipos.Framework.Config;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object for the HIPOS OpenFunds confirmation modal.
/// Handles interactions with the preview_doc confirmation dialog.
/// </summary>
public class HiposConfirmationOpenFundsPage : BasePage
{
    private static readonly string[] PreviewDocPath = { "preview_doc" };
    private static readonly string[] OkButtonPath = { "preview_doc", "button_Ok" };

    public HiposConfirmationOpenFundsPage(Window window) : base(window)
    {
    }

    /// <summary>
    /// Waits until the preview_doc modal appears.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (optional)</param>
    /// <returns>True if the modal appeared, false if timeout</returns>
    public bool WaitForPreviewDocToAppear(int? timeoutMs = null)
    {
        var config = ConfigManager.Instance;
        var baseTimeout = timeoutMs ?? DefaultTimeout;
        
        // Usar timeout adaptativo si está habilitado
        var timeout = config.AdaptiveTimeoutsEnabled
            ? AdaptiveTimeoutManager.Instance.GetAdaptiveTimeout(baseTimeout)
            : baseTimeout;
        
        var path = string.Join(" > ", PreviewDocPath);
        Log.Information("Esperando a que aparezca la modal preview_doc (timeout: {Timeout}ms)", timeout);
        
        return WaitHelper.WaitUntilAdaptive(
            () => ElementExistsByPath(PreviewDocPath),
            timeout,
            conditionDescription: $"modal preview_doc '{path}' aparezca");
    }

    /// <summary>
    /// Clicks on the "OK" button inside the preview_doc modal.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the preview_doc or button_Ok does not exist</exception>
    public void ClickOkButton()
    {
        EnsureWindowInForeground();
        Log.Information("Haciendo clic en el botón 'OK' dentro de 'preview_doc'");

        // Verificar que el preview_doc existe
        if (!ElementExistsByPath(PreviewDocPath))
        {
            throw new InvalidOperationException(
                "El elemento 'preview_doc' no existe. Asegúrate de que la modal de confirmación esté visible.");
        }

        // Hacer clic en el botón OK
        ClickElement(OkButtonPath);
        Log.Information("Clic en el botón 'OK' realizado exitosamente");
    }

    /// <summary>
    /// Waits until the preview_doc modal disappears.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (optional)</param>
    /// <returns>True if the modal disappeared, false if timeout</returns>
    public bool WaitForPreviewDocToDisappear(int? timeoutMs = null)
    {
        return WaitForElementToDisappear(PreviewDocPath, timeoutMs);
    }
}
