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

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

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
            this.Size = new Size(700, 500);
            this.BackColor = Color.Black;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            // Center on screen
            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(
                (screen.Width - this.Width) / 2,
                (screen.Height - this.Height) / 3
            );

            // Dismiss on click outside
            this.Deactivate += (s, e) => this.Hide();
        }

        private async void InitializeWebView()
        {
            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            this.Controls.Add(_webView);

            await _webView.EnsureCoreWebView2Async(null);

            // Allow calling back into C# from JS
            _webView.CoreWebView2.AddHostObjectToScript("echo", new EchoBridge());

            // Load the UI
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
            // Hide from Alt+Tab
            SetWindowLong(Handle, GWL_EXSTYLE,
                GetWindowLong(Handle, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
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