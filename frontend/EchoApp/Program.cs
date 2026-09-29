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
            // Ensure only a single instance of Echo runs at a time
            using var mutex = new Mutex(true, "EchoApp_SingleInstance", out bool isNewInstance);
            if (!isNewInstance) return;

            ApplicationConfiguration.Initialize();

            // Run application via BootstrapAppContext to handle initial setup before launching main context
            Application.Run(new BootstrapAppContext());
        }
    }
}
