using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using Foundation.Domain.Abstractions;
using Foundation.Domain.Interfaces.UnitOfWork;
using NetArchTest.Rules;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;

namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    public class ArchitectureTests
    {
        private const string Prefix = "PainelGestao.OrganizaliFinancas";

        [Theory]
        [InlineData("Domain", new[] { "Application", "Infrastructure", "Api" })]
        [InlineData("Application", new[] { "Infrastructure", "Api" })]
        [InlineData("Infrastructure", new[] { "Application", "Api" })]
        [InlineData("Shared", new[] { "Domain", "Application", "Infrastructure", "Api" })]
        public void Layers_DoNotUseForbiddenDependencies(string layer, string[] forbidden)
        {
            var assembly = Assembly.Load($"{Prefix}.{layer}");
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden.Select(name => $"{Prefix}.{name}").ToArray())
                .GetResult();

            (result.FailingTypeNames ?? []).Should().BeEmpty();
        }

        [Theory]
        [InlineData("Domain", new[] { "Shared" })]
        [InlineData("Application", new[] { "Domain", "Shared" })]
        [InlineData("Infrastructure", new[] { "Domain", "Shared" })]
        [InlineData("Api", new[] { "Application", "Infrastructure", "Shared" })]
        [InlineData("Shared", new string[] { })]
        public void ProjectReferences_RespectLayerBoundariesEvenWhenUnused(string layer, string[] allowed)
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(root, "src", $"{Prefix}.{layer}", $"{Prefix}.{layer}.csproj");
            var references = XDocument.Load(path).Descendants("ProjectReference")
                .Select(reference => Path.GetFileNameWithoutExtension((string?)reference.Attribute("Include")))
                .ToArray();

            references.Except(allowed.Select(item => $"{Prefix}.{item}"))
                .Should().BeEmpty("project references must respect the permitted layer dependencies");
        }

        [Theory]
        [InlineData("Domain")]
        [InlineData("Application")]
        [InlineData("Api")]
        [InlineData("Shared")]
        public void RuntimePersistenceLibraries_StayOutsideInnerLayersAndApi(string layer)
        {
            var result = Types.InAssembly(Assembly.Load($"{Prefix}.{layer}"))
                .ShouldNot()
                .HaveDependencyOnAny("Dapper", "Microsoft.EntityFrameworkCore")
                .GetResult();

            (result.FailingTypeNames ?? []).Should().BeEmpty();
        }

        [Fact]
        public void TenantEntity_UsesFoundationIdentityAndAudit()
        {
            typeof(TenantEntity).BaseType.Should().Be(typeof(Entity));
            typeof(TenantEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(property => property.Name)
                .Should().Equal("IdTenant");
            typeof(IDapperUnitOfWork).IsAssignableFrom(typeof(IUnitOfWork)).Should().BeTrue();
        }

        [Fact]
        public void Api_DoesNotIntroduceControllers()
        {
            var types = Assembly.Load($"{Prefix}.Api").GetTypes();

            types.Should().NotContain(type => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, $"{Prefix}.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Run architecture tests inside the repository checkout.");
        }
    }
}
