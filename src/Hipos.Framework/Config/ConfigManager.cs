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