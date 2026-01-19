using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Serilog;
using System.Diagnostics;

namespace Hipos.Framework.Core;

/// <summary>
/// Singleton para lanzar y gestionar aplicaciones Windows.
/// Usa FlaUI con UIA3 automation.
/// </summary>
public class AppLauncher
{
    private static AppLauncher? _instance;
    private static readonly object _lock = new();
    
    private Application? _application;
    private UIA3Automation? _automation;
    private Window? _mainWindow;

    private AppLauncher()
    {
        _automation = new UIA3Automation();
    }

    /// <summary>
    /// Obtiene la instancia singleton del AppLauncher.
    /// </summary>
    public static AppLauncher Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ??= new AppLauncher();
            }
        }
    }

    /// <summary>
    /// Lanza la aplicación desde la ruta especificada.
    /// </summary>
    /// <param name="exePath">Ruta completa al ejecutable</param>
    /// <param name="timeoutMs">Timeout en milisegundos para esperar la ventana principal</param>
    /// <returns>La ventana principal de la aplicación</returns>
    public Window LaunchApp(string exePath, int timeoutMs = 10000)
    {
        Log.Information("Lanzando aplicación: {ExePath}", exePath);

        try
        {
            var attached = TryAttachToRunningProcess(exePath, out var processId);
            if (!attached)
            {
                _application = Application.Launch(exePath);
                processId = _application.ProcessId;
                Log.Information("Proceso lanzado con PID: {ProcessId}", processId);
            }
            else
            {
                Log.Information("Adjuntado a proceso existente con PID: {ProcessId}", processId);
            }
            
            // Dar tiempo al proceso para inicializarse
            Thread.Sleep(attached ? 500 : 1500);
            
            // Variables para búsqueda híbrida
            var startTime = DateTime.Now;
            var relaxedModeLogged = false;
            var allWindowsFound = new System.Collections.Generic.List<string>();
            var processName = Path.GetFileNameWithoutExtension(exePath);
            
            // Esperar a que la ventana principal esté disponible
            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
                var useStrictMode = elapsedMs < 5000; // First 5 seconds = strict
                
                // Log cuando cambiamos a modo relaxed
                if (!useStrictMode && !relaxedModeLogged)
                {
                    Log.Information("⚠️ Switching to relaxed search mode (by window title)");
                    relaxedModeLogged = true;
                }
                
                try
                {
                    // Método 1: GetMainWindow (siempre intentamos primero)
                    _mainWindow = _application.GetMainWindow(_automation);
                    if (_mainWindow != null && !_mainWindow.IsOffscreen)
                    {
                        Log.Information("✓ Ventana principal encontrada vía GetMainWindow: '{Title}' (PID: {ProcessId}, Mode: Standard)", 
                            _mainWindow.Title, processId);
                        BringWindowToFront(_mainWindow);
                        return _mainWindow;
                    }
                }
                catch
                {
                    // GetMainWindow puede fallar, intentamos método alternativo
                }

                // Método 2: Búsqueda híbrida (strict → relaxed)
                try
                {
                    var allWindows = _automation!.GetDesktop().FindAllChildren();
                    
                    foreach (var element in allWindows)
                    {
                        try
                        {
                            var window = element.AsWindow();
                            if (window == null) continue;
                            
                            var windowProcessId = window.Properties.ProcessId.ValueOrDefault;
                            var windowTitle = window.Title ?? "";
                            
                            // Trackear ventanas encontradas para debugging
                            if (!string.IsNullOrEmpty(windowTitle) && !window.IsOffscreen)
                            {
                                var windowInfo = $"{windowTitle} (PID: {windowProcessId}, Class: {window.ClassName})";
                                if (!allWindowsFound.Contains(windowInfo))
                                {
                                    allWindowsFound.Add(windowInfo);
                                }
                            }
                            
                            // Verificar que sea una ventana válida básica
                            if (string.IsNullOrEmpty(windowTitle) || window.IsOffscreen)
                            {
                                continue;
                            }
                            
                            // FASE 1: STRICT MODE (primeros 5 segundos)
                            if (useStrictMode)
                            {
                                // Solo ventanas del PID exacto
                                if (windowProcessId != processId)
                                {
                                    continue;
                                }
                                
                                // Ventana encontrada en modo strict
                                _mainWindow = window;
                                Log.Information("✓ Ventana encontrada: '{Title}' (PID: {ProcessId}, Mode: Strict, Clase: {ClassName})", 
                                    _mainWindow.Title, windowProcessId, _mainWindow.ClassName);
                                BringWindowToFront(_mainWindow);
                                return _mainWindow;
                            }
                            // FASE 2: RELAXED MODE (después de 5 segundos)
                            else
                            {
                                // Excluir ventanas del sistema
                                var excludedTitles = new[] { 
                                    "Barra de tareas", "Taskbar", "Program Manager", 
                                    "Microsoft Text Input Application", "MSCTFIME UI",
                                    "Cursor", "Visual Studio", "Visual Studio Code"
                                };
                                
                                if (excludedTitles.Any(t => windowTitle.Contains(t, StringComparison.OrdinalIgnoreCase)))
                                {
                                    continue;
                                }
                                
                                // Buscar por título para apps conocidas
                                bool matchesTitle = false;
                                
                                if (processName.Equals("calc", StringComparison.OrdinalIgnoreCase) ||
                                    processName.Equals("calculator", StringComparison.OrdinalIgnoreCase))
                                {
                                    matchesTitle = windowTitle.Contains("Calculadora", StringComparison.OrdinalIgnoreCase) ||
                                                   windowTitle.Contains("Calculator", StringComparison.OrdinalIgnoreCase);
                                }
                                else if (processName.Equals("notepad", StringComparison.OrdinalIgnoreCase))
                                {
                                    matchesTitle = windowTitle.Contains("Notepad", StringComparison.OrdinalIgnoreCase) ||
                                                   windowTitle.Contains("Bloc de notas", StringComparison.OrdinalIgnoreCase);
                                }
                                else
                                {
                                    // Para otras apps, aceptar cualquier ventana visible
                                    matchesTitle = true;
                                }
                                
                                if (matchesTitle)
                                {
                                    _mainWindow = window;
                                    Log.Information("✓ Ventana encontrada: '{Title}' (PID: {ProcessId}, Mode: Relaxed, Clase: {ClassName})", 
                                        _mainWindow.Title, windowProcessId, _mainWindow.ClassName);
                                    BringWindowToFront(_mainWindow);
                                    return _mainWindow;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Debug("Error al procesar ventana: {Error}", ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("Error en búsqueda alternativa: {Error}", ex.Message);
                }

                Thread.Sleep(500);
            }

            // Timeout - mostrar todas las ventanas encontradas para debugging
            Log.Warning("❌ No se encontró la ventana después de {TimeoutMs}ms. Ventanas detectadas: {Count}", 
                timeoutMs, allWindowsFound.Count);
            
            if (allWindowsFound.Count > 0)
            {
                Log.Warning("Ventanas disponibles:");
                foreach (var win in allWindowsFound.Take(10)) // Máximo 10 para no saturar logs
                {
                    Log.Warning("  - {Window}", win);
                }
            }
            
            throw new TimeoutException(
                $"No se pudo obtener la ventana principal después de {timeoutMs}ms (PID: {processId}). " +
                $"Ventanas encontradas: {allWindowsFound.Count}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al lanzar aplicación: {ExePath}", exePath);
            throw;
        }
    }

    private bool TryAttachToRunningProcess(string exePath, out int processId)
    {
        processId = 0;

        try
        {
            var processName = Path.GetFileNameWithoutExtension(exePath);
            if (string.IsNullOrWhiteSpace(processName))
            {
                return false;
            }

            var processes = Process.GetProcessesByName(processName);
            var process = processes.FirstOrDefault(p => !p.HasExited);
            if (process == null)
            {
                return false;
            }

            processId = process.Id;
            _application = Application.Attach(processId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo adjuntar a proceso existente: {ExePath}", exePath);
            return false;
        }
    }

    /// <summary>
    /// Trae la ventana al primer plano y la activa.
    /// </summary>
    /// <param name="window">La ventana a traer al frente</param>
    private void BringWindowToFront(Window window)
    {
        try
        {
            // Método 1: SetForeground (más confiable)
            if (window.Properties.NativeWindowHandle.IsSupported)
            {
                var handle = window.Properties.NativeWindowHandle.Value;
                SetForegroundWindow(handle);
                Log.Debug("Ventana traída al frente usando SetForegroundWindow");
            }

            // Método 2: Focus (respaldo)
            window.Focus();
            Log.Debug("Ventana enfocada usando Focus()");

            // Dar tiempo para que la ventana responda
            Thread.Sleep(500);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo traer la ventana al frente, continuando de todas formas");
        }
    }

    /// <summary>
    /// Importación de la API de Windows para traer ventanas al frente.
    /// </summary>
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Cierra la aplicación de forma segura.
    /// </summary>
    public void CloseApp()
    {
        try
        {
            if (_mainWindow != null && !_mainWindow.IsOffscreen)
            {
                Log.Information("Cerrando ventana principal");
                _mainWindow.Close();
                _mainWindow = null;
            }

            if (_application != null && !_application.HasExited)
            {
                Log.Information("Cerrando aplicación");
                _application.Close();
                _application.Dispose();
                _application = null;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al cerrar aplicación, intentando forzar cierre");
            try
            {
                _application?.Kill();
            }
            catch (Exception killEx)
            {
                Log.Error(killEx, "Error al forzar cierre de aplicación");
            }
        }
    }

    /// <summary>
    /// Obtiene la ventana principal actual.
    /// </summary>
    public Window? MainWindow => _mainWindow;

    /// <summary>
    /// Trae la ventana principal al primer plano (método público).
    /// Útil si la ventana pierde el foco durante la ejecución de tests.
    /// </summary>
    public void EnsureWindowIsInForeground()
    {
        if (_mainWindow != null && !_mainWindow.IsOffscreen)
        {
            BringWindowToFront(_mainWindow);
        }
    }

    /// <summary>
    /// Obtiene la instancia de Application.
    /// </summary>
    public Application? Application => _application;

    /// <summary>
    /// Obtiene la instancia de Automation.
    /// </summary>
    public UIA3Automation? Automation => _automation;

    /// <summary>
    /// Libera recursos.
    /// </summary>
    public void Dispose()
    {
        CloseApp();
        _automation?.Dispose();
        _automation = null;
    }
}
