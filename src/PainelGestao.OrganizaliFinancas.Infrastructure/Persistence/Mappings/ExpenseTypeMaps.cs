using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class ExpenseTypeMap : EntityMap<ExpenseType>, IEntityTypeConfiguration<ExpenseType>
    {
        public override void Configure(EntityTypeBuilder<ExpenseType> builder)
        {
            builder.ToTable("ExpenseType"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class ExpenseTypeVersionMap : EntityMap<ExpenseTypeVersion>, IEntityTypeConfiguration<ExpenseTypeVersion>
    {
        public override void Configure(EntityTypeBuilder<ExpenseTypeVersion> builder)
        {
            builder.ToTable("ExpenseTypeVersion"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Behavior).HasMaxLength(20).IsRequired();
            builder.Property(x => x.FieldsJson).HasColumnType("longtext").IsRequired();
            builder.HasIndex(x => new { x.IdTenant, x.IdExpenseType, x.Version }).IsUnique();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<ExpenseType>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdExpenseType })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Category>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdDefaultCategory })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
