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

            // Prime FolderManagerWindow's handle now, the same way _searchWindow
            // gets primed below via HotkeyManager's constructor. Both windows'
            // OnLoad hides them once as part of first-time setup — if we don't
            // force that to happen here, the FIRST real ShowManager() call later
            // (from RunFirstLaunchSetupAsync) would trigger OnLoad's one-time
            // Hide() and the window would flash and vanish instead of staying open.
            _ = _folderManagerWindow.Handle;

            Icon trayIconImage;
            try
            {
                trayIconImage = new Icon(Path.Combine(Application.StartupPath, "echo.ico"));
            }
            catch
            {
                trayIconImage = SystemIcons.Application; // fallback if the file's missing
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
        /// On a genuine first run (backend reports zero indexed folders),
        /// automatically starts indexing the user's Pictures folder and opens
        /// the Folder Manager so they can see/adjust it. On every later run,
        /// the backend already has folders and this is a no-op.
        ///
        /// CLIP/torch take a while to load before uvicorn actually starts
        /// accepting connections — this retries with a short delay instead of
        /// giving up after one attempt.
        /// </summary>
        private async Task RunFirstLaunchSetupAsync()
        {
            const int maxAttempts = 60;      // ~2 minutes total at 2s apart —
                                              // CLIP/torch cold start has taken
                                              // up to ~90s on a loaded machine,
                                              // so this needs real margin
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
                        return; // not first run — user already has folders configured

                    string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    if (Directory.Exists(picturesPath))
                    {
                        var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
                        await _http.PostAsync(
                            $"{BaseUrl}/index?folder={Uri.EscapeDataString(picturesPath)}", content);
                    }

                    _folderManagerWindow.ShowManager();
                    return; // succeeded — stop retrying
                }
                catch
                {
                    // Most likely the backend just isn't listening yet. Wait
                    // and try again rather than giving up after one attempt.
                    if (attempt == maxAttempts)
                        return; // backend genuinely never came up — give up quietly,
                                // user can still add folders manually via the tray menu
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