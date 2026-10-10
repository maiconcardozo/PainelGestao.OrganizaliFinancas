using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class UserMap : EntityMap<User>, IEntityTypeConfiguration<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");
            base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(180).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class SubscriptionMap : EntityMap<Subscription>, IEntityTypeConfiguration<Subscription>
    {
        public override void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.ToTable("Subscription");
            base.Configure(builder);
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => x.IdTenant).IsUnique();
            builder.HasIndex(x => x.ProvisioningKey).IsUnique();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<User>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdOwnerUser })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class SubscriptionEventMap : EntityMap<SubscriptionEvent>, IEntityTypeConfiguration<SubscriptionEvent>
    {
        public override void Configure(EntityTypeBuilder<SubscriptionEvent> builder)
        {
            builder.ToTable("SubscriptionEvent");
            base.Configure(builder);
            builder.Property(x => x.Status).HasConversion<int>();
            builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            builder.HasOne<Subscription>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdSubscription })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
