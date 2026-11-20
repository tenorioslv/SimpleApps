using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
namespace RockstarGuardian.Services;
public static class LauncherProcesses
{
    public static IReadOnlyList<int> Find(string launcher)
    {
        var ids = new List<int>();
        if (!File.Exists(launcher)) return ids;
        string folder = Path.GetDirectoryName(Path.GetFullPath(launcher))!;
        string social = Path.GetFullPath(Path.Combine(folder, "..", "Social Club")) + Path.DirectorySeparatorChar;
        foreach (string name in new[] { "Launcher", "LauncherPatcher", "SocialClubHelper" })
        foreach (var p in Process.GetProcessesByName(name)) using (p)
        {
            try
            {
                string file = p.MainModule?.FileName ?? "";
                if (file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || (name == "SocialClubHelper" && file.StartsWith(social, StringComparison.OrdinalIgnoreCase))) ids.Add(p.Id);
            }
            catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
        }
        return ids;
    }
}
