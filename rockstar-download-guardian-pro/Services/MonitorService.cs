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
public sealed record Snapshot(double Speed, long Bytes, double? EtaSeconds, string State, bool Online, int Level);
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
    private Task<bool>? click;
    private int level, restarts;
    private DateTime lastActivity, stageAt, stableAt, cooldown;
    private bool sawActivity, finished;
    [StructLayout(LayoutKind.Sequential)] private struct Io { public ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes; }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetProcessIoCounters(IntPtr process, out Io counters);
    public MonitorService(Settings s) { settings = s; }
    public async Task RunAsync(CancellationToken ct)
    {
        logs.Reset(settings.LogPath); previous.Clear(); history.Clear();
        level = restarts = 0; sawActivity = finished = false;
        lastActivity = stableAt = DateTime.UtcNow; cooldown = DateTime.MinValue;
        long lastBytes = -1; var watch = Stopwatch.StartNew(); double lastSample = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var sample = await Task.Run(() => Sample(ct), ct);
                double seconds = Math.Max(1, watch.Elapsed.TotalSeconds - lastSample); lastSample = watch.Elapsed.TotalSeconds;
                double rate = sample.Written / seconds / 1_000_000d;
                var now = DateTime.UtcNow;
                bool growth = lastBytes >= 0 && sample.Size > lastBytes;
                if (lastBytes != sample.Size || rate > 0.01) stableAt = now;
                lastBytes = sample.Size;
                bool error = await Task.Run(() => logs.ReadError(settings.LogPath), ct);
                if (rate > .01 || growth)
                {
                    lastActivity = now; sawActivity = true;
                    if (level > 0) { Event?.Invoke("Atividade retomada após recuperação."); Notify?.Invoke(false); level = 0; }
                }
                history.Enqueue(rate); if (history.Count > 12) history.Dequeue();
                double mean = history.Average();
                double target = settings.TargetGb * 1_000_000_000d;
                bool done = RecoveryRules.IsComplete(sawActivity, sample.Size, target, now - stableAt, level);
                if (done && !finished) { finished = true; Notify?.Invoke(true); Completed?.Invoke(); }
                var state = finished ? "Alvo atingido • verifique no Launcher" : !sample.Running ? "Aguardando Launcher" : level > 0 ? "Recuperando conexão" : rate > .01 || growth ? "Baixando / gravando" : "Observando atividade";
                Updated?.Invoke(new(rate, sample.Size, mean > .01 ? Math.Max(0, target - sample.Size) / (mean * 1_000_000) : null, state, NetworkInterface.GetIsNetworkAvailable(), level));
                if (finished) return;
                // Ao atingir o alvo, observar estabilidade em vez de reiniciar durante a finalização.
                if (sample.Running && now >= cooldown && !(sawActivity && sample.Size >= target))
                {
                    if (level == 0 && RecoveryRules.ShouldStart(error, now - lastActivity))
                    { level = 1; stageAt = now; Event?.Invoke("Nível 1: procurando Retry / Resume via UI Automation."); await TryClick(ct); }
                    else if (level == 1 && RecoveryRules.ShouldEscalate(level, now - stageAt))
                    { level = 2; stageAt = now; Event?.Invoke("Nível 2: limpeza do cache DNS."); try { await ProcessManager.FlushDnsAsync(ct); } catch (IOException) { Event?.Invoke("DNS não foi limpo. Verifique permissões."); } await TryClick(ct); }
                    else if (level == 2 && RecoveryRules.ShouldEscalate(level, now - stageAt))
                    {
                        if (++restarts > 3) throw new InvalidOperationException("Limite de três reinícios atingido. Pause e verifique o Launcher.");
                        level = 3; Event?.Invoke("Nível 3: reinício limpo do Launcher.");
                        await processes.RestartAsync(settings.LauncherPath, settings.GamePath, ct);
                        stageAt = lastActivity = DateTime.UtcNow;
                        cooldown = stageAt.AddMinutes(2); // evita ciclos destrutivos durante a inicialização
                    }
                    else if (level == 3 && now - stageAt >= TimeSpan.FromSeconds(25))
                    { level = 0; lastActivity = now; Event?.Invoke("Launcher reaberto; aguardando atividade antes de nova tentativa."); }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                Event?.Invoke("Monitoramento interrompido: " + (e is UnauthorizedAccessException ? "sem permissão de leitura ou controle." : e is InvalidOperationException ? e.Message : "falha ao acessar o Launcher, pasta ou log. Confira os caminhos e permissões."));
                Updated?.Invoke(new(0, 0, null, "Erro crítico • revise caminhos e permissões", NetworkInterface.GetIsNetworkAvailable(), -1));
                return;
            }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
    private async Task TryClick(CancellationToken ct)
    {
        if (click is { IsCompleted: false }) { Event?.Invoke("UI Automation ocupada; seguindo com observação."); return; }
        click = automation.RetryAsync();
        try { bool ok = await click.WaitAsync(TimeSpan.FromSeconds(4), ct); Event?.Invoke(ok ? "Comando de retomada enviado." : "Botão de retomada não exposto pelo Launcher."); }
        catch (TimeoutException) { Event?.Invoke("Launcher não respondeu à UI Automation em 4 segundos."); }
    }
    private (long Size, ulong Written, bool Running) Sample(CancellationToken ct)
    {
        long total = Size(new DirectoryInfo(settings.GamePath), ct);
        ulong delta = 0; bool running = false; var alive = new HashSet<int>();
        foreach (var p in Process.GetProcessesByName("Launcher")) using (p)
        {
            try
            {
                if (!string.Equals(p.MainModule?.FileName, settings.LauncherPath, StringComparison.OrdinalIgnoreCase)) continue;
                running = true; alive.Add(p.Id);
                if (!GetProcessIoCounters(p.Handle, out var io)) throw new IOException("Contadores de E/S indisponíveis.");
                if (previous.TryGetValue(p.Id, out ulong old) && io.WriteBytes >= old) delta += io.WriteBytes - old;
                previous[p.Id] = io.WriteBytes;
            }
            catch (InvalidOperationException) { } // processo encerrou durante a amostra
        }
        foreach (var pid in previous.Keys.Where(x => !alive.Contains(x)).ToArray()) previous.Remove(pid);
        return (total, delta, running);
    }
    private static long Size(DirectoryInfo dir, CancellationToken ct)
    {
        if (!dir.Exists) throw new IOException("Pasta inexistente.");
        if ((dir.Attributes & FileAttributes.ReparsePoint) != 0) return 0;
        long bytes = 0;
        foreach (var f in dir.EnumerateFiles()) { ct.ThrowIfCancellationRequested(); if ((f.Attributes & FileAttributes.ReparsePoint) == 0) bytes += f.Length; }
        foreach (var child in dir.EnumerateDirectories()) bytes += Size(child, ct);
        return bytes;
    }
}
