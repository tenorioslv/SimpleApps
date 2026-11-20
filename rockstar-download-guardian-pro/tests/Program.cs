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
Console.WriteLine($"{checks} verificações aprovadas.");
