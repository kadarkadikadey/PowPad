using System.Windows;

namespace PowPad
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // This catch-all helps you see errors that don't show up in the main window
            this.DispatcherUnhandledException += (s, ex) =>
            {
                System.Windows.MessageBox.Show($"MeowAssist Error: {ex.Exception.Message}");
                ex.Handled = true;
            };
        }
    }
}