# Desenvolvimento Local

## Pre-requisitos

- .NET SDK compativel com `net10.0`
- SQL Server ou SQL Server Express
- Visual Studio, Rider ou VS Code
- PowerShell

## Restaurar e compilar

```powershell
dotnet restore PersonalHub.sln
dotnet build PersonalHub.sln
```

Se o build falhar com erro de copia de DLL ou PDB, feche a aplicacao em execucao, IIS Express ou a instancia do Visual Studio que esteja segurando arquivos em `bin`.

## Rodar testes

```powershell
dotnet test PersonalHub.sln
```

Para validacao rapida depois de compilar:

```powershell
dotnet test PersonalHub.sln --no-restore --no-build
```

## Rodar a aplicacao MVC

```powershell
dotnet run --project src/PersonalHub.Web/PersonalHub.Web.csproj
```

A rota padrao abre em `Conta/Login`.

## Rodar a API

```powershell
dotnet run --project src/PersonalHub.Api/PersonalHub.Api.csproj
```

Em desenvolvimento, Swagger e habilitado pela API.

## Connection string

A chave usada pelo EF Core e:

```text
ConnectionStrings:ContextConnection
```

O MVC e a API ja possuem `UserSecretsId`. Em ambiente `Development`, a configuracao e carregada automaticamente. Configure a conexao local do MVC com:

```powershell
dotnet user-secrets set "ConnectionStrings:ContextConnection" "Server=.\SQLEXPRESS;Database=PersonalHub-dev;Trusted_Connection=True;TrustServerCertificate=true;" --project src/PersonalHub.Web/PersonalHub.Web.csproj
```

Para a API, se necessario, configure o segredo separadamente, pois cada projeto tem seu proprio armazenamento:

```powershell
dotnet user-secrets set "ConnectionStrings:ContextConnection" "Server=.\SQLEXPRESS;Database=PersonalHub-dev;Trusted_Connection=True;TrustServerCertificate=true;" --project src/PersonalHub.Api/PersonalHub.Api.csproj
```

Os bancos copiados ja contem seus usuarios. Para um banco vazio, configure opcionalmente `Development:SeedUser:UserName`, `Name`, `LastName`, `Email` e `Password` via User Secrets. Para conferir as chaves, use `dotnet user-secrets list`; a saida contem valores e nao deve ser compartilhada. Em Staging e Production, configure `ConnectionStrings__ContextConnection` por variavel de ambiente ou provedor de segredos.

Nao versionar senhas reais em `appsettings.*.json`.

## Migrations e banco

As migrations ficam em `src/PersonalHub.Infrastructure/Migrations` e usam o assembly `PersonalHub.Infrastructure`.

Antes de criar migrations:

- Confira se a alteracao de modelo esta intencional.
- Confira qual projeto sera usado como startup.
- Rode testes de dominio quando a mudanca afetar regras de despesas.

Exemplo de comando base:

```powershell
dotnet ef database update --project src/PersonalHub.Infrastructure/PersonalHub.Infrastructure.csproj --startup-project src/PersonalHub.Web/PersonalHub.Web.csproj
```

## Seed

O app MVC chama `GerarDadosDefaultDoApp()` durante a inicializacao. Antes de alterar seed, verifique `src/PersonalHub.Web/Configurations` e `src/PersonalHub.Infrastructure/Data/DespesaContextExtensions.cs`.

## Problemas comuns

- O projeto usa `net10.0`.
- Arquivos em `bin` podem ficar travados pelo Visual Studio ou IIS Express.
- O registro do MediatR na API carrega handlers da camada de aplicacao.
- Warnings de nullable existem em dominio, aplicacao e MVC.
