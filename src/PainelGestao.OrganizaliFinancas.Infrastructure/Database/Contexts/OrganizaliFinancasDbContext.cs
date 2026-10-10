using Microsoft.EntityFrameworkCore;
using PainelGestao.OrganizaliFinancas.Domain.Entities;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Database.Contexts
{
    /// <summary>Schema and migrations only; runtime data access uses Dapper.</summary>
    public class OrganizaliFinancasDbContext(DbContextOptions<OrganizaliFinancasDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizaliFinancasDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
