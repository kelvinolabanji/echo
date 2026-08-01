using Microsoft.Win32;
using System.Windows.Forms;

namespace EchoApp
{
    public static class StartupManager
    {
        private static readonly string AppName = "EchoApp";

        public static void SetStartup(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enable)
            {
                key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
    }
}