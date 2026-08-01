// BackendDownloader.cs
//
// Ensures the backend (echo-backend.exe + CLIP weights) is present before
// EchoApp tries to spawn it. If missing — i.e. first run after a fresh
// install — downloads the backend package and extracts it.
//
// Wire this in wherever you currently spawn the backend process (AppContext.cs),
// and call EnsureBackendInstalledAsync() before Process.Start on echo-backend.exe.
//
// You'll need to fill in BackendDownloadUrl and ExpectedSha256 once you've
// zipped dist\echo-backend + your CLIP weights folder and uploaded it
// (e.g. as a GitHub Release asset — those URLs look like:
//   https://github.com/{user}/{repo}/releases/download/{tag}/echo-backend.zip
// which also gives you a stable direct-download link with no server needed).

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace EchoApp
{
    public class BackendDownloader
    {
        // TODO: fill these in once the package is hosted
        private const string BackendDownloadUrl =
            "https://github.com/kelvinolabanji/echo/releases/download/v1.0.1/echo-backend.zip";

        // SHA-256 of the zip file itself. Compute with:
        //   certutil -hashfile echo-backend.zip SHA256
        private const string ExpectedSha256 = "84b3e043caefd34c10bd679a723b5da624d01b152d7a315fa6a673262a26bd9b";

        private readonly string _appDir;
        private readonly string _backendDir;
        private readonly string _backendExePath;

        public BackendDownloader()
        {
            _appDir = AppDomain.CurrentDomain.BaseDirectory;
            _backendDir = Path.Combine(_appDir, "backend");
            _backendExePath = Path.Combine(_backendDir, "echo-backend.exe");
        }

        public bool IsBackendInstalled => File.Exists(_backendExePath);

        /// <summary>
        /// Returns immediately if the backend is already present.
        /// Otherwise downloads, verifies, and extracts it, reporting
        /// 0.0-1.0 progress split roughly 90% download / 10% extract.
        /// </summary>
        public async Task<bool> EnsureBackendInstalledAsync(
            IProgress<(double fraction, string status)> progress,
            CancellationToken cancellationToken = default)
        {
            if (IsBackendInstalled)
                return true;

            Directory.CreateDirectory(_backendDir);
            string tempZipPath = Path.Combine(Path.GetTempPath(), "echo-backend-download.zip");

            try
            {
                progress?.Report((0.0, "Connecting..."));
                await DownloadWithProgressAsync(BackendDownloadUrl, tempZipPath, progress, cancellationToken);

                progress?.Report((0.90, "Verifying download..."));
                if (!VerifySha256(tempZipPath, ExpectedSha256))
                {
                    throw new InvalidDataException(
                        "Downloaded backend package failed checksum verification. " +
                        "The download may be corrupted or the hosted file was updated " +
                        "without updating ExpectedSha256.");
                }

                progress?.Report((0.93, "Extracting..."));
                // Extract to a staging dir first so a failed/cancelled extract
                // never leaves a half-populated backend folder behind.
                string stagingDir = _backendDir + "_staging";
                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, recursive: true);

                ZipFile.ExtractToDirectory(tempZipPath, stagingDir);

                // Handle either zip layout: echo-backend.exe sitting right at
                // the zip's root, OR nested one level inside a wrapper folder
                // (this happens if the zip was made via right-click -> "Send to
                // -> Compressed folder" on the echo-backend folder itself,
                // rather than zipping its contents). Searching for the exe and
                // promoting whatever folder actually contains it handles both.
                string[] matches = Directory.GetFiles(
                    stagingDir, "echo-backend.exe", SearchOption.AllDirectories);

                if (matches.Length == 0)
                {
                    throw new FileNotFoundException(
                        "echo-backend.exe was not found anywhere inside the " +
                        "downloaded package. Check how echo-backend.zip was built.");
                }

                string actualBackendRoot = Path.GetDirectoryName(matches[0])!;

                if (Directory.Exists(_backendDir))
                    Directory.Delete(_backendDir, recursive: true);

                Directory.Move(actualBackendRoot, _backendDir);

                // Clean up the staging dir — if the exe was nested, this removes
                // the now-empty wrapper folder left behind alongside it.
                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, recursive: true);

                progress?.Report((1.0, "Done"));
                return true;
            }
            finally
            {
                if (File.Exists(tempZipPath))
                {
                    try { File.Delete(tempZipPath); } catch { /* best effort cleanup */ }
                }
            }
        }

        private static async Task DownloadWithProgressAsync(
            string url,
            string destinationPath,
            IProgress<(double fraction, string status)> progress,
            CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient
            {
                // First-run download of a large ML package over a slow connection
                // is exactly the scenario a short timeout breaks.
                Timeout = TimeSpan.FromMinutes(30)
            };

            using var response = await httpClient.GetAsync(
                url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(
                destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes.HasValue)
                {
                    // Reserve the last 10% of the progress bar for verify + extract
                    double fraction = 0.90 * ((double)totalRead / totalBytes.Value);
                    double mb = totalRead / 1024.0 / 1024.0;
                    double totalMb = totalBytes.Value / 1024.0 / 1024.0;
                    progress?.Report((fraction, $"Downloading... {mb:F0} MB / {totalMb:F0} MB"));
                }
                else
                {
                    progress?.Report((0.0, $"Downloading... {totalRead / 1024.0 / 1024.0:F0} MB"));
                }
            }
        }

        private static bool VerifySha256(string filePath, string expectedHash)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = sha256.ComputeHash(stream);
            string actualHash = Convert.ToHexString(hashBytes);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
