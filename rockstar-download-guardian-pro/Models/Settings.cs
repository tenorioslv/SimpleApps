using System;
using System.IO;
namespace RockstarGuardian.Models;
// Apenas caminhos de operação em memória. Nenhuma configuração é salva.
public sealed class Settings
{
    public string LauncherPath { get; set; } = "";
    public string LogPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Rockstar Games", "Launcher", "launcher.log");
}
