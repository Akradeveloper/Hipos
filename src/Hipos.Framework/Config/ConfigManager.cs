using Microsoft.Extensions.Configuration;
using Serilog;

namespace Hipos.Framework.Config;

/// <summary>
/// Gestiona la configuración de la aplicación desde appsettings.json
/// y variables de entorno.
/// </summary>
public class ConfigManager
{
    private static ConfigManager? _instance;
    private static readonly object _lock = new();
    
    private readonly IConfiguration _configuration;

    private ConfigManager()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        _configuration = builder.Build();
    }

    /// <summary>
    /// Obtiene la instancia singleton del ConfigManager.
    /// </summary>
    public static ConfigManager Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ??= new ConfigManager();
            }
        }
    }

    /// <summary>
    /// Ruta al ejecutable de la aplicación a testear.
    /// </summary>
    public string AppPath => GetValue("AppPath", string.Empty);

    /// <summary>
    /// Timeout por defecto en milisegundos.
    /// </summary>
    public int DefaultTimeout => int.Parse(GetValue("DefaultTimeout", "5000"));

    /// <summary>
    /// Indica si los timeouts adaptativos están habilitados.
    /// </summary>
    public bool AdaptiveTimeoutsEnabled => bool.Parse(GetValue("Timeouts:Adaptive", "false"));

    /// <summary>
    /// Timeout inicial para timeouts adaptativos.
    /// </summary>
    public int InitialTimeout => int.Parse(GetValue("Timeouts:InitialTimeout", "5000"));

    /// <summary>
    /// Timeout mínimo permitido.
    /// </summary>
    public int MinTimeout => int.Parse(GetValue("Timeouts:MinTimeout", "2000"));

    /// <summary>
    /// Timeout máximo permitido.
    /// </summary>
    public int MaxTimeout => int.Parse(GetValue("Timeouts:MaxTimeout", "30000"));

    /// <summary>
    /// Tamaño de la ventana deslizante para tiempos de respuesta.
    /// </summary>
    public int ResponseTimeWindow => int.Parse(GetValue("Timeouts:ResponseTimeWindow", "10"));

    /// <summary>
    /// Indica si la grabación de video está habilitada.
    /// </summary>
    public bool VideoRecordingEnabled => bool.Parse(GetValue("VideoRecording:Enabled", "false"));

    /// <summary>
    /// Modo de grabación de video: "Always", "OnFailure", "OnSuccess", "Disabled".
    /// </summary>
    public string VideoRecordingMode => GetValue("VideoRecording:Mode", "Disabled");

    /// <summary>
    /// Directorio donde se guardarán los videos.
    /// </summary>
    public string VideoDirectory => GetValue("VideoRecording:VideoDirectory", "reports/videos");

    /// <summary>
    /// Frame rate para la grabación de video.
    /// </summary>
    public int VideoFrameRate => int.Parse(GetValue("VideoRecording:FrameRate", "10"));

    /// <summary>
    /// Calidad del video: "low", "medium", "high".
    /// </summary>
    public string VideoQuality => GetValue("VideoRecording:Quality", "medium");

    /// <summary>
    /// Obtiene un valor de configuración por clave.
    /// </summary>
    /// <param name="key">Clave de configuración (puede usar : para anidación)</param>
    /// <param name="defaultValue">Valor por defecto si no se encuentra</param>
    /// <returns>El valor de configuración</returns>
    public string GetValue(string key, string defaultValue)
    {
        var value = _configuration[key];
        
        if (string.IsNullOrEmpty(value))
        {
            Log.Debug("Configuración '{Key}' no encontrada, usando valor por defecto: {Default}", 
                key, defaultValue);
            return defaultValue;
        }

        return value;
    }

    /// <summary>
    /// Obtiene una sección de configuración completa.
    /// </summary>
    /// <param name="sectionName">Nombre de la sección</param>
    /// <returns>La sección de configuración</returns>
    public IConfigurationSection GetSection(string sectionName)
    {
        return _configuration.GetSection(sectionName);
    }
}