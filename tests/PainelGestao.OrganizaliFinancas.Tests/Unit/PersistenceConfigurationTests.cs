using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Infrastructure.Database.Factories;

namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    public class PersistenceConfigurationTests
    {
        [Fact]
        public void Schema_UsesFoundationIdentityAndAuditWithoutOpeningConnection()
        {
            using var context = new OrganizaliFinancasDbContextFactory().CreateDbContext([]);
            var entity = context.Model.FindEntityType(typeof(Tenant))!;
            entity.GetTableName().Should().Be("Tenant");
            entity.FindProperty("Name")!.GetMaxLength().Should().Be(160);
            entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Single().Name == "Code");
            context.Database.GenerateCreateScript().Should().Contain("CREATE TABLE");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Tenant_RejectsMissingName(string name)
        {
            var create = () => Tenant.Create(name);
            create.Should().Throw<ArgumentException>();
        }
    }
}
