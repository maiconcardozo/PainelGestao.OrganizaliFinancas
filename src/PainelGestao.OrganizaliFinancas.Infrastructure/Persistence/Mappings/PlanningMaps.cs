using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class BudgetLimitMap : EntityMap<BudgetLimit>, IEntityTypeConfiguration<BudgetLimit>
    {
        public override void Configure(EntityTypeBuilder<BudgetLimit> builder)
        {
            builder.ToTable("BudgetLimit"); base.Configure(builder);
            builder.Property(x => x.Month).HasColumnType("date");
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.HasIndex(x => new { x.IdTenant, x.IdCategory, x.Month }).IsUnique();
            builder.HasIndex(x => new { x.IdTenant, x.Month });
            builder.HasOne<Category>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdCategory })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class AlertPreferenceMap : EntityMap<AlertPreference>, IEntityTypeConfiguration<AlertPreference>
    {
        public override void Configure(EntityTypeBuilder<AlertPreference> builder)
        {
            builder.ToTable("AlertPreference"); base.Configure(builder);
            builder.HasIndex(x => x.IdTenant).IsUnique();
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class AlertStateMap : EntityMap<AlertState>, IEntityTypeConfiguration<AlertState>
    {
        public override void Configure(EntityTypeBuilder<AlertState> builder)
        {
            builder.ToTable("AlertState"); base.Configure(builder);
            builder.Property(x => x.AlertKey).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Fingerprint).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(10).IsRequired();
            builder.Property(x => x.SnoozedUntil).HasColumnType("date");
            builder.HasIndex(x => new { x.IdTenant, x.AlertKey }).IsUnique();
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
