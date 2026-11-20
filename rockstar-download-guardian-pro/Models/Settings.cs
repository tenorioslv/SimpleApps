using System;
using System.IO;
using System.Text.Json;
namespace RockstarGuardian.Models;
public sealed class Settings
{
    public string GamePath { get; set; } = "";
    public string LauncherPath { get; set; } = "";
    public string LogPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Rockstar Games", "Launcher", "launcher.log");
    public double TargetGb { get; set; } = 115;
    public bool ShutdownWhenDone { get; set; }
    public static string DataPath => Path.Combine(AppContext.BaseDirectory, "data", "settings.json");
    public static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(DataPath)) ?? new(); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataPath)!);
        File.WriteAllText(DataPath + ".tmp", JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(DataPath + ".tmp", DataPath, true);
    }
}
