using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Net.Http;

namespace EchoApp
{
    public class AppContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private SearchWindow _searchWindow;
        private FolderManagerWindow _folderManagerWindow;
        private HotkeyManager _hotkeyManager;

        public AppContext()
        {
            _searchWindow = new SearchWindow();
            _folderManagerWindow = new FolderManagerWindow();

            _trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Echo",
                ContextMenuStrip = BuildTrayMenu()
            };

            _hotkeyManager = new HotkeyManager(_searchWindow.Handle, () =>
            {
                _searchWindow.ShowSearch();
            });
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
                _trayIcon?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}