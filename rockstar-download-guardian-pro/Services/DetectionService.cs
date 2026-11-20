using System;
using System.IO;
using Microsoft.Win32;
using RockstarGuardian.Models;
using System.Diagnostics;
namespace RockstarGuardian.Services;
public static class DetectionService
{
    public static void Detect(Settings s)
    {
        foreach (var p in Process.GetProcessesByName("Launcher")) using (p)
        {
            try { var file = p.MainModule?.FileName; if (File.Exists(file)) { s.LauncherPath = file; break; } }
            catch (System.ComponentModel.Win32Exception) { }
            catch (InvalidOperationException) { }
        }
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            foreach (var name in new[] { "Grand Theft Auto V", "Grand Theft Auto V Enhanced", "Launcher" })
            {
                using var key = root.OpenSubKey(@"SOFTWARE\Rockstar Games\" + name);
                var folder = key?.GetValue("InstallFolder") as string;
                if (string.IsNullOrEmpty(folder)) continue;
                if (name == "Launcher" && File.Exists(Path.Combine(folder, "Launcher.exe")) && string.IsNullOrEmpty(s.LauncherPath)) s.LauncherPath = Path.Combine(folder, "Launcher.exe");
                else if (name != "Launcher" && Directory.Exists(folder) && string.IsNullOrEmpty(s.GamePath)) s.GamePath = folder;
            }
        }
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var launcher = Path.Combine(pf, "Rockstar Games", "Launcher", "Launcher.exe");
        var game = Path.Combine(pf, "Rockstar Games", "Grand Theft Auto V");
        if (s.LauncherPath == "" && File.Exists(launcher)) s.LauncherPath = launcher;
        if (s.GamePath == "" && Directory.Exists(game)) s.GamePath = game;
    }
    public static void Refresh(Settings settings)
    {
        if (!File.Exists(settings.LauncherPath)) Detect(settings);
        string redirected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Rockstar Games", "Launcher", "launcher.log");
        string standard = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Rockstar Games", "Launcher", "launcher.log");
        if (!File.Exists(settings.LogPath)) settings.LogPath = File.Exists(redirected) ? redirected : File.Exists(standard) ? standard : redirected;
    }
}
