using PainelGestao.OrganizaliFinancas.Application.Configuration;
using PainelGestao.OrganizaliFinancas.Infrastructure.Configuration;
using PainelGestao.OrganizaliFinancas.Api.Configuration;
using PainelGestao.OrganizaliFinancas.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiDocumentation();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseApiDocumentation();
app.MapHealthEndpoints();

app.Run();

public partial class Program;
