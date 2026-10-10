using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class TenantMap : EntityMap<Tenant>, IEntityTypeConfiguration<Tenant>
    {
        public override void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenant");
            base.Configure(builder);
            builder.Property(entity => entity.Name).IsRequired().HasMaxLength(160);
        }
    }
}
