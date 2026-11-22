# Redesign visual: design system e piloto de despesas

## Objetivo

Definir uma base visual compacta, consistente e acessível, inspirada nos princípios funcionais do Linear, preservando a identidade verde do Financeiro. A primeira entrega aplica essa base somente à listagem de despesas para revisão antes de expandi-la.

## Comportamento confirmado

- A interface usa ASP.NET Core MVC/Razor, Bootstrap 5, jQuery e DataTables.
- O tema `dark` ou `light` é persistido em `localStorage` (`personalhub.theme`) e aplicado antes da renderização do conteúdo.
- A listagem de despesas usa DataTables com paginação e processamento server-side, busca, ordenação, filtros de período, usuários e tags.
- As rotas, endpoints, regras financeiras e contratos existentes permanecem inalterados.
- A cor principal efetiva é verde: `#00FF88` no tema escuro e `#007F50` no tema claro (o bloco Light tardio prevalece sobre o token inicial). Ciano é uma cor secundária/informativa.

## Escopo

- Adicionar tokens semânticos Light/Dark ao CSS global existente, sem migrar nesta etapa os componentes que não participam do piloto.
- Aplicar o novo tratamento visual à listagem de despesas, incluindo seu cabeçalho, navegação atual, filtros, menus de seleção, tabela e controles DataTables.
- Manter a navegação horizontal existente. Avaliar uma eventual migração de navegação em etapa posterior, depois da aprovação do piloto.
- Preservar seletor e armazenamento de tema, markup funcional, filtros, paginação, ordenação, busca e ações de linha.
- Manter a tabela como tabela. Em telas estreitas, permitir rolagem horizontal com largura mínima adequada.

## Fora de escopo

- Redesign das páginas de dashboard, rendimentos, relatórios, cadastro, login, modais e demais telas.
- Mudanças em domínio, API, persistência, autenticação, rotas ou regras de negócio.
- Novas bibliotecas, virtualização ou alteração do mecanismo DataTables.
- Opção de seguir o tema do sistema operacional, que ainda não existe na aplicação.
- Conversão da navegação horizontal para sidebar nesta entrega.

## Proposta de tokens

Os nomes abaixo ficam centralizados em `site.css` e serão usados pelo piloto. Os valores podem variar semanticamente entre temas para manter contraste, mantendo `--ds-brand` ancorado nos verdes existentes.

| Token | Light | Dark |
| --- | --- | --- |
| background | `#F4F6F4` | `#0B0B0B` |
| foreground | `#1D2921` | `#E5E9E6` |
| surface | `#FBFCFA` | `#111111` |
| surface-elevated | `#FFFFFF` | `#191919` |
| surface-hover | `#EDF2EE` | `#222222` |
| border / border-subtle | `#87948A` / `#E5EAE6` | `#686868` / `#252525` |
| brand | `#007F50` | `#00FF88` |
| primary / primary-hover | `#006D45` / `#005B3A` | `#00D66F` / `#35FFA2` |
| primary-foreground | `#FFFFFF` | `#06110B` |
| muted / muted-foreground | `#EEF1EE` / `#66736B` | `#191919` / `#9A9A9A` |
| accent | `#EDF4EF` | `#202020` |
| table-header | `#E5EAE6` | `#242424` |
| table-row / table-row-alt | `#FBFCFA` / `#F3F5F3` | `#111111` / `#181818` |
| table-row-hover | `#E9F0EB` | `#2A2A2A` |
| focus-ring | `#006D45` | `#00FF88` |
| destructive | `#B93849` | `#F07380` |
| success | `#0B7950` | `#48CC91` |
| warning | `#936300` | `#E6B857` |
| info | `#006B66` | `#59CCC2` |

## Escalas do piloto

- Tipografia: sistema sem nova fonte, base 14px, metadados 12px e título de página 24px.
- Espaçamento: incrementos de 4px (4, 8, 12, 16, 24, 32px).
- Controles: 36px de altura, mantendo áreas interativas confortáveis.
- Bordas: 1px, contraste sutil e raio de 6px; superfícies principais até 8px.
- Elevação: sem sombra em elementos de página; menus podem usar sombra discreta.
- Interações: 120–160ms; respeitar `prefers-reduced-motion`.
- Refinamento após feedback: superfícies Dark em carvão neutro, verde restrito a ações/seleção, tabela zebra com cabeçalho distinto nos dois temas, busca com recuo próprio para o ícone, controles do DataTables com tipografia normalizada e neutralização das superfícies herdadas da tabela Bootstrap.

## Expansão global

Após a aprovação da listagem piloto, o layout compartilhado foi aplicado globalmente pelo MVC. Os tokens semânticos agora alimentam navegação, dashboard, relatórios, listagens, formulários, tabelas, modais, sidebars e assistente, sem alteração de rotas ou regras de negócio.

## Fluxos e cenários de aceite

### Listar e filtrar despesas

Given que a pessoa autenticada acessa a lista de despesas em qualquer tema suportado  
When busca, altera datas, usuários ou tags  
Then os filtros continuam recarregando a listagem server-side e os controles apresentam estados legíveis de foco, hover e seleção.

### Usar a tabela

Given que a tabela contém resultados  
When a pessoa ordena colunas, navega pelas páginas ou aciona uma ação da linha  
Then DataTables mantém ordenação, paginação e links existentes, com cabeçalho e linhas compactos e hover perceptível.

### Usar tela estreita

Given que a largura disponível não comporta todas as colunas  
When a pessoa consulta a listagem em viewport estreita  
Then os filtros se reorganizam e a tabela permanece tabular com rolagem horizontal, sem ocultar ações.

### Preservar Light/Dark

Given uma preferência Light ou Dark já salva  
When a página carrega ou a pessoa alterna o tema  
Then a preferência e o seletor continuam funcionando e as superfícies, textos, bordas, foco e cores de estado permanecem legíveis.

## Riscos e validação

- `site.css` acumulou overrides legados. O novo padrão compartilhado fica sob `body.app-design-system`, enquanto os refinamentos exclusivos do piloto permanecem prefixados por `#design-system-pilot`.
- O tema do sistema operacional não é atualmente suportado; será mantida a preferência explícita existente.
- A largura mínima da tabela continua exigindo rolagem horizontal em telas pequenas, preservando todas as colunas e ações.
- Validação da expansão: build MVC concluído sem erros (com avisos preexistentes de nomes de migrações), `git diff --check` limpo e revisão dos estados responsivos/reduced-motion no CSS. No navegador Orca, corpo, títulos e controles foram conferidos com a fonte system-ui do Bootstrap; a sessão disponível está na tela de login, então listagens autenticadas e seus fluxos precisam de conferência visual final.

## Aprovação e expansão

O piloto da listagem de despesas foi aprovado pela pessoa solicitante. Em seguida, o padrão foi expandido pelo layout compartilhado para navegação, dashboard, relatórios, listas, formulários, modais, sidebars e assistente. A expansão preserva os fluxos existentes e não altera contratos de API ou regras de negócio.
