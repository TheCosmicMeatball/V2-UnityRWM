using System;
using System.IO;
using System.Text;
using UnityEngine;

// Writes Unity logs to a fixed folder on Windows standalone builds.
// Initializes before the first scene so all logs are captured.
public static class PersistentFileLogger
{
    // Update this path if needed; requested absolute path for host machine
    private const string TargetLogDir = @"C:\\Users\\User\\Robots Game\\Unity RWM v2\\Logs";

    private static readonly object _lock = new object();
    private static StreamWriter _writer;
    private static string _filePath;
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (_initialized)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(TargetLogDir);
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            _filePath = Path.Combine(TargetLogDir, $"host-{timestamp}.log");
            _writer = new StreamWriter(_filePath, append: false, encoding: new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            Application.logMessageReceivedThreaded += HandleLog;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Application.quitting += OnQuitting;

            _initialized = true;
            SafeWriteLine($"[PersistentFileLogger] Started. Writing to {_filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PersistentFileLogger] Failed to initialize: {ex.Message}");
        }
#endif
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        string ts = DateTime.Now.ToString("HH:mm:ss.fff");
        string lvl = type.ToString().ToUpperInvariant();
        if (type == LogType.Exception)
        {
            SafeWriteLine($"[{ts}] [{lvl}] {condition}\n{stackTrace}");
        }
        else
        {
            SafeWriteLine($"[{ts}] [{lvl}] {condition}");
        }
#endif
    }

    private static void SafeWriteLine(string line)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try
        {
            lock (_lock)
            {
                _writer?.WriteLine(line);
            }
        }
        catch { /* swallow logging errors */ }
#endif
    }

    private static void OnQuitting()
    {
        Shutdown();
    }

    private static void OnProcessExit(object sender, EventArgs e)
    {
        Shutdown();
    }

    private static void Shutdown()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try
        {
            Application.logMessageReceivedThreaded -= HandleLog;
        }
        catch { }

        try
        {
            lock (_lock)
            {
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
            }
        }
        catch { }
        _initialized = false;
#endif
    }
}

