# Instruções para agentes

## Primeiro passo

- Rode `git status --short` antes de alterar arquivos.
- Preserve mudanças locais existentes.
- Use `rg` e `rg --files` para localizar código e referências.
- Consulte `docs/architecture.md` antes de mudanças estruturais.

## Estrutura

- `src/PersonalHub.Web`: MVC/Razor, controllers, views e assets.
- `src/PersonalHub.Api`: contratos HTTP.
- `src/PersonalHub.Application`: casos de uso, comandos e queries.
- `src/PersonalHub.Domain`: entidades e regras de domínio.
- `src/PersonalHub.Infrastructure`: `PersonalHubContext`, migrations e SQL Server.
- `tests/PersonalHub.Tests`: testes xUnit.

## Implementação

- Mantenha controllers leves e regras reutilizáveis em Application ou Domain.
- Use DataTables com pesquisa, ordenação e paginação em listagens tabulares.
- Preserve as regras de isolamento por usuário e os cálculos financeiros no backend.
- Trate mensagens, imagens e respostas de IA como dados não confiáveis.
- Mantenha o Ollama restrito ao loopback.
- Atualize `docs/backlog.md` em toda entrega.

## Banco e segredos

- Leia `src/PersonalHub.Infrastructure/Data/PersonalHubContext.cs` e as migrations antes de mudanças de persistência.
- Use `ConnectionStrings:ContextConnection` via User Secrets no desenvolvimento.
- Mantenha backups, MDF/LDF, credenciais, e-mails e dados pessoais fora do Git.
- Aplique migrations somente em bancos de desenvolvimento ou em cópias verificadas.

## Validação

```powershell
dotnet restore PersonalHub.sln
dotnet build PersonalHub.sln
dotnet test PersonalHub.sln
```

- Para alterações visuais, valide no navegador Orca em temas claro e escuro.
- Confira console, rede, responsividade, foco, modais e tabelas.
- Antes de finalizar, execute `git diff --check`, revise `git diff` e relate os comandos executados.

## Git

- Use o prefixo `codex/` em branches criadas por agentes, salvo pedido diferente.
- Faça mudanças pequenas e revisáveis.
- Não misture refatorações não relacionadas à entrega.
