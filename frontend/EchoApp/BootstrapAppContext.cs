using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EchoApp
{
    /// <summary>
    /// Initial application context that runs during Application.Run().
    /// Handles first-time setup UI, starts the backend, then launches the main AppContext.
    /// </summary>
    public class BootstrapAppContext : ApplicationContext
    {
        private SetupProgressForm? _progressForm;
        private AppContext? _mainAppContext;

        public BootstrapAppContext()
        {
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            StartupManager.SetStartup(true);

            var downloader = new BackendDownloader();

            // Only show setup form if backend assets need to be downloaded
            if (!downloader.IsBackendInstalled)
            {
                _progressForm = new SetupProgressForm();
                _progressForm.Show();
            }

            var progress = new Progress<(double fraction, string status)>(p =>
                _progressForm?.Report(p.fraction, p.status));

            bool started = await BackendManager.EnsureAndStartBackendAsync(progress);

            _progressForm?.Close();
            _progressForm = null;

            if (!started)
            {
                Application.Exit();
                return;
            }

            // Ensure the backend process is terminated when the app exits
            Application.ApplicationExit += (s, e) => BackendManager.StopBackend();

            // Hand off execution to the main application context
            _mainAppContext = new AppContext();
        }
    }
}
