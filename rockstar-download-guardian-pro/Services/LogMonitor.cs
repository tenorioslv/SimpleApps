using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
namespace RockstarGuardian.Services;
public sealed class LogMonitor
{
    private long offset;
    private DateTime created;
    private string pending = "";
    public void Reset(string path)
    {
        var f = new FileInfo(path); offset = f.Exists ? f.Length : 0; created = f.Exists ? f.CreationTimeUtc : default; pending = "";
    }
    public bool ReadError(string path)
    {
        if (!File.Exists(path)) return false;
        var f = new FileInfo(path);
        if (f.Length < offset || f.CreationTimeUtc != created) { offset = 0; pending = ""; created = f.CreationTimeUtc; }
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        // Leitura limitada, sem armazenar conteúdo dos logs.
        if (stream.Length - offset > 65536) { offset = stream.Length - 65536; pending = ""; }
        stream.Position = offset;
        var bytes = new byte[65536]; int n = stream.Read(bytes); offset += n;
        var text = pending + Encoding.UTF8.GetString(bytes, 0, n);
        int last = text.LastIndexOf('\n');
        if (last < 0) { pending = text.Length < 4096 ? text : ""; return false; }
        pending = text[(last + 1)..];
        return Regex.IsMatch(text[..last], @"(?i)(network.{0,40}(error|fail)|connection.{0,40}(lost|fail|error)|timed?\s*out|timeout|download.{0,40}(fail|error))");
    }
}
