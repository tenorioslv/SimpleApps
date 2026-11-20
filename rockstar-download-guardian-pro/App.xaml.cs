using System.Threading;
using System.Windows;
using System.Windows.Threading;
namespace RockstarGuardian;
public partial class App : Application
{
    private Mutex? instance;
    private EventWaitHandle? openRequest;
    private DispatcherTimer? activationTimer;
    protected override void OnStartup(StartupEventArgs e)
    {
        instance = new Mutex(true, "Local\\RockstarDownloadGuardianPro", out bool first);
        if (!first)
        {
            try { using var request = EventWaitHandle.OpenExisting("Local\\RockstarGuardianOpenPanel"); request.Set(); }
            catch (WaitHandleCannotBeOpenedException) { MessageBox.Show("O Guardian está iniciando. Aguarde e abra novamente."); }
            Shutdown(); return;
        }
        openRequest = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\RockstarGuardianOpenPanel");
        activationTimer = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(500) };
        activationTimer.Tick += (_, _) =>
        {
            if (openRequest.WaitOne(0) && MainWindow != null)
            { MainWindow.Show(); MainWindow.WindowState = WindowState.Normal; MainWindow.Activate(); }
        };
        activationTimer.Start();
        base.OnStartup(e);
    }
    protected override void OnExit(ExitEventArgs e) { activationTimer?.Stop(); openRequest?.Dispose(); instance?.Dispose(); base.OnExit(e); }
}
