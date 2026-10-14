using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class SavingsGoalMap : EntityMap<SavingsGoal>, IEntityTypeConfiguration<SavingsGoal>
    {
        public override void Configure(EntityTypeBuilder<SavingsGoal> builder)
        {
            builder.ToTable("SavingsGoal"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.Property(x => x.TargetAmount).HasPrecision(19, 2);
            builder.Property(x => x.TargetDate).HasColumnType("date");
            builder.Property(x => x.Note).HasMaxLength(300);
            builder.HasIndex(x => new { x.IdTenant, x.IsActive });
            builder.HasOne<FinancialAccount>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdFinancialAccount })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class AttachmentMap : EntityMap<Attachment>, IEntityTypeConfiguration<Attachment>
    {
        public override void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.ToTable("Attachment"); base.Configure(builder);
            builder.Property(x => x.TargetKind).HasMaxLength(20).IsRequired();
            builder.Property(x => x.FileName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ContentType).HasMaxLength(80).IsRequired();
            builder.Property(x => x.StorageKey).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => new { x.IdTenant, x.TargetKind, x.TargetCode });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class ImportBatchMap : EntityMap<ImportBatch>, IEntityTypeConfiguration<ImportBatch>
    {
        public override void Configure(EntityTypeBuilder<ImportBatch> builder)
        {
            builder.ToTable("ImportBatch"); base.Configure(builder);
            builder.Property(x => x.FileName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Format).HasMaxLength(10).IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<FinancialAccount>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdFinancialAccount })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    /// <summary>Schema-only entity: lines are written with SQL that resolves the batch by code inside the same transaction.</summary>
    public class ImportLineRow
    {
        public int Id { get; set; } public Guid Code { get; set; } public int IdTenant { get; set; } public int IdImportBatch { get; set; }
        public string Fingerprint { get; set; } = string.Empty; public DateTime LineDate { get; set; } public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty; public string? ExternalId { get; set; } public string Action { get; set; } = string.Empty;
        public Guid? ResultCode { get; set; } public bool IsActive { get; set; } public DateTime DtCreated { get; set; } public string CreatedBy { get; set; } = string.Empty;
    }
    public class ImportLineMap : IEntityTypeConfiguration<ImportLineRow>
    {
        public void Configure(EntityTypeBuilder<ImportLineRow> builder)
        {
            builder.ToTable("ImportLine");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Code).HasColumnType("char(36)");
            builder.Property(x => x.Fingerprint).HasMaxLength(40).IsRequired();
            builder.Property(x => x.LineDate).HasColumnType("date");
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ExternalId).HasMaxLength(100);
            builder.Property(x => x.Action).HasMaxLength(20).IsRequired();
            builder.Property(x => x.ResultCode).HasColumnType("char(36)");
            builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            builder.Property(x => x.DtCreated).HasColumnType("datetime(6)");
            builder.HasIndex(x => new { x.IdTenant, x.Fingerprint });
            builder.HasOne<ImportBatch>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdImportBatch })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
