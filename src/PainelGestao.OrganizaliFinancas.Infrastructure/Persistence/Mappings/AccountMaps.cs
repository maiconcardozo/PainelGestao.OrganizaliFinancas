using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class FinancialAccountMap : EntityMap<FinancialAccount>, IEntityTypeConfiguration<FinancialAccount>
    {
        public override void Configure(EntityTypeBuilder<FinancialAccount> builder)
        {
            builder.ToTable("FinancialAccount"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.Property(x => x.OpeningDate).HasColumnType("date");
            builder.Property(x => x.OpeningBalance).HasPrecision(19, 2);
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class TransferMap : EntityMap<Transfer>, IEntityTypeConfiguration<Transfer>
    {
        public override void Configure(EntityTypeBuilder<Transfer> builder)
        {
            builder.ToTable("Transfer"); base.Configure(builder);
            builder.Property(x => x.TransferDate).HasColumnType("date");
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ReversedBy).HasMaxLength(64);
            builder.Property(x => x.ReversalReason).HasMaxLength(300);
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasIndex(x => new { x.IdTenant, x.TransferDate });
            builder.HasOne<FinancialAccount>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdSourceAccount })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<FinancialAccount>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdTargetAccount })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class AccountMovementMap : EntityMap<AccountMovement>, IEntityTypeConfiguration<AccountMovement>
    {
        public override void Configure(EntityTypeBuilder<AccountMovement> builder)
        {
            builder.ToTable("AccountMovement"); base.Configure(builder);
            builder.Property(x => x.MovementDate).HasColumnType("date");
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.Property(x => x.Kind).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasIndex(x => new { x.IdTenant, x.IdFinancialAccount, x.MovementDate });
            // A movement is compensated at most once.
            builder.HasIndex(x => new { x.IdTenant, x.IdReversedMovement }).IsUnique();
            builder.HasOne<FinancialAccount>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdFinancialAccount })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Transfer>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdTransfer })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Receipt>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdReceipt })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Payment>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdPayment })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<AccountMovement>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdReversedMovement })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class IdempotencyRecordMap : EntityMap<IdempotencyRecord>, IEntityTypeConfiguration<IdempotencyRecord>
    {
        public override void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecord"); base.Configure(builder);
            builder.Property(x => x.Operation).HasMaxLength(60).IsRequired();
            builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => new { x.IdTenant, x.Operation, x.RequestKey }).IsUnique();
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
