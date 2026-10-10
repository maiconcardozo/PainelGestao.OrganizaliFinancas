using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class RoleMap : EntityMap<Role>, IEntityTypeConfiguration<Role>
    {
        public override void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Role"); base.Configure(builder);
            builder.Property(x => x.Key).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
            builder.HasIndex(x => new { x.IdTenant, x.Key }).IsUnique();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class UserRoleMap : EntityMap<UserRole>, IEntityTypeConfiguration<UserRole>
    {
        public override void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRole"); base.Configure(builder);
            builder.HasIndex(x => new { x.IdUser, x.IdRole }).IsUnique();
            builder.HasOne<User>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdUser })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Role>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdRole })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class MemberInvitationMap : EntityMap<MemberInvitation>, IEntityTypeConfiguration<MemberInvitation>
    {
        public override void Configure(EntityTypeBuilder<MemberInvitation> builder)
        {
            builder.ToTable("MemberInvitation"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(180).IsRequired();
            builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasOne<Role>().WithMany().HasForeignKey(x => new { x.IdTenant, x.IdRole })
                .HasPrincipalKey(x => new { x.IdTenant, x.Id }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
