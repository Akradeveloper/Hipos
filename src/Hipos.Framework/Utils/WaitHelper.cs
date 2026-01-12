using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Serilog;

namespace Hipos.Framework.Utils;

/// <summary>
/// Utilidades para esperas explícitas y polling.
/// </summary>
public static class WaitHelper
{
    private const int DefaultPollingIntervalMs = 500;

    /// <summary>
    /// Espera hasta que una condición sea verdadera.
    /// </summary>
    /// <param name="condition">Función que retorna true cuando la condición se cumple</param>
    /// <param name="timeoutMs">Timeout en milisegundos</param>
    /// <param name="pollingIntervalMs">Intervalo de polling en milisegundos</param>
    /// <param name="conditionDescription">Descripción de la condición para logging</param>
    /// <returns>True si la condición se cumplió, false si timeout</returns>
    public static bool WaitUntil(
        Func<bool> condition, 
        int timeoutMs, 
        int pollingIntervalMs = DefaultPollingIntervalMs,
        string conditionDescription = "condición")
    {
        Log.Debug("Esperando {Description} (timeout: {Timeout}ms)", conditionDescription, timeoutMs);

        var startTime = DateTime.Now;
        var attempts = 0;

        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
        {
            attempts++;
            
            try
            {
                if (condition())
                {
                    Log.Debug("{Description} cumplida después de {Attempts} intentos", 
                        conditionDescription, attempts);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error verificando condición en intento {Attempts}", attempts);
            }

            Thread.Sleep(pollingIntervalMs);
        }

        Log.Warning("{Description} no se cumplió después de {Timeout}ms ({Attempts} intentos)", 
            conditionDescription, timeoutMs, attempts);
        return false;
    }

    /// <summary>
    /// Espera hasta que un elemento esté disponible por su AutomationId.
    /// </summary>
    /// <param name="parent">Elemento padre donde buscar</param>
    /// <param name="automationId">AutomationId del elemento a buscar</param>
    /// <param name="timeoutMs">Timeout en milisegundos</param>
    /// <returns>El elemento encontrado, o null si timeout</returns>
    public static AutomationElement? WaitForElement(
        AutomationElement parent, 
        string automationId, 
        int timeoutMs)
    {
        Log.Debug("Esperando elemento con AutomationId: {AutomationId}", automationId);

        AutomationElement? foundElement = null;

        var found = WaitUntil(
            () =>
            {
                try
                {
                    foundElement = parent.FindFirstDescendant(cf => 
                        cf.ByAutomationId(automationId));
                    return foundElement != null;
                }
                catch
                {
                    return false;
                }
            },
            timeoutMs,
            conditionDescription: $"elemento '{automationId}'");

        if (found && foundElement != null)
        {
            Log.Information("Elemento encontrado: {AutomationId}", automationId);
            return foundElement;
        }

        Log.Warning("Elemento no encontrado: {AutomationId}", automationId);
        return null;
    }

    /// <summary>
    /// Espera hasta que aparezca una ventana con el título especificado.
    /// </summary>
    /// <param name="title">Título de la ventana (puede ser parcial)</param>
    /// <param name="timeoutMs">Timeout en milisegundos</param>
    /// <returns>True si la ventana apareció, false si timeout</returns>
    public static bool WaitForWindowTitle(string title, int timeoutMs)
    {
        Log.Debug("Esperando ventana con título: {Title}", title);

        var found = WaitUntil(
            () =>
            {
                try
                {
                    var mainWindow = Core.AppLauncher.Instance.MainWindow;
                    return mainWindow != null && 
                           mainWindow.Title.Contains(title, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            },
            timeoutMs,
            conditionDescription: $"ventana '{title}'");

        return found;
    }

    /// <summary>
    /// Espera hasta que un elemento esté habilitado.
    /// </summary>
    /// <param name="element">Elemento a verificar</param>
    /// <param name="timeoutMs">Timeout en milisegundos</param>
    /// <returns>True si el elemento se habilitó, false si timeout</returns>
    public static bool WaitForElementEnabled(AutomationElement element, int timeoutMs)
    {
        Log.Debug("Esperando que elemento esté habilitado: {Name}", element.Name);

        return WaitUntil(
            () => element.IsEnabled,
            timeoutMs,
            conditionDescription: $"elemento '{element.Name}' habilitado");
    }

    /// <summary>
    /// Espera hasta que un elemento sea clickeable.
    /// </summary>
    /// <param name="element">Elemento a verificar</param>
    /// <param name="timeoutMs">Timeout en milisegundos</param>
    /// <returns>True si el elemento es clickeable, false si timeout</returns>
    public static bool WaitForElementClickable(AutomationElement element, int timeoutMs)
    {
        Log.Debug("Esperando que elemento sea clickeable: {Name}", element.Name);

        return WaitUntil(
            () => element.IsEnabled && element.IsOffscreen == false,
            timeoutMs,
            conditionDescription: $"elemento '{element.Name}' clickeable");
    }
}
