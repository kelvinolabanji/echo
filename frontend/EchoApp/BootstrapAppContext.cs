using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EchoApp
{
    /// <summary>
    /// Runs first, inside Application.Run's message loop. Kicks off the backend
    /// download/start as a background task, shows SetupProgressForm only if a
    /// download is actually needed, then hands off to the real AppContext.
    ///
    /// Why this exists instead of just awaiting in Program.cs: doing the await
    /// BEFORE Application.Run() starts deadlocks, because WinForms can only
    /// deliver "the awaited task finished" back to your code by pumping
    /// messages — and that pump doesn't exist until Application.Run() is
    /// already running. Doing the async work as this context's very first
    /// action means the message loop is already live when the continuation
    /// needs to fire.
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
                // Error already shown inside EnsureAndStartBackendAsync.
                Application.Exit();
                return;
            }

            Application.ApplicationExit += (s, e) => BackendManager.StopBackend();

            // Hand off to the real app (tray icon, hotkey, search/folder windows).
            // Application.Exit() from AppContext's tray menu still works correctly
            // from here — it ends the whole message loop regardless of which
            // ApplicationContext Application.Run() was originally called with.
            _mainAppContext = new AppContext();
        }
    }
}
