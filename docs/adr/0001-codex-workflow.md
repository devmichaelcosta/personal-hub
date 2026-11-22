# ADR 0001: Fluxo De Trabalho Com Codex

## Status

Aceito.

## Contexto

O projeto vai receber melhorias e novas features com apoio do Codex. Para reduzir custo de contexto, evitar retrabalho e preservar alteracoes locais, o repositorio precisa de um fluxo simples e repetivel.

## Decisao

Trabalhar em ciclos pequenos:

1. Planejar a mudanca com objetivo, escopo e criterios de aceite.
2. Verificar `git status --short`.
3. Localizar codigo com `rg`.
4. Implementar somente o necessario para a tarefa.
5. Rodar testes ou explicar por que nao foi possivel.
6. Revisar `git diff`.
7. Atualizar documentacao quando a mudanca alterar fluxo, arquitetura ou setup.

## Consequencias

- O Codex deve gastar menos tokens redescobrindo a arquitetura.
- Mudancas ficam menores e mais faceis de revisar.
- Alteracoes locais do usuario ficam protegidas.
- Features maiores devem ser quebradas em etapas.
