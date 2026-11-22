using PersonalHub.Web.Filters;
using PersonalHub.Web.Services.Chat;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace PersonalHub.Web.Configurations
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection AddDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IValidationAttributeAdapterProvider, MoedaValidationAttributeAdapterProvider>();
            services.AddHttpClient<IChatOllamaClient, ChatOllamaClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(3);
            });
            services.AddScoped<ChatAssistenteService>();


            //builder.Services.AddHttpClient<DespesaApiClient>(config =>
            //{
            //    config.BaseAddress = new Uri("https://localhost:7290/api/v1/");
            //});

            //builder.Services.AddMediatR(cf => cf.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));

            //var assembly = AppDomain.CurrentDomain.Load("PersonalHub.Application");
            //builder.Services.AddMediatR(typeof(Program).GetTypeInfo().Assembly);
            //builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
            //builder.Services.AddMediatR(c =>
            //{
            //    c.RegisterServicesFromAssembly(typeof(Program).GetTypeInfo().Assembly);
            //});
            return services;
        }
    }
}
