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
}
