using RockstarGuardian.Services;
int checks = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
Check(!RecoveryRules.ShouldStart(false, TimeSpan.FromSeconds(24)), "Sem reinício prematuro");
Check(RecoveryRules.ShouldStart(false, TimeSpan.FromSeconds(25)), "Limiar 25 s");
Check(RecoveryRules.ShouldStart(true, TimeSpan.Zero), "Erro novo antecipa recuperação");
Check(!RecoveryRules.ShouldEscalate(1, TimeSpan.FromSeconds(14)), "Nível 1 aguarda");
Check(RecoveryRules.ShouldEscalate(1, TimeSpan.FromSeconds(15)), "Nível 2 em 15 s");
Check(!RecoveryRules.ShouldEscalate(2, TimeSpan.FromSeconds(19)), "Nível 2 aguarda");
Check(RecoveryRules.ShouldEscalate(2, TimeSpan.FromSeconds(20)), "Nível 3 em 20 s");
Check(!RecoveryRules.IsComplete(false, 115, 115, TimeSpan.FromSeconds(70), 0), "Não concluir sem atividade");
Check(!RecoveryRules.IsComplete(true, 114, 115, TimeSpan.FromSeconds(70), 0), "Não concluir antes do alvo");
Check(!RecoveryRules.IsComplete(true, 115, 115, TimeSpan.FromSeconds(59), 0), "Exigir estabilidade");
Check(!RecoveryRules.IsComplete(true, 115, 115, TimeSpan.FromSeconds(70), 1), "Não concluir em recuperação");
Check(RecoveryRules.IsComplete(true, 115, 115, TimeSpan.FromSeconds(60), 0), "Concluir sob condições configuradas");
// Arquivos de teste ficam somente no diretório ignorado do próprio app.
string folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "fixtures"));
Directory.CreateDirectory(folder); string path = Path.Combine(folder, "launcher.log");
try
{
    File.WriteAllText(path, "network error histórico\n"); var log = new LogMonitor(); log.Reset(path);
    Check(!log.ReadError(path), "Ignorar erros anteriores ao início");
    File.AppendAllText(path, "download failed"); Check(!log.ReadError(path), "Esperar fim da linha");
    File.AppendAllText(path, "\n"); Check(log.ReadError(path), "Detectar falha nova");
    Check(!log.ReadError(path), "Não repetir mesmo erro");
    File.WriteAllText(path, "timeout\n"); Check(log.ReadError(path), "Detectar truncamento");
    File.AppendAllText(path, "download successful\n"); Check(!log.ReadError(path), "Não tratar sucesso como erro");
}
finally { File.Delete(path); Directory.Delete(folder); }
Check(LauncherSignals.IsRetry("  TENTE NOVAMENTE...  "), "Reconhecer erro em português");
Check(LauncherSignals.IsRetry("Retry Download"), "Reconhecer retry do download");
Check(LauncherSignals.IsRetry("Tentar\nnovamente"), "Reconhecer quebra de linha");
Check(!LauncherSignals.IsRetry("Cancelar"), "Nunca invocar cancelar");
Check(!LauncherSignals.IsRetry("Comprar agora"), "Nunca invocar compra");
Check(LauncherSignals.IsResume("RETOMAR DOWNLOAD"), "Reconhecer retomada");
Check(!LauncherSignals.ShouldRecover(false, false, false, false, TimeSpan.FromHours(1)), "Não reiniciar Launcher ocioso");
Check(!LauncherSignals.ShouldRecover(false, false, false, true, TimeSpan.FromHours(1)), "Respeitar pausa sem erro");
Check(LauncherSignals.ShouldRecover(false, true, false, false, TimeSpan.Zero), "Retry inicia recuperação imediata sem pasta");
Check(LauncherSignals.ShouldRecover(true, false, false, false, TimeSpan.Zero), "Novo erro de log inicia recuperação sem pasta");
Check(LauncherSignals.ShouldRecover(false, false, true, false, TimeSpan.FromSeconds(25)), "Download sem progresso inicia recuperação");
Check(!LauncherSignals.ShouldRecover(false, false, true, false, TimeSpan.FromSeconds(24)), "Não recuperar antes do limiar");
Console.WriteLine($"{checks} verificações aprovadas.");
