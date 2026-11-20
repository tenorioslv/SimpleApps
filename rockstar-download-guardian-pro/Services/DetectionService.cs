using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using RockstarGuardian.Models;
namespace RockstarGuardian.Services;
public static class DetectionService
{
    public static void Refresh(Settings settings)
    {
        foreach (var p in Process.GetProcessesByName("Launcher")) using (p)
        {
            try { var file = p.MainModule?.FileName; if (File.Exists(file)) { settings.LauncherPath = file; break; } }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { }
        }
        if (!File.Exists(settings.LauncherPath))
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                    using var key = root.OpenSubKey(@"SOFTWARE\Rockstar Games\Launcher");
                    var folder = key?.GetValue("InstallFolder") as string;
                    if (folder != null && File.Exists(Path.Combine(folder, "Launcher.exe"))) settings.LauncherPath = Path.Combine(folder, "Launcher.exe");
                }
                catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
            }
            string standard = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "Launcher.exe");
            if (!File.Exists(settings.LauncherPath) && File.Exists(standard)) settings.LauncherPath = standard;
        }
        string redirectedLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Rockstar Games", "Launcher", "launcher.log");
        string standardLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Rockstar Games", "Launcher", "launcher.log");
        if (!File.Exists(settings.LogPath)) settings.LogPath = File.Exists(redirectedLog) ? redirectedLog : File.Exists(standardLog) ? standardLog : redirectedLog;
    }
}
