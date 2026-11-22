using PersonalHub.Domain.Entidades;
using PersonalHub.Infrastructure.Data;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PersonalHub.Web.Configurations;

public static class PersonalHubContextSeed
{
    private const string MigrationRecorrencia = "20261113000214_DespesasRecorrentes";
    private const string VersaoEfCore = "8.0.28";

    public static IServiceProvider GerarDadosDefaultDoApp(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<PersonalHubContext>();

            RegistrarHistoricoDeSchemaLegado(context);
            context.GetService<IMigrator>().Migrate();

            if (!context.Tags.Any(t => t.Nome == "Mercado"))
                context.Tags.Add(new Tag("Mercado"));

            if (!context.Tags.Any(t => t.Nome == "NU Bank"))
                context.Tags.Add(new Tag("NU Bank"));

            if (!context.Tags.Any(t => t.Nome == "Caixa Econômica Fed."))
                context.Tags.Add(new Tag("Caixa Econômica Fed."));

            if (!context.Tags.Any(t => t.Nome == "Moradia"))
                context.Tags.Add(new Tag("Moradia"));

            context.SaveChanges();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }

        return services;
    }

    private static void RegistrarHistoricoDeSchemaLegado(PersonalHubContext context)
    {
        List<string> migrations = context.Database.GetMigrations().ToList();
        int indiceMigrationRecorrencia = migrations.IndexOf(MigrationRecorrencia);

        if (indiceMigrationRecorrencia < 0 || context.Database.GetAppliedMigrations().Any())
            return;

        DbConnection connection = context.Database.GetDbConnection();
        bool fecharConexao = connection.State != ConnectionState.Open;
        if (fecharConexao)
            connection.Open();

        try
        {
            using DbCommand validarSchema = connection.CreateCommand();
            validarSchema.CommandText = @"
SELECT CASE WHEN
    OBJECT_ID(N'dbo.Despesas', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.Parcelas', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.Rendimentos', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.Tags', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.TagsDespesas', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.Tarefas', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.UsuariosDespesas', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.TiposUsuarios', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NOT NULL AND
    OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NOT NULL AND
    COL_LENGTH(N'dbo.Despesas', N'Recorrente') IS NULL AND
    OBJECT_ID(N'dbo.OcorrenciasDespesasRecorrentes', N'U') IS NULL
THEN 1 ELSE 0 END";

            if (Convert.ToInt32(validarSchema.ExecuteScalar()) != 1)
                return;

            using DbCommand criarHistorico = connection.CreateCommand();
            criarHistorico.CommandText = @"
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory (
        MigrationId nvarchar(150) NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
        ProductVersion nvarchar(32) NOT NULL
    );
END";
            criarHistorico.ExecuteNonQuery();

            for (int indice = 0; indice < indiceMigrationRecorrencia; indice++)
            {
                using DbCommand registrarMigration = connection.CreateCommand();
                registrarMigration.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = @MigrationId)
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (@MigrationId, @ProductVersion)";

                DbParameter migrationId = registrarMigration.CreateParameter();
                migrationId.ParameterName = "@MigrationId";
                migrationId.Value = migrations[indice];
                registrarMigration.Parameters.Add(migrationId);

                DbParameter productVersion = registrarMigration.CreateParameter();
                productVersion.ParameterName = "@ProductVersion";
                productVersion.Value = VersaoEfCore;
                registrarMigration.Parameters.Add(productVersion);

                registrarMigration.ExecuteNonQuery();
            }
        }
        finally
        {
            if (fecharConexao)
                connection.Close();
        }
    }
}
