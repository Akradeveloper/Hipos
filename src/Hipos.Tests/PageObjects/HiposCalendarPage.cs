using FlaUI.Core.AutomationElements;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.PageObjects;

/// <summary>
/// Page Object for the HIPOS calendar date picker.
/// </summary>
public class HiposCalendarPage : BasePage
{
    private static readonly string[] DatePickerPath = { "date_picker" };
    private const string DayElementPattern = "day_*";

    public HiposCalendarPage(Window window) : base(window)
    {
    }

    /// <summary>
    /// Selects the last available day in the calendar.
    /// Iterates through all day elements in reverse order and selects the first one that is not unavailable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no available day is found or if the date_picker element does not exist</exception>
    public void SelectLastAvailableDay()
    {
        EnsureWindowInForeground();
        Log.Information("Buscando el último día disponible en el calendario");

        // Verificar que el date_picker existe
        if (!ElementExistsByPath(DatePickerPath))
        {
            throw new InvalidOperationException(
                "El elemento 'date_picker' no existe. Asegúrate de que el calendario esté visible.");
        }

        // Obtener todos los elementos day_* del calendario
        var dayElements = FindElementsByPattern(DatePickerPath, DayElementPattern).ToList();

        if (dayElements.Count == 0)
        {
            throw new InvalidOperationException(
                $"No se encontraron elementos que coincidan con el patrón '{DayElementPattern}' en el calendario.");
        }

        Log.Debug("Se encontraron {Count} elementos de día en el calendario", dayElements.Count);

        // Iterar en orden inverso (del último al primero) para encontrar el último día disponible
        for (int i = dayElements.Count - 1; i >= 0; i--)
        {
            var dayElement = dayElements[i];
            var dayName = dayElement.GetName();
            
            Log.Debug("Verificando día: {DayName}", dayName);

            // Verificar si el día está disponible (no unavailable)
            if (!dayElement.IsUnavailable())
            {
                Log.Information("Día disponible encontrado: {DayName}. Seleccionando...", dayName);
                dayElement.Click();
                
                // Pequeña espera para que la selección se procese
                System.Threading.Thread.Sleep(200);
                
                Log.Information("Día seleccionado exitosamente: {DayName}", dayName);
                return;
            }
            else
            {
                Log.Debug("Día {DayName} está unavailable, continuando búsqueda...", dayName);
            }
        }

        // Si llegamos aquí, no se encontró ningún día disponible
        throw new InvalidOperationException(
            "No se encontró ningún día disponible en el calendario. Todos los días están marcados como 'unavailable'.");
    }

    /// <summary>
    /// Waits until the date_picker element disappears.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (optional)</param>
    /// <returns>True if the element disappeared, false if timeout</returns>
    public bool WaitForDatePickerToDisappear(int? timeoutMs = null)
    {
        return WaitForElementToDisappear(DatePickerPath, timeoutMs);
    }
}