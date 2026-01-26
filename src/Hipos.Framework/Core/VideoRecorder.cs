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

                // Buscar FFmpeg (prioriza tools/ffmpeg/ffmpeg.exe del proyecto)
                var ffmpegPath = FindFfmpeg();
                if (string.IsNullOrEmpty(ffmpegPath))
                {
                    Log.Warning("FFmpeg no encontrado. La grabación de video no está disponible.");
                    Log.Information("Para habilitar grabación de video, coloca ffmpeg.exe en: tools/ffmpeg/ffmpeg.exe");
                    return false;
                }

                Log.Debug("Usando FFmpeg desde: {FfmpegPath}", ffmpegPath);

                // Configurar parámetros de calidad
                var videoQuality = GetQualitySettings(quality, frameRate);

                // Preparar argumentos de FFmpeg para captura de pantalla
                // Usa gdigrab para capturar el escritorio en Windows
                // -movflags +faststart: permite reproducir el video mientras se está grabando
                // -tune zerolatency: reduce la latencia para mejor finalización
                var arguments = $"-f gdigrab -framerate {frameRate} -i desktop " +
                               $"-vf \"scale=iw*{videoQuality.ScaleFactor}:ih*{videoQuality.ScaleFactor}\" " +
                               $"-c:v libx264 -preset {videoQuality.Preset} -crf {videoQuality.Crf} " +
                               $"-tune zerolatency -movflags +faststart " +
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
                
                // Iniciar captura asíncrona de errores para debugging
                var errorOutput = new System.Text.StringBuilder();
                _ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorOutput.AppendLine(e.Data);
                        // Log errores importantes en tiempo real
                        if (e.Data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                            e.Data.Contains("failed", StringComparison.OrdinalIgnoreCase))
                        {
                            Log.Warning("FFmpeg: {ErrorLine}", e.Data);
                        }
                    }
                };
                
                _ffmpegProcess.Start();
                _ffmpegProcess.BeginErrorReadLine();

                // Esperar un momento para verificar que inició correctamente
                Thread.Sleep(1000);
                
                if (_ffmpegProcess.HasExited)
                {
                    var error = errorOutput.ToString();
                    if (string.IsNullOrEmpty(error))
                    {
                        error = _ffmpegProcess.StandardError.ReadToEnd();
                    }
                    Log.Error("FFmpeg falló al iniciar (código: {ExitCode}). Error: {Error}", 
                        _ffmpegProcess.ExitCode, error);
                    Log.Error("Argumentos usados: {Arguments}", arguments);
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
                var videoPath = _currentVideoPath;
                
                // Enviar señal de terminación a FFmpeg (q para salir graciosamente)
                if (!_ffmpegProcess.HasExited)
                {
                    try
                    {
                        Log.Debug("Enviando señal de terminación a FFmpeg...");
                        _ffmpegProcess.StandardInput.Write('q');
                        _ffmpegProcess.StandardInput.Flush();
                        _ffmpegProcess.StandardInput.Close();
                        
                        // Dar tiempo a FFmpeg para que finalice correctamente el archivo
                        // Esperar hasta 10 segundos para que termine graciosamente
                        if (!_ffmpegProcess.WaitForExit(10000))
                        {
                            Log.Warning("FFmpeg no terminó en 10 segundos, forzando cierre...");
                            _ffmpegProcess.Kill();
                            _ffmpegProcess.WaitForExit(3000);
                        }
                        else
                        {
                            Log.Debug("FFmpeg terminó correctamente (código: {ExitCode})", _ffmpegProcess.ExitCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "No se pudo enviar señal de terminación a FFmpeg, forzando cierre...");
                        try
                        {
                            _ffmpegProcess.Kill();
                            _ffmpegProcess.WaitForExit(3000);
                        }
                        catch { }
                    }
                }

                // Esperar un momento adicional para asegurar que el archivo se escribió completamente
                Thread.Sleep(500);
                
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
    /// Busca FFmpeg en ubicaciones del proyecto primero, luego en PATH.
    /// Prioriza tools/ffmpeg/ffmpeg.exe dentro del proyecto.
    /// </summary>
    private static string? FindFfmpeg()
    {
        Log.Debug("Buscando FFmpeg...");

        // 1. PRIORIDAD: Buscar en tools/ffmpeg/ del proyecto (ubicación preferida)
        var projectRoot = GetProjectRootDirectory();
        if (!string.IsNullOrEmpty(projectRoot))
        {
            var projectFfmpegPath = Path.Combine(projectRoot, "tools", "ffmpeg", "ffmpeg.exe");
            if (File.Exists(projectFfmpegPath))
            {
                Log.Information("FFmpeg encontrado en el proyecto: {Path}", projectFfmpegPath);
                return projectFfmpegPath;
            }
            Log.Debug("FFmpeg no encontrado en: {Path}", projectFfmpegPath);
        }

        // 2. Buscar en el directorio de ejecución de la aplicación
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var baseFfmpegPath = Path.Combine(baseDirectory, "ffmpeg.exe");
        if (File.Exists(baseFfmpegPath))
        {
            Log.Information("FFmpeg encontrado en directorio de ejecución: {Path}", baseFfmpegPath);
            return baseFfmpegPath;
        }

        // 3. Buscar en el directorio actual y subdirectorios comunes
        var currentDir = Directory.GetCurrentDirectory();
        var commonLocations = new[]
        {
            Path.Combine(currentDir, "tools", "ffmpeg", "ffmpeg.exe"),
            Path.Combine(currentDir, "ffmpeg.exe"),
            Path.Combine(currentDir, "tools", "ffmpeg.exe"),
            Path.Combine(currentDir, "bin", "ffmpeg.exe"),
            Path.Combine(currentDir, "ffmpeg", "ffmpeg.exe")
        };

        foreach (var location in commonLocations)
        {
            if (File.Exists(location))
            {
                Log.Information("FFmpeg encontrado en ubicación común: {Path}", location);
                return location;
            }
        }

        // 4. Buscar en PATH del sistema
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                var ffmpegPath = Path.Combine(path, "ffmpeg.exe");
                if (File.Exists(ffmpegPath))
                {
                    Log.Information("FFmpeg encontrado en PATH: {Path}", ffmpegPath);
                    return ffmpegPath;
                }
            }
        }

        Log.Warning("FFmpeg no encontrado en ninguna ubicación. La grabación de video no estará disponible.");
        Log.Information("Para habilitar grabación de video, coloca ffmpeg.exe en: tools/ffmpeg/ffmpeg.exe (recomendado)");
        
        return null;
    }

    /// <summary>
    /// Intenta obtener el directorio raíz del proyecto buscando archivos característicos.
    /// </summary>
    private static string? GetProjectRootDirectory()
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDir);

            // Buscar hacia arriba hasta encontrar archivos característicos del proyecto
            while (directory != null && directory.Exists)
            {
                var slnFiles = directory.GetFiles("*.sln");
                var gitDir = directory.GetDirectories(".git").FirstOrDefault();
                var toolsDir = directory.GetDirectories("tools").FirstOrDefault();

                if (slnFiles.Length > 0 || gitDir != null || toolsDir != null)
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            // Si no se encuentra, usar el directorio actual
            return Directory.GetCurrentDirectory();
        }
        catch
        {
            return Directory.GetCurrentDirectory();
        }
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
