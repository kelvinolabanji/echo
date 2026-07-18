using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace EchoApp
{
    public class AppContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private SearchWindow _searchWindow;
        private HotkeyManager _hotkeyManager;

        public AppContext()
        {
            _searchWindow = new SearchWindow();

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
            menu.Items.Add("Open Echo", null, (s, e) => _searchWindow.ShowSearch());
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