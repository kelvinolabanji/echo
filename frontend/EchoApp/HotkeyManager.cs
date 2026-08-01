using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EchoApp
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint VK_F = 0x46;

        private IntPtr _handle;
        private Action _onHotkey;
        private HotkeyWindow _window;

        public HotkeyManager(IntPtr handle, Action onHotkey)
        {
            _onHotkey = onHotkey;
            _window = new HotkeyWindow(onHotkey);

            bool registered = RegisterHotKey(_window.Handle, HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_F);
            if (!registered)
            {
                // Fails silently by default if another running app already owns
                // this combo (a Chrome extension, another utility, etc.) —
                // surfacing it here instead of leaving the hotkey just not work
                // with no explanation.
                MessageBox.Show(
                    "Echo couldn't register the Win+Shift+F hotkey — another " +
                    "running app is probably already using it (check browser " +
                    "extension shortcuts, screen recorders, etc.). Echo will " +
                    "still work from the tray icon menu.",
                    "Echo — hotkey unavailable");
            }
        }

        public void Dispose()
        {
            UnregisterHotKey(_window.Handle, HOTKEY_ID);
            _window.Dispose();
        }

        private class HotkeyWindow : NativeWindow, IDisposable
        {
            private const int WM_HOTKEY = 0x0312;
            private Action _onHotkey;

            public HotkeyWindow(Action onHotkey)
            {
                _onHotkey = onHotkey;
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                    _onHotkey?.Invoke();
                base.WndProc(ref m);
            }

            public void Dispose() => DestroyHandle();
        }
    }
}