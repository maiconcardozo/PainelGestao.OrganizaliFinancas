using Dapper;
using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Households;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Households
{
    public class HouseholdRepository(IDapperUnitOfWork work) : IHouseholdRepository
    {
        public async Task<IReadOnlyList<Person>> ListPeopleAsync(int idTenant, CancellationToken ct) =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<Person>(new CommandDefinition(
                HouseholdSql.People, new { IdTenant = idTenant }, transaction, cancellationToken: ct)), ct)).ToArray();
        public Task<Person?> FindPersonAsync(int idTenant, Guid code, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<Person>(new CommandDefinition(
                HouseholdSql.Person, new { IdTenant = idTenant, Code = code }, transaction, cancellationToken: ct)), ct);
        public Task<HouseholdSettings?> GetSettingsAsync(int idTenant, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<HouseholdSettings>(new CommandDefinition(
                HouseholdSql.Settings, new { IdTenant = idTenant }, transaction, cancellationToken: ct)), ct);
        public void AddPerson(Person person) => new DapperRepository<Person>(work).Add(person);
        public void UpdatePerson(Person person) => work.AddPendingWrite(HouseholdSql.UpdatePerson, person);
        public void SaveSettings(HouseholdSettings settings, bool exists)
        {
            if (exists) work.AddPendingWrite(HouseholdSql.UpdateSettings, settings);
            else new DapperRepository<HouseholdSettings>(work).Add(settings);
        }
    }
}
