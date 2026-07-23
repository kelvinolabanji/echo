using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace EchoApp
{
    public class SearchWindow : Form
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int attrValue, int attrSize);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
        private const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private WebView2 _webView;

        public SearchWindow()
        {
            InitializeWindow();
            InitializeWebView();
        }

        private void InitializeWindow()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(1000, 700);
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(
                (screen.Width - this.Width) / 2,
                (screen.Height - this.Height) / 3
            );

            this.Deactivate += (s, e) => this.Hide();
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
                AppDomain.CurrentDomain.BaseDirectory, "ui", "index.html");
            _webView.CoreWebView2.Navigate("file:///" + htmlPath.Replace("\\", "/"));
        }

        public void ShowSearch()
        {
            this.Show();
            this.Activate();
            _webView?.Focus();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            SetWindowLong(Handle, GWL_EXSTYLE,
                GetWindowLong(Handle, GWL_EXSTYLE) | WS_EX_TOOLWINDOW | WS_EX_NOREDIRECTIONBITMAP);

            int round = DWMWCP_ROUND;
            DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));

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
    }
}