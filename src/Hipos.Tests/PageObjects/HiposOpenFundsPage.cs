using FlaUI.Core.AutomationElements;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object for the HIPOS OpenFunds counting button.
/// Handles interactions with the counting button and its OK button.
/// </summary>
public class HiposOpenFundsPage : BasePage
{
    private static readonly string[] CountingButtonPath = { "counting_button" };
    private static readonly string[] OkButtonPath = { "counting_button", "ok" };

    public HiposOpenFundsPage(Window window) : base(window)
    {
    }

    /// <summary>
    /// Clicks on the "OK" button inside the counting_button.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the counting_button or OK button does not exist</exception>
    public void ClickOkButton()
    {
        EnsureWindowInForeground();
        Log.Information("Haciendo clic en el botón 'OK' dentro de 'counting_button'");

        // Verificar que el counting_button existe
        if (!ElementExistsByPath(CountingButtonPath))
        {
            throw new InvalidOperationException(
                "El elemento 'counting_button' no existe. Asegúrate de que el botón esté visible.");
        }

        // Hacer clic en el botón OK
        ClickElement(OkButtonPath);
        Log.Information("Clic en el botón 'OK' realizado exitosamente");
    }
}