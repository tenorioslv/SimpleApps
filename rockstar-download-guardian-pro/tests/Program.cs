using RockstarGuardian.Services;
int checks = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
Check(LauncherSignals.IsRetry("  TENTE NOVAMENTE...  "), "Reconhecer aviso em português");
Check(LauncherSignals.IsRetry("Retry Download"), "Reconhecer retry do download");
Check(LauncherSignals.IsRetry("Tentar\nnovamente"), "Reconhecer quebra de linha");
Check(!LauncherSignals.IsRetry("Cancelar"), "Nunca invocar cancelar");
Check(!LauncherSignals.IsRetry("Comprar agora"), "Nunca invocar compra");
Check(LauncherSignals.IsResume("RETOMAR DOWNLOAD"), "Reconhecer retomada");
Check(RetryPolicy.Choose(false, false, false) == RetryAction.None, "Sem aviso: não agir");
Check(RetryPolicy.Choose(true, false, false) == RetryAction.Retry, "Agir quando Retry aparecer");
Check(RetryPolicy.Choose(false, true, false) == RetryAction.None, "Não retomar pausa voluntária sem erro");
Check(RetryPolicy.Choose(false, true, true) == RetryAction.Resume, "Retomar no contexto de erro");
Check(RetryPolicy.Choose(false, false, true) == RetryAction.None, "Erro sem controle não produz clique cego");
Check(RetryPolicy.Choose(true, true, true) == RetryAction.Retry, "Priorizar nova tentativa");
Check(!RetryPolicy.CanInvoke(TimeSpan.FromSeconds(14.99)), "Evitar spam de cliques");
Check(RetryPolicy.CanInvoke(TimeSpan.FromSeconds(15)), "Permitir nova tentativa após intervalo");
DateTime now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
Check(!RetryPolicy.HasRecentError(now, null), "Nenhum erro anterior");
Check(RetryPolicy.HasRecentError(now, now.AddSeconds(-60)), "Contexto válido na fronteira");
Check(!RetryPolicy.HasRecentError(now, now.AddSeconds(-61)), "Não reutilizar erro antigo");
Check(!RetryPolicy.HasRecentError(now, now.AddSeconds(1)), "Não aceitar horário futuro");
string folder = Path.Combine(AppContext.BaseDirectory, "fixtures");
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
