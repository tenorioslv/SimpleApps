using System;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using RockstarGuardian.Models;
using RockstarGuardian.Services;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
namespace RockstarGuardian;
public partial class MainWindow : Window
{
    private CancellationTokenSource? monitoring;
    private Task? loop;
    private readonly Forms.NotifyIcon tray = new();
    private bool exiting, busy;
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);
    public MainWindow()
    {
        InitializeComponent();
        tray.Text = "Rockstar Guardian • observação de erros"; SetIcon(Drawing.Color.Gray);
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Abrir Painel", null, (_, _) => OpenPanel());
        menu.Items.Add("Pausar/Retomar Proteção", null, async (_, _) => await ToggleAsync());
        menu.Items.Add("Sair", null, async (_, _) => await ExitAsync());
        tray.ContextMenuStrip = menu; tray.DoubleClick += (_, _) => OpenPanel(); tray.Visible = true;
        StateChanged += (_, _) => { if (WindowState == WindowState.Minimized) Hide(); };
        Closing += OnClosing;
        Loaded += async (_, _) => await ToggleAsync();
    }
    private async Task ToggleAsync()
    {
        if (busy) return;
        if (monitoring != null) { await StopAsync(); return; }
        monitoring = new CancellationTokenSource(); var token = monitoring.Token;
        ToggleButton.Content = "Auto-Retry ativado"; ProtectionGlow.Opacity = 1;
        var monitor = new MonitorService(new Settings());
        monitor.Event += text => Dispatcher.BeginInvoke(() => { if (!token.IsCancellationRequested) AddEvent(text); });
        monitor.Updated += sample => Dispatcher.BeginInvoke(() => { if (!token.IsCancellationRequested) Render(sample); });
        StatusLabel.Text = "● Procurando Rockstar Launcher";
        DetailLabel.Text = "A proteção já começou. Nenhuma configuração é necessária.";
        AddEvent("Proteção de erros iniciada automaticamente. Aguardando avisos com opção de nova tentativa.");
        loop = RunMonitorAsync(monitor, token);
    }
    private async Task RunMonitorAsync(MonitorService service, CancellationToken token)
    {
        try { await service.RunAsync(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch
        {
            StatusLabel.Text = "● Não foi possível continuar a observação";
            DetailLabel.Text = "Pause e retome para tentar novamente.";
            SetIcon(Drawing.Color.IndianRed);
            AddEvent("Falha inesperada na observação. Nenhuma alteração foi feita no Launcher.");
        }
    }
    private async Task StopAsync()
    {
        if (monitoring == null) return;
        busy = true;
        try
        {
            monitoring.Cancel(); if (loop != null) await loop;
            monitoring.Dispose(); monitoring = null; loop = null;
            ToggleButton.Content = "Auto-Retry pausado";
            StatusLabel.Text = "Proteção pausada"; ProtectionGlow.Opacity = 0.15;
            DetailLabel.Text = "Nenhum botão será acionado enquanto estiver pausado.";
            SetIcon(Drawing.Color.Gray); AddEvent("Proteção pausada.");
        }
        finally { busy = false; }
    }
    private void Render(Snapshot sample)
    {
        StatusLabel.Text = sample.State;
        DetailLabel.Text = sample.Detail;
        AttemptsLabel.Text = sample.Attempts.ToString();
        SetIcon(sample.Warning ? Drawing.Color.Gold : sample.Connected ? Drawing.Color.MediumSeaGreen : Drawing.Color.Gray);
    }
    private void SetIcon(Drawing.Color color)
    {
        using var bitmap = new Drawing.Bitmap(32, 32); using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new Drawing.SolidBrush(color);
        graphics.FillPolygon(brush, new[] { new Drawing.Point(16, 2), new Drawing.Point(29, 7), new Drawing.Point(27, 19), new Drawing.Point(23, 25), new Drawing.Point(16, 30), new Drawing.Point(9, 25), new Drawing.Point(5, 19), new Drawing.Point(3, 7) });
        using var boltBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(16,24,27));
        graphics.FillPolygon(boltBrush, new[] { new Drawing.Point(18,8), new Drawing.Point(11,17), new Drawing.Point(16,17), new Drawing.Point(14,25), new Drawing.Point(23,13), new Drawing.Point(17,13) });
        var handle = bitmap.GetHicon(); using var native = Drawing.Icon.FromHandle(handle);
        var old = tray.Icon; tray.Icon = (Drawing.Icon)native.Clone(); old?.Dispose(); DestroyIcon(handle);
    }
    private void AddEvent(string message)
    {
        Activity.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
        if (Activity.Text.Length > 16000) Activity.Text = Activity.Text[^12000..]; Activity.ScrollToEnd();
    }
    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
    private void OpenPanel() { Show(); WindowState = WindowState.Normal; Activate(); }
    private async void ToggleClick(object sender, RoutedEventArgs e) => await ToggleAsync();
    private async void ExitClick(object sender, RoutedEventArgs e) => await ExitAsync();
    private async void DeleteClick(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        await StopAsync();
        try
        {
            // Remove somente configurações legadas do próprio Guardian, se existirem.
            string path = Path.Combine(AppContext.BaseDirectory, "data", "settings.json");
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            Activity.Clear(); AttemptsLabel.Text = "0";
            AddEvent("Sessão limpa e configurações antigas removidas. Retome quando desejar.");
        }
        catch { AddEvent("Não foi possível apagar configurações antigas. A proteção permanece pausada."); }
    }
    private void OnClosing(object? sender, CancelEventArgs e) { if (!exiting) { e.Cancel = true; Hide(); } }
    private async Task ExitAsync()
    {
        if (busy) return;
        await StopAsync(); exiting = true;
        tray.Visible = false; var icon = tray.Icon; tray.Dispose(); icon?.Dispose(); Close(); System.Windows.Application.Current.Shutdown();
    }
}
