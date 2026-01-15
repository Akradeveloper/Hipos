using Hipos.Framework.Config;
using Hipos.Framework.Core;
using Hipos.Framework.Utils;
using Serilog;
using TechTalk.SpecFlow;
using NUnit.Framework;

namespace Hipos.Tests.Hooks;

/// <summary>
/// Hooks de SpecFlow para manejar el ciclo de vida de los tests.
/// Reemplaza la funcionalidad de BaseTest para tests BDD.
/// </summary>
[Binding]
public class TestHooks
{
    private static bool _isInitialized = false;
    private static readonly object _initLock = new();
    private static CucumberJsonReportGenerator? _cucumberReportGenerator;
    
    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        lock (_initLock)
        {
            if (_isInitialized) return;
            
            // Configurar Serilog
            var logPath = ConfigManager.Instance.GetValue("Serilog:WriteTo:0:Args:path", "logs/test-.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("==== Starting BDD test suite ====");

            // Inicializar ExtentReports
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "reports", "extent-report.html");
            ExtentReportManager.InitializeReport(reportPath);

            // Initialize Cucumber JSON report generator
            var cucumberJsonPath = ConfigManager.Instance.GetValue("Reporting:CucumberJsonPath", "reports/cucumber.json");
            var fullCucumberPath = Path.Combine(Directory.GetCurrentDirectory(), cucumberJsonPath);
            _cucumberReportGenerator = new CucumberJsonReportGenerator();
            _cucumberReportGenerator.Initialize(fullCucumberPath);
            Log.Information("Cucumber JSON report generator initialized: {Path}", fullCucumberPath);

            try
            {
                // Obtener ruta de la aplicación desde configuración
                var appPath = ConfigManager.Instance.AppPath;
                var timeout = ConfigManager.Instance.DefaultTimeout;

                // Lanzar aplicación UNA SOLA VEZ para toda la suite
                var appLauncher = AppLauncher.Instance;
                var mainWindow = appLauncher.LaunchApp(appPath, timeout);

                // Guardar en el helper estático
                StepDefinitions.TestContextHelper.AppLauncher = appLauncher;
                StepDefinitions.TestContextHelper.MainWindow = mainWindow;

                Log.Information("Application launched for BDD test suite");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al lanzar aplicación en BeforeTestRun");
                throw;
            }
            
            _isInitialized = true;
        }
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        lock (_initLock)
        {
            if (!_isInitialized) return;
            
            Log.Information("==== Finishing BDD test suite ====");
            
            // Close application ONLY ONCE at the end of the entire suite - FORCE COMPLETE CLOSURE
            try
            {
                var appLauncher = StepDefinitions.TestContextHelper.AppLauncher;
                if (appLauncher != null)
                {
                    Log.Information("==== Closing application completely ====");
                    
                    // Get PID before closing
                    var application = appLauncher.Application;
                    int? processId = null;
                    if (application != null && !application.HasExited)
                    {
                        processId = application.ProcessId;
                        Log.Information("Process to close - PID: {ProcessId}", processId);
                    }
                    
                    // Method 1: Close normally
                    appLauncher.CloseApp();
                    Thread.Sleep(1500);
                    
                    // Method 2: Verify and force close using System.Diagnostics.Process
                    if (processId.HasValue)
                    {
                        try
                        {
                            var process = System.Diagnostics.Process.GetProcessById(processId.Value);
                            if (process != null && !process.HasExited)
                            {
                                Log.Warning("Process still active (PID: {ProcessId}), forcing close with Kill()...", processId);
                                process.Kill();
                                if (!process.WaitForExit(3000))
                                {
                                    Log.Warning("El proceso no terminó en 3 segundos, forzando nuevamente...");
                                    try
                                    {
                                        process.Kill();
                                        process.WaitForExit(2000);
                                    }
                                    catch { }
                                }
                                process.Dispose();
                                Log.Information("Process terminated forcefully");
                            }
                        }
                        catch (ArgumentException)
                        {
                            // Process no longer exists, that's ok
                            Log.Information("Process already terminated (PID: {ProcessId})", processId);
                        }
                        catch (Exception procEx)
                        {
                            Log.Warning(procEx, "Error al verificar/forzar cierre del proceso por PID");
                        }
                    }
                    
                    // Método 3: Verificar desde FlaUI y forzar si es necesario
                    if (application != null && !application.HasExited)
                    {
                        Log.Warning("FlaUI reporta que la aplicación aún está activa, forzando cierre...");
                        try
                        {
                            application.Kill();
                            Thread.Sleep(1000);
                        }
                        catch (Exception killEx)
                        {
                            Log.Warning(killEx, "Error al forzar cierre desde FlaUI");
                        }
                    }
                    
                    // Método 4: Buscar procesos por nombre como último recurso
                    try
                    {
                        var appPath = ConfigManager.Instance.AppPath;
                        var processName = Path.GetFileNameWithoutExtension(appPath);
                        var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                        if (processes.Length > 0)
                        {
                            Log.Warning("Encontrados {Count} procesos con nombre '{ProcessName}', forzando cierre...", processes.Length, processName);
                            foreach (var proc in processes)
                            {
                                try
                                {
                                    if (!proc.HasExited)
                                    {
                                        proc.Kill();
                                        proc.WaitForExit(2000);
                                    }
                                    proc.Dispose();
                                }
                                catch (Exception procEx)
                                {
                                    Log.Warning(procEx, "Error al cerrar proceso: {ProcessName}", processName);
                                }
                            }
                        }
                    }
                    catch (Exception nameEx)
                    {
                        Log.Warning(nameEx, "Error al buscar procesos por nombre");
                    }
                    
                    Log.Information("✓ Application closed completely");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al cerrar aplicación en AfterTestRun");
                
                // Último recurso: buscar y matar todos los procesos de la calculadora
                try
                {
                    var appPath = ConfigManager.Instance.AppPath;
                    var processName = Path.GetFileNameWithoutExtension(appPath);
                    var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                    foreach (var proc in processes)
                    {
                        try
                        {
                            if (!proc.HasExited)
                            {
                                Log.Warning("Forcing closure of residual process: PID {ProcessId}", proc.Id);
                                proc.Kill();
                                proc.WaitForExit(2000);
                            }
                            proc.Dispose();
                        }
                        catch { }
                    }
                }
                catch (Exception finalEx)
                {
                    Log.Error(finalEx, "Error en último intento de cierre");
                }
            }
            
            // Finalizar reporte ExtentReports
            try
            {
                ExtentReportManager.FlushReport();
                Log.Information("Reporte HTML generado: {ReportPath}", ExtentReportManager.ReportPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error al finalizar reporte");
            }
            
            // Generate Cucumber JSON report
            try
            {
                _cucumberReportGenerator?.GenerateReport();
                Log.Information("Cucumber JSON report generated: {ReportPath}", _cucumberReportGenerator?.ReportPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error generating Cucumber JSON report");
            }
            
            // Dispose del AppLauncher
            try
            {
                var appLauncherToDispose = StepDefinitions.TestContextHelper.AppLauncher;
                appLauncherToDispose?.Dispose();
                StepDefinitions.TestContextHelper.AppLauncher = null;
                StepDefinitions.TestContextHelper.MainWindow = null;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error al hacer dispose del AppLauncher");
            }
            
            Log.CloseAndFlush();
            
            _isInitialized = false;
        }
    }

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        var scenarioTitle = scenarioContext.ScenarioInfo.Title;
        var scenarioDescription = scenarioContext.ScenarioInfo.Description;
        
        Log.Information("==== Starting scenario: {ScenarioTitle} ====", scenarioTitle);
        
        // Register scenario in Cucumber JSON
        try
        {
            _cucumberReportGenerator?.StartScenario(scenarioContext.ScenarioInfo, featureContext.FeatureInfo);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error registering scenario in Cucumber JSON");
        }
        
        // Asegurar que la ventana esté SIEMPRE en primer plano antes de cada escenario
        try
        {
            var appLauncher = StepDefinitions.TestContextHelper.AppLauncher;
            var mainWindow = StepDefinitions.TestContextHelper.MainWindow;
            
            if (appLauncher != null && mainWindow != null)
            {
                // Forzar que la ventana esté en primer plano
                appLauncher.EnsureWindowIsInForeground();
                
                // Verificar y reintentar si es necesario
                Thread.Sleep(300);
                appLauncher.EnsureWindowIsInForeground();
                
                Log.Information("✓ Window ensured in foreground for scenario: {ScenarioTitle}", scenarioTitle);
            }
            else
            {
                Log.Warning("AppLauncher o MainWindow no están disponibles en BeforeScenario");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error crítico: No se pudo traer la ventana al frente en BeforeScenario");
        }
        
        // Crear test en ExtentReports
        var test = ExtentReportManager.CreateTest(scenarioTitle, scenarioDescription ?? string.Empty);
        
        // Asignar tags como categorías
        var tags = scenarioContext.ScenarioInfo.Tags;
        if (tags != null && tags.Length > 0)
        {
            ExtentReportManager.AssignCategory(tags);
        }
        
        // Guardar el nombre del escenario en el contexto para uso posterior
        scenarioContext["ScenarioTitle"] = scenarioTitle;
    }

    [BeforeStep]
    public void BeforeStep(ScenarioContext scenarioContext)
    {
        try
        {
            var stepInfo = scenarioContext.StepContext.StepInfo;
            _cucumberReportGenerator?.StartStep(stepInfo);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error registering step start in Cucumber JSON");
        }
    }

    [AfterStep]
    public void AfterStep(ScenarioContext scenarioContext)
    {
        try
        {
            var stepStatus = scenarioContext.ScenarioExecutionStatus;
            var error = scenarioContext.TestError;
            _cucumberReportGenerator?.FinishStep(stepStatus, error);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error registering step finish in Cucumber JSON");
        }
    }

    [AfterScenario]
    public void AfterScenario(ScenarioContext scenarioContext)
    {
        var scenarioTitle = scenarioContext.Get<string>("ScenarioTitle") ?? "Unknown";
        var testStatus = scenarioContext.ScenarioExecutionStatus;
        
        Log.Information("Scenario {ScenarioTitle} finished with status: {Status}", scenarioTitle, testStatus);
        
        string? screenshotPath = null;
        
        try
        {
            // If scenario failed, take screenshot and attach it
            if (testStatus == ScenarioExecutionStatus.TestError || 
                testStatus == ScenarioExecutionStatus.BindingError ||
                testStatus == ScenarioExecutionStatus.UndefinedStep)
            {
                Log.Warning("Scenario failed, capturing screenshot");
                screenshotPath = ScreenshotHelper.TakeScreenshot(scenarioTitle);
                
                if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
                {
                    Log.Information("Screenshot capturado: {Path}", screenshotPath);
                    ExtentReportManager.AttachScreenshot(screenshotPath);
                }
                
                // Log del error en ExtentReports
                if (scenarioContext.TestError != null)
                {
                    ExtentReportManager.LogFail(scenarioContext.TestError.Message, scenarioContext.TestError);
                }
            }
            else if (testStatus == ScenarioExecutionStatus.OK)
            {
                ExtentReportManager.LogPass($"Scenario '{scenarioTitle}' completed successfully");
            }
            
            // Adjuntar logs del escenario al reporte
            AttachLogsToReport();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al capturar evidencias en AfterScenario");
        }
        
        // Finish scenario in Cucumber JSON
        try
        {
            var includeScreenshots = ConfigManager.Instance.GetValue("Reporting:IncludeScreenshots", "true") == "true";
            var screenshotToInclude = includeScreenshots ? screenshotPath : null;
            _cucumberReportGenerator?.FinishScenario(testStatus, scenarioContext.TestError, screenshotToInclude);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error finishing scenario in Cucumber JSON");
        }
        
        Log.Information("==== Scenario finished: {ScenarioTitle} ====\n", scenarioTitle);
    }

    /// <summary>
    /// Adjunta los logs del escenario al reporte.
    /// </summary>
    private void AttachLogsToReport()
    {
        try
        {
            var logPath = ConfigManager.Instance.GetValue("Serilog:WriteTo:0:Args:path", "logs/test-.log");
            var logFile = FindLatestLogFile(logPath);

            if (!string.IsNullOrEmpty(logFile) && File.Exists(logFile))
            {
                // En SpecFlow no hay TestContext.AddTestAttachment, así que solo lo logueamos
                Log.Debug("Logs disponibles en: {LogFile}", logFile);
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
