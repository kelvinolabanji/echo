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

        // Path where the backend executable is stored relative to the app
        private static string BackendDir =>
            Path.Combine(Application.StartupPath, "backend");

        private static string BackendExePath =>
            Path.Combine(BackendDir, "echo-backend.exe");

        /// <summary>
        /// Downloads the backend package on initial launch if missing, then starts the backend process.
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
                    WorkingDirectory = BackendDir, // Set working directory to backend subfolder
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
