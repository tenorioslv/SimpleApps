# Rockstar Download Guardian Pro

Aplicativo original para Windows 11 x64, C# / .NET 8 e WPF. Basta executá-lo: a observação e a recuperação do Rockstar Games Launcher começam automaticamente, sem selecionar a pasta do GTA V, preencher formulários ou clicar em Iniciar.

## Uso automático

- Abra o atalho **Rockstar Download Guardian Pro** na Área de Trabalho.
- O app detecta o Launcher em execução, o Registro e os caminhos padrão. Observa também janelas pertencentes ao SocialClubHelper e LauncherPatcher da mesma instalação.
- Procura os logs em Documentos, inclusive Documentos redirecionados pelo Windows/OneDrive. Se o Launcher ainda não estiver aberto ou acessível, repete a detecção em segundo plano.
- Ao encontrar um botão reconhecido de nova tentativa, envia o comando de retomada por UI Automation ou pela acessibilidade MSAA, sem mover o mouse ou pedir confirmação. Verifica novamente a cada ciclo, limitando a frequência de comandos.
- Fechar ou minimizar recolhe à bandeja. Abrir o atalho novamente traz a mesma instância de volta. O menu da bandeja permite abrir, pausar/retomar, solicitar reinício manual ou sair.

Nenhuma configuração é obrigatória. A pasta do GTA é opcional e serve apenas para estimar tamanho, ETA, conclusão e permitir limpeza de arquivos vazios no reinício. Sem pasta válida, a recuperação continua pelos processos, interface e logs; o app não inventa porcentagem, ETA ou conclusão e não agenda desligamento por tamanho.

## Como a recuperação decide agir

1. Um controle reconhecido de **Tentar novamente / Tente novamente / Retry / Try again / Retry download** ou um erro novo no log inicia a recuperação. Um download que a interface indica estar ativo também pode iniciar recuperação após 25 segundos sem atividade. Launcher ocioso ou pausado sem erro não é motivo suficiente para reiniciar.
2. Procura **Continuar / Resume / Retomar download** apenas no contexto de erro ou recuperação. Uma pausa explícita sem erro não é retomada automaticamente.
3. Após 15 segundos sem melhora, tenta `ipconfig /flushdns` sem console e procura o controle de retomada novamente. A limpeza afeta o cache DNS, não rotas; se falhar por permissões, registra a limitação e continua.
4. Após mais 20 segundos sem melhora, reinicia apenas processos da instalação Rockstar detectada. Aguarda cinco segundos e reabre o Launcher minimizado, se o próprio Launcher aceitar esse estado. Sem uma pasta de jogo válida, não remove arquivo algum. Com pasta válida, só remove .lock/.part de zero bytes, sem percorrer links ou junctions.
5. Há intervalo de dois minutos após reinício. Após três reinícios, suspende novos reinícios por 30 minutos; a observação e as tentativas de acionar Retry continuam. A proteção contra suspensão só é solicitada quando há download identificado ou recuperação ativa.

Uma recuperação só gera notificação de retomada quando há sinais posteriores de atividade de download; o envio de um clique sozinho não prova que o download voltou. Logs e interface inacessíveis não são tratados como prova de download travado.

## Preferências opcionais

O painel mostra um resumo do modo automático e uma seção recolhida de preferências. Pause para editar e salvar caminhos substitutos, tamanho alvo ou desligamento automático. O salvamento exige aviso de privacidade e checkbox desmarcado por padrão. Depois, retome a observação. Abrir o programa nas próximas vezes volta a iniciar a observação automaticamente.

O desligamento é desativado por padrão. Quando habilitado com pasta válida, exige atividade de download observada, tamanho alvo atingido e 60 segundos de estabilidade, seguidos de uma contagem adicional de 60 segundos para cancelar. Pausar, apagar dados ou sair cancela essa contagem. Tamanho lógico não confirma integridade: arquivos pré-alocados podem causar conclusão antecipada. Confirme a edição e o tamanho do jogo antes de habilitar essa opção.

## Privacidade

A observação automática processa caminhos, identificadores dos processos Rockstar, E/S, rótulos acessíveis de controles e trechos novos dos logs apenas em memória. Não pede nome, e-mail ou outros dados ao usuário. Não tem servidor, telemetria, analytics ou rastreamento e não grava conteúdo dos logs. O Launcher pode acessar serviços externos por conta própria.

Somente ao optar por **Salvar preferências opcionais**, caminhos e preferências são gravados em `data/settings.json` ao lado do executável, após consentimento. Caminhos podem conter o nome do perfil Windows. O JSON não é criptografado; use as permissões da conta Windows. **Apagar meus dados** pausa a observação, remove o JSON e limpa a sessão; não apaga jogo, logs da Rockstar nem metadados do sistema. Nova execução do app inicia novamente a observação em memória.

## Limites técnicos

O app não é afiliado à Rockstar e não garante resolver falhas de servidor, autenticação ou corrupção. UI Automation e o mecanismo alternativo MSAA dependem dos controles que o Launcher expõe, inclusive quando minimizado. Controles sem nome/padrão de ação ou um Launcher elevado podem impedir o comando automático. Nesses casos, o app registra o estado e usa os outros sinais disponíveis; não faz cliques cegos nem tenta burlar autenticação.

A taxa em MB/s é a E/S agregada de gravação dos processos Rockstar selecionados, via GetProcessIoCounters; não é uma medição exclusiva de disco nem de tráfego do GTA. A média móvel usa até 12 amostras. O indicador de rede mostra interface disponível, não garante conexão com servidores Rockstar. A leitura ocorre em ciclos de cinco segundos após o processamento, com até três segundos por mecanismo para consulta da interface e leitura de log limitada a 64 KiB. Provedores bloqueados não geram consultas simultâneas ilimitadas.

## Compilar e testar

Com SDK .NET 8 para Windows:

```powershell
.\build.ps1
```

O script executa os testes e publica `publish/Rockstar Download Guardian Pro.exe` como executável único win-x64 com runtime embutido. O SDK local, se disponível em `.tools/dotnet`, e os caches ficam dentro da pasta do app. Caminhos longos são encurtados por um mapeamento de unidade temporário removido ao final. Não há pacotes externos ao framework Microsoft. `create-desktop-shortcut.ps1` recria o atalho quando solicitado.

Os testes verificam tempos de escalonamento, condições de conclusão, logs incrementais/truncados e reconhecimento de sinais/decisão de recuperação sem pasta. Não limpam DNS, encerram o Launcher nem desligam o computador.

Referências: [InvokePattern](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.invokepattern), [E/S por processo](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getprocessiocounters) e [SetThreadExecutionState](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate).
