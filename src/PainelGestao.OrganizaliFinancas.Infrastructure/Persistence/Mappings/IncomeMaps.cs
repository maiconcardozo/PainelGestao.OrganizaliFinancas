using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class IncomeSourceMap : EntityMap<IncomeSource>, IEntityTypeConfiguration<IncomeSource>
    {
        public override void Configure(EntityTypeBuilder<IncomeSource> builder)
        {
            builder.ToTable("IncomeSource"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdPerson })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<IncomeType>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdIncomeType })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class IncomeSourceVersionMap : EntityMap<IncomeSourceVersion>, IEntityTypeConfiguration<IncomeSourceVersion>
    {
        public override void Configure(EntityTypeBuilder<IncomeSourceVersion> builder)
        {
            builder.ToTable("IncomeSourceVersion"); base.Configure(builder);
            builder.Property(x => x.ValidFrom).HasColumnType("date");
            builder.Property(x => x.EndDate).HasColumnType("date");
            builder.Property(x => x.Frequency).HasMaxLength(20).IsRequired();
            builder.Property(x => x.AmountMode).HasMaxLength(10).IsRequired();
            builder.Property(x => x.ExpectedNetAmount).HasPrecision(19, 2);
            builder.Property(x => x.GrossAmount).HasPrecision(19, 2);
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasIndex(x => new { x.IdTenant, x.IdIncomeSource, x.Version }).IsUnique();
            builder.HasOne<IncomeSource>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdIncomeSource })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class IncomeForecastMap : EntityMap<IncomeForecast>, IEntityTypeConfiguration<IncomeForecast>
    {
        public override void Configure(EntityTypeBuilder<IncomeForecast> builder)
        {
            builder.ToTable("IncomeForecast"); base.Configure(builder);
            builder.Property(x => x.OccurrenceDate).HasColumnType("date");
            builder.Property(x => x.Competence).HasColumnType("date");
            builder.Property(x => x.ExpectedAmount).HasPrecision(19, 2);
            builder.Property(x => x.Status).HasMaxLength(10).IsRequired();
            builder.Property(x => x.CloseReason).HasMaxLength(300);
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            // Idempotent generation: one forecast per source and date.
            builder.HasIndex(x => new { x.IdTenant, x.IdIncomeSource, x.OccurrenceDate }).IsUnique();
            builder.HasIndex(x => new { x.IdTenant, x.Competence });
            builder.HasOne<IncomeSource>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdIncomeSource })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<IncomeSourceVersion>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdIncomeSourceVersion })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class ReceiptMap : EntityMap<Receipt>, IEntityTypeConfiguration<Receipt>
    {
        public override void Configure(EntityTypeBuilder<Receipt> builder)
        {
            builder.ToTable("Receipt"); base.Configure(builder);
            builder.Property(x => x.ReceiptDate).HasColumnType("date");
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ReversedBy).HasMaxLength(64);
            builder.Property(x => x.ReversalReason).HasMaxLength(300);
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasIndex(x => new { x.IdTenant, x.ReceiptDate });
            builder.HasIndex(x => new { x.IdTenant, x.IdPerson, x.ReceiptDate });
            builder.HasOne<FinancialAccount>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdFinancialAccount })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdPerson })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<IncomeSource>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdIncomeSource })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<IncomeType>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdIncomeType })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class ReceiptAllocationMap : EntityMap<ReceiptAllocation>, IEntityTypeConfiguration<ReceiptAllocation>
    {
        public override void Configure(EntityTypeBuilder<ReceiptAllocation> builder)
        {
            builder.ToTable("ReceiptAllocation"); base.Configure(builder);
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.HasIndex(x => new { x.IdTenant, x.IdReceipt, x.IdIncomeForecast }).IsUnique();
            builder.HasOne<Receipt>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdReceipt })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<IncomeForecast>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdIncomeForecast })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
