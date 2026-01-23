using Hipos.Framework.Config;
using Serilog;

namespace Hipos.Framework.Utils;

/// <summary>
/// Gestiona timeouts adaptativos basados en tiempos de respuesta medidos de la aplicación.
/// Ajusta automáticamente los timeouts según la velocidad real de la app.
/// </summary>
public class AdaptiveTimeoutManager
{
    private readonly Queue<double> _responseTimes = new();
    private readonly int _windowSize;
    private readonly int _minTimeout;
    private readonly int _maxTimeout;
    private readonly int _initialTimeout;
    private readonly double _safetyFactor;
    private static AdaptiveTimeoutManager? _instance;
    private static readonly object _lock = new();

    private AdaptiveTimeoutManager()
    {
        var config = ConfigManager.Instance;
        _windowSize = config.ResponseTimeWindow;
        _minTimeout = config.MinTimeout;
        _maxTimeout = config.MaxTimeout;
        _initialTimeout = config.InitialTimeout;
        _safetyFactor = 2.0; // Factor de seguridad fijo
    }

    /// <summary>
    /// Obtiene la instancia singleton del AdaptiveTimeoutManager.
    /// </summary>
    public static AdaptiveTimeoutManager Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ??= new AdaptiveTimeoutManager();
            }
        }
    }

    /// <summary>
    /// Registra un tiempo de respuesta de la aplicación.
    /// </summary>
    /// <param name="milliseconds">Tiempo de respuesta en milisegundos</param>
    public void RecordResponseTime(double milliseconds)
    {
        lock (_lock)
        {
            _responseTimes.Enqueue(milliseconds);
            
            // Mantener solo los últimos N tiempos (ventana deslizante)
            while (_responseTimes.Count > _windowSize)
            {
                _responseTimes.Dequeue();
            }
        }
    }

    /// <summary>
    /// Obtiene un timeout adaptativo basado en tiempos de respuesta medidos.
    /// </summary>
    /// <param name="baseTimeout">Timeout base si no hay datos suficientes</param>
    /// <returns>Timeout adaptativo calculado</returns>
    public int GetAdaptiveTimeout(int baseTimeout)
    {
        lock (_lock)
        {
            // Si no hay suficientes datos, usar timeout base
            if (_responseTimes.Count < 3)
            {
                return baseTimeout;
            }

            // Calcular percentil 95 de tiempos de respuesta
            var sortedTimes = _responseTimes.OrderBy(t => t).ToList();
            var percentile95Index = (int)Math.Ceiling(sortedTimes.Count * 0.95) - 1;
            var percentile95 = sortedTimes[percentile95Index];

            // Calcular timeout: percentil 95 * factor de seguridad
            var calculatedTimeout = (int)(percentile95 * _safetyFactor);

            // Aplicar límites
            var adaptiveTimeout = Math.Max(_minTimeout, Math.Min(_maxTimeout, calculatedTimeout));

            Log.Debug("Timeout adaptativo calculado: {Calculated}ms (basado en {Count} mediciones, P95: {Percentile}ms)", 
                adaptiveTimeout, _responseTimes.Count, (int)percentile95);

            return adaptiveTimeout;
        }
    }

    /// <summary>
    /// Obtiene el timeout adaptativo usando el timeout inicial como base.
    /// </summary>
    /// <returns>Timeout adaptativo</returns>
    public int GetAdaptiveTimeout()
    {
        return GetAdaptiveTimeout(_initialTimeout);
    }

    /// <summary>
    /// Resetea el historial de tiempos de respuesta.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _responseTimes.Clear();
            Log.Debug("Historial de tiempos de respuesta reseteado");
        }
    }

    /// <summary>
    /// Obtiene estadísticas de tiempos de respuesta.
    /// </summary>
    /// <returns>Estadísticas o null si no hay datos</returns>
    public ResponseTimeStats? GetStats()
    {
        lock (_lock)
        {
            if (_responseTimes.Count == 0)
            {
                return null;
            }

            var times = _responseTimes.ToList();
            return new ResponseTimeStats
            {
                Count = times.Count,
                Min = times.Min(),
                Max = times.Max(),
                Average = times.Average(),
                Median = times.OrderBy(t => t).Skip(times.Count / 2).First(),
                Percentile95 = times.OrderBy(t => t).Skip((int)(times.Count * 0.95)).First()
            };
        }
    }

    /// <summary>
    /// Estadísticas de tiempos de respuesta.
    /// </summary>
    public class ResponseTimeStats
    {
        public int Count { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public double Median { get; set; }
        public double Percentile95 { get; set; }
    }
}
