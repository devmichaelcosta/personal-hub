using PersonalHub.Web.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.AddMvcConfig();
builder.Services
    .AddIdentityConfig()
    .AddDependencies();

WebApplication app = builder.Build();

app.Services
    .GerarDadosDefaultDoApp();
app.Services
    .GerarDadosDefaultIdentity();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/erro/500");
    app.UseStatusCodePagesWithRedirects("/erro/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection()
    .UseStaticFiles()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseGlobalizationConfig();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Conta}/{action=Login}/{id?}");
app.Run();
