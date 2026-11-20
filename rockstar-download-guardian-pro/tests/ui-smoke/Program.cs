using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using RockstarGuardian.Services;
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var app = new Application();
        var content = new StackPanel();
        var window = new Window { Title = "Guardian • teste simulado de erro", Width = 460, Height = 180, Content = content };
        window.Loaded += async (_, _) =>
        {
            try
            {
                string path = Process.GetCurrentProcess().MainModule!.FileName!;
                var engine = new AutomationService();
                var before = await engine.InspectAsync(path, true, false, CancellationToken.None);
                if (before.Invoked) throw new Exception("Ação sem botão de erro");
                int clicks = 0;
                var retry = new Button { Content = "TENTE NOVAMENTE", Margin = new Thickness(20) };
                retry.Click += (_, _) => clicks++;
                content.Children.Add(new TextBlock { Text = "Erro fictício de conexão" });
                content.Children.Add(retry);
                await Task.Delay(500);
                var after = await engine.InspectAsync(path, true, false, CancellationToken.None);
                await Task.Delay(200);
                if (!after.Invoked || clicks != 1) throw new Exception("Botão que apareceu não foi acionado");
                await engine.InspectAsync(path, true, false, CancellationToken.None);
                if (clicks != 1) throw new Exception("Comando repetido antes do intervalo");
                content.Children.Clear();
                var resume = new Button { Content = "Continuar", Margin = new Thickness(20) };
                int resumed = 0; resume.Click += (_, _) => resumed++;
                content.Children.Add(resume); await Task.Delay(300);
                var other = new AutomationService();
                await other.InspectAsync(path, true, false, CancellationToken.None);
                if (resumed != 0) throw new Exception("Retomada sem erro");
                var recovery = await other.InspectAsync(path, true, true, CancellationToken.None);
                await Task.Delay(200);
                if (!recovery.Invoked || resumed != 1) throw new Exception("Retomada com erro não enviada");
                Console.WriteLine("5 verificações de interface aprovadas: ausência, aparecimento de Retry, intervalo e retomada contextual.");
                app.Shutdown(0);
            }
            catch (Exception error) { Console.Error.WriteLine(error); app.Shutdown(1); }
        };
        return app.Run(window);
    }
}
