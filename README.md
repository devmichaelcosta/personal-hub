# Personal Hub

Central pessoal executada na rede interna para organizar despesas, tarefas, compras futuras e anotações. A primeira versão preserva o módulo financeiro completo, o redesign em temas claro/escuro e o assistente local com Ollama.

## Tecnologias

- .NET 10 e ASP.NET Core MVC/Razor;
- ASP.NET Core Identity;
- Entity Framework Core 10 e SQL Server;
- MediatR e xUnit;
- Bootstrap, jQuery e DataTables;
- Ollama local para o assistente.

## Estrutura

```text
src/
|-- PersonalHub.Web/             # Interface MVC/Razor
|-- PersonalHub.Api/             # API HTTP
|-- PersonalHub.Application/     # Casos de uso, comandos e queries
|-- PersonalHub.Domain/          # Entidades e regras de domínio
`-- PersonalHub.Infrastructure/  # EF Core, migrations e persistência
tests/
`-- PersonalHub.Tests/           # Testes xUnit
```

## Configuração local

Pré-requisitos: SDK .NET 10, SQL Server e, para o chat, Ollama com o modelo `qwen3.5:4b`.

Configure a conexão somente por User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:ContextConnection" "Server=.\SQLEXPRESS;Database=PersonalHub-dev;Trusted_Connection=True;TrustServerCertificate=True" --project src/PersonalHub.Web/PersonalHub.Web.csproj
dotnet user-secrets set "ConnectionStrings:ContextConnection" "Server=.\SQLEXPRESS;Database=PersonalHub-dev;Trusted_Connection=True;TrustServerCertificate=True" --project src/PersonalHub.Api/PersonalHub.Api.csproj
```

Os bancos locais esperados são:

- `PersonalHub`: cópia integral do banco principal;
- `PersonalHub-dev`: cópia integral do banco de desenvolvimento.

Nenhum backup, arquivo MDF/LDF, credencial ou dado pessoal deve ser versionado.

## Executar

```powershell
dotnet restore PersonalHub.sln
dotnet build PersonalHub.sln
dotnet run --project src/PersonalHub.Web/PersonalHub.Web.csproj --launch-profile "Self Hosting"
```

- Web: `https://localhost:7190` ou `http://localhost:5190`;
- API/Swagger: `https://localhost:7290/swagger`.

## Testes

```powershell
dotnet test PersonalHub.sln
```

Os testes de integração SQL do chat usam bancos temporários e só executam quando a configuração específica de integração está habilitada.

## Segurança e privacidade

- A aplicação é destinada à rede interna.
- O navegador chama apenas o MVC; o servidor acessa o Ollama em loopback.
- Imagens enviadas ao chat são processadas em memória e não são persistidas.
- Conteúdo financeiro não deve ser enviado a serviços externos de logging.
- Segredos e dados pessoais permanecem no SQL Server e em User Secrets locais.
