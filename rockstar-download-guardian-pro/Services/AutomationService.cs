using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
namespace RockstarGuardian.Services;
public sealed record LauncherUiState(bool Retry, bool Resume, bool Downloading, bool Paused, bool Accessible, bool Invoked);
public sealed class AutomationService
{
    private Task<LauncherUiState>? pending;
    private Task<LauncherUiState>? legacy;
    private long lastCommand;
    private bool InvokeCommand(Action action)
    {
        long now = DateTime.UtcNow.Ticks, previous = Interlocked.Read(ref lastCommand);
        if (now - previous < TimeSpan.FromSeconds(15).Ticks || Interlocked.CompareExchange(ref lastCommand, now, previous) != previous) return false;
        action(); return true;
    }
    public async Task<LauncherUiState> InspectAsync(string launcher, bool invokeRetry, bool allowResume, CancellationToken ct)
    {
        LauncherUiState state = new(false, false, false, false, false, false);
        if (pending is not { IsCompleted: false }) state = await InspectPrimaryAsync(launcher, invokeRetry, allowResume, ct);
        if (state.Invoked || legacy is { IsCompleted: false }) return state;
        var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(3));
        legacy = Task.Run(() =>
        {
            try { return LegacyAutomation.Inspect(launcher, invokeRetry, allowResume, InvokeCommand, deadline.Token); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new(false, false, false, false, false, false); }
            finally { deadline.Dispose(); }
        });
        try
        {
            var other = await legacy.WaitAsync(TimeSpan.FromSeconds(3), ct);
            bool retry = state.Retry || other.Retry, resume = state.Resume || other.Resume, downloading = state.Downloading || other.Downloading;
            return new(retry, resume, downloading, resume && !retry && !downloading, state.Accessible || other.Accessible, state.Invoked || other.Invoked);
        }
        catch (TimeoutException) { return state; }
    }
    private async Task<LauncherUiState> InspectPrimaryAsync(string launcher, bool invokeRetry, bool allowResume, CancellationToken ct)
    {
        var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(3));
        pending = Task.Run(() =>
        {
            try { return Inspect(launcher, invokeRetry, allowResume, InvokeCommand, deadline.Token); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new(false, false, false, false, false, false); }
            finally { deadline.Dispose(); }
        });
        try { return await pending.WaitAsync(TimeSpan.FromSeconds(3), ct); }
        catch (TimeoutException) { return new(false, false, false, false, false, false); }
    }
    private static LauncherUiState Inspect(string launcher, bool invokeRetry, bool allowResume, Func<Action, bool> invoke, CancellationToken ct)
    {
        bool retry = false, resume = false, downloading = false, accessible = false, invoked = false;
        foreach (int pid in LauncherProcesses.Find(launcher))
        {
            try
            {
                var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, pid));
                foreach (AutomationElement window in windows)
                {
                    ct.ThrowIfCancellationRequested();
                    var elements = window.FindAll(TreeScope.Descendants, new OrCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Hyperlink), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text)));
                    for (int i = 0; i < Math.Min(elements.Count, 500); i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var element = elements[i]; var current = element.Current;
                        accessible = true;
                        bool actionable = current.ControlType == ControlType.Button || current.ControlType == ControlType.Hyperlink;
                        downloading |= LauncherSignals.IsDownloadActive(current.Name);
                        bool isRetry = actionable && current.IsEnabled && LauncherSignals.IsRetry(current.Name);
                        bool isResume = actionable && current.IsEnabled && LauncherSignals.IsResume(current.Name);
                        retry |= isRetry; resume |= isResume;
                        if (!invoked && ((isRetry && invokeRetry) || (isResume && allowResume)))
                        {
                            ct.ThrowIfCancellationRequested();
                            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern)) invoked = invoke(() => ((InvokePattern)pattern).Invoke());
                        }
                    }
                }
            }
            catch (Exception e) when (e is ElementNotAvailableException or InvalidOperationException or System.Runtime.InteropServices.COMException or UnauthorizedAccessException) { }
        }
        return new(retry, resume, downloading, resume && !retry && !downloading, accessible, invoked);
    }
}
