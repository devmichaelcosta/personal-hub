# Especificacao: Edicao de parcelas e filtro mensal

## Objetivo

Permitir ajustar a quantidade de parcelas pela edicao aberta no dashboard, tornar evidente que o valor total e calculado e nao editavel, e fazer o filtro mensal funcionar de forma consistente em navegadores Linux.

## Usuario e permissoes

Usuario autenticado com permissao atual para editar despesas pelo modal do dashboard. A edicao deve respeitar a despesa e suas parcelas ja existentes.

## Fluxo esperado

1. O usuario abre a edicao de uma parcela no dashboard.
2. O formulario mostra a quantidade atual de parcelas e permite informar uma nova quantidade positiva.
3. O usuario pode aumentar ou reduzir a quantidade; o servidor compara o total solicitado com as parcelas persistidas e cria ou remove parcelas conforme a regra aprovada.
4. O valor total aparece calculado e com estilo visual de campo desabilitado, sem ser editavel.
5. O filtro mensal permite selecionar ano e mes em Linux e atualiza os dados do dashboard.

## Regras confirmadas

- O formulario de edicao do dashboard deve permitir editar `QtdParcelas`.
- `Valor total` e somente leitura, calculado pelo sistema e apresentado com estilo padrao de campo desabilitado.
- O filtro de mes nao deve depender do seletor nativo `input type="month"` se este impedir a selecao no navegador Linux usado.
- A quantidade informada deve ser maior ou igual a 1.
- A atualizacao deve persistir tanto o total da despesa quanto as parcelas correspondentes.

## Regra confirmada pelo usuario

Ao diminuir a quantidade, remover sempre as parcelas finais por numero, mesmo que estejam pagas. A quantidade escolhida determina o conjunto de parcelas que permanece.

## Criterios de aceite

- Given uma despesa no dashboard, when o usuario aumenta a quantidade, then o salvamento cria as parcelas faltantes com numeracao, valores e vencimentos coerentes.
- Given uma despesa no dashboard, when o usuario reduz a quantidade, then as parcelas finais por numero sao removidas, inclusive se pagas, sem deixar `QtdParcelas` e a colecao de parcelas inconsistentes.
- Given uma edicao valida, when o usuario salva, then o valor agregado da despesa corresponde a soma das parcelas que permanecem e das novas parcelas criadas.
- Given o campo de valor total, when o usuario visualiza o formulario de cadastro ou edicao, then fica visualmente claro que o campo nao pode ser editado.
- Given o dashboard em navegador Linux, when o usuario escolhe mes e ano, then o filtro atualiza os dados e o periodo exibido no cabecalho.
- Given quantidade invalida ou conflito de estado das parcelas, when o usuario salva, then a alteracao e rejeitada sem persistir dados parciais.

## Persistencia e compatibilidade

Nao se espera alteracao de schema. A edicao do dashboard deve manter compatibilidade com as parcelas existentes e preservar os campos de vencimento e pagamento de acordo com as regras aprovadas.

## Fora de escopo

- Alterar edicao de despesa fora do modal do dashboard.
- Redefinir regras de rateio ou de usuarios envolvidos.

## Riscos e pendencias

- A remocao de parcelas pagas pode excluir historico de pagamento; a solicitacao do usuario determina remover as parcelas finais mesmo quando pagas.
- O controle mensal deve continuar enviando o formato `yyyy-MM`, aceito pelo controller.
