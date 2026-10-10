using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    internal static class ExpenseMapping
    {
        /// <summary>Descriptive references shared by obligations and recurring templates; all composite with the household.</summary>
        public static void References<T>(EntityTypeBuilder<T> builder) where T : TenantEntity
        {
            builder.Property<string>("Description").HasMaxLength(200).IsRequired();
            builder.Property<string>("FieldValuesJson").HasColumnType("longtext").IsRequired();
            builder.Property<string?>("SplitMode").HasMaxLength(10);
            builder.HasOne<ExpenseType>().WithMany().HasForeignKey("IdTenant", "IdExpenseType").HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Category>().WithMany().HasForeignKey("IdTenant", "IdCategory").HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Creditor>().WithMany().HasForeignKey("IdTenant", "IdCreditor").HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<PaymentMethod>().WithMany().HasForeignKey("IdTenant", "IdPaymentMethod").HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Person>().WithMany().HasForeignKey("IdTenant", "IdPayerPerson").HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class RecurringExpenseMap : EntityMap<RecurringExpense>, IEntityTypeConfiguration<RecurringExpense>
    {
        public override void Configure(EntityTypeBuilder<RecurringExpense> builder)
        {
            builder.ToTable("RecurringExpense"); base.Configure(builder);
            ExpenseMapping.References(builder);
            builder.Property(x => x.SharesJson).HasColumnType("longtext").IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
        }
    }
    public class RecurringExpenseVersionMap : EntityMap<RecurringExpenseVersion>, IEntityTypeConfiguration<RecurringExpenseVersion>
    {
        public override void Configure(EntityTypeBuilder<RecurringExpenseVersion> builder)
        {
            builder.ToTable("RecurringExpenseVersion"); base.Configure(builder);
            builder.Property(x => x.ValidFrom).HasColumnType("date");
            builder.Property(x => x.EndDate).HasColumnType("date");
            builder.Property(x => x.Frequency).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasIndex(x => new { x.IdTenant, x.IdRecurringExpense, x.Version }).IsUnique();
            builder.HasOne<RecurringExpense>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdRecurringExpense })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class InstallmentPlanMap : EntityMap<InstallmentPlan>, IEntityTypeConfiguration<InstallmentPlan>
    {
        public override void Configure(EntityTypeBuilder<InstallmentPlan> builder)
        {
            builder.ToTable("InstallmentPlan"); base.Configure(builder);
            builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
            builder.Property(x => x.TotalAmount).HasPrecision(19, 2);
            builder.Property(x => x.FirstDueDate).HasColumnType("date");
            builder.Property(x => x.Frequency).HasMaxLength(20).IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class ObligationMap : EntityMap<Obligation>, IEntityTypeConfiguration<Obligation>
    {
        public override void Configure(EntityTypeBuilder<Obligation> builder)
        {
            builder.ToTable("Obligation"); base.Configure(builder);
            ExpenseMapping.References(builder);
            builder.Property(x => x.Competence).HasColumnType("date");
            builder.Property(x => x.DueDate).HasColumnType("date");
            builder.Property(x => x.OccurrenceDate).HasColumnType("date");
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.Property(x => x.Status).HasMaxLength(10).IsRequired();
            builder.Property(x => x.CancelReason).HasMaxLength(300);
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            // Idempotent recurrence: one obligation per rule and date (NULLs do not collide for non-recurring rows).
            builder.HasIndex(x => new { x.IdTenant, x.IdRecurringExpense, x.OccurrenceDate }).IsUnique();
            builder.HasIndex(x => new { x.IdTenant, x.DueDate });
            builder.HasIndex(x => new { x.IdTenant, x.Competence });
            builder.HasOne<RecurringExpense>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdRecurringExpense })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<InstallmentPlan>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdInstallmentPlan })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class ObligationShareMap : EntityMap<ObligationShare>, IEntityTypeConfiguration<ObligationShare>
    {
        public override void Configure(EntityTypeBuilder<ObligationShare> builder)
        {
            builder.ToTable("ObligationShare"); base.Configure(builder);
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.Property(x => x.Percent).HasPrecision(9, 6);
            builder.HasIndex(x => new { x.IdTenant, x.IdObligation, x.IsActive });
            builder.HasOne<Obligation>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdObligation })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdPerson })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class ObligationAdjustmentMap : EntityMap<ObligationAdjustment>, IEntityTypeConfiguration<ObligationAdjustment>
    {
        public override void Configure(EntityTypeBuilder<ObligationAdjustment> builder)
        {
            builder.ToTable("ObligationAdjustment"); base.Configure(builder);
            builder.Property(x => x.Kind).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.Property(x => x.AdjustmentDate).HasColumnType("date");
            builder.Property(x => x.Reason).HasMaxLength(300).IsRequired();
            builder.HasOne<Obligation>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdObligation })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class PaymentMap : EntityMap<Payment>, IEntityTypeConfiguration<Payment>
    {
        public override void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payment"); base.Configure(builder);
            builder.Property(x => x.PaymentDate).HasColumnType("date");
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ReversedBy).HasMaxLength(64);
            builder.Property(x => x.ReversalReason).HasMaxLength(300);
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasIndex(x => new { x.IdTenant, x.PaymentDate });
            builder.HasOne<FinancialAccount>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdFinancialAccount })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdPayerPerson })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<PaymentMethod>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdPaymentMethod })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class PaymentAllocationMap : EntityMap<PaymentAllocation>, IEntityTypeConfiguration<PaymentAllocation>
    {
        public override void Configure(EntityTypeBuilder<PaymentAllocation> builder)
        {
            builder.ToTable("PaymentAllocation"); base.Configure(builder);
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.HasIndex(x => new { x.IdTenant, x.IdPayment, x.IdObligation }).IsUnique();
            builder.HasIndex(x => new { x.IdTenant, x.IdObligation });
            builder.HasOne<Payment>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdPayment })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Obligation>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdObligation })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
