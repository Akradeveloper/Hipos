using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using TechTalk.SpecFlow;

namespace Hipos.Framework.Utils;

/// <summary>
/// Generador de reportes en formato Cucumber JSON compatible con Jira/Xray.
/// </summary>
public class CucumberJsonReportGenerator
{
    private readonly List<CucumberFeature> _features = new();
    private CucumberFeature? _currentFeature;
    private CucumberScenario? _currentScenario;
    private CucumberStep? _currentStep;
    private readonly Dictionary<string, CucumberFeature> _featureCache = new();
    private readonly object _lock = new();
    private string? _reportPath;
    private DateTime _stepStartTime;

    /// <summary>
    /// Inicializa el generador de reportes.
    /// </summary>
    /// <param name="reportPath">Ruta donde se guardará el archivo cucumber.json</param>
    public void Initialize(string reportPath)
    {
        lock (_lock)
        {
            _reportPath = reportPath;
            _features.Clear();
            _featureCache.Clear();
            
            // Crear directorio si no existe
            var directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            Log.Information("CucumberJsonReportGenerator inicializado: {ReportPath}", reportPath);
        }
    }

    /// <summary>
    /// Registra el inicio de un escenario.
    /// </summary>
    public void StartScenario(ScenarioInfo scenarioInfo, FeatureInfo featureInfo)
    {
        lock (_lock)
        {
            // Obtener o crear feature
            var featureId = GenerateId(featureInfo.Title);
            if (!_featureCache.TryGetValue(featureId, out _currentFeature))
            {
                _currentFeature = new CucumberFeature
                {
                    Id = featureId,
                    Name = featureInfo.Title,
                    Description = featureInfo.Description ?? string.Empty,
                    Line = 1,
                    Keyword = "Feature",
                    Uri = $"Features/{Path.GetFileName(scenarioInfo.GetType().Assembly.Location)}.feature",
                    Tags = featureInfo.Tags?.Select(t => new CucumberTag { Name = t, Line = 1 }).ToArray() ?? Array.Empty<CucumberTag>()
                };
                _features.Add(_currentFeature);
                _featureCache[featureId] = _currentFeature;
            }

            // Crear escenario
            var scenarioId = $"{featureId};{GenerateId(scenarioInfo.Title)}";
            _currentScenario = new CucumberScenario
            {
                Id = scenarioId,
                Name = scenarioInfo.Title,
                Description = scenarioInfo.Description ?? string.Empty,
                Line = 1,
                Keyword = "Scenario",
                Type = "scenario",
                Tags = scenarioInfo.Tags?.Select(t => new CucumberTag { Name = t, Line = 1 }).ToArray() ?? Array.Empty<CucumberTag>(),
                StartTimestamp = DateTime.UtcNow
            };
            
            _currentFeature.Elements.Add(_currentScenario);
            
            Log.Debug("Escenario iniciado: {ScenarioName}", scenarioInfo.Title);
        }
    }

    /// <summary>
    /// Registra el inicio de un step.
    /// </summary>
    public void StartStep(StepInfo stepInfo)
    {
        lock (_lock)
        {
            if (_currentScenario == null)
            {
                Log.Warning("No hay escenario activo para registrar step");
                return;
            }

            _stepStartTime = DateTime.UtcNow;
            
            _currentStep = new CucumberStep
            {
                Name = stepInfo.Text,
                Line = 1,
                Keyword = stepInfo.StepDefinitionType.ToString() + " ",
                Match = new CucumberMatch { Location = "StepDefinition" }
            };
            
            // Agregar argumentos si existen
            if (stepInfo.Table != null)
            {
                _currentStep.Rows = stepInfo.Table.Rows.Select(row => new CucumberRow
                {
                    Cells = row.Values.ToArray()
                }).ToArray();
            }
            
            _currentScenario.Steps.Add(_currentStep);
            
            Log.Debug("Step iniciado: {StepKeyword} {StepText}", stepInfo.StepDefinitionType, stepInfo.Text);
        }
    }

    /// <summary>
    /// Registra el resultado de un step.
    /// </summary>
    public void FinishStep(ScenarioExecutionStatus status, Exception? error = null)
    {
        lock (_lock)
        {
            if (_currentStep == null)
            {
                Log.Warning("No hay step activo para finalizar");
                return;
            }

            var duration = (long)(DateTime.UtcNow - _stepStartTime).TotalMilliseconds * 1_000_000; // Convertir a nanosegundos
            
            _currentStep.Result = new CucumberResult
            {
                Status = MapStatus(status),
                Duration = duration,
                ErrorMessage = error?.Message
            };
            
            Log.Debug("Step finalizado: {Status}, Duración: {Duration}ms", _currentStep.Result.Status, duration / 1_000_000);
        }
    }

    /// <summary>
    /// Registra el fin de un escenario.
    /// </summary>
    public void FinishScenario(ScenarioExecutionStatus status, Exception? error = null, string? screenshotPath = null)
    {
        lock (_lock)
        {
            if (_currentScenario == null)
            {
                Log.Warning("No hay escenario activo para finalizar");
                return;
            }

            // Agregar screenshot como after hook si existe
            if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
            {
                var screenshotData = Convert.ToBase64String(File.ReadAllBytes(screenshotPath));
                var embedding = new CucumberEmbedding
                {
                    Data = screenshotData,
                    MimeType = "image/png",
                    Name = Path.GetFileName(screenshotPath)
                };

                // Agregar como hook "after"
                var afterHook = new CucumberHook
                {
                    Match = new CucumberMatch { Location = "AfterScenario" },
                    Result = new CucumberResult
                    {
                        Status = "passed",
                        Duration = 0
                    },
                    Embeddings = new[] { embedding }
                };
                
                _currentScenario.After = new[] { afterHook };
            }

            Log.Debug("Escenario finalizado: {ScenarioName}, Status: {Status}", _currentScenario.Name, status);
            
            _currentScenario = null;
            _currentStep = null;
        }
    }

    /// <summary>
    /// Genera el archivo cucumber.json con todos los resultados.
    /// </summary>
    public void GenerateReport()
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(_reportPath))
            {
                Log.Warning("No se ha configurado la ruta del reporte");
                return;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(_features, options);
                File.WriteAllText(_reportPath, json);
                
                Log.Information("Reporte Cucumber JSON generado exitosamente: {ReportPath}", _reportPath);
                Log.Information("Total features: {FeatureCount}, Total scenarios: {ScenarioCount}", 
                    _features.Count, 
                    _features.Sum(f => f.Elements.Count));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al generar reporte Cucumber JSON");
                throw;
            }
        }
    }

    /// <summary>
    /// Mapea el estado de SpecFlow al formato Cucumber.
    /// </summary>
    private string MapStatus(ScenarioExecutionStatus status)
    {
        return status switch
        {
            ScenarioExecutionStatus.OK => "passed",
            ScenarioExecutionStatus.TestError => "failed",
            ScenarioExecutionStatus.BindingError => "failed",
            ScenarioExecutionStatus.UndefinedStep => "undefined",
            ScenarioExecutionStatus.StepDefinitionPending => "pending",
            ScenarioExecutionStatus.Skipped => "skipped",
            _ => "undefined"
        };
    }

    /// <summary>
    /// Genera un ID válido a partir de un texto.
    /// </summary>
    private string GenerateId(string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("ñ", "n")
            .Replace("¿", "").Replace("?", "")
            .Replace("¡", "").Replace("!", "")
            .Replace(",", "").Replace(".", "")
            .Replace("(", "").Replace(")", "");
    }

    public string? ReportPath => _reportPath;
}

#region Modelos Cucumber JSON

/// <summary>
/// Representa un Feature en el formato Cucumber JSON.
/// </summary>
public class CucumberFeature
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public CucumberTag[] Tags { get; set; } = Array.Empty<CucumberTag>();

    [JsonPropertyName("elements")]
    public List<CucumberScenario> Elements { get; set; } = new();
}

/// <summary>
/// Representa un Scenario en el formato Cucumber JSON.
/// </summary>
public class CucumberScenario
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public CucumberTag[] Tags { get; set; } = Array.Empty<CucumberTag>();

    [JsonPropertyName("steps")]
    public List<CucumberStep> Steps { get; set; } = new();

    [JsonPropertyName("start_timestamp")]
    public DateTime StartTimestamp { get; set; }

    [JsonPropertyName("after")]
    public CucumberHook[]? After { get; set; }
}

/// <summary>
/// Representa un Step en el formato Cucumber JSON.
/// </summary>
public class CucumberStep
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public CucumberResult Result { get; set; } = new();

    [JsonPropertyName("match")]
    public CucumberMatch Match { get; set; } = new();

    [JsonPropertyName("rows")]
    public CucumberRow[]? Rows { get; set; }
}

/// <summary>
/// Representa el resultado de un Step.
/// </summary>
public class CucumberResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public long Duration { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Representa el match de un Step.
/// </summary>
public class CucumberMatch
{
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Representa un Tag.
/// </summary>
public class CucumberTag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }
}

/// <summary>
/// Representa una fila de tabla en un Step.
/// </summary>
public class CucumberRow
{
    [JsonPropertyName("cells")]
    public string[] Cells { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Representa un Hook (before/after).
/// </summary>
public class CucumberHook
{
    [JsonPropertyName("match")]
    public CucumberMatch Match { get; set; } = new();

    [JsonPropertyName("result")]
    public CucumberResult Result { get; set; } = new();

    [JsonPropertyName("embeddings")]
    public CucumberEmbedding[]? Embeddings { get; set; }
}

/// <summary>
/// Representa un embedding (screenshot, archivo adjunto).
/// </summary>
public class CucumberEmbedding
{
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

#endregion
