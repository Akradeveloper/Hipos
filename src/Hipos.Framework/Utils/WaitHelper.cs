using Serilog;

namespace Hipos.Framework.Utils;

/// <summary>
/// Utilidades para esperas explícitas y polling.
/// </summary>
public static class WaitHelper
{
    private const int DefaultPollingIntervalMs = 500;
    private const int MinPollingIntervalMs = 100;
    private const int MaxPollingIntervalMs = 1000;

    /// <summary>
    /// Espera hasta que una condición sea verdadera con polling fijo.
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
    /// Espera hasta que una condición sea verdadera con polling adaptativo.
    /// El polling empieza rápido y aumenta gradualmente si la condición no se cumple.
    /// Registra tiempos de respuesta para timeouts adaptativos.
    /// </summary>
    /// <param name="condition">Función que retorna true cuando la condición se cumple</param>
    /// <param name="timeoutMs">Timeout en milisegundos</param>
    /// <param name="conditionDescription">Descripción de la condición para logging</param>
    /// <param name="recordResponseTime">Si true, registra el tiempo de respuesta para timeouts adaptativos</param>
    /// <returns>True si la condición se cumplió, false si timeout</returns>
    public static bool WaitUntilAdaptive(
        Func<bool> condition,
        int timeoutMs,
        string conditionDescription = "condición",
        bool recordResponseTime = true)
    {
        Log.Debug("Esperando {Description} con polling adaptativo (timeout: {Timeout}ms)", conditionDescription, timeoutMs);

        var startTime = DateTime.Now;
        var attempts = 0;
        var currentPollingInterval = MinPollingIntervalMs;
        var elapsedMs = 0.0;

        while (elapsedMs < timeoutMs)
        {
            attempts++;
            
            try
            {
                if (condition())
                {
                    elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
                    
                    // Registrar tiempo de respuesta para timeouts adaptativos
                    if (recordResponseTime && elapsedMs > 0)
                    {
                        try
                        {
                            AdaptiveTimeoutManager.Instance.RecordResponseTime(elapsedMs);
                        }
                        catch
                        {
                            // Ignorar errores al registrar tiempo (no crítico)
                        }
                    }
                    
                    Log.Debug("{Description} cumplida después de {Attempts} intentos ({Elapsed}ms)", 
                        conditionDescription, attempts, (int)elapsedMs);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error verificando condición en intento {Attempts}", attempts);
            }

            Thread.Sleep(currentPollingInterval);

            // Calcular tiempo transcurrido
            elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;

            // Ajustar polling adaptativamente (backoff exponencial)
            if (elapsedMs < 2000)
            {
                // Primeros 2 segundos: polling rápido
                currentPollingInterval = MinPollingIntervalMs;
            }
            else if (elapsedMs < 5000)
            {
                // Entre 2-5 segundos: polling medio
                currentPollingInterval = 300;
            }
            else
            {
                // Después de 5 segundos: polling más lento
                currentPollingInterval = Math.Min(MaxPollingIntervalMs, currentPollingInterval + 100);
            }
        }

        Log.Warning("{Description} no se cumplió después de {Timeout}ms ({Attempts} intentos)", 
            conditionDescription, timeoutMs, attempts);
        return false;
    }
}
