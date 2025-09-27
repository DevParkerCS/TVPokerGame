using System.Diagnostics;
using System.IO;
using System;
using UnityEngine;

public class LocalServerLauncher : MonoBehaviour
{
    [SerializeField] public int port = 5757;

    private Process _server;
    private string ServerWorkingDir =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Server"));

    private static LocalServerLauncher _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        try
        {
            string distPath = Path.Combine(ServerWorkingDir, "dist");
            string entryJs = Path.Combine(distPath, "index.js");
            if (!File.Exists(entryJs))
            {
                UnityEngine.Debug.LogError($"Server not built. Run `npm run build` in {ServerWorkingDir}");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "node", // requires Node installed; we can package later
                Arguments = $"\"{entryJs}\" {port}",
                WorkingDirectory = ServerWorkingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _server = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _server.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.Log($"[server] {e.Data}"); };
            _server.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.LogError($"[server] {e.Data}"); };

            bool started = _server.Start();
            if (!started)
            {
                UnityEngine.Debug.LogError("Failed to start local Socket.IO server.");
                return;
            }

            _server.BeginOutputReadLine();
            _server.BeginErrorReadLine();
            UnityEngine.Debug.Log($"Local server launching on port {port}...");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to launch server: {ex.Message}");
        }
    }

    void OnApplicationQuit()
    {
        try
        {
            if (_server != null && !_server.HasExited)
            {
                _server.Kill();
                _server.Dispose();
            }
        }
        catch { /* swallow on quit */ }
    }
}
