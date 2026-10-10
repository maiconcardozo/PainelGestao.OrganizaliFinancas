using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class CreditCardMap : EntityMap<CreditCard>, IEntityTypeConfiguration<CreditCard>
    {
        public override void Configure(EntityTypeBuilder<CreditCard> builder)
        {
            builder.ToTable("CreditCard"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(60).IsRequired();
            builder.Property(x => x.CreditLimit).HasPrecision(19, 2);
            builder.Property(x => x.ClosingDayRule).HasMaxLength(10).IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdHolderPerson })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class CardInvoiceMap : EntityMap<CardInvoice>, IEntityTypeConfiguration<CardInvoice>
    {
        public override void Configure(EntityTypeBuilder<CardInvoice> builder)
        {
            builder.ToTable("CardInvoice"); base.Configure(builder);
            builder.Property(x => x.ReferenceMonth).HasColumnType("date");
            builder.Property(x => x.ClosingDate).HasColumnType("date");
            builder.Property(x => x.DueDate).HasColumnType("date");
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            // One invoice per card and month, so concurrent purchases never split a cycle.
            builder.HasIndex(x => new { x.IdTenant, x.IdCreditCard, x.ReferenceMonth }).IsUnique();
            builder.HasOne<CreditCard>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdCreditCard })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class CardPurchaseMap : EntityMap<CardPurchase>, IEntityTypeConfiguration<CardPurchase>
    {
        public override void Configure(EntityTypeBuilder<CardPurchase> builder)
        {
            builder.ToTable("CardPurchase"); base.Configure(builder);
            builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
            builder.Property(x => x.FieldValuesJson).HasColumnType("longtext").IsRequired();
            builder.Property(x => x.PurchaseDate).HasColumnType("date");
            builder.Property(x => x.TotalAmount).HasPrecision(19, 2);
            builder.Property(x => x.RefundedAmount).HasPrecision(19, 2);
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasIndex(x => new { x.IdTenant, x.PurchaseDate });
            builder.HasOne<CreditCard>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdCreditCard })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdBuyerPerson })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<ExpenseType>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdExpenseType })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Category>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdCategory })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class CardItemMap : EntityMap<CardItem>, IEntityTypeConfiguration<CardItem>
    {
        public override void Configure(EntityTypeBuilder<CardItem> builder)
        {
            builder.ToTable("CardItem"); base.Configure(builder);
            builder.Property(x => x.Kind).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(19, 2);
            builder.Property(x => x.Competence).HasColumnType("date");
            builder.Property(x => x.Note).HasMaxLength(300);
            builder.HasIndex(x => new { x.IdTenant, x.IdCardInvoice, x.IsActive });
            builder.HasIndex(x => new { x.IdTenant, x.Competence });
            builder.HasOne<CardPurchase>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdCardPurchase })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CardInvoice>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdCardInvoice })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
