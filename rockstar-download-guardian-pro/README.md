# Rockstar Download Guardian Pro

Aplicativo desktop original para Windows 11 x64 em C# / .NET 8 e WPF, com bandeja do sistema via Windows Forms. Observa a instalação do GTA V, gravações do processo Launcher e novas linhas do launcher.log. Tenta recuperar interrupções sem mover o mouse. Não é afiliado à Rockstar e não garante recuperação de problemas de servidor, autenticação ou arquivos corrompidos.

## Executar e usar

1. Execute o arquivo publicado `publish/Rockstar Download Guardian Pro.exe` ou use o atalho na Área de Trabalho.
2. Revise a pasta específica do jogo, o Launcher.exe oficial e o launcher.log. A detecção verifica Registro 32/64 bits e caminhos padrão. Instalações Steam/Epic podem exigir seleção manual.
3. Configure o alvo (padrão 115 GB decimais; ajuste à edição instalada). Leia o aviso local e marque o consentimento, desmarcado em toda abertura.
4. Clique em **Iniciar monitoramento**. Fechar ou minimizar recolhe à bandeja. O menu permite abrir, pausar/retomar, reiniciar e sair. Pause também quando pausar intencionalmente o download no Launcher.
5. O desligamento automático é opcional, desativado inicialmente. Ao atingir o alvo após observar atividade e manter 60 segundos sem mudanças, o app mostra uma contagem adicional de 60 segundos com cancelamento. Pausar, apagar dados ou sair cancela a contagem. A conclusão por tamanho é uma heurística: pré-alocação pode gerar falso positivo. O app não confirma integridade nem licença do GTA.

## Recuperação

- Erro novo no log ou 25 segundos sem E/S/crescimento: procura botões Retry, Resume, Tentar novamente ou Continuar por UI Automation. Não movimenta cursor, não ativa a janela e não garante suporte se o Launcher não expuser acessibilidade, se estiver elevado ou se o botão não existir.
- Após mais 15 segundos sem retomada: executa `ipconfig /flushdns` sem console e tenta o botão de novo. Isso limpa o cache DNS, não altera rotas. Pode exigir privilégios.
- Após mais 20 segundos: encerra apenas processos Launcher/LauncherPatcher da pasta escolhida e SocialClubHelper dessa instalação/pasta Social Club irmã; aguarda cinco segundos, remove somente .lock/.part de zero bytes na pasta do jogo e reabre minimizado (o Launcher pode ignorar a solicitação de minimizar).
- Máximo de três reinícios por sessão e intervalo de dois minutos após cada reinício. Erros de acesso interrompem o monitoramento em vez de simular velocidade zero. Junctions e links não são percorridos. Não execute como administrador a menos que necessário para acessar um Launcher já elevado.

## Métricas e limitações

Amostras sequenciais, sem sobreposição, a cada cinco segundos após o processamento. O total é a soma dos tamanhos lógicos dos arquivos da pasta. E/S em MB/s usa o delta de `GetProcessIoCounters.WriteTransferCount` do Launcher selecionado dividido pelo tempo decorrido real: inclui outras gravações desse processo e não equivale a uma medição exclusiva do disco ou da rede do GTA. Não atribui E/S de processos filhos ao jogo. A ETA usa média móvel de até 12 amostras. A indicação de rede mostra disponibilidade de interface, não conectividade com servidores Rockstar. A varredura pode demorar em discos lentos/pastas grandes. A leitura de log consulta somente conteúdo novo, limita cada leitura a 64 KiB e reinicia ao detectar rotação/truncamento.

## Privacidade e dados

Sem servidores, contas, telemetria, analytics, cookies ou bibliotecas de terceiros. Processa caminhos locais (podem conter nome do perfil Windows), tamanhos de arquivos, contadores do Launcher, disponibilidade da rede e trechos do log em memória. Nunca persiste o conteúdo do log. Configurações ficam em `data/settings.json` ao lado do executável, protegido do Git pelo `.gitignore`. O botão **Apagar meus dados** interrompe ações, remove configurações e limpa histórico/campos da sessão; não apaga jogo, logs originais ou metadados de sistema operacional. Não há criptografia do JSON: use a pasta de usuário com permissões adequadas. O próprio Launcher pode acessar serviços externos independentemente deste app.

## Compilar um executável único

Instale SDK .NET 8 para Windows e execute nesta pasta:

```powershell
dotnet publish RockstarGuardian.csproj -c Release -r win-x64 --self-contained true -o publish
```

WPF, Windows Forms e UIAutomation são referências do framework Microsoft, sem pacotes externos. O executável contém o runtime; bibliotecas nativas podem ser extraídas pelo .NET ao executar. Código fonte em App/MainWindow, modelos em Models e serviços separados em Services. Configurações, SDK local, objetos e binários gerados não são versionados.

`build.ps1` executa os testes e publica o executável. `create-desktop-shortcut.ps1` cria o atalho na Área de Trabalho quando solicitado. Os testes em `tests/` verificam os limiares da cascata, as condições de conclusão e a leitura incremental/truncamento de logs; não encerram o Launcher, limpam DNS nem desligam o computador.

Referências técnicas: [GetProcessIoCounters](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getprocessiocounters), [SetThreadExecutionState](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate) e [SDK local do .NET](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script).
