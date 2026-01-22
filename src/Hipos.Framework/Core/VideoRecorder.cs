using System.Diagnostics;
using Serilog;

namespace Hipos.Framework.Core;

/// <summary>
/// Helper para grabar videos de la pantalla durante la ejecución de tests.
/// </summary>
public static class VideoRecorder
{
    private static Process? _ffmpegProcess;
    private static string? _currentVideoPath;
    private static string? _currentTestName;
    private static readonly object _lock = new();
    private static bool _isRecording = false;

    /// <summary>
    /// Indica si actualmente se está grabando un video.
    /// </summary>
    public static bool IsRecording
    {
        get
        {
            lock (_lock)
            {
                return _isRecording && _ffmpegProcess != null && !_ffmpegProcess.HasExited;
            }
        }
    }

    /// <summary>
    /// Inicia la grabación de video de la pantalla.
    /// </summary>
    /// <param name="testName">Nombre del test para identificar el video</param>
    /// <param name="videoDirectory">Directorio donde se guardará el video</param>
    /// <param name="frameRate">Frame rate para la grabación (por defecto 10)</param>
    /// <param name="quality">Calidad del video: "low", "medium", "high" (por defecto "medium")</param>
    /// <returns>True si la grabación se inició correctamente, false en caso contrario</returns>
    public static bool StartRecording(string testName, string videoDirectory, int frameRate = 10, string quality = "medium")
    {
        lock (_lock)
        {
            if (_isRecording)
            {
                Log.Warning("Ya hay una grabación en curso. Deteniendo la anterior...");
                StopRecording();
            }

            try
            {
                _currentTestName = testName;

                // Crear directorio si no existe
                if (!Directory.Exists(videoDirectory))
                {
                    Directory.CreateDirectory(videoDirectory);
                    Log.Debug("Directorio de videos creado: {Directory}", videoDirectory);
                }

                // Generar nombre de archivo único
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var sanitizedTestName = SanitizeFileName(testName);
                var fileName = $"{sanitizedTestName}_{timestamp}.mp4";
                _currentVideoPath = Path.Combine(videoDirectory, fileName);

                // Buscar FFmpeg en PATH o ubicaciones comunes
                var ffmpegPath = FindFfmpeg();
                if (string.IsNullOrEmpty(ffmpegPath))
                {
                    Log.Warning("FFmpeg no encontrado. La grabación de video no está disponible.");
                    Log.Information("Para habilitar grabación de video, instala FFmpeg y agrégalo al PATH, o coloca ffmpeg.exe en el directorio del proyecto.");
                    return false;
                }

                // Configurar parámetros de calidad
                var videoQuality = GetQualitySettings(quality, frameRate);

                // Preparar argumentos de FFmpeg para captura de pantalla
                // Usa gdigrab para capturar el escritorio en Windows
                var arguments = $"-f gdigrab -framerate {frameRate} -i desktop " +
                               $"-vf \"scale=iw*{videoQuality.ScaleFactor}:ih*{videoQuality.ScaleFactor}\" " +
                               $"-c:v libx264 -preset {videoQuality.Preset} -crf {videoQuality.Crf} " +
                               $"-pix_fmt yuv420p -y \"{_currentVideoPath}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                };

                _ffmpegProcess = new Process { StartInfo = startInfo };
                _ffmpegProcess.Start();

                // Esperar un momento para verificar que inició correctamente
                Thread.Sleep(500);
                
                if (_ffmpegProcess.HasExited && _ffmpegProcess.ExitCode != 0)
                {
                    var error = _ffmpegProcess.StandardError.ReadToEnd();
                    Log.Error("FFmpeg falló al iniciar: {Error}", error);
                    _ffmpegProcess.Dispose();
                    _ffmpegProcess = null;
                    _currentVideoPath = null;
                    return false;
                }

                _isRecording = true;
                Log.Information("Grabación de video iniciada: {VideoPath}", _currentVideoPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al iniciar grabación de video para test: {TestName}", testName);
                _ffmpegProcess?.Dispose();
                _ffmpegProcess = null;
                _currentVideoPath = null;
                _isRecording = false;
                return false;
            }
        }
    }

    /// <summary>
    /// Detiene la grabación de video actual.
    /// </summary>
    /// <returns>Ruta del archivo de video grabado, o null si no se grabó o hubo error</returns>
    public static string? StopRecording()
    {
        lock (_lock)
        {
            if (!_isRecording || _ffmpegProcess == null)
            {
                return null;
            }

            try
            {
                // Enviar señal de terminación a FFmpeg (q para salir graciosamente)
                if (!_ffmpegProcess.HasExited)
                {
                    try
                    {
                        _ffmpegProcess.StandardInput.Write('q');
                        _ffmpegProcess.StandardInput.Flush();
                        _ffmpegProcess.StandardInput.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "No se pudo enviar señal de terminación a FFmpeg, forzando cierre...");
                    }
                    
                    // Esperar a que termine (máximo 5 segundos)
                    if (!_ffmpegProcess.WaitForExit(5000))
                    {
                        Log.Warning("FFmpeg no terminó en 5 segundos, forzando cierre...");
                        _ffmpegProcess.Kill();
                        _ffmpegProcess.WaitForExit(2000);
                    }
                }

                var videoPath = _currentVideoPath;
                _ffmpegProcess.Dispose();
                _ffmpegProcess = null;
                _isRecording = false;

                // Verificar que el archivo existe y tiene contenido
                if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
                {
                    var fileInfo = new FileInfo(videoPath);
                    if (fileInfo.Length > 0)
                    {
                        Log.Information("Grabación de video finalizada: {VideoPath} ({Size} bytes)", 
                            videoPath, fileInfo.Length);
                        _currentVideoPath = null;
                        _currentTestName = null;
                        return videoPath;
                    }
                    else
                    {
                        Log.Warning("El archivo de video está vacío, eliminándolo: {VideoPath}", videoPath);
                        File.Delete(videoPath);
                    }
                }
                else
                {
                    Log.Warning("No se encontró el archivo de video grabado: {VideoPath}", videoPath);
                }

                _currentVideoPath = null;
                _currentTestName = null;
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al detener grabación de video");
                
                try
                {
                    _ffmpegProcess?.Kill();
                    _ffmpegProcess?.Dispose();
                }
                catch { }

                _ffmpegProcess = null;
                _isRecording = false;
                _currentVideoPath = null;
                _currentTestName = null;
                return null;
            }
        }
    }

    /// <summary>
    /// Obtiene la ruta del video actualmente grabado (si existe).
    /// </summary>
    /// <returns>Ruta del video o null si no hay grabación activa</returns>
    public static string? GetVideoPath()
    {
        lock (_lock)
        {
            return _currentVideoPath;
        }
    }

    /// <summary>
    /// Elimina un archivo de video si existe.
    /// </summary>
    /// <param name="videoPath">Ruta del archivo de video a eliminar</param>
    public static void DeleteVideo(string? videoPath)
    {
        if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
        {
            return;
        }

        try
        {
            File.Delete(videoPath);
            Log.Debug("Video eliminado: {VideoPath}", videoPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo eliminar el video: {VideoPath}", videoPath);
        }
    }

    /// <summary>
    /// Busca FFmpeg en el PATH o ubicaciones comunes.
    /// </summary>
    private static string? FindFfmpeg()
    {
        // Buscar en PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var ffmpegPath = Path.Combine(path, "ffmpeg.exe");
                if (File.Exists(ffmpegPath))
                {
                    return ffmpegPath;
                }
            }
        }

        // Buscar en el directorio actual y subdirectorios comunes
        var currentDir = Directory.GetCurrentDirectory();
        var commonLocations = new[]
        {
            Path.Combine(currentDir, "ffmpeg.exe"),
            Path.Combine(currentDir, "tools", "ffmpeg.exe"),
            Path.Combine(currentDir, "bin", "ffmpeg.exe"),
            Path.Combine(currentDir, "ffmpeg", "ffmpeg.exe")
        };

        foreach (var location in commonLocations)
        {
            if (File.Exists(location))
            {
                return location;
            }
        }

        return null;
    }

    /// <summary>
    /// Obtiene la configuración de calidad según el parámetro.
    /// </summary>
    private static VideoQualitySettings GetQualitySettings(string quality, int frameRate)
    {
        return quality.ToLowerInvariant() switch
        {
            "low" => new VideoQualitySettings { ScaleFactor = 0.5, Preset = "ultrafast", Crf = 28 },
            "high" => new VideoQualitySettings { ScaleFactor = 1.0, Preset = "medium", Crf = 18 },
            _ => new VideoQualitySettings { ScaleFactor = 0.75, Preset = "fast", Crf = 23 } // medium
        };
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

    /// <summary>
    /// Configuración de calidad de video.
    /// </summary>
    private class VideoQualitySettings
    {
        public double ScaleFactor { get; set; }
        public string Preset { get; set; } = "fast";
        public int Crf { get; set; } = 23;
    }
}
