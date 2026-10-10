using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class RefreshTokenMap : EntityMap<RefreshToken>, IEntityTypeConfiguration<RefreshToken>
    {
        public override void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshToken");
            base.Configure(builder);
            builder.Property(x=>x.TokenHash).HasMaxLength(128).IsRequired();
            builder.HasIndex(x=>x.TokenHash).IsUnique();
            builder.HasOne<User>().WithMany().HasForeignKey(x=>x.IdUser).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class PasswordRecoveryMap : EntityMap<PasswordRecovery>, IEntityTypeConfiguration<PasswordRecovery>
    {
        public override void Configure(EntityTypeBuilder<PasswordRecovery> builder)
        {
            builder.ToTable("PasswordRecovery");
            base.Configure(builder);
            builder.Property(x=>x.TokenHash).HasMaxLength(128).IsRequired();
            builder.HasIndex(x=>x.TokenHash).IsUnique();
            builder.HasOne<User>().WithMany().HasForeignKey(x=>x.IdUser).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
