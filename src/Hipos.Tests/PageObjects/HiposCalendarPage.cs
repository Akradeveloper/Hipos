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
    /// Selects a random available day in the calendar.
    /// Finds all available days and selects one randomly from the list.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no available day is found or if the date_picker element does not exist</exception>
    public void SelectLastAvailableDay()
    {
        EnsureWindowInForeground();
        Log.Information("Buscando días disponibles en el calendario para selección aleatoria");

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

        // Primero, encontrar todos los días disponibles
        var availableDays = new List<(int index, MsaaHelper.MsaaElement element)>();
        
        for (int i = dayElements.Count - 1; i >= 0; i--)
        {
            var dayElement = dayElements[i];
            var dayName = dayElement.GetName();
            
            Log.Debug("Verificando día: {DayName}", dayName);

            // Verificar si el día está disponible (no unavailable)
            if (!dayElement.IsUnavailable())
            {
                availableDays.Add((i, dayElement));
                Log.Debug("Día disponible encontrado: {DayName} (índice: {Index})", dayName, i);
            }
        }

        if (availableDays.Count == 0)
        {
            throw new InvalidOperationException(
                "No se encontró ningún día disponible en el calendario. Todos los días están marcados como 'unavailable'.");
        }

        // Seleccionar un día aleatorio de todos los días disponibles
        var random = new Random();
        int randomIndex = random.Next(0, availableDays.Count);
        var targetDay = availableDays[randomIndex];
        var targetDayName = targetDay.element.GetName();

        Log.Information("Seleccionando día aleatorio disponible: {DayName} (posición {Position} de {Total} días disponibles)", 
            targetDayName, randomIndex + 1, availableDays.Count);
        
        targetDay.element.Click();
        
        // Pequeña espera para que la selección se procese
        System.Threading.Thread.Sleep(200);
        
        Log.Information("Día seleccionado exitosamente: {DayName}", targetDayName);
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
