# Rockstar Download Guardian Pro

Interface escura com painel simplificado e ícone original de escudo, aplicado ao executável, janela e bandeja. `Assets/generate-icon.ps1` recria o ícone em seis tamanhos, sem dependências externas.

Aplicativo Windows 11 x64 em C# / .NET 8 e WPF focado em esperar o erro do Rockstar Games Launcher e tentar acionar **Tentar novamente / Retry** automaticamente quando esse controle aparecer.

## Uso

Abra o atalho **Rockstar Download Guardian Pro**. A proteção começa automaticamente, sem pasta do GTA, formulários ou botão de iniciar. Fechar ou minimizar recolhe à bandeja; o atalho reabre a mesma instância. O menu da bandeja permite abrir, pausar/retomar ou sair.

O painel mostra apenas o estado da proteção, o número de comandos enviados nesta sessão e o histórico em memória. Verde indica observação ativa, amarelo indica erro/comando de nova tentativa, cinza indica espera/pausa e vermelho indica falha inesperada na proteção. Nenhuma dessas cores representa velocidade de download.

## Funcionamento

- Detecta o Launcher pelos processos, Registro e caminho padrão. Observa controles do Launcher, LauncherPatcher e SocialClubHelper da mesma instalação.
- Procura botões e links com rótulos reconhecidos como Tentar novamente, Tente novamente, Retry e Try again. Não presume que o botão exista antes de um erro.
- Usa UI Automation e, como alternativa, acessibilidade MSAA para solicitar a ação padrão do controle. Não move o cursor, não usa cliques cegos nem confirma compras, login ou outros botões.
- Respeita um intervalo mínimo de 15 segundos entre comandos, inclusive entre mecanismos. Se o erro persistir e o controle continuar disponível, tenta novamente.
- Lê somente trechos novos do launcher.log em Documentos, incluindo Documentos redirecionados, para reconhecer contexto de falha. Continuar/Resume só pode ser acionado após um erro recente, com contexto limitado a 60 segundos. Uma pausa sem erro não é retomada.
- Verifica novamente após cada ciclo de dois segundos, além do tempo de consulta de acessibilidade. Cada mecanismo tem espera de até três segundos e não cria consultas concorrentes ilimitadas quando um provedor bloqueia.

Não mede MB/s, tamanho, porcentagem, disco ou tráfego de download. Não procura arquivos do GTA, calcula ETA, detecta conclusão, desliga o PC, impede suspensão, limpa DNS, reinicia processos ou remove arquivos do jogo.

O botão pode aparecer apenas quando ocorrer o erro; a ausência dele durante uma verificação é normal. Um comando enviado não comprova que a conexão foi recuperada. O aplicativo não é afiliado à Rockstar. A ação depende de o Launcher disponibilizar o controle via acessibilidade; controles sem nome/ação, sessão bloqueada ou diferença de privilégios podem impedir o acionamento. Não há garantia de resolver falhas do servidor ou da conta.

## Privacidade

Processa apenas caminhos operacionais do Launcher/log, identificadores dos processos e rótulos dos controles, além de trechos novos do log em memória. Não solicita dados pessoais, envia dados a servidores, grava logs da sessão ou salva configurações. Caminhos podem conter o nome do perfil Windows, mas não são persistidos. O Launcher pode acessar seus próprios serviços externos independentemente deste app.

**Apagar meus dados** pausa a proteção, limpa o histórico/contador em memória e remove somente `data/settings.json` e seu temporário de versões antigas do Guardian, caso existam. Não lê essas configurações na inicialização nem toca nos dados do GTA ou nos logs originais da Rockstar. Retome a proteção quando desejar.

## Compilar e verificar

Com SDK .NET 8 para Windows, execute `./build.ps1`. O script usa o SDK local em `.tools/dotnet` se disponível, executa os testes e publica `publish/Rockstar Download Guardian Pro.exe`, executável único win-x64 com runtime embutido. Há somente referências do framework Microsoft. SDK, caches e binários não são versionados.

Os testes de lógica validam reconhecimento dos rótulos, intervalo entre comandos, contexto de erro e leitura incremental do log. O teste WPF em `tests/ui-smoke` abre uma janela fictícia: primeiro sem botão, depois com TENTE NOVAMENTE, e verifica o acionamento real por acessibilidade, o intervalo e a retomada contextual. Ele filtra pelo caminho de seu próprio executável e não age sobre o Launcher instalado. Não provoca erros reais de conexão.

`create-desktop-shortcut.ps1` recria o atalho quando solicitado. Referências: [InvokePattern](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.invokepattern) e [IAccessible.accDoDefaultAction](https://learn.microsoft.com/en-us/dotnet/api/accessibility.iaccessible.accdodefaultaction).
