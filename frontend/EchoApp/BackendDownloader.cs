// BackendDownloader.cs
//
// Downloads, verifies, and unpacks the standalone echo-backend package from 
// GitHub Releases if it isn't present on initial startup.

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
        private const string BackendDownloadUrl =
            "https://github.com/kelvinolabanji/echo/releases/download/v1.0.1/echo-backend.zip";

        // Expected SHA-256 hash of echo-backend.zip
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
        /// Downloads, verifies, and extracts the backend executable if it isn't found locally.
        /// Reports 0.0-1.0 progress across downloading and extraction.
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
                        "The download may be corrupted or ExpectedSha256 needs an update.");
                }

                progress?.Report((0.93, "Extracting..."));
                // Extract to a staging directory to prevent leaving partial files if interrupted
                string stagingDir = _backendDir + "_staging";
                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, recursive: true);

                ZipFile.ExtractToDirectory(tempZipPath, stagingDir);

                // Handle zip files whether echo-backend.exe is in the root or nested inside a folder
                string[] matches = Directory.GetFiles(
                    stagingDir, "echo-backend.exe", SearchOption.AllDirectories);

                if (matches.Length == 0)
                {
                    throw new FileNotFoundException(
                        "echo-backend.exe was not found inside the downloaded zip package.");
                }

                string actualBackendRoot = Path.GetDirectoryName(matches[0])!;

                if (Directory.Exists(_backendDir))
                    Directory.Delete(_backendDir, recursive: true);

                Directory.Move(actualBackendRoot, _backendDir);

                // Clean up remaining staging files
                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, recursive: true);

                progress?.Report((1.0, "Done"));
                return true;
            }
            finally
            {
                if (File.Exists(tempZipPath))
                {
                    try { File.Delete(tempZipPath); } catch { /* Ignore cleanup errors */ }
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
                Timeout = TimeSpan.FromMinutes(30) // Extended timeout for slow connections
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
                    // Scale download progress up to 90% to leave room for verification and extraction
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
