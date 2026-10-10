using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class IncomeTypeMap : EntityMap<IncomeType>, IEntityTypeConfiguration<IncomeType>
    {
        public override void Configure(EntityTypeBuilder<IncomeType> builder)
        {
            builder.ToTable("IncomeType"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

