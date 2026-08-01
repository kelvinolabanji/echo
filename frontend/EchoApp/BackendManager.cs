using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EchoApp
{
    public static class BackendManager
    {
        private static Process? _backendProcess;

        // Backend lives in a subfolder, not next to EchoApp.exe — this matches
        // both the installer layout (echo-setup.iss extracts the frontend only)
        // and where BackendDownloader.cs extracts the downloaded package to.
        private static string BackendDir =>
            Path.Combine(Application.StartupPath, "backend");

        private static string BackendExePath =>
            Path.Combine(BackendDir, "echo-backend.exe");

        /// <summary>
        /// Ensures the backend package is present (downloading it on first run
        /// if needed) and starts it. Call this instead of StartBackend() directly
        /// so a fresh install doesn't try to spawn a process that doesn't exist yet.
        /// </summary>
        public static async Task<bool> EnsureAndStartBackendAsync(
            IProgress<(double fraction, string status)>? downloadProgress = null)
        {
            var downloader = new BackendDownloader();

            if (!downloader.IsBackendInstalled)
            {
                try
                {
                    bool ok = await downloader.EnsureBackendInstalledAsync(downloadProgress);
                    if (!ok)
                        return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Couldn't download the Echo backend: {ex.Message}\n\n" +
                        "Check your internet connection and try again.",
                        "Echo setup failed");
                    return false;
                }
            }

            StartBackend();
            return true;
        }

        public static void StartBackend()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = BackendExePath,
                    // Critical: without this, the backend's cwd defaults to
                    // EchoApp's own folder, not its own subfolder — which used
                    // to matter for relative-path data files (now fixed on the
                    // Python side too, but this is still correct practice).
                    WorkingDirectory = BackendDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                _backendProcess = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting backend: {ex.Message}");
            }
        }

        public static void StopBackend()
        {
            try
            {
                if (_backendProcess != null && !_backendProcess.HasExited)
                {
                    _backendProcess.Kill();
                    _backendProcess.Dispose();
                }
            }
            catch { }
        }
    }
}
