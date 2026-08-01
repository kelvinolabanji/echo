using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EchoApp
{
    /// <summary>
    /// Polls /index/progress for the lifetime of the app and shows a tray
    /// balloon tip whenever an indexing run starts or finishes — covers both
    /// the automatic first-run Pictures scan and any folder the user adds
    /// manually later, since both go through the same backend progress state.
    /// </summary>
    public class IndexingWatcher : IDisposable
    {
        private const string BaseUrl = "http://127.0.0.1:8000";
        private static readonly HttpClient _http = new HttpClient();

        private readonly NotifyIcon _trayIcon;
        private readonly CancellationTokenSource _cts = new();
        private bool _wasRunning = false;

        public IndexingWatcher(NotifyIcon trayIcon)
        {
            _trayIcon = trayIcon;
        }

        public void Start()
        {
            _ = PollLoopAsync(_cts.Token);
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string json = await _http.GetStringAsync($"{BaseUrl}/index/progress", token);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    bool running = root.TryGetProperty("running", out var runningProp)
                        && runningProp.ValueKind == JsonValueKind.True;

                    string folder = root.TryGetProperty("folder", out var folderProp)
                        ? (folderProp.GetString() ?? "") : "";

                    int indexed = root.TryGetProperty("indexed", out var indexedProp)
                        ? indexedProp.GetInt32() : 0;

                    int skipped = root.TryGetProperty("skipped", out var skippedProp)
                        ? skippedProp.GetInt32() : 0;

                    if (running && !_wasRunning)
                    {
                        string folderName = string.IsNullOrEmpty(folder)
                            ? "folder" : Path.GetFileName(folder.TrimEnd('\\', '/'));

                        _trayIcon.BalloonTipTitle = "Echo";
                        _trayIcon.BalloonTipText = $"Indexing started: {folderName}";
                        _trayIcon.ShowBalloonTip(3000);
                    }
                    else if (!running && _wasRunning)
                    {
                        string suffix = skipped > 0 ? $" ({skipped} already up to date)" : "";
                        _trayIcon.BalloonTipTitle = "Echo";
                        _trayIcon.BalloonTipText =
                            $"Indexing complete — {indexed} photo{(indexed == 1 ? "" : "s")} indexed{suffix}";
                        _trayIcon.ShowBalloonTip(4000);
                    }

                    _wasRunning = running;
                }
                catch
                {
                    // Backend not reachable yet, or a transient hiccup — just
                    // try again next tick rather than surfacing every miss.
                }

                try
                {
                    await Task.Delay(1500, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
