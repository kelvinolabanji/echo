using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EchoApp
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class EchoBridge
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string BASE_URL = "http://127.0.0.1:8000";

        public string Search(string query)
        {
            try
            {
                var response = _http.GetStringAsync($"{BASE_URL}/search?q={Uri.EscapeDataString(query)}&top_k=20").Result;
                return response;
            }
            catch
            {
                return "{\"results\": []}";
            }
        }

        public string GetThumbnail(string path)
        {
            return $"{BASE_URL}/thumbnail?path={Uri.EscapeDataString(path)}";
        }

        public void OpenFile(string path)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        public void OpenInFolder(string path)
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
        }

        public string IndexFolder(string folderPath)
        {
            try
            {
                var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
                var response = _http.PostAsync($"{BASE_URL}/index?folder={Uri.EscapeDataString(folderPath)}", content).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
            catch
            {
                return "{\"error\": \"Backend not running\"}";
            }
        }

        public string GetProgress()
        {
            try
            {
                return _http.GetStringAsync($"{BASE_URL}/index/progress").Result;
            }
            catch
            {
                return "{\"running\": false}";
            }
        }

        public string CancelIndexing()
        {
            try
            {
                var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
                return _http.PostAsync($"{BASE_URL}/index/cancel", content).Result.Content.ReadAsStringAsync().Result;
            }
            catch
            {
                return "{\"error\": \"Backend not running\"}";
            }
        }

        public string GetFolders()
        {
            try
            {
                return _http.GetStringAsync($"{BASE_URL}/folders").Result;
            }
            catch
            {
                return "[]";
            }
        }

        public string UnindexFolder(string folderPath)
        {
            try
            {
                var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
                return _http.PostAsync($"{BASE_URL}/unindex?folder={Uri.EscapeDataString(folderPath)}", content).Result.Content.ReadAsStringAsync().Result;
            }
            catch
            {
                return "{\"error\": \"Backend not running\"}";
            }
        }

        public string PickFolder()
        {
            string result = "";
            var thread = new Thread(() =>
            {
                var dialog = new FolderBrowserDialog()
                {
                    Description = "Select a folder to index with Echo",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = false
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                    result = dialog.SelectedPath;
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return result;
        }
    }
}