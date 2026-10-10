using Foundation.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Persistence.Mappings
{
    public class PersonMap : EntityMap<Person>, IEntityTypeConfiguration<Person>
    {
        public override void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.ToTable("Person"); base.Configure(builder);
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.HasAlternateKey(x => new { x.IdTenant, x.Id });
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class HouseholdSettingsMap : EntityMap<HouseholdSettings>, IEntityTypeConfiguration<HouseholdSettings>
    {
        public override void Configure(EntityTypeBuilder<HouseholdSettings> builder)
        {
            builder.ToTable("HouseholdSettings"); base.Configure(builder);
            builder.Property(x => x.TimeZone).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Language).HasMaxLength(10).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.StartDate).HasColumnType("date");
            builder.HasIndex(x => x.IdTenant).IsUnique();
            builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.IdTenant).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
