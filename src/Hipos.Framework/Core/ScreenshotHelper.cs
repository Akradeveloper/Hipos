using FlaUI.Core.Capturing;
using Serilog;
using System.Drawing.Imaging;

namespace Hipos.Framework.Core;

/// <summary>
/// Helper para capturar screenshots de la aplicación.
/// </summary>
public static class ScreenshotHelper
{
    private static readonly string ScreenshotDirectory = Path.Combine(
        Directory.GetCurrentDirectory(), 
        "allure-results", 
        "screenshots");

    /// <summary>
    /// Toma un screenshot de la ventana actual.
    /// </summary>
    /// <param name="testName">Nombre del test para identificar el screenshot</param>
    /// <returns>Ruta del archivo de screenshot, o null si falla</returns>
    public static string? TakeScreenshot(string testName)
    {
        try
        {
            // Crear directorio si no existe
            if (!Directory.Exists(ScreenshotDirectory))
            {
                Directory.CreateDirectory(ScreenshotDirectory);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{SanitizeFileName(testName)}_{timestamp}.png";
            var filePath = Path.Combine(ScreenshotDirectory, fileName);

            // Capturar screenshot usando FlaUI
            var mainWindow = AppLauncher.Instance.MainWindow;
            
            if (mainWindow != null && !mainWindow.IsOffscreen)
            {
                using var bitmap = mainWindow.Capture();
                bitmap.Save(filePath, ImageFormat.Png);
                
                Log.Information("Screenshot capturado: {FilePath}", filePath);
                return filePath;
            }
            else
            {
                Log.Warning("No hay ventana principal disponible para capturar screenshot");
                
                // Intentar capturar toda la pantalla como fallback
                var captureImage = Capture.Screen();
                captureImage.Bitmap.Save(filePath, ImageFormat.Png);
                
                Log.Information("Screenshot de pantalla completa capturado: {FilePath}", filePath);
                return filePath;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al capturar screenshot para test: {TestName}", testName);
            return null;
        }
    }

    /// <summary>
    /// Limpia el nombre de archivo de caracteres inválidos.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 50 ? sanitized.Substring(0, 50) : sanitized;
    }
}
