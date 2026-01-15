using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using TechTalk.SpecFlow;

namespace Hipos.Framework.Utils;

/// <summary>
/// Generates Cucumber JSON format reports compatible with Jira/Xray.
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
    /// Initializes the report generator.
    /// </summary>
    /// <param name="reportPath">Path where the cucumber.json file will be saved</param>
    public void Initialize(string reportPath)
    {
        lock (_lock)
        {
            _reportPath = reportPath;
            _features.Clear();
            _featureCache.Clear();
            
            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            Log.Information("CucumberJsonReportGenerator initialized: {ReportPath}", reportPath);
        }
    }

    /// <summary>
    /// Registers the start of a scenario.
    /// </summary>
    public void StartScenario(ScenarioInfo scenarioInfo, FeatureInfo featureInfo)
    {
        lock (_lock)
        {
            // Get or create feature
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

            // Create scenario
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
            
            Log.Debug("Scenario started: {ScenarioName}", scenarioInfo.Title);
        }
    }

    /// <summary>
    /// Registers the start of a step.
    /// </summary>
    public void StartStep(StepInfo stepInfo)
    {
        lock (_lock)
        {
            if (_currentScenario == null)
            {
                Log.Warning("No active scenario to register step");
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
            
            // Add arguments if they exist
            if (stepInfo.Table != null)
            {
                _currentStep.Rows = stepInfo.Table.Rows.Select(row => new CucumberRow
                {
                    Cells = row.Values.ToArray()
                }).ToArray();
            }
            
            _currentScenario.Steps.Add(_currentStep);
            
            Log.Debug("Step started: {StepKeyword} {StepText}", stepInfo.StepDefinitionType, stepInfo.Text);
        }
    }

    /// <summary>
    /// Registers the result of a step.
    /// </summary>
    public void FinishStep(ScenarioExecutionStatus status, Exception? error = null)
    {
        lock (_lock)
        {
            if (_currentStep == null)
            {
                Log.Warning("No active step to finish");
                return;
            }

            var duration = (long)(DateTime.UtcNow - _stepStartTime).TotalMilliseconds * 1_000_000; // Convert to nanoseconds
            
            _currentStep.Result = new CucumberResult
            {
                Status = MapStatus(status),
                Duration = duration,
                ErrorMessage = error?.Message
            };
            
            Log.Debug("Step finished: {Status}, Duration: {Duration}ms", _currentStep.Result.Status, duration / 1_000_000);
        }
    }

    /// <summary>
    /// Registers the end of a scenario.
    /// </summary>
    public void FinishScenario(ScenarioExecutionStatus status, Exception? error = null, string? screenshotPath = null)
    {
        lock (_lock)
        {
            if (_currentScenario == null)
            {
                Log.Warning("No active scenario to finish");
                return;
            }

            // Add screenshot as after hook if exists
            if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
            {
                var screenshotData = Convert.ToBase64String(File.ReadAllBytes(screenshotPath));
                var embedding = new CucumberEmbedding
                {
                    Data = screenshotData,
                    MimeType = "image/png",
                    Name = Path.GetFileName(screenshotPath)
                };

                // Add as "after" hook
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

            Log.Debug("Scenario finished: {ScenarioName}, Status: {Status}", _currentScenario.Name, status);
            
            _currentScenario = null;
            _currentStep = null;
        }
    }

    /// <summary>
    /// Generates the cucumber.json file with all results.
    /// </summary>
    public void GenerateReport()
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(_reportPath))
            {
                Log.Warning("Report path has not been configured");
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
                
                Log.Information("Cucumber JSON report generated successfully: {ReportPath}", _reportPath);
                Log.Information("Total features: {FeatureCount}, Total scenarios: {ScenarioCount}", 
                    _features.Count, 
                    _features.Sum(f => f.Elements.Count));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating Cucumber JSON report");
                throw;
            }
        }
    }

    /// <summary>
    /// Maps SpecFlow status to Cucumber format.
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
    /// Generates a valid ID from text.
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

#region Cucumber JSON Models

/// <summary>
/// Represents a Feature in Cucumber JSON format.
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
/// Represents a Scenario in Cucumber JSON format.
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
/// Represents a Step in Cucumber JSON format.
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
/// Represents the result of a Step.
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
/// Represents the match of a Step.
/// </summary>
public class CucumberMatch
{
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Tag.
/// </summary>
public class CucumberTag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }
}

/// <summary>
/// Represents a table row in a Step.
/// </summary>
public class CucumberRow
{
    [JsonPropertyName("cells")]
    public string[] Cells { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Represents a Hook (before/after).
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
/// Represents an embedding (screenshot, attached file).
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
