using PersonalHub.Domain.Entidades;
using PersonalHub.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace PersonalHub.Web.Configurations
{
    public static class IdentityConfig
    {
        public static IServiceCollection AddIdentityConfig(this IServiceCollection services)
        {
            services.AddScoped<UserManager<Usuario>>();
            
            services.AddIdentity<Usuario, TipoUsuario>(options => {

                options.User = new UserOptions
                {
                    RequireUniqueEmail = true
                };

                options.Lockout = new LockoutOptions
                {
                    AllowedForNewUsers = true,
                    MaxFailedAccessAttempts = 5,
                    DefaultLockoutTimeSpan = new TimeSpan(0, 10, 0)
                };

                options.Password = new PasswordOptions
                {
                    RequireDigit = false,
                    RequiredLength = 6,
                    RequireLowercase = false,
                    RequireNonAlphanumeric = false,
                    RequireUppercase = false
                };

                options.SignIn = new SignInOptions
                {
                    RequireConfirmedEmail = false,
                    RequireConfirmedPhoneNumber = false
                };

            }).AddEntityFrameworkStores<PersonalHubContext>()
              .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                //options.Cookie.HttpOnly = true;
                //options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

                options.LoginPath = "/Conta/Login";
                options.AccessDeniedPath = "/Conta/AccessDenied";
                //options.SlidingExpiration = true;
            });

            return services;
        }
    }
}
