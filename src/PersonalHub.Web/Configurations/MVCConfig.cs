using PersonalHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Web.Configurations
{
    public static class MVCConfig
    {
        public static WebApplicationBuilder AddMvcConfig(this WebApplicationBuilder builder)
        { 
            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>();
            }

            builder.Configuration.AddEnvironmentVariables();

            builder.Services.AddHttpContextAccessor();

            builder.Services
                .AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            builder.Services.AddDbContext<PersonalHubContext>(opt =>
            {
                opt.UseSqlServer(builder.Configuration.GetConnectionString("ContextConnection"));
            });

            return builder;
        }
    }
}
