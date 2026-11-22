# Backlog

Este backlog separa preparacao tecnica de features de produto para facilitar tarefas pequenas com Codex.

## Preparacao tecnica

- Concluido: criar a base Personal Hub, renomear projetos e namespaces, migrar para .NET 10, remover telemetria externa e copiar integralmente os bancos para `PersonalHub` e `PersonalHub-dev`; detalhes em `docs/specs/migracao-personal-hub.md`.

### P0

- Concluido: remover credenciais reais de `appsettings.Development.json` e substituir por placeholders seguros.
- Concluido: documentar User Secrets para `ContextConnection`.
- Concluido: migrar a conexao local do MVC para User Secrets e remover connection strings reais dos arquivos `appsettings.*.json` do MVC.
- Concluido: documentar rotina para processos que travam DLL/PDB durante `dotnet build`.

### P1

- Concluido: reorganizar os projetos de codigo em `src` e os testes em `tests`.
- Concluido: migrar o projeto para `net8.0`.
- Concluido: remover pacote vulneravel/dispensavel de RestSharp.
- Concluido: remover pacote de scaffolding que trazia vulnerabilidades transitivas no build normal.
- Concluido: manter o registro do MediatR apontando para handlers de `PersonalHub.Application`.
- Concluido: revisar e zerar warnings de nullable no build.

### P2

- Concluido: ampliar testes de dominio para criacao de parcelas, valores, vencimentos, tags e usuarios envolvidos.
- Concluido: cobrir atualizacao de despesas com testes.
- Concluido: ajustar resposta de exclusao de despesas na camada de aplicacao.
- Concluido: consolidar a query de listagem/filtro de despesas entre MVC e camada de aplicacao.

## Features de produto

### P0

- Concluido: padronizar botões primários pelo estilo do botão Entrar, com superfície transparente, borda e brilho ciano e capitalização de frase consistente com “Cadastrar”.
- Concluido: aplicar o acento do botão Entrar aos controles de filtro, foco de campos, menu superior e ações do assistente.
- Concluido: alinhar confirmações de despesas geradas pelo assistente ao padrão do botão Entrar e restaurar o ciano de interação original no Dark Mode.
- Concluido: aplicar o acento de interação aos checkboxes, alternância de tema e estados ativos do DataTables.
- Concluido: padronizar os destaques cianos em valores, marcadores de status, ordenação de tabelas e relatórios, preservando alerta e erro em amarelo e vermelho.
- Concluido: após aprovação do piloto, expandir o design system visual para navegação, dashboard, relatórios, listagens, formulários, modais, sidebars e assistente.
- Concluido: criar tokens semânticos Light/Dark e aplicar o padrão visual inicial à listagem de despesas como piloto, refinado com zebra, cabeçalho distinto, contraste, busca, DataTables e neutralização das superfícies Bootstrap; piloto aprovado e padrão expandido para as demais telas.
- Concluido: permitir cadastrar despesas recorrentes mensais sem parcelas, com ocorrencias e pagamento independentes por mes; regras em `docs/specs/despesas-recorrentes.md`.
- Concluido: avisar na confirmacao de exclusao de despesa recorrente que todas as ocorrencias mensais, inclusive as pagas, serao removidas e nao havera novos lancamentos.
- Concluido: posicionar todos os Toasts de retorno à esquerda, exibir o retorno do cadastro de despesa no dashboard, restringir o valor da parcela a decimal positivo em PT-BR e adequar o seletor de tema à referência visual.
- Concluido: permitir alterar o vencimento da parcela na edição pelo dashboard e ordenar as colunas da tabela.
- Concluido: adicionar filtro mensal ao dashboard, substituindo o filtro por período de datas.
- Concluido: permitir limpar o filtro mensal do dashboard para exibir despesas de todos os meses.
- Concluido: abrir os rendimentos do período em modal ao clicar no widget de rendimentos do dashboard.
- Concluido: padronizar listagens tabulares com DataTables, pesquisa, ordenacao e paginacao.
- Concluido: corrigir o posicionamento dos indicadores de ordenacao do DataTables nas tabelas da interface.
- Concluido: produzir e aplicar favicon proprio do Financeiro no site com PNG, SVG, ICO fallback e versionamento para evitar cache do navegador.
- Concluido: readequar os widgets do dashboard para reduzir altura do card de despesas e manter espaçamentos uniformes entre os indicadores.
- Concluido: no dashboard, ajustar a busca para manter o foco no campo depois da atualização automática ao parar de digitar, preservando o comportamento atual de recarregar os dados.
- Concluido: no dashboard, ajustar os cards grandes de valores por prioridade: destacar Despesas como card principal maior e reorganizar Rendimentos e Saldo como cards secundarios menores, em linha horizontal ou coluna vertical conforme melhor layout responsivo.
- Concluido: revisar os padroes de verde neon para garantir consistencia em checkboxes e tela de login.
- Concluido: deixar a pagina de login mais profissional mantendo o esquema de cores e adicionando a funcionalidade de lembrar acesso.
- Concluido: no dashboard, trocar a cor do widget Rendimentos para azul.
- Concluido: no dashboard, trocar a cor do widget Saldo para verde.
- Concluido: no dashboard, fazer a cor do valor seguir a cor do marcador de status e padronizar o verde do checkbox com o verde principal usado no menu.
- Concluido: reposicionar o total de despesas pagas em uma linha propria abaixo da tabela e acima dos totalizadores do dashboard.
- Concluido: corrigir acentuação e textos em português nas páginas, menus, botões, filtros, tabelas, alertas e relatórios.
- Concluido: no cadastro de rendimento pelo dashboard, usar sempre o usuário atual e remover a troca de usuário do formulário.
- Concluido: restringir a listagem da página de rendimentos ao usuário autenticado.
- Concluido: aplicar migrations na inicialização e criar os dados padrão ausentes, incluindo usuários e função de Identity.
- Concluido: permitir ajustar a quantidade de parcelas na edição do dashboard, exibir valor total com aparência de campo desabilitado nos modais de despesa e tornar o filtro mensal utilizável em Linux; regras detalhadas em `docs/specs/edicao-parcelas-e-filtro-mensal.md`.
- Concluido: adicionar widget no fim do dashboard exibindo o total de despesas marcadas como pagas, respeitando todos os filtros atuais.
- Concluido: remover scroll interno da tabela de despesas do dashboard, permitindo que a tabela cresca naturalmente e deixando apenas o scroll do navegador na pagina.
- Concluido: garantir que qualquer filtro aplicado no dashboard e qualquer criacao ou edicao de despesas atualize a tabela de despesas imediatamente.
- Concluido: melhorar filtros da pagina de despesas por periodo, tag, usuario e texto.
- Concluido: adicionar filtro por tags no dashboard, com experiencia semelhante ao seletor multiusuario.
- Concluido: adicionar relatorios mensais e comparacao entre meses.
- Concluido: ajustar bordas dos links da navbar somente em active e hover sem sublinhado.
- Concluido: blindar estados active, hover e focus da navbar contra fundo branco.
- Concluido: corrigir controles da navbar para preto/verde e remover transicao cinza no hover.
- Concluido: padronizar botao de tema e hover dos itens da navbar com estado active.
- Concluido: aplicar correcao final de espacamentos e menu do usuario no fim da cascata CSS.
- Concluido: padronizar espacamentos, botao do usuario e hovers de acoes da tabela.
- Concluido: corrigir alinhamento do dashboard, dropdown, item ativo e cores dos status nos dois temas.
- Concluido: corrigir white mode do dashboard preservando o dark mode.
- Concluido: ajustar foco dos filtros, botao atualizar e icones dos widgets do dashboard conforme referencia visual.
- Concluido: corrigir renderizacao dos widgets inferiores do dashboard.
- Concluido: paginar listagens de despesas e rendimentos com DataTables server-side e visual do dashboard.
- Concluido: ajustar posicionamento, modais e widgets do redesign dark premium conforme referencia visual.
- Concluido: corrigir redesign dark premium da pagina principal para aproximar da referencia visual.
- Concluido: redesign dark premium da pagina principal com linguagem visual preto/verde neon/cyan.
- Concluido: melhorar layout do dashboard principal e adicionar alternancia persistente entre light/dark mode.
- Concluido: evoluir dashboard mensal com filtro multiusuario e indicadores praticos.
- Concluido: adicionar cadastro rapido de rendimentos pelo dashboard com atualizacao dos widgets.

### P1

- Concluído: implementar assistente completo de despesas por chat com IA local, incluindo consultas, relatórios, cadastro por texto/imagem e histórico persistente; confirmação individual ou em lote atômica; edição revisável da despesa inteira seguindo o fluxo do dashboard; proteção contra duplicidades em despesas, parcelas e ocorrências recorrentes; e detecção de propostas desatualizadas. Especificação em `docs/specs/chat-consulta-despesas.md`.

### P2

- Promovido para P0: evoluir dashboard mensal com totais, despesas em aberto, pagas e vencidas.
- Concluido: adicionar fluxo mais claro para marcar parcelas como pagas no dashboard.

### P3

- Concluído: criar testes unitários e de integração para consulta mensal, rateio, isolamento de usuários, status em aberto, recorrência, confirmação em lote e falha do Ollama; validar os fluxos principais no navegador Orca.
- Concluído: corrigir o espaçamento dos ícones de usuário e senha na tela de login, sobrescrito pelo estilo global dos campos.
- Melhorar recorrencia de despesas fixas.

## Como escolher a proxima tarefa

Escolha uma tarefa pequena, com criterio de aceite claro e validacao objetiva. Para novas features, preencha `docs/feature-template.md` antes de pedir implementacao.

## Manutencao do backlog

Sempre que uma tarefa for planejada, iniciada ou concluida, atualize este arquivo na mesma entrega. Itens entregues devem ficar marcados como `Concluido`; itens ainda nao implementados devem permanecer como pendentes na prioridade correta.

Regra permanente: toda alteracao visual deve ser verificada em light mode e dark mode antes da entrega, incluindo contraste, estados de foco, modais, menus, tabelas e responsividade.
