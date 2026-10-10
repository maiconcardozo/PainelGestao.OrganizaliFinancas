using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PainelGestao.OrganizaliFinancas.Infrastructure.Database.Contexts;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Database.Factories
{
    public class OrganizaliFinancasDbContextFactory : IDesignTimeDbContextFactory<OrganizaliFinancasDbContext>
    {
        public OrganizaliFinancasDbContext CreateDbContext(string[] args)
        {
            // Offline schema generation only. Port 1 prevents accidental use of a local database.
            var options = new DbContextOptionsBuilder<OrganizaliFinancasDbContext>()
                .UseMySQL("server=127.0.0.1;port=1;database=organizali_design;user=design;password=design")
                .Options;
            return new OrganizaliFinancasDbContext(options);
        }
    }
}
