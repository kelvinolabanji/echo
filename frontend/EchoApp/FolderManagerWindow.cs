using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace EchoApp
{
    public class FolderManagerWindow : Form
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int attrValue, int attrSize);

        // --- Windows DWM P/Invoke for OS Desktop Blur ---
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
        private const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private enum AccentState
        {
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor; // ARGB
            public int AnimationId;
        }

        private WebView2 _webView;

        public FolderManagerWindow()
        {
            InitializeWindow();
            InitializeWebView();
        }

        private void InitializeWindow()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(660, 520);
            
            // CRITICAL: Must be Black (or null) so it doesn't wash out the acrylic blur with solid white.
            this.BackColor = Color.Black; 
            
            this.ShowInTaskbar = false;
            this.TopMost = true;
        }

        private async void InitializeWebView()
        {
            _webView = new WebView2();
            _webView.DefaultBackgroundColor = Color.Transparent;
            _webView.Dock = DockStyle.Fill;
            this.Controls.Add(_webView);

            await _webView.EnsureCoreWebView2Async(null);
            _webView.CoreWebView2.AddHostObjectToScript("echo", new EchoBridge());
            
            string htmlPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "ui", "folders.html");
            _webView.CoreWebView2.Navigate("file:///" + htmlPath.Replace("\\", "/"));
        }

        private void EnableAcrylicBlur()
        {
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                // 0xTTBBGGRR format. 0x20FFFFFF adds a very subtle 12% white tint to the desktop blur.
                GradientColor = 0x20FFFFFF 
            };

            var accentSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(this.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        public void ShowManager()
        {
            this.Show();
            this.Activate();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            SetWindowLong(Handle, GWL_EXSTYLE,
                GetWindowLong(Handle, GWL_EXSTYLE) | WS_EX_NOREDIRECTIONBITMAP);

            int round = DWMWCP_ROUND;
            DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));

            // Activate native Windows blur immediately when form loads
            EnableAcrylicBlur();

            this.Hide();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Hide();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.Hide();
        }
    }
}