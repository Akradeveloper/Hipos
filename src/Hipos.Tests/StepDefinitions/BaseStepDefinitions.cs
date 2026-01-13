using FlaUI.Core.AutomationElements;
using Hipos.Framework.Core;
using Hipos.Framework.Utils;
using Serilog;

namespace Hipos.Tests.StepDefinitions;

/// <summary>
/// Clase base para todas las step definitions.
/// Proporciona acceso a MainWindow, AppLauncher y utilidades comunes.
/// </summary>
public abstract class BaseStepDefinitions
{
    protected static Window? MainWindow => TestContextHelper.MainWindow;
    protected static AppLauncher? AppLauncher => TestContextHelper.AppLauncher;
    
    protected void LogInfo(string message)
    {
        Log.Information(message);
        ExtentReportManager.LogInfo(message);
    }
    
    protected void LogPass(string message)
    {
        Log.Information("✓ {Message}", message);
        ExtentReportManager.LogPass(message);
    }
    
    protected void LogFail(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            Log.Error(exception, "✗ {Message}", message);
            ExtentReportManager.LogFail(message, exception);
        }
        else
        {
            Log.Error("✗ {Message}", message);
            ExtentReportManager.LogFail(message);
        }
    }
    
    protected void LogWarning(string message)
    {
        Log.Warning("⚠ {Message}", message);
        ExtentReportManager.LogWarning(message);
    }
}

/// <summary>
/// Helper estático para compartir el contexto de la aplicación entre hooks y step definitions.
/// </summary>
public static class TestContextHelper
{
    private static Window? _mainWindow;
    private static AppLauncher? _appLauncher;
    private static readonly object _lock = new();
    
    public static Window? MainWindow
    {
        get
        {
            lock (_lock)
            {
                return _mainWindow;
            }
        }
        set
        {
            lock (_lock)
            {
                _mainWindow = value;
            }
        }
    }
    
    public static AppLauncher? AppLauncher
    {
        get
        {
            lock (_lock)
            {
                return _appLauncher;
            }
        }
        set
        {
            lock (_lock)
            {
                _appLauncher = value;
            }
        }
    }
}
