using PersonalHub.Application.CasosUso;
using PersonalHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PersonalHubContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ContextConnection"));
});

//var assembly = AppDomain.CurrentDomain.Load("PersonalHub.Application");
//builder.Services.AddMediatR(typeof(Program).GetTypeInfo().Assembly);
//builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
//builder.Services.AddMediatR(c =>
//{
//    c.RegisterServicesFromAssembly(typeof(Program).GetTypeInfo().Assembly);
//});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DeletarDespesaCommandHandler>());
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
