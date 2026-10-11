using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Expenses;
using PainelGestao.OrganizaliFinancas.Application.Income;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services;
using PainelGestao.OrganizaliFinancas.Shared.Settings;
namespace PainelGestao.OrganizaliFinancas.Application.Planning
{
    /// <summary>Platform routine, never exposed over HTTP: it only rolls each household's own rules forward and reads no other data.</summary>
    public sealed record RollHorizonCommand(string Actor) : IRequest<HorizonRollDto>;
    public sealed record HorizonRollDto(bool Skipped, int Households, int Inserted, int Updated, int Cancelled);

    /// <summary>
    /// Keeps the D23 horizon current for every active household (income forecasts and recurring bills), one transaction per household.
    /// A named database lock makes it safe when several API replicas start the routine at the same time; generation is idempotent anyway.
    /// </summary>
    public class RollHorizonHandler(IUnitOfWork work, IJobLock jobLock, OrganizaliFinancasSettings settings) : IRequestHandler<RollHorizonCommand, HorizonRollDto>
    {
        public const string LockName = "organizali_horizon";
        public async Task<HorizonRollDto> Handle(RollHorizonCommand request, CancellationToken ct)
        {
            var handle = await jobLock.TryAcquireAsync(LockName, ct);
            if (handle is null) return new HorizonRollDto(true, 0, 0, 0, 0);
            await using (handle)
            {
                int inserted = 0, updated = 0, cancelled = 0;
                var households = await work.PilotRepository.ActiveHouseholdIdsAsync(ct);
                foreach (var idTenant in households)
                {
                    var (income, expenses) = await work.ExecuteInTransactionAsync(async () =>
                    {
                        await work.MemberRepository.LockHouseholdAsync(idTenant, ct);
                        return (await ForecastGenerator.GenerateAllAsync(work, idTenant, settings.ForecastHorizonMonths, request.Actor, ct),
                            await GenerateRecurrencesHandler.GenerateAllAsync(work, idTenant, settings.ForecastHorizonMonths, request.Actor, ct));
                    }, ct);
                    inserted += income.Inserted + expenses.Inserted;
                    updated += income.Updated + expenses.Updated;
                    cancelled += income.Cancelled + expenses.Cancelled;
                }
                return new HorizonRollDto(false, households.Count, inserted, updated, cancelled);
            }
        }
    }
}
