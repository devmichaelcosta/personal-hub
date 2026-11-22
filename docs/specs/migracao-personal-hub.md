# Migração para Personal Hub

## Objetivo

Criar uma base independente em .NET 10 para o Personal Hub, preservando o módulo financeiro, o novo layout, o assistente local e todos os dados atuais em bancos novos.

## Regras confirmadas

- Os projetos, assemblies e namespaces técnicos usam `PersonalHub.*`.
- Despesas permanece como nome do módulo financeiro e de suas rotas, entidades e tabelas.
- O repositório possui histórico novo e não contém segredos nem dados pessoais.
- KissLog e qualquer envio externo de logs são removidos.
- O assistente usa Ollama somente em loopback.
- `Despesas` é copiado para `PersonalHub` e `Despesas-dev` para `PersonalHub-dev`; as origens permanecem intactas.
- IDs, relacionamentos, usuários Identity e hashes de senha são preservados nas cópias.

## Critérios de aceite

- Given a solução migrada, when ela é restaurada e compilada, then todos os projetos usam `net10.0` e namespaces `PersonalHub.*`.
- Given os bancos de origem, when a cópia é concluída, then as contagens e relacionamentos existentes permanecem nas bases novas.
- Given um usuário existente, when autentica no Personal Hub, then suas despesas, rendimentos e conversas continuam disponíveis.
- Given o repositório público, when seu conteúdo é inspecionado, then não existem connection strings reais, e-mails pessoais, tokens, backups ou arquivos de banco.
- Given uma tela autenticada, when é aberta nos temas claro e escuro, then o novo layout e o chat continuam funcionais.

## Fora de escopo

- Unificar os dois bancos em uma única base.
- Alterar regras financeiras, rotas ou contratos HTTP do módulo de despesas.
- Expor o Ollama ou o SQL Server fora da rede interna.
