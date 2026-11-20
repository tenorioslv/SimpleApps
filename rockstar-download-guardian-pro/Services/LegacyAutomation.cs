using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Accessibility;
namespace RockstarGuardian.Services;
public static class LegacyAutomation
{
    private delegate bool EnumWindow(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindow callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr window, EnumWindow callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("oleacc.dll")] private static extern int AccessibleObjectFromWindow(IntPtr window, uint objectId, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IAccessible accessible);
    [DllImport("oleacc.dll")] private static extern int AccessibleChildren(IAccessible accessible, int first, int count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] children, out int obtained);
    // Compatibilidade com interfaces que expõem MSAA em vez de InvokePattern.
    public static LauncherUiState Inspect(string launcher, bool retryAction, bool resumeAction, Func<Action, bool> invoke, CancellationToken ct)
    {
        bool retry = false, resume = false, downloading = false, exposed = false, invoked = false;
        var ids = new HashSet<int>(LauncherProcesses.Find(launcher)); var handles = new List<IntPtr>();
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out uint pid);
            if (ids.Contains((int)pid))
            {
                handles.Add(window);
                EnumChildWindows(window, (child, _) => { GetWindowThreadProcessId(child, out uint childPid); if (ids.Contains((int)childPid) && handles.Count < 200) handles.Add(child); return !ct.IsCancellationRequested; }, IntPtr.Zero);
            }
            return !ct.IsCancellationRequested;
        }, IntPtr.Zero);
        int remaining = 700;
        void Visit(IAccessible acc, object child, int depth)
        {
            ct.ThrowIfCancellationRequested();
            if (--remaining < 0 || depth > 20) return;
            try
            {
                string label = acc.get_accName(child) ?? "";
                int role = Convert.ToInt32(acc.get_accRole(child));
                bool enabled = (Convert.ToInt32(acc.get_accState(child)) & 1) == 0;
                bool control = role is 43 or 30; // push button, link
                exposed |= control;
                downloading |= LauncherSignals.IsDownloadActive(label);
                bool isRetry = enabled && control && LauncherSignals.IsRetry(label);
                bool isResume = enabled && control && LauncherSignals.IsResume(label);
                retry |= isRetry; resume |= isResume;
                if (!invoked && ((isRetry && retryAction) || (isResume && resumeAction)) && !string.IsNullOrEmpty(acc.get_accDefaultAction(child)))
                { ct.ThrowIfCancellationRequested(); invoked = invoke(() => acc.accDoDefaultAction(child)); }
                if (child is not int value || value != 0) return;
                int count = Math.Min(acc.accChildCount, Math.Max(0, remaining));
                if (count == 0) return;
                var children = new object[count];
                if (AccessibleChildren(acc, 0, count, children, out int obtained) < 0) return;
                for (int i = 0; i < obtained; i++)
                    if (children[i] is IAccessible nested) Visit(nested, 0, depth + 1);
                    else if (children[i] is int id) Visit(acc, id, depth + 1);
            }
            catch (Exception e) when (e is COMException or ArgumentException or InvalidCastException or FormatException or NotImplementedException) { }
        }
        foreach (IntPtr window in handles)
        {
            ct.ThrowIfCancellationRequested();
            var iid = typeof(IAccessible).GUID;
            if (AccessibleObjectFromWindow(window, unchecked((uint)-4), ref iid, out var root) >= 0 && root != null) Visit(root, 0, 0);
            if (remaining <= 0) break;
        }
        return new(retry, resume, downloading, resume && !retry && !downloading, exposed, invoked);
    }
}
