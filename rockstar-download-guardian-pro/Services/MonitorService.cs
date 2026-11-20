using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RockstarGuardian.Models;
namespace RockstarGuardian.Services;
public sealed record Snapshot(double Speed, long Bytes, double? EtaSeconds, string State, bool Online, int Level, bool Active = false);
public sealed class MonitorService
{
    public event Action<Snapshot>? Updated;
    public event Action<string>? Event;
    public event Action<bool>? Notify;
    public event Action? Completed;
    private readonly Settings settings;
    private readonly LogMonitor logs = new();
    private readonly AutomationService automation = new();
    private readonly ProcessManager processes = new();
    private readonly Dictionary<int, ulong> previous = new();
    private readonly Queue<double> history = new();
    private int level, restarts;
    private DateTime lastActivity, stageAt, stableAt, cooldown, nextClick;
    private bool sawActivity, finished;
    [StructLayout(LayoutKind.Sequential)] private struct Io { public ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes; }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetProcessIoCounters(IntPtr process, out Io counters);
    public MonitorService(Settings s) { settings = s; }
    public async Task RunAsync(CancellationToken ct)
    {
        string logPath = "";
        lastActivity = stableAt = DateTime.UtcNow;
        long lastBytes = -1; var watch = Stopwatch.StartNew(); double lastSample = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Run(() => DetectionService.Refresh(settings), ct);
                if (logPath != settings.LogPath) { logPath = settings.LogPath; logs.Reset(logPath); }
                var sample = await Task.Run(() => Sample(ct), ct);
                double seconds = Math.Max(1, watch.Elapsed.TotalSeconds - lastSample); lastSample = watch.Elapsed.TotalSeconds;
                double rate = sample.Written / seconds / 1_000_000d;
                var now = DateTime.UtcNow;
                bool growth = lastBytes >= 0 && sample.Size > lastBytes;
                if (lastBytes != sample.Size || rate > .01) stableAt = now;
                lastBytes = sample.Size;
                bool error = false;
                try { error = await Task.Run(() => logs.ReadError(logPath), ct); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* UI continua independente do log. */ }
                var ui = await automation.InspectAsync(settings.LauncherPath, now >= nextClick, level > 0 || error, ct);
                ct.ThrowIfCancellationRequested();
                if (ui.Invoked) { nextClick = DateTime.UtcNow.AddSeconds(15); Event?.Invoke("Comando de retomada enviado automaticamente ao Launcher."); }
                bool activity = rate > .01 || growth;
                if (activity)
                {
                    lastActivity = now;
                    // E/S de logs sozinha não prova um download.
                    sawActivity |= ui.Downloading || growth;
                    if (level > 0 && (ui.Downloading || growth) && !ui.Retry)
                    { Event?.Invoke("Atividade de download retomada após recuperação."); Notify?.Invoke(false); level = 0; }
                }
                if (ui.Paused && !error && level == 0) lastActivity = now;
                history.Enqueue(rate); if (history.Count > 12) history.Dequeue();
                double mean = history.Average();
                double target = settings.TargetGb * 1_000_000_000d;
                bool knownFolder = sample.Size >= 0;
                bool done = knownFolder && RecoveryRules.IsComplete(sawActivity, sample.Size, target, now - stableAt, level);
                if (done && !finished) { finished = true; Notify?.Invoke(true); Completed?.Invoke(); }
                string state = finished ? "Alvo atingido • verifique no Launcher" : !sample.Running ? "Automático • aguardando Launcher" : level > 0 ? "Recuperando download automaticamente" : ui.Retry ? "Erro detectado • retomando" : ui.Paused ? "Download pausado no Launcher" : ui.Downloading ? "Download em observação" : !ui.Accessible ? "Observando processos e logs • interface não exposta" : "Launcher conectado • aguardando download";
                Updated?.Invoke(new(rate, sample.Size, knownFolder && mean > .01 ? Math.Max(0, target - sample.Size) / (mean * 1_000_000) : null, state, NetworkInterface.GetIsNetworkAvailable(), level, ui.Downloading || level > 0));
                if (finished) return;
                if (!sample.Running) { level = 0; lastActivity = now; }
                else if (now >= cooldown && !(knownFolder && sawActivity && sample.Size >= target))
                {
                    if (level == 0 && LauncherSignals.ShouldRecover(error, ui.Retry, ui.Downloading, ui.Paused, now - lastActivity))
                    {
                        level = 1; stageAt = now;
                        Event?.Invoke("Nível 1: erro ou download sem progresso; retomada automática.");
                        if (!ui.Invoked) await RetryAsync(ct);
                    }
                    else if (level == 1 && RecoveryRules.ShouldEscalate(level, now - stageAt))
                    {
                        level = 2; stageAt = now; Event?.Invoke("Nível 2: limpando cache DNS e tentando novamente.");
                        try { await ProcessManager.FlushDnsAsync(ct); }
                        catch (Exception e) when (e is IOException or System.ComponentModel.Win32Exception) { Event?.Invoke("Cache DNS indisponível; prosseguindo sem alterar permissões."); }
                        await RetryAsync(ct);
                    }
                    else if (level == 2 && RecoveryRules.ShouldEscalate(level, now - stageAt))
                    {
                        if (++restarts > 3)
                        {
                            Event?.Invoke("Limite de reinícios atingido. Observação e Retry continuam; novos reinícios suspensos por 30 minutos.");
                            restarts = 0; level = 0; cooldown = now.AddMinutes(30);
                        }
                        else
                        {
                            level = 3; Event?.Invoke("Nível 3: reiniciando a instalação detectada do Launcher.");
                            await processes.RestartAsync(settings.LauncherPath, settings.GamePath, ct);
                            stageAt = lastActivity = DateTime.UtcNow; cooldown = stageAt.AddMinutes(2);
                        }
                    }
                    else if (level == 3) { level = 0; lastActivity = now; }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                Event?.Invoke("Acesso ao Launcher indisponível; nova detecção automática em 5 segundos.");
                Updated?.Invoke(new(0, -1, null, "Aguardando acesso ao Launcher • repetindo detecção", NetworkInterface.GetIsNetworkAvailable(), -1));
                level = 0; cooldown = DateTime.UtcNow.AddSeconds(30);
            }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
    private async Task RetryAsync(CancellationToken ct)
    {
        var ui = await automation.InspectAsync(settings.LauncherPath, true, true, ct);
        nextClick = DateTime.UtcNow.AddSeconds(15);
        Event?.Invoke(ui.Invoked ? "Retomada enviada sem mover o mouse." : "Aguardando o Launcher disponibilizar o controle de retomada.");
    }
    private (long Size, ulong Written, bool Running) Sample(CancellationToken ct)
    {
        long total = -1;
        if (!string.IsNullOrWhiteSpace(settings.GamePath) && Directory.Exists(settings.GamePath))
        {
            try { total = Size(new DirectoryInfo(settings.GamePath), ct); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* Pasta é opcional. */ }
        }
        ulong delta = 0; var alive = new HashSet<int>();
        foreach (int pid in LauncherProcesses.Find(settings.LauncherPath))
        {
            try
            {
                using var p = Process.GetProcessById(pid); alive.Add(pid);
                if (!GetProcessIoCounters(p.Handle, out var io)) continue;
                if (previous.TryGetValue(pid, out ulong old) && io.WriteBytes >= old) delta += io.WriteBytes - old;
                previous[pid] = io.WriteBytes;
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException or System.ComponentModel.Win32Exception) { }
        }
        foreach (int pid in previous.Keys.Where(x => !alive.Contains(x)).ToArray()) previous.Remove(pid);
        return (total, delta, alive.Count > 0);
    }
    private static long Size(DirectoryInfo dir, CancellationToken ct)
    {
        if (!dir.Exists || (dir.Attributes & FileAttributes.ReparsePoint) != 0) return 0;
        long bytes = 0;
        foreach (var f in dir.EnumerateFiles()) { ct.ThrowIfCancellationRequested(); if ((f.Attributes & FileAttributes.ReparsePoint) == 0) bytes += f.Length; }
        foreach (var child in dir.EnumerateDirectories()) bytes += Size(child, ct);
        return bytes;
    }
}
