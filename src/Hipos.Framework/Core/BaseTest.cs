using FlaUI.Core.AutomationElements;
using NUnit.Framework;
using Serilog;
using Hipos.Framework.Config;
using Hipos.Framework.Utils;

namespace Hipos.Framework.Core;

/// <summary>
/// Clase base para todos los tests de UI.
/// Maneja el ciclo de vida de la aplicación, screenshots y reporting.
/// </summary>
public abstract class BaseTest
{
    protected Window? MainWindow { get; private set; }
    protected AppLauncher AppLauncher { get; private set; } = null!;
    
    private string? _testName;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Configurar Serilog
        var logPath = ConfigManager.Instance.GetValue("Serilog:WriteTo:0:Args:path", "logs/test-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("==== Iniciando suite de tests ====");

        // Inicializar ExtentReports
        var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "reports", "extent-report.html");
        ExtentReportManager.InitializeReport(reportPath);

        try
        {
            // Obtener ruta de la aplicación desde configuración
            var appPath = ConfigManager.Instance.AppPath;
            var timeout = ConfigManager.Instance.DefaultTimeout;

            // Lanzar aplicación UNA SOLA VEZ para toda la suite
            AppLauncher = AppLauncher.Instance;
            MainWindow = AppLauncher.LaunchApp(appPath, timeout);

            Log.Information("Aplicación lanzada para suite de tests");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al lanzar aplicación en OneTimeSetUp");
            throw;
        }
    }

    [SetUp]
    public void SetUp()
    {
        _testName = TestContext.CurrentContext.Test.Name;
        Log.Information("==== Iniciando test: {TestName} ====", _testName);
        
        // Asegurar que la ventana esté en primer plano antes de cada test
        try
        {
            AppLauncher?.EnsureWindowIsInForeground();
            Log.Debug("Ventana asegurada en primer plano para test: {TestName}", _testName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo traer la ventana al frente en SetUp");
        }
        
        // Crear test en ExtentReports
        var testDescription = TestContext.CurrentContext.Test.Properties.Get("Description")?.ToString();
        var test = ExtentReportManager.CreateTest(_testName!, testDescription);
        
        // Asignar categorías si existen
        var categories = TestContext.CurrentContext.Test.Properties["Category"];
        if (categories != null)
        {
            var categoryList = categories.ToString()?.Split(',');
            if (categoryList != null && categoryList.Length > 0)
            {
                ExtentReportManager.AssignCategory(categoryList);
            }
        }
    }

    [TearDown]
    public void TearDown()
    {
        var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
        Log.Information("Test {TestName} finalizado con estado: {Status}", _testName, testStatus);

        try
        {
            // Si el test falló, tomar screenshot y adjuntarlo
            if (testStatus == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                Log.Warning("Test falló, capturando screenshot");
                var screenshotPath = ScreenshotHelper.TakeScreenshot(_testName ?? "unknown");
                
                if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
                {
                    Log.Information("Screenshot capturado: {Path}", screenshotPath);
                    TestContext.AddTestAttachment(screenshotPath, "Screenshot on Failure");
                    ExtentReportManager.AttachScreenshot(screenshotPath);
                }
                
                // Log del error en ExtentReports
                var errorMessage = TestContext.CurrentContext.Result.Message;
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ExtentReportManager.LogFail(errorMessage);
                }
            }

            // Adjuntar logs del test al reporte
            AttachLogsToReport();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al capturar evidencias en TearDown");
        }

        Log.Information("==== Test finalizado: {TestName} ====\n", _testName);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Log.Information("==== Finalizando suite de tests ====");
        
        // Cerrar aplicación UNA SOLA VEZ al final de toda la suite
        try
        {
            AppLauncher?.CloseApp();
            Log.Information("Aplicación cerrada");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cerrar aplicación en OneTimeTearDown");
        }
        
        // Finalizar reporte ExtentReports
        ExtentReportManager.FlushReport();
        Log.Information("Reporte HTML generado: {ReportPath}", ExtentReportManager.ReportPath);
        
        AppLauncher?.Dispose();
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Adjunta los logs del test al reporte.
    /// </summary>
    private void AttachLogsToReport()
    {
        try
        {
            var logPath = ConfigManager.Instance.GetValue("Serilog:WriteTo:0:Args:path", "logs/test-.log");
            var logFile = FindLatestLogFile(logPath);

            if (!string.IsNullOrEmpty(logFile) && File.Exists(logFile))
            {
                TestContext.AddTestAttachment(logFile, "Test Logs");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudieron adjuntar los logs al reporte");
        }
    }

    /// <summary>
    /// Encuentra el archivo de log más reciente basado en el patrón.
    /// </summary>
    private string? FindLatestLogFile(string pattern)
    {
        try
        {
            var directory = Path.GetDirectoryName(pattern) ?? "logs";
            var fileName = Path.GetFileName(pattern);
            
            if (!Directory.Exists(directory))
                return null;

            // Buscar archivos que coincidan con el patrón
            var files = Directory.GetFiles(directory, "test-*.log")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            return files.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
