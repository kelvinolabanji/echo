using System;
using System.Threading;
using System.Windows.Forms;

namespace EchoApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            using var mutex = new Mutex(true, "EchoApp_SingleInstance", out bool isNewInstance);
            if (!isNewInstance) return;

            ApplicationConfiguration.Initialize();

            // BootstrapAppContext kicks off backend setup (downloading it on
            // first run if needed) as its first action once the message loop
            // is already running, then hands off to the real AppContext.
            Application.Run(new BootstrapAppContext());
        }
    }
}