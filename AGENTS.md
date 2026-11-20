# Regras permanentes do SimpleApps

Estas instruções são obrigatórias para todas as sessões e agentes que trabalharem neste repositório e se aplicam à raiz e a todas as subpastas. Leia este arquivo antes de iniciar qualquer tarefa. Preserve estas regras nas próximas alterações.

## 1. Organização e envio automático

- Trabalhe exclusivamente neste repositório SimpleApps. Nunca crie, altere ou exclua arquivos fora da pasta SimpleApps.
- Sempre que o usuário pedir um novo app, crie uma subpasta diretamente na raiz com o nome do app em letras minúsculas, sem espaços, usando hífens quando necessário (exemplo: `meu-app`).
- Salve todos os arquivos específicos do app exclusivamente nessa subpasta.
- Crie um `README.md` dentro da pasta de cada app, em português, explicando o que ele faz, como usar e quais dados processa, se houver.
- Atualize o `README.md` principal na raiz, acrescentando o nome e uma breve descrição do novo app.
- Ao finalizar o trabalho, execute `git add`, `git commit` e `git push` apenas dentro deste repositório. Adicione apenas os arquivos pertinentes à tarefa, revisados e sem dados sensíveis.
- Antes de qualquer envio, confirme a raiz do repositório e que o destino remoto corresponde ao SimpleApps autorizado pelo usuário. Não envie para outros repositórios, não use push forçado e não sobrescreva alterações alheias.
- Se autenticação, acesso ao remoto ou sincronização impedirem o envio, preserve o trabalho local e informe claramente a pendência; nunca afirme que sincronizou sem verificar.

## 2. Proteção de dados e LGPD — Lei nº 13.709/2018

- **Minimização de dados:** por padrão, todos os apps devem funcionar localmente no navegador ou dispositivo do próprio usuário, usando memória temporária ou `localStorage`. Não envie nem salve dados pessoais em servidores externos, salvo solicitação explícita do usuário.
- Prefira memória temporária quando persistência não for necessária. Armazene apenas o mínimo necessário e explique qualquer persistência local.
- **Sem rastreamento oculto:** nunca adicione cookies de rastreamento, pixels de terceiros, analytics invasivos ou coleta silenciosa de IP, localização, e-mail, CPF, telefone ou nome.
- Sempre que um app solicitar qualquer dado do usuário, mesmo apenas nome ou e-mail, inclua obrigatoriamente na interface:
  1. Aviso claro e visível em português explicando para que o dado será usado, onde será processado e se será armazenado.
  2. Caixa de consentimento explícito (`checkbox`) desmarcada por padrão, exigida antes do envio ou processamento dos dados solicitados.
  3. Opção fácil e acessível para o usuário apagar seus próprios dados a qualquer momento, incluindo os dados persistidos localmente pelo app.
- Não trate a mera existência de um checkbox como garantia integral de conformidade. Revise o fluxo efetivo de dados e documente limitações relevantes.
- **Dados de teste:** nunca use nomes, CPFs, e-mails ou telefones reais como exemplos no código, testes, documentação ou banco de dados. Use apenas dados fictícios genéricos, como `usuario@exemplo.com`, sem copiar dados pessoais de fontes reais.

## 3. Segurança técnica e transparência

- Crie e mantenha o `.gitignore` da raiz bloqueando arquivos sensíveis: `.env`, chaves de API, senhas, bancos de dados locais e arquivos pessoais. Acrescente padrões específicos quando necessário.
- Nunca inclua segredos, credenciais ou dados pessoais reais no código ou em commits. O `.gitignore` não protege arquivos já rastreados: revise também o diff e os arquivos preparados para commit.
- Use apenas código original e bibliotecas open-source de uso livre, verificando e respeitando suas licenças e atribuições.
- Nunca altere arquivos fora da pasta SimpleApps, inclusive configurações globais do Git ou de outras ferramentas.
- Antes de **qualquer `git push`**, mostre na conversa um checklist rápido em português confirmando:
  - Quais arquivos foram criados e quais foram alterados.
  - Se o app coleta ou processa dados pessoais e como atende às proteções de LGPD definidas aqui; se não houver app nesta tarefa, declare isso.
  - Que os arquivos a enviar foram revisados e não contêm senhas, chaves, segredos ou dados pessoais reais usados como conteúdo ou exemplos.
- Faça verificações proporcionais à tarefa e informe o resultado e eventuais pendências com transparência.
