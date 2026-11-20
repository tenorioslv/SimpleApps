using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Automation;
namespace RockstarGuardian.Services;
public sealed class AutomationService
{
    public Task<bool> RetryAsync() => Task.Run(() =>
    {
        foreach (var process in Process.GetProcessesByName("Launcher"))
        using (process)
        {
            try
            {
                var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id));
                foreach (AutomationElement window in windows)
                {
                    var buttons = window.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                    foreach (AutomationElement button in buttons)
                    {
                        var label = button.Current.Name.Trim();
                        if (Array.Exists(new[] { "Tentar novamente", "Retry", "Continuar", "Resume" }, x => label.Equals(x, StringComparison.OrdinalIgnoreCase)) && button.Current.IsEnabled && button.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
                        { ((InvokePattern)pattern).Invoke(); return true; }
                    }
                }
            }
            catch (Exception e) when (e is ElementNotAvailableException or InvalidOperationException or System.Runtime.InteropServices.COMException) { }
        }
        return false;
    });
}
