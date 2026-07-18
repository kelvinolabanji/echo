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
            Application.Run(new AppContext());
        }
    }
}