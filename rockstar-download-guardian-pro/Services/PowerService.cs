using System.Runtime.InteropServices;
namespace RockstarGuardian.Services;
public static class PowerService
{
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint SetThreadExecutionState(uint flags);
    // Chamado exclusivamente na thread da interface, tanto para adquirir como liberar.
    public static bool SetActive(bool active) => SetThreadExecutionState(active ? 0x80000001u : 0x80000000u) != 0;
}
