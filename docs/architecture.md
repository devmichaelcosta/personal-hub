# Arquitetura

## Visao geral

O repositorio contem uma solucao .NET 10 para organizacao pessoal. A aplicacao principal e um app MVC/Razor, com uma API separada e camadas para dominio, infraestrutura e aplicacao. O modulo financeiro e a primeira area funcional do Personal Hub.

## Camadas

### MVC/Razor

O projeto `src/PersonalHub.Web` contem a interface web atual. Ele concentra controllers, views, view models, filtros, configuracao de Identity, configuracao MVC e arquivos estaticos.

Use esta camada para fluxos de tela ja existentes, renderizacao Razor, validacao de formularios e interacoes web tradicionais.

Listagens tabulares da interface devem usar DataTables como padrao, sempre com pesquisa, ordenacao e paginacao, seguindo o padrao das paginas de Despesas e Rendimentos.

### API

O projeto `src/PersonalHub.Api` expoe endpoints HTTP. Hoje ha endpoints para despesas usando MediatR.

Use esta camada para contratos HTTP e orquestracao leve. Regras de negocio devem ficar em dominio ou aplicacao.

### Aplicacao

O projeto `src/PersonalHub.Application` contem comandos, queries, DTOs e handlers. Ele e o lugar preferencial para casos de uso que podem ser chamados pela API ou reaproveitados por outras interfaces.

O padrao atual usa MediatR em partes da funcionalidade de despesas.

### Dominio

O projeto `src/PersonalHub.Domain` contem entidades como `Despesa`, `Parcela`, `Rendimento`, `Tag`, `Usuario` e contratos como `IDespesa`.

Regras financeiras e invariantes principais devem ser implementadas e testadas aqui sempre que forem independentes de interface e banco.

### Infraestrutura

O projeto `src/PersonalHub.Infrastructure` contem `PersonalHubContext`, migrations e mapeamento EF Core. O contexto herda de `IdentityDbContext<Usuario, TipoUsuario, string>`.

O banco usado e SQL Server, com a connection string `ContextConnection`. Os bancos locais `PersonalHub` e `PersonalHub-dev` preservam os dados dos ambientes anteriores sem versiona-los.

## Autenticacao

A aplicacao MVC usa ASP.NET Core Identity com entidades customizadas `Usuario` e `TipoUsuario`. O login padrao esta em `/Conta/Login`.

## Direcao recomendada

- Manter MVC responsavel por telas.
- Manter API responsavel por contratos HTTP.
- Mover regras reutilizaveis para `PersonalHub.Application` e `PersonalHub.Domain`.
- Aumentar cobertura de testes nas regras de dominio antes de refactors maiores.
- Tratar configuracao sensivel fora do Git.
