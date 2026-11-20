using System;
using System.IO;
using System.Globalization;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using RockstarGuardian.Models;
using RockstarGuardian.Services;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
namespace RockstarGuardian;
public partial class MainWindow : Window
{
    private Settings settings;
    private CancellationTokenSource? monitoring;
    private Task? loop;
    private readonly Forms.NotifyIcon tray = new();
    private readonly DispatcherTimer shutdownTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private int countdown;
    private bool exiting, busy;
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);
    public MainWindow()
    {
        InitializeComponent();
        settings = Settings.Load();
        try { DetectionService.Detect(settings); } catch { AddEvent("Detecção será repetida em segundo plano."); }
        ShowSettings();
        tray.Text = "Rockstar Guardian Pro • Pausado"; SetIcon(Drawing.Color.Gray);
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Abrir Painel", null, (_, _) => Dispatcher.Invoke(OpenPanel));
        menu.Items.Add("Pausar/Retomar Monitoramento", null, async (_, _) => await ToggleAsync());
        menu.Items.Add("Forçar Reinício do Launcher", null, async (_, _) => await RestartAsync());
        menu.Items.Add("Sair", null, async (_, _) => await ExitAsync());
        tray.ContextMenuStrip = menu; tray.DoubleClick += (_, _) => OpenPanel(); tray.Visible = true;
        StateChanged += (_, _) => { if (WindowState == WindowState.Minimized) Hide(); };
        Closing += OnClosing;
        shutdownTimer.Tick += (_, _) =>
        {
            ShutdownLabel.Text = $"Desligamento em {--countdown} segundos";
            if (countdown > 0) return;
            shutdownTimer.Stop(); ShutdownPanel.Visibility = Visibility.Collapsed;
            // Só agenda no sistema após a contagem local terminar. Cancelar/pausar/sair não deixa uma tarefa pendente.
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), "/s /t 0") { UseShellExecute = false, CreateNoWindow = true }); }
            catch { AddEvent("Não foi possível solicitar desligamento."); }
        };
        AddEvent("Modo automático: Launcher e logs são detectados sem selecionar pastas.");
        Loaded += async (_, _) => await ToggleAsync();
    }
    private void ShowSettings()
    {
        GamePath.Text = settings.GamePath; LauncherPath.Text = settings.LauncherPath; LogPath.Text = settings.LogPath;
        Target.Text = settings.TargetGb.ToString(CultureInfo.CurrentCulture); ShutdownOption.IsChecked = settings.ShutdownWhenDone;
    }
    private bool ReadSettings()
    {
        if (Consent.IsChecked != true) { MessageBox.Show(this, "Marque o consentimento de processamento local antes de continuar."); return false; }
        if (!double.TryParse(Target.Text, out double gb) || !double.IsFinite(gb) || gb < 1 || gb > 10000) { MessageBox.Show(this, "Informe um alvo entre 1 e 10000 GB."); return false; }
        if (!string.IsNullOrWhiteSpace(LauncherPath.Text) && (!File.Exists(LauncherPath.Text) || !Path.GetFileName(LauncherPath.Text).Equals("Launcher.exe", StringComparison.OrdinalIgnoreCase))) { MessageBox.Show(this, "O caminho opcional precisa apontar para Launcher.exe. Deixe vazio para detectar automaticamente."); return false; }
        try
        {
            string game = string.IsNullOrWhiteSpace(GamePath.Text) ? "" : Path.GetFullPath(GamePath.Text);
            if (game != "" && (!Directory.Exists(game) || game.TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetPathRoot(game)?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) || (File.GetAttributes(game) & FileAttributes.ReparsePoint) != 0)) { MessageBox.Show(this, "Deixe a pasta vazia para modo automático ou indique uma pasta específica válida."); return false; }
            var detected = new Settings(); DetectionService.Detect(detected);
            settings = new Settings { GamePath = game, LauncherPath = string.IsNullOrWhiteSpace(LauncherPath.Text) ? detected.LauncherPath : Path.GetFullPath(LauncherPath.Text), LogPath = string.IsNullOrWhiteSpace(LogPath.Text) ? detected.LogPath : Path.GetFullPath(LogPath.Text), TargetGb = gb, ShutdownWhenDone = ShutdownOption.IsChecked == true };
            settings.Save(); return true;
        }
        catch { MessageBox.Show(this, "Não foi possível salvar. Confira os caminhos e a permissão de escrita na pasta do app."); return false; }
    }
    private async Task ToggleAsync()
    {
        if (busy) return;
        if (monitoring != null) { await StopAsync(); return; }
        monitoring = new CancellationTokenSource(); var token = monitoring.Token;
        ConfigPanel.IsEnabled = Consent.IsEnabled = false; ToggleButton.Content = "Pausar monitoramento";
        var monitor = new MonitorService(settings);
        monitor.Event += text => Dispatcher.BeginInvoke(() => { if (!token.IsCancellationRequested) AddEvent(text); });
        monitor.Updated += sample => Dispatcher.BeginInvoke(() => { if (!token.IsCancellationRequested) Render(sample); });
        monitor.Notify += complete => Dispatcher.BeginInvoke(() => { if (!token.IsCancellationRequested) tray.ShowBalloonTip(4000, "Rockstar Guardian Pro", complete ? "Alvo atingido e gravação estável. Confirme no Launcher." : "Atividade retomada após tentativa de recuperação.", Forms.ToolTipIcon.Info); });
        monitor.Completed += () => Dispatcher.BeginInvoke(() =>
        {
            if (token.IsCancellationRequested) return;
            PowerService.SetActive(false);
            if (settings.ShutdownWhenDone)
            {
                countdown = 60; ShutdownLabel.Text = "Desligamento em 60 segundos"; ShutdownPanel.Visibility = Visibility.Visible;
                OpenPanel(); shutdownTimer.Start();
            }
        });
        StatusLabel.Text = "● Proteção automática ativa"; AddEvent("Observação automática iniciada. Retry será acionado ao detectar falha.");
        loop = RunMonitorAsync(monitor, token);
        await Task.CompletedTask;
    }
    private async Task RunMonitorAsync(MonitorService service, CancellationToken token)
    {
        try { await service.RunAsync(token); }
        catch (OperationCanceledException) { }
        catch { AddEvent("Falha inesperada no monitoramento. Pause e revise as configurações."); StatusLabel.Text = "Erro crítico"; SetIcon(Drawing.Color.IndianRed); }
        finally { PowerService.SetActive(false); }
    }
    private async Task StopAsync()
    {
        if (monitoring == null) return;
        busy = true;
        try
        {
            CancelShutdown(); monitoring.Cancel(); if (loop != null) await loop;
            monitoring.Dispose(); monitoring = null; loop = null;
            ConfigPanel.IsEnabled = Consent.IsEnabled = true; ToggleButton.Content = "Iniciar monitoramento";
            StatusLabel.Text = "● Pausado"; SetIcon(Drawing.Color.Gray); AddEvent("Monitoramento pausado.");
        }
        finally { busy = false; PowerService.SetActive(false); }
    }
    private void Render(Snapshot s)
    {
        PowerService.SetActive(s.Active);
        SpeedLabel.Text = $"{s.Speed:F2} MB/s"; SizeLabel.Text = s.Bytes >= 0 ? $"{s.Bytes / 1_000_000_000d:F1} / {settings.TargetGb:G} GB" : "Automático";
        EtaLabel.Text = s.EtaSeconds is double eta && eta < TimeSpan.MaxValue.TotalSeconds ? $"{(int)(eta / 3600)}h {((int)(eta / 60) % 60)}m" : "Aguardando";
        Progress.IsIndeterminate = s.Bytes < 0 && s.Active;
        Progress.Value = s.Bytes >= 0 ? Math.Clamp(s.Bytes / (settings.TargetGb * 1_000_000_000d) * 100, 0, 100) : 0;
        StatusLabel.Text = "● " + s.State;
        NetworkLabel.Text = (s.Online ? "Rede disponível" : "Sem interface de rede conectada") + " • detecção automática • interface + processos + logs";
        SetIcon(s.Level < 0 ? Drawing.Color.IndianRed : s.Level > 0 ? Drawing.Color.Gold : s.Speed > .01 ? Drawing.Color.MediumSeaGreen : Drawing.Color.Gray);
    }
    private void SetIcon(Drawing.Color color)
    {
        using var bitmap = new Drawing.Bitmap(32, 32); using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new Drawing.SolidBrush(color); graphics.FillEllipse(brush, 3, 3, 26, 26);
        var handle = bitmap.GetHicon(); using var native = Drawing.Icon.FromHandle(handle);
        var old = tray.Icon; tray.Icon = (Drawing.Icon)native.Clone(); old?.Dispose(); DestroyIcon(handle);
    }
    private void AddEvent(string message)
    {
        Activity.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
        if (Activity.Text.Length > 16000) Activity.Text = Activity.Text[^12000..]; Activity.ScrollToEnd();
    }
    private void OpenPanel() { Show(); WindowState = WindowState.Normal; Activate(); }
    private async Task RestartAsync()
    {
        if (busy) return;
        if (monitoring != null) await StopAsync();
        OpenPanel();
        try { DetectionService.Refresh(settings); } catch { }
        if (!File.Exists(settings.LauncherPath)) { AddEvent("Aguardando detecção da instalação do Launcher."); return; }
        if (MessageBox.Show(this, "Encerrar os processos Rockstar da instalação selecionada e reabrir o Launcher? O download poderá ser interrompido. Só arquivos .lock/.part vazios serão removidos.", "Reinício do Launcher", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        busy = true; ConfigPanel.IsEnabled = false;
        try { await new ProcessManager().RestartAsync(settings.LauncherPath, settings.GamePath, CancellationToken.None); AddEvent("Launcher reiniciado. Inicie o monitoramento quando desejar."); }
        catch { AddEvent("Reinício não concluído. Verifique permissões e caminhos."); }
        finally { busy = false; ConfigPanel.IsEnabled = true; }
    }
    private void CancelShutdown() { shutdownTimer.Stop(); ShutdownPanel.Visibility = Visibility.Collapsed; }
    private async void ToggleClick(object sender, RoutedEventArgs e) => await ToggleAsync();
    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (monitoring != null || busy) { MessageBox.Show(this, "Pause o monitoramento para salvar preferências opcionais."); return; }
        if (ReadSettings()) { ShowSettings(); AddEvent("Preferências opcionais salvas localmente. Retome para aplicar."); }
    }
    private async void RestartClick(object sender, RoutedEventArgs e) => await RestartAsync();
    private async void ExitClick(object sender, RoutedEventArgs e) => await ExitAsync();
    private void CancelShutdownClick(object sender, RoutedEventArgs e) { CancelShutdown(); AddEvent("Desligamento cancelado."); }
    private async void DeleteClick(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        await StopAsync(); CancelShutdown();
        try
        {
            if (File.Exists(Settings.DataPath)) File.Delete(Settings.DataPath);
            if (File.Exists(Settings.DataPath + ".tmp")) File.Delete(Settings.DataPath + ".tmp");
            settings = new Settings { LogPath = "" }; ShowSettings(); Consent.IsChecked = false; Activity.Clear();
            SpeedLabel.Text = "— MB/s"; SizeLabel.Text = "— / 115 GB"; EtaLabel.Text = "Aguardando"; Progress.Value = 0;
            AddEvent("Configurações e histórico da sessão apagados. Arquivos do jogo e logs da Rockstar preservados.");
        }
        catch { MessageBox.Show(this, "Não foi possível remover o JSON. Confira a permissão de escrita."); }
    }
    private void BrowseGame(object sender, RoutedEventArgs e)
    { using var d = new Forms.FolderBrowserDialog { Description = "Selecione a pasta de instalação do GTA V", UseDescriptionForTitle = true }; if (d.ShowDialog() == Forms.DialogResult.OK) GamePath.Text = d.SelectedPath; }
    private void BrowseLauncher(object sender, RoutedEventArgs e)
    { var d = new Microsoft.Win32.OpenFileDialog { Filter = "Launcher|Launcher.exe" }; if (d.ShowDialog(this) == true) LauncherPath.Text = d.FileName; }
    private void BrowseLog(object sender, RoutedEventArgs e)
    { var d = new Microsoft.Win32.OpenFileDialog { Filter = "Log do Launcher|*.log" }; if (d.ShowDialog(this) == true) LogPath.Text = d.FileName; }
    private void OnClosing(object? sender, CancelEventArgs e) { if (!exiting) { e.Cancel = true; Hide(); } }
    private async Task ExitAsync()
    {
        if (busy) return;
        await StopAsync(); CancelShutdown(); PowerService.SetActive(false); exiting = true;
        tray.Visible = false; var icon = tray.Icon; tray.Dispose(); icon?.Dispose(); Close(); System.Windows.Application.Current.Shutdown();
    }
}
