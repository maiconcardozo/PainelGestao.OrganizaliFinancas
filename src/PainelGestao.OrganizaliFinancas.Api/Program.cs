using PainelGestao.OrganizaliFinancas.Application.Configuration;
using PainelGestao.OrganizaliFinancas.Infrastructure.Configuration;
using PainelGestao.OrganizaliFinancas.Api.Configuration;
using PainelGestao.OrganizaliFinancas.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiDocumentation();
builder.AddOperations();
if (string.IsNullOrEmpty(builder.Configuration["Operation"]))
    builder.Services.AddApiSecurity(builder.Configuration);

var app = builder.Build();
if (await app.RunAdministrativeOperationAsync()) return;
app.UseOperations();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseApiDocumentation();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthEndpoints();
app.MapAuthenticationEndpoints();
app.MapMemberEndpoints();
app.MapHouseholdEndpoints();
app.MapCatalogEndpoints();
app.MapExpenseTypeEndpoints();
app.MapFinanceEndpoints();
app.MapExpenseEndpoints();
app.MapCardEndpoints();
app.MapPlanningEndpoints();
app.MapComplementEndpoints();

app.Run();

public partial class Program;
