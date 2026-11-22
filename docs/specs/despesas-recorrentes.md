# Especificacao: despesas recorrentes mensais

## Objetivo

Permitir cadastrar uma despesa que se repete mensalmente na data definida, sem quantidade de parcelas.

## Usuario e permissoes

Usuario autenticado que cadastra e acompanha despesas no MVC. O dashboard respeita os filtros existentes de usuarios, pesquisa, mes e tags.

## Fluxo esperado

1. O usuario escolhe o tipo "Recorrente mensal" no cadastro.
2. Informa titulo, valor mensal, vencimento inicial, usuarios e tags.
3. A despesa aparece no dashboard no vencimento de cada mes, com estado de pagamento proprio para cada ocorrencia.
4. O usuario pode marcar cada ocorrencia mensal como paga ou nao paga.
5. O usuario pode editar a definicao da recorrencia ou remove-la; remove-la interrompe novas ocorrencias.

## Regras confirmadas pelo usuario

- A despesa se repete mensalmente na data informada e nao tem parcelas.
- A recorrencia continua por tempo indeterminado ate a definicao ser removida.
- Cada mes possui uma ocorrencia e estado de pagamento independente.
- A ocorrencia e criada na primeira consulta ao dashboard ou aos relatorios em que a data de vencimento daquele mes ja chegou. Se o app nao for acessado no vencimento, ocorrencias vencidas pendentes sao criadas na consulta seguinte.

## Pressupostos adotados

- O vencimento mensal usa o dia da data inicial; se um mes nao tiver esse dia, usa-se o ultimo dia do mes.
- O valor e a data de uma ocorrencia ficam registrados quando ela e criada. Editar o valor afeta ocorrencias ainda nao criadas, inclusive meses vencidos que ainda nao foram materializados. Editar o vencimento muda o inicio e o dia da serie; meses anteriores sem ocorrencia deixam de ser materializados a partir da nova data. Nome, descricao, usuarios e tags continuam vinculados a definicao.
- Os filtros e totalizadores mensais do dashboard e dos relatorios incluem ocorrencias recorrentes.
- A exclusao da definicao remove suas ocorrencias, seguindo o comportamento existente de exclusao de despesas e parcelas.

## Persistencia e compatibilidade

- Despesas existentes continuam nao recorrentes.
- A definicao recorrente e armazenada em `Despesas` com um indicador e sem linhas em `Parcelas`.
- Uma tabela de ocorrencias armazena mes, vencimento, valor e estado pago de cada mes. Uma chave unica por despesa e mes impede duplicidade.
- A migration e aplicada pela rotina de inicializacao existente.

## Criterios de aceite

- Given um cadastro recorrente valido, when o usuario salva, then o sistema guarda uma definicao mensal sem parcelas e apresenta o valor como mensal.
- Given uma recorrencia cujo vencimento do mes ja ocorreu, when o dashboard e consultado, then existe uma unica ocorrencia no mes com o valor e a data esperados.
- Given uma recorrencia cujo vencimento do mes ainda nao chegou, when o dashboard e consultado, then a ocorrencia ainda nao aparece.
- Given o dia de vencimento maior que os dias de um mes, when a ocorrencia desse mes e criada, then o vencimento usa o ultimo dia do mes.
- Given uma ocorrencia recorrente, when o usuario marca ou desmarca como paga, then somente o estado daquele mes e alterado.
- Given uma recorrencia com meses vencidos ainda nao materializados desde o vencimento inicial atual, when o dashboard ou os relatorios sao consultados, then todas as ocorrencias vencidas desse periodo sao recuperadas sem duplicar as existentes.
- Given um dashboard ou relatorio mensal filtrado, when ha ocorrencias recorrentes no periodo, then elas entram na listagem e nos totais respeitando os filtros existentes.
- Given uma despesa recorrente, when o usuario consulta a listagem ou os detalhes, then o sistema a identifica como mensal e nao mostra parcelas.
- Given uma despesa recorrente, when o usuario solicita sua remocao no dashboard ou na listagem, then a confirmacao avisa que todas as ocorrencias, inclusive as pagas, serao excluidas e que nao havera novos lancamentos.

## Fora de escopo

- Criacao de um agendador ou servico em segundo plano.
- Recorrencia semanal, anual ou com data final.
- Alteracao das regras de parcelas de despesas comuns.

## Riscos e pressupostos

- A materializacao depende de uma consulta ao dashboard ou aos relatorios; vencimentos passados sao recuperados quando uma dessas paginas for aberta novamente.
- Alterar o vencimento reposiciona o inicio e o dia da serie. Ocorrencias ja criadas preservam suas datas e valores.
