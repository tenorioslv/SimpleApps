using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RockstarGuardian.Models;
namespace RockstarGuardian.Services;
public sealed record Snapshot(string State, string Detail, int Attempts, bool Warning, bool Connected);
public sealed class MonitorService
{
    public event Action<Snapshot>? Updated;
    public event Action<string>? Event;
    private readonly Settings settings;
    private readonly LogMonitor logs = new();
    private readonly AutomationService automation = new();
    public MonitorService(Settings settings) { this.settings = settings; }
    public async Task RunAsync(CancellationToken ct)
    {
        string logPath = "";
        DateTime? errorAt = null;
        int attempts = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Run(() => DetectionService.Refresh(settings), ct);
                if (logPath != settings.LogPath) { logPath = settings.LogPath; logs.Reset(logPath); }
                bool logError = false;
                try { logError = await Task.Run(() => logs.ReadError(logPath), ct); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
                if (logError) { errorAt = DateTime.UtcNow; Event?.Invoke("Erro novo no log; aguardando o controle de nova tentativa."); }
                bool connected = await Task.Run(() => LauncherProcesses.Find(settings.LauncherPath).Count > 0, ct);
                var ui = connected ? await automation.InspectAsync(settings.LauncherPath, true, RetryPolicy.HasRecentError(DateTime.UtcNow, errorAt), ct) : new LauncherUiState(false, false, false, false);
                ct.ThrowIfCancellationRequested();
                if (ui.Retry) errorAt = DateTime.UtcNow;
                if (ui.Invoked)
                {
                    attempts++;
                    Event?.Invoke("Botão de nova tentativa acionado automaticamente. Aguardando resposta do Launcher.");
                }
                string state = !connected ? "Aguardando Rockstar Launcher" : ui.Invoked ? "Nova tentativa enviada" : ui.Retry ? "Erro detectado • tentando novamente" : "Proteção ativa • vigiando avisos de erro";
                string detail = !connected ? "A detecção será repetida automaticamente." : ui.Invoked ? "O comando foi enviado. Isso ainda não confirma que o erro foi resolvido." : ui.Retry ? "O botão será acionado respeitando o intervalo de 15 segundos." : "Nenhum botão de erro acionável encontrado neste ciclo. Quando aparecer, será verificado automaticamente.";
                Updated?.Invoke(new(state, detail, attempts, ui.Retry || ui.Invoked, connected));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception or System.Runtime.InteropServices.COMException)
            {
                Event?.Invoke("Acesso temporariamente indisponível; a observação será repetida.");
                Updated?.Invoke(new("Repetindo detecção do Launcher", "Não foi possível consultar os controles neste ciclo.", attempts, true, false));
            }
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }
}
