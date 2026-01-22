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
    private static readonly ConfigManager _config = ConfigManager.Instance;
    
    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        lock (_initLock)
        {
            if (_isInitialized) return;
            
            InitializeLogging();
            InitializeReports();
            LaunchApplication();
            
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
            
            CloseApplicationCompletely();
            FinalizeReports();
            DisposeAppLauncher();
            
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
                
                // Verificar que la ventana esté realmente en primer plano usando wait adaptativo
                var windowInForeground = WaitHelper.WaitUntilAdaptive(
                    () =>
                    {
                        try
                        {
                            return mainWindow != null && !mainWindow.IsOffscreen;
                        }
                        catch
                        {
                            return false;
                        }
                    },
                    timeoutMs: 500,
                    conditionDescription: "ventana en primer plano");
                
                if (windowInForeground)
                {
                    Log.Information("✓ Window ensured in foreground for scenario: {ScenarioTitle}", scenarioTitle);
                }
                else
                {
                    Log.Warning("No se pudo verificar que la ventana esté en primer plano");
                }
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
        
        // Iniciar grabación de video si está habilitada
        StartVideoRecordingIfNeeded(scenarioTitle, scenarioContext);
    }
    
    /// <summary>
    /// Inicia la grabación de video si está habilitada y el modo lo requiere.
    /// </summary>
    private void StartVideoRecordingIfNeeded(string scenarioTitle, ScenarioContext scenarioContext)
    {
        try
        {
            if (!_config.VideoRecordingEnabled)
            {
                return;
            }

            var mode = _config.VideoRecordingMode;
            
            // Determinar si debemos iniciar la grabación según el modo
            // "Always" y "OnSuccess" inician al principio
            // "OnFailure" no inicia hasta que sepamos que falló
            bool shouldStartRecording = mode.Equals("Always", StringComparison.OrdinalIgnoreCase) ||
                                       mode.Equals("OnSuccess", StringComparison.OrdinalIgnoreCase);

            if (shouldStartRecording)
            {
                var videoDirectory = Path.Combine(Directory.GetCurrentDirectory(), _config.VideoDirectory);
                var frameRate = _config.VideoFrameRate;
                var quality = _config.VideoQuality;

                var started = VideoRecorder.StartRecording(scenarioTitle, videoDirectory, frameRate, quality);
                if (started)
                {
                    Log.Information("Grabación de video iniciada para escenario: {ScenarioTitle} (modo: {Mode})", 
                        scenarioTitle, mode);
                    scenarioContext["VideoRecordingStarted"] = true;
                }
                else
                {
                    Log.Warning("No se pudo iniciar la grabación de video para escenario: {ScenarioTitle}", scenarioTitle);
                    scenarioContext["VideoRecordingStarted"] = false;
                }
            }
            else
            {
                scenarioContext["VideoRecordingStarted"] = false;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al intentar iniciar grabación de video para escenario: {ScenarioTitle}", scenarioTitle);
            scenarioContext["VideoRecordingStarted"] = false;
        }
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
        
        Log.Information("Scenario {ScenarioTitle} finished with status: {Status}", scenarioTitle, testStatus);
        
        string? screenshotPath = null;
        string? videoPath = null;
        
        try
        {
            // Manejar grabación de video
            videoPath = HandleVideoRecording(scenarioTitle, testStatus, scenarioContext);
            
            // Si el escenario falló, capturar screenshot y adjuntarlo
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
            var includeScreenshots = _config.GetValue("Reporting:IncludeScreenshots", "true") == "true";
            var screenshotToInclude = includeScreenshots ? screenshotPath : null;
            _cucumberReportGenerator?.FinishScenario(testStatus, scenarioContext.TestError, screenshotToInclude);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al finalizar escenario en Cucumber JSON");
        }
        
        Log.Information("==== Scenario finished: {ScenarioTitle} ====\n", scenarioTitle);
    }

    /// <summary>
    /// Configura Serilog para logging.
    /// </summary>
    private static void InitializeLogging()
    {
        var logPath = _config.GetValue("Serilog:WriteTo:0:Args:path", "logs/test-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("==== Starting BDD test suite ====");
    }

    /// <summary>
    /// Inicializa ExtentReports y Cucumber JSON report generator.
    /// </summary>
    private static void InitializeReports()
    {
        // Inicializar ExtentReports
        var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "reports", "extent-report.html");
        ExtentReportManager.InitializeReport(reportPath);

        // Inicializar Cucumber JSON report generator
        var cucumberJsonPath = _config.GetValue("Reporting:CucumberJsonPath", "reports/cucumber.json");
        var fullCucumberPath = Path.Combine(Directory.GetCurrentDirectory(), cucumberJsonPath);
        _cucumberReportGenerator = new CucumberJsonReportGenerator();
        _cucumberReportGenerator.Initialize(fullCucumberPath);
        Log.Information("Cucumber JSON report generator initialized: {Path}", fullCucumberPath);
    }

    /// <summary>
    /// Lanza la aplicación y guarda referencias en TestContextHelper.
    /// </summary>
    private static void LaunchApplication()
    {
        try
        {
            var appPath = _config.AppPath;
            var timeout = _config.DefaultTimeout;

            var appLauncher = AppLauncher.Instance;
            var mainWindow = appLauncher.LaunchApp(appPath, timeout);

            StepDefinitions.TestContextHelper.AppLauncher = appLauncher;
            StepDefinitions.TestContextHelper.MainWindow = mainWindow;

            Log.Information("Application launched for BDD test suite");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al lanzar aplicación en BeforeTestRun");
            throw;
        }
    }

    /// <summary>
    /// Cierra la aplicación completamente usando múltiples métodos de fuerza.
    /// </summary>
    private static void CloseApplicationCompletely()
    {
        var appLauncher = StepDefinitions.TestContextHelper.AppLauncher;
        if (appLauncher == null) return;

        try
        {
            Log.Information("==== Closing application completely ====");

            var application = appLauncher.Application;
            int? processId = null;
            if (application != null && !application.HasExited)
            {
                processId = application.ProcessId;
                Log.Information("Process to close - PID: {ProcessId}", processId);
            }

            // Método 1: Cierre normal
            appLauncher.CloseApp();
            
            // Esperar a que el proceso termine usando wait adaptativo
            if (processId.HasValue)
            {
                WaitHelper.WaitUntilAdaptive(
                    () =>
                    {
                        try
                        {
                            var proc = System.Diagnostics.Process.GetProcessById(processId.Value);
                            return proc.HasExited;
                        }
                        catch (ArgumentException)
                        {
                            // Proceso ya no existe
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    },
                    timeoutMs: 2000,
                    conditionDescription: "proceso terminado");
            }
            else
            {
                // Si no tenemos PID, esperar un tiempo mínimo
                Thread.Sleep(500);
            }

            // Método 2: Forzar cierre por PID si es necesario
            if (processId.HasValue)
            {
                ForceCloseProcessById(processId.Value);
            }

            // Método 3: Verificar desde FlaUI y forzar si es necesario
            if (application != null && !application.HasExited)
            {
                ForceCloseFlaUIApplication(application);
            }

            // Método 4: Buscar procesos por nombre como último recurso
            var appPath = _config.AppPath;
            var processName = Path.GetFileNameWithoutExtension(appPath);
            ForceCloseProcessByName(processName);

            Log.Information("✓ Application closed completely");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cerrar aplicación en AfterTestRun");
            
            // Último recurso: buscar y matar todos los procesos por nombre
            try
            {
                var appPath = _config.AppPath;
                var processName = Path.GetFileNameWithoutExtension(appPath);
                ForceCloseProcessByName(processName);
            }
            catch (Exception finalEx)
            {
                Log.Error(finalEx, "Error en último intento de cierre");
            }
        }
    }

    /// <summary>
    /// Fuerza el cierre de un proceso por su PID.
    /// </summary>
    private static void ForceCloseProcessById(int processId)
    {
        try
        {
            var process = System.Diagnostics.Process.GetProcessById(processId);
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

    /// <summary>
    /// Fuerza el cierre de procesos por nombre.
    /// </summary>
    private static void ForceCloseProcessByName(string processName)
    {
        try
        {
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
                            Log.Warning("Forcing closure of residual process: PID {ProcessId}", proc.Id);
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
    }

    /// <summary>
    /// Fuerza el cierre de la aplicación desde FlaUI.
    /// </summary>
    private static void ForceCloseFlaUIApplication(FlaUI.Core.Application application)
    {
        Log.Warning("FlaUI reporta que la aplicación aún está activa, forzando cierre...");
        try
        {
            application.Kill();
            
            // Esperar a que termine usando wait adaptativo
            WaitHelper.WaitUntilAdaptive(
                () => application.HasExited,
                timeoutMs: 2000,
                conditionDescription: "aplicación terminada después de Kill()");
        }
        catch (Exception killEx)
        {
            Log.Warning(killEx, "Error al forzar cierre desde FlaUI");
        }
    }

    /// <summary>
    /// Finaliza los reportes (ExtentReports y Cucumber JSON).
    /// </summary>
    private static void FinalizeReports()
    {
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
            Log.Information("Cucumber JSON report generated: {ReportPath}", _cucumberReportGenerator?.ReportPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error generating Cucumber JSON report");
        }
    }

    /// <summary>
    /// Hace dispose del AppLauncher y limpia referencias.
    /// </summary>
    private static void DisposeAppLauncher()
    {
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
    }

    /// <summary>
    /// Adjunta los logs del escenario al reporte.
    /// </summary>
    private void AttachLogsToReport()
    {
        try
        {
            var logPath = _config.GetValue("Serilog:WriteTo:0:Args:path", "logs/test-.log");
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
    /// Maneja la grabación de video: detiene la grabación y decide si guardarla según el modo y resultado.
    /// </summary>
    private string? HandleVideoRecording(string scenarioTitle, ScenarioExecutionStatus testStatus, ScenarioContext scenarioContext)
    {
        try
        {
            if (!_config.VideoRecordingEnabled)
            {
                return null;
            }

            var mode = _config.VideoRecordingMode;
            var wasRecordingStarted = scenarioContext.Get<bool>("VideoRecordingStarted");
            
            // Si el modo es "OnFailure" y el test falló, iniciar grabación ahora (aunque sea tarde, 
            // al menos capturamos el estado final)
            if (mode.Equals("OnFailure", StringComparison.OrdinalIgnoreCase) && 
                (testStatus == ScenarioExecutionStatus.TestError || 
                 testStatus == ScenarioExecutionStatus.BindingError ||
                 testStatus == ScenarioExecutionStatus.UndefinedStep))
            {
                if (!wasRecordingStarted)
                {
                    // Iniciar grabación rápida para capturar al menos el estado final
                    var videoDirectory = Path.Combine(Directory.GetCurrentDirectory(), _config.VideoDirectory);
                    VideoRecorder.StartRecording(scenarioTitle + "_failure", videoDirectory, 
                        _config.VideoFrameRate, _config.VideoQuality);
                    Thread.Sleep(1000); // Grabar 1 segundo al menos
                }
            }

            // Detener grabación si estaba activa
            string? videoPath = null;
            if (wasRecordingStarted || VideoRecorder.IsRecording)
            {
                videoPath = VideoRecorder.StopRecording();
            }

            // Determinar si debemos guardar el video según el modo y resultado
            bool shouldSaveVideo = false;
            
            if (mode.Equals("Always", StringComparison.OrdinalIgnoreCase))
            {
                shouldSaveVideo = true;
            }
            else if (mode.Equals("OnFailure", StringComparison.OrdinalIgnoreCase) &&
                     (testStatus == ScenarioExecutionStatus.TestError || 
                      testStatus == ScenarioExecutionStatus.BindingError ||
                      testStatus == ScenarioExecutionStatus.UndefinedStep))
            {
                shouldSaveVideo = true;
            }
            else if (mode.Equals("OnSuccess", StringComparison.OrdinalIgnoreCase) &&
                     testStatus == ScenarioExecutionStatus.OK)
            {
                shouldSaveVideo = true;
            }

            if (shouldSaveVideo && !string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
            {
                Log.Information("Video guardado para escenario '{ScenarioTitle}': {VideoPath}", scenarioTitle, videoPath);
                ExtentReportManager.AttachVideo(videoPath);
                return videoPath;
            }
            else if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
            {
                // Eliminar video si no debe guardarse
                Log.Debug("Eliminando video que no debe guardarse (modo: {Mode}, estado: {Status})", mode, testStatus);
                VideoRecorder.DeleteVideo(videoPath);
                return null;
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al manejar grabación de video para escenario: {ScenarioTitle}", scenarioTitle);
            // Asegurar que la grabación se detenga incluso si hay error
            try
            {
                VideoRecorder.StopRecording();
            }
            catch { }
            return null;
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
