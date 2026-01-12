using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using Serilog;

namespace Hipos.Framework.Utils;

/// <summary>
/// Gestiona la generación de reportes HTML con ExtentReports.
/// </summary>
public class ExtentReportManager
{
    private static ExtentReports? _extent;
    private static ExtentTest? _currentTest;
    private static readonly object _lock = new();
    private static string? _reportPath;

    /// <summary>
    /// Inicializa el reporte ExtentReports.
    /// </summary>
    /// <param name="reportPath">Ruta donde se guardará el reporte HTML</param>
    public static void InitializeReport(string reportPath)
    {
        lock (_lock)
        {
            if (_extent != null) return;

            _reportPath = reportPath;
            
            // Crear directorio si no existe
            var directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Configurar el reporter HTML
            var htmlReporter = new ExtentSparkReporter(reportPath);
            
            // Configuración del reporte
            htmlReporter.Config.DocumentTitle = "Hipos Test Report";
            htmlReporter.Config.ReportName = "UI Automation Test Results";
            htmlReporter.Config.Theme = AventStack.ExtentReports.Reporter.Config.Theme.Dark;
            htmlReporter.Config.Encoding = "UTF-8";
            
            // Inicializar ExtentReports
            _extent = new ExtentReports();
            _extent.AttachReporter(htmlReporter);
            
            // Información del sistema
            _extent.AddSystemInfo("Framework", "Hipos Automation Framework");
            _extent.AddSystemInfo("Environment", "Windows");
            _extent.AddSystemInfo("Test Runner", "NUnit 4.2");
            _extent.AddSystemInfo("Automation Tool", "FlaUI 4.0");
            
            Log.Information("ExtentReports inicializado: {ReportPath}", reportPath);
        }
    }

    /// <summary>
    /// Crea un nuevo test en el reporte.
    /// </summary>
    /// <param name="testName">Nombre del test</param>
    /// <param name="description">Descripción del test</param>
    /// <returns>ExtentTest instance</returns>
    public static ExtentTest CreateTest(string testName, string? description = null)
    {
        lock (_lock)
        {
            if (_extent == null)
            {
                throw new InvalidOperationException(
                    "ExtentReports no está inicializado. Llama a InitializeReport primero.");
            }

            _currentTest = _extent.CreateTest(testName, description ?? string.Empty);
            Log.Debug("Test creado en reporte: {TestName}", testName);
            return _currentTest;
        }
    }

    /// <summary>
    /// Asigna categorías al test actual.
    /// </summary>
    public static void AssignCategory(params string[] categories)
    {
        lock (_lock)
        {
            if (_currentTest != null)
            {
                _currentTest.AssignCategory(categories);
            }
        }
    }

    /// <summary>
    /// Asigna el autor del test.
    /// </summary>
    public static void AssignAuthor(params string[] authors)
    {
        lock (_lock)
        {
            if (_currentTest != null)
            {
                _currentTest.AssignAuthor(authors);
            }
        }
    }

    /// <summary>
    /// Registra un paso exitoso.
    /// </summary>
    public static void LogPass(string message)
    {
        lock (_lock)
        {
            _currentTest?.Pass(message);
            Log.Information("✓ {Message}", message);
        }
    }

    /// <summary>
    /// Registra un paso informativo.
    /// </summary>
    public static void LogInfo(string message)
    {
        lock (_lock)
        {
            _currentTest?.Info(message);
            Log.Information("ℹ {Message}", message);
        }
    }

    /// <summary>
    /// Registra un paso de advertencia.
    /// </summary>
    public static void LogWarning(string message)
    {
        lock (_lock)
        {
            _currentTest?.Warning(message);
            Log.Warning("⚠ {Message}", message);
        }
    }

    /// <summary>
    /// Registra un paso fallido.
    /// </summary>
    public static void LogFail(string message, Exception? exception = null)
    {
        lock (_lock)
        {
            if (exception != null)
            {
                _currentTest?.Fail(exception);
                Log.Error(exception, "✗ {Message}", message);
            }
            else
            {
                _currentTest?.Fail(message);
                Log.Error("✗ {Message}", message);
            }
        }
    }

    /// <summary>
    /// Adjunta un screenshot al test actual.
    /// </summary>
    public static void AttachScreenshot(string screenshotPath)
    {
        lock (_lock)
        {
            if (_currentTest != null && File.Exists(screenshotPath))
            {
                try
                {
                    var fileName = Path.GetFileName(screenshotPath);
                    _currentTest.AddScreenCaptureFromPath(screenshotPath, fileName);
                    Log.Information("Screenshot adjuntado: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "No se pudo adjuntar screenshot: {Path}", screenshotPath);
                }
            }
        }
    }

    /// <summary>
    /// Marca el test actual como omitido.
    /// </summary>
    public static void LogSkip(string message)
    {
        lock (_lock)
        {
            _currentTest?.Skip(message);
            Log.Information("⊘ Test omitido: {Message}", message);
        }
    }

    /// <summary>
    /// Finaliza y guarda el reporte.
    /// </summary>
    public static void FlushReport()
    {
        lock (_lock)
        {
            _extent?.Flush();
            Log.Information("Reporte HTML generado: {ReportPath}", _reportPath);
        }
    }

    /// <summary>
    /// Obtiene el test actual.
    /// </summary>
    public static ExtentTest? CurrentTest => _currentTest;

    /// <summary>
    /// Obtiene la ruta del reporte.
    /// </summary>
    public static string? ReportPath => _reportPath;
}
