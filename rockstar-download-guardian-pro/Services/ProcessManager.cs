using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
namespace RockstarGuardian.Services;
public sealed class ProcessManager
{
    public static async Task FlushDnsAsync(CancellationToken ct)
    {
        using var p = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "ipconfig.exe"), "/flushdns") { UseShellExecute = false, CreateNoWindow = true }) ?? throw new IOException("Não foi possível iniciar ipconfig.");
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0) throw new IOException("Limpeza DNS falhou; pode exigir permissão de administrador.");
    }
    public async Task RestartAsync(string launcher, string game, CancellationToken ct)
    {
        if (!File.Exists(launcher) || !Path.GetFileName(launcher).Equals("Launcher.exe", StringComparison.OrdinalIgnoreCase)) throw new IOException("Selecione o Launcher.exe oficial.");
        var launcherFolder = Path.GetDirectoryName(Path.GetFullPath(launcher))!;
        foreach (var name in new[] { "Launcher", "LauncherPatcher", "SocialClubHelper" })
        foreach (var p in Process.GetProcessesByName(name))
        using (p)
        {
            ct.ThrowIfCancellationRequested();
            var path = p.MainModule?.FileName ?? "";
            // Somente processos Rockstar na instalação selecionada ou pasta Social Club irmã.
            var social = Path.GetFullPath(Path.Combine(launcherFolder, "..", "Social Club")) + Path.DirectorySeparatorChar;
            if (path.StartsWith(launcherFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || (name == "SocialClubHelper" && path.StartsWith(social, StringComparison.OrdinalIgnoreCase)))
            { p.Kill(); await p.WaitForExitAsync(ct); }
        }
        await Task.Delay(5000, ct);
        await Task.Run(() => Clean(new DirectoryInfo(game), ct), ct);
        ct.ThrowIfCancellationRequested();
        Process.Start(new ProcessStartInfo(launcher) { WorkingDirectory = launcherFolder, UseShellExecute = true, WindowStyle = ProcessWindowStyle.Minimized });
    }
    private static void Clean(DirectoryInfo folder, CancellationToken ct)
    {
        if (!folder.Exists || (folder.Attributes & FileAttributes.ReparsePoint) != 0) return;
        foreach (var f in folder.EnumerateFiles())
        {
            ct.ThrowIfCancellationRequested();
            if ((f.Extension.Equals(".lock", StringComparison.OrdinalIgnoreCase) || f.Extension.Equals(".part", StringComparison.OrdinalIgnoreCase)) && f.Length == 0 && (f.Attributes & FileAttributes.ReparsePoint) == 0)
            { try { f.Refresh(); if (f.Exists && f.Length == 0) f.Delete(); } catch (IOException) { } }
        }
        foreach (var child in folder.EnumerateDirectories()) Clean(child, ct);
    }
}
