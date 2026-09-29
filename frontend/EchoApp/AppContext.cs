using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EchoApp
{
    public class AppContext : ApplicationContext
    {
        private const string BaseUrl = "http://127.0.0.1:8000";
        private static readonly HttpClient _http = new HttpClient();

        private NotifyIcon _trayIcon;
        private SearchWindow _searchWindow;
        private FolderManagerWindow _folderManagerWindow;
        private HotkeyManager _hotkeyManager;
        private IndexingWatcher _indexingWatcher;

        public AppContext()
        {
            _searchWindow = new SearchWindow();
            _folderManagerWindow = new FolderManagerWindow();

            // Force creation of the window handle so OnLoad runs now rather than 
            // flashing and hiding on the first call to ShowManager() later.
            _ = _folderManagerWindow.Handle;

            Icon trayIconImage;
            try
            {
                trayIconImage = new Icon(Path.Combine(Application.StartupPath, "echo.ico"));
            }
            catch
            {
                trayIconImage = SystemIcons.Application; // Default fallback icon
            }

            _trayIcon = new NotifyIcon()
            {
                Icon = trayIconImage,
                Visible = true,
                Text = "Echo",
                ContextMenuStrip = BuildTrayMenu()
            };

            _hotkeyManager = new HotkeyManager(_searchWindow.Handle, () =>
            {
                _searchWindow.ShowSearch();
            });

            _indexingWatcher = new IndexingWatcher(_trayIcon);
            _indexingWatcher.Start();

            _ = RunFirstLaunchSetupAsync();
        }

        /// <summary>
        /// Indexes Pictures by default on initial launch and opens Folder Manager.
        /// Retries with a delay while waiting for the Python backend to finish starting up.
        /// </summary>
        private async Task RunFirstLaunchSetupAsync()
        {
            const int maxAttempts = 60; // Up to ~2 minutes to allow for backend model loading
            const int delayMs = 2000;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    string foldersJson = await _http.GetStringAsync($"{BaseUrl}/folders");
                    using var doc = JsonDocument.Parse(foldersJson);

                    bool hasAnyFolders = doc.RootElement.ValueKind == JsonValueKind.Array
                        && doc.RootElement.GetArrayLength() > 0;

                    if (hasAnyFolders)
                        return; // Folders are already configured

                    string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    if (Directory.Exists(picturesPath))
                    {
                        var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
                        await _http.PostAsync(
                            $"{BaseUrl}/index?folder={Uri.EscapeDataString(picturesPath)}", content);
                    }

                    _folderManagerWindow.ShowManager();
                    return;
                }
                catch
                {
                    // Backend isn't ready yet; keep retrying until max attempts
                    if (attempt == maxAttempts)
                        return;
                }

                await Task.Delay(delayMs);
            }
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();

            var header = new ToolStripLabel("Echo");
            header.Font = new Font(header.Font, FontStyle.Bold);
            menu.Items.Add(header);
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("Search photos", null, (s, e) => _searchWindow.ShowSearch());
            menu.Items.Add("Manage folders", null, (s, e) => _folderManagerWindow.ShowManager());
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("Exit", null, (s, e) =>
            {
                _trayIcon.Visible = false;
                Application.Exit();
            });

            return menu;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hotkeyManager?.Dispose();
                _indexingWatcher?.Dispose();
                _trayIcon?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
