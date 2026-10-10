using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class CatalogRevisionMap : EntityMap<CatalogRevision>, IEntityTypeConfiguration<CatalogRevision>
    {
        public override void Configure(EntityTypeBuilder<CatalogRevision> builder)
        {
            builder.ToTable("CatalogRevision"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Catalog).HasMaxLength(40).IsRequired();
            builder.HasIndex(x => new { x.IdTenant, x.Catalog, x.ItemCode, x.Version }).IsUnique();
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
