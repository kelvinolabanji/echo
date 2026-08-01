using System;
using System.Drawing;
using System.Windows.Forms;

namespace EchoApp
{
    /// <summary>
    /// Simple modal progress window shown only on first run, while the
    /// backend + CLIP weights are being downloaded. Doesn't try to match the
    /// frosted-glass aesthetic of SearchWindow/FolderManagerWindow — this is
    /// a one-time setup step, a plain window is fine and simpler to keep working.
    /// </summary>
    public class SetupProgressForm : Form
    {
        private readonly ProgressBar _progressBar;
        private readonly Label _statusLabel;

        public SetupProgressForm()
        {
            Text = "Setting up Echo";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 110);
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            TopMost = true;

            var titleLabel = new Label
            {
                Text = "Finishing setup — downloading the Echo AI model...",
                AutoSize = false,
                Size = new Size(380, 20),
                Location = new Point(20, 15)
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(20, 45),
                Size = new Size(380, 20),
                Minimum = 0,
                Maximum = 1000
            };

            _statusLabel = new Label
            {
                Text = "Connecting...",
                AutoSize = false,
                Size = new Size(380, 20),
                Location = new Point(20, 75),
                ForeColor = Color.DimGray
            };

            Controls.Add(titleLabel);
            Controls.Add(_progressBar);
            Controls.Add(_statusLabel);
        }

        public void Report(double fraction, string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Report(fraction, status)));
                return;
            }

            _progressBar.Value = Math.Max(0, Math.Min(1000, (int)(fraction * 1000)));
            _statusLabel.Text = status;
        }
    }
}
