using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    public interface IHouseholdRepository
    {
        Task<IReadOnlyList<Person>> ListPeopleAsync(int idTenant, CancellationToken ct);
        Task<Person?> FindPersonAsync(int idTenant, Guid code, CancellationToken ct);
        Task<HouseholdSettings?> GetSettingsAsync(int idTenant, CancellationToken ct);
        void AddPerson(Person person);
        void UpdatePerson(Person person);
        void SaveSettings(HouseholdSettings settings, bool exists);
    }
}
