using FlaUI.Core.Exceptions;
using Serilog;

namespace Hipos.Framework.Utils;

/// <summary>
/// Política de reintentos para operaciones que pueden fallar de forma transitoria.
/// NO reintenta fallos de aserción.
/// </summary>
public static class RetryPolicy
{
    /// <summary>
    /// Ejecuta una acción con reintentos en caso de errores transitorios.
    /// </summary>
    /// <param name="action">Acción a ejecutar</param>
    /// <param name="maxRetries">Número máximo de reintentos</param>
    /// <param name="delayMs">Delay entre reintentos en milisegundos</param>
    public static void Execute(Action action, int maxRetries = 3, int delayMs = 1000)
    {
        Execute(() =>
        {
            action();
            return true;
        }, maxRetries, delayMs);
    }

    /// <summary>
    /// Ejecuta una función con reintentos en caso de errores transitorios.
    /// </summary>
    /// <typeparam name="T">Tipo de retorno</typeparam>
    /// <param name="func">Función a ejecutar</param>
    /// <param name="maxRetries">Número máximo de reintentos</param>
    /// <param name="delayMs">Delay entre reintentos en milisegundos</param>
    /// <returns>Resultado de la función</returns>
    public static T Execute<T>(Func<T> func, int maxRetries = 3, int delayMs = 1000)
    {
        var attempts = 0;
        var exceptions = new List<Exception>();

        while (attempts <= maxRetries)
        {
            attempts++;

            try
            {
                return func();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);

                // No reintentar si es un fallo de aserción (AssertionException de NUnit)
                if (IsAssertionException(ex))
                {
                    Log.Warning("Fallo de aserción detectado, no se reintentará");
                    throw;
                }

                // No reintentar si no es un error transitorio
                if (!IsTransientError(ex))
                {
                    Log.Warning("Error no transitorio detectado, no se reintentará: {ExceptionType}", 
                        ex.GetType().Name);
                    throw;
                }

                if (attempts > maxRetries)
                {
                    Log.Error("Se alcanzó el máximo de reintentos ({MaxRetries})", maxRetries);
                    throw new AggregateException(
                        $"Operación falló después de {maxRetries} reintentos", 
                        exceptions);
                }

                Log.Warning("Intento {Attempt} falló con error transitorio: {Message}. Reintentando en {Delay}ms...",
                    attempts, ex.Message, delayMs);

                Thread.Sleep(delayMs);
            }
        }

        throw new InvalidOperationException("Esto no debería ocurrir");
    }

    /// <summary>
    /// Determina si una excepción es un error transitorio que puede reintentarse.
    /// </summary>
    private static bool IsTransientError(Exception ex)
    {
        // Errores transitorios típicos en automatización UI
        return ex is ElementNotAvailableException
            || ex is TimeoutException
            || ex is InvalidOperationException && ex.Message.Contains("not available")
            || ex is InvalidOperationException && ex.Message.Contains("not clickeable")
            || ex is InvalidOperationException && ex.Message.Contains("not enabled");
    }

    /// <summary>
    /// Determina si una excepción es un fallo de aserción.
    /// </summary>
    private static bool IsAssertionException(Exception ex)
    {
        var exceptionType = ex.GetType().FullName ?? string.Empty;
        
        // NUnit AssertionException
        return exceptionType.Contains("AssertionException")
            || exceptionType.Contains("Assert");
    }
}
