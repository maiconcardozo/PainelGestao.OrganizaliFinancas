using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class CreditorMap : EntityMap<Creditor>, IEntityTypeConfiguration<Creditor>
    {
        public override void Configure(EntityTypeBuilder<Creditor> builder)
        {
            builder.ToTable("Creditor"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

