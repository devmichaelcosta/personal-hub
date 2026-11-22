using PersonalHub.Domain.Entidades;
using Microsoft.AspNetCore.Identity;

namespace PersonalHub.Web.Configurations;

public static class IdentitySeed
{
    public static void GerarDadosDefaultIdentity(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var userManager = services.GetRequiredService<UserManager<Usuario>>();
            var roleManager = services.GetRequiredService<RoleManager<TipoUsuario>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            if (!roleManager.RoleExistsAsync("admin").Result)
            {
                var role = new TipoUsuario("admin");
                IdentityResult roleResult = roleManager.CreateAsync(role).Result;
            }

            string? userName = configuration["Development:SeedUser:UserName"];
            if (string.IsNullOrWhiteSpace(userName) || userManager.Users.Any(e => e.UserName == userName))
                return;

            string? name = configuration["Development:SeedUser:Name"];
            string? lastName = configuration["Development:SeedUser:LastName"];
            string? email = configuration["Development:SeedUser:Email"];
            string? password = configuration["Development:SeedUser:Password"];

            if (new[] { name, lastName, email, password }.Any(string.IsNullOrWhiteSpace))
            {
                logger.LogWarning("O usuario de desenvolvimento nao foi criado porque sua configuracao esta incompleta.");
                return;
            }

            var user = new Usuario(userName, name!, lastName!, email!);
            IdentityResult result = userManager.CreateAsync(user, password!).Result;
            if (result.Succeeded)
                userManager.AddToRoleAsync(user, "admin").Wait();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
