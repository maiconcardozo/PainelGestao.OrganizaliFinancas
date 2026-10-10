using Microsoft.OpenApi;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class SwaggerConfiguration
    {
        public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Organizali Finanças",
                Version = "v1",
                Description = "Base técnica em desenvolvimento. Acesso, cadastros e operações financeiras entram nos próximos marcos."
            }));
            return services;
        }

        public static WebApplication UseApiDocumentation(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment()) return app;
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Organizali Finanças v1");
                options.DocumentTitle = "Organizali Finanças — API";
                options.InjectStylesheet("/swagger-ui/organizali-docs.css");
                options.InjectJavascript("/swagger-ui/organizali-docs.js");
                options.HeadContent = "<meta name=\"theme-color\" content=\"#202020\">";
            });
            return app;
        }
    }
}
