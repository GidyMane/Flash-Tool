using System.Windows;
namespace M32RR_FLASH_TOOL
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Create the main window
            MainWindow window = new MainWindow();
            // Show the main window
            window.Show();
        }
    }
}












