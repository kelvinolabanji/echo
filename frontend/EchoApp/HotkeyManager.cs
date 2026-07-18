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
            RegisterHotKey(_window.Handle, HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_F);
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