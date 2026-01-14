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

            Log.Information("==== Iniciando suite de tests BDD ====");

            // Inicializar ExtentReports
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "reports", "extent-report.html");
            ExtentReportManager.InitializeReport(reportPath);

            // Inicializar generador de reportes Cucumber JSON
            var cucumberJsonPath = ConfigManager.Instance.GetValue("Reporting:CucumberJsonPath", "reports/cucumber.json");
            var fullCucumberPath = Path.Combine(Directory.GetCurrentDirectory(), cucumberJsonPath);
            _cucumberReportGenerator = new CucumberJsonReportGenerator();
            _cucumberReportGenerator.Initialize(fullCucumberPath);
            Log.Information("Generador Cucumber JSON inicializado: {Path}", fullCucumberPath);

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

                Log.Information("Aplicación lanzada para suite de tests BDD");
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
            
            Log.Information("==== Finalizando suite de tests BDD ====");
            
            // Cerrar aplicación UNA SOLA VEZ al final de toda la suite - FORZAR CIERRE COMPLETO
            try
            {
                var appLauncher = StepDefinitions.TestContextHelper.AppLauncher;
                if (appLauncher != null)
                {
                    Log.Information("==== Cerrando aplicación completamente ====");
                    
                    // Obtener el PID antes de cerrar
                    var application = appLauncher.Application;
                    int? processId = null;
                    if (application != null && !application.HasExited)
                    {
                        processId = application.ProcessId;
                        Log.Information("Proceso a cerrar - PID: {ProcessId}", processId);
                    }
                    
                    // Método 1: Cerrar normalmente
                    appLauncher.CloseApp();
                    Thread.Sleep(1500);
                    
                    // Método 2: Verificar y forzar cierre usando System.Diagnostics.Process
                    if (processId.HasValue)
                    {
                        try
                        {
                            var process = System.Diagnostics.Process.GetProcessById(processId.Value);
                            if (process != null && !process.HasExited)
                            {
                                Log.Warning("Proceso aún activo (PID: {ProcessId}), forzando cierre con Kill()...", processId);
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
                                Log.Information("Proceso terminado forzosamente");
                            }
                        }
                        catch (ArgumentException)
                        {
                            // Proceso ya no existe, está bien
                            Log.Information("Proceso ya terminado (PID: {ProcessId})", processId);
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
                    
                    Log.Information("✓ Aplicación cerrada completamente");
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
                                Log.Warning("Forzando cierre de proceso residual: PID {ProcessId}", proc.Id);
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
            
            // Generar reporte Cucumber JSON
            try
            {
                _cucumberReportGenerator?.GenerateReport();
                Log.Information("Reporte Cucumber JSON generado: {ReportPath}", _cucumberReportGenerator?.ReportPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error al generar reporte Cucumber JSON");
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
        
        Log.Information("==== Iniciando escenario: {ScenarioTitle} ====", scenarioTitle);
        
        // Registrar escenario en Cucumber JSON
        try
        {
            _cucumberReportGenerator?.StartScenario(scenarioContext.ScenarioInfo, featureContext.FeatureInfo);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al registrar escenario en Cucumber JSON");
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
                
                Log.Information("✓ Ventana asegurada en primer plano para escenario: {ScenarioTitle}", scenarioTitle);
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
            Log.Warning(ex, "Error al registrar inicio de step en Cucumber JSON");
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
            Log.Warning(ex, "Error al registrar fin de step en Cucumber JSON");
        }
    }

    [AfterScenario]
    public void AfterScenario(ScenarioContext scenarioContext)
    {
        var scenarioTitle = scenarioContext.Get<string>("ScenarioTitle") ?? "Unknown";
        var testStatus = scenarioContext.ScenarioExecutionStatus;
        
        Log.Information("Escenario {ScenarioTitle} finalizado con estado: {Status}", scenarioTitle, testStatus);
        
        string? screenshotPath = null;
        
        try
        {
            // Si el escenario falló, tomar screenshot y adjuntarlo
            if (testStatus == ScenarioExecutionStatus.TestError || 
                testStatus == ScenarioExecutionStatus.BindingError ||
                testStatus == ScenarioExecutionStatus.UndefinedStep)
            {
                Log.Warning("Escenario falló, capturando screenshot");
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
                ExtentReportManager.LogPass($"Escenario '{scenarioTitle}' completado exitosamente");
            }
            
            // Adjuntar logs del escenario al reporte
            AttachLogsToReport();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al capturar evidencias en AfterScenario");
        }
        
        // Finalizar escenario en Cucumber JSON
        try
        {
            var includeScreenshots = ConfigManager.Instance.GetValue("Reporting:IncludeScreenshots", "true") == "true";
            var screenshotToInclude = includeScreenshots ? screenshotPath : null;
            _cucumberReportGenerator?.FinishScenario(testStatus, scenarioContext.TestError, screenshotToInclude);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al finalizar escenario en Cucumber JSON");
        }
        
        Log.Information("==== Escenario finalizado: {ScenarioTitle} ====\n", scenarioTitle);
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
