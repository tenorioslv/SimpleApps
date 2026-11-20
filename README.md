# SimpleApps

Apps simples.

Repositório de aplicativos simples, organizados em pastas independentes e documentados em português.

## Aplicativos

- [Rockstar Download Guardian Pro](rockstar-download-guardian-pro/) — Aplicativo Windows 11 em C# / .NET 8 WPF que espera avisos de erro do Rockstar Launcher e tenta acionar Tentar novamente automaticamente. Inicia sem configuração, permanece na bandeja e não mede o download.

## Organização

Cada aplicativo terá sua própria subpasta na raiz, com nome em letras minúsculas e sem espaços, como `meu-app`, e um `README.md` com instruções de uso e descrição dos dados processados.

As regras permanentes de trabalho estão em [AGENTS.md](AGENTS.md).

## Privacidade e segurança

Por padrão, os apps funcionarão localmente no navegador ou dispositivo do usuário, sem envio de dados pessoais a servidores externos e sem rastreamento oculto. Quando solicitarem dados, deverão apresentar aviso de privacidade em português, consentimento explícito desmarcado por padrão e opção para apagar os dados.

O `.gitignore` bloqueia padrões comuns de segredos, bancos locais e pastas privadas. Todo envio deve ser revisado: esse arquivo não detecta todo dado sensível nem impede o envio de arquivos já rastreados.
