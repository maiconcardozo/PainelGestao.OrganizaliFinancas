using System.Globalization;
using System.Text.Json;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Expenses
{
    internal sealed record ResolvedDetails(ObligationDetails Details, IReadOnlyList<ShareInput> Shares);

    /// <summary>Resolves public codes to household rows and applies the expense type version (M3.3) to the informed field values.</summary>
    internal static class ExpenseDetailsResolver
    {
        private static readonly Dictionary<string, string> Behaviors = new()
        {
            ["single"] = "conta avulsa", ["recurring"] = "conta recorrente", ["installments"] = "compra parcelada", ["card"] = "compra no cartão"
        };

        /// <param name="previous">On edits, references kept unchanged may be inactive; new references must be active.</param>
        public static async Task<ResolvedDetails> ResolveAsync(IUnitOfWork work, int idTenant, ExpenseDetailsPayload payload, string behavior,
            ObligationDetails? previous, CancellationToken ct)
        {
            int? idType = null, typeVersion = null, idCategory = null;
            var fields = payload.Fields ?? [];
            var fieldsJson = "{}";
            Guid? categoryCode = payload.CategoryCode;
            if (payload.ExpenseTypeCode is Guid typeCode)
            {
                var type = await work.ExpenseTypeRepository.FindAsync(idTenant, typeCode, ct) ?? throw new NotFoundException("Tipo de despesa não encontrado.");
                var version = await work.ExpenseTypeRepository.FindVersionAsync(idTenant, typeCode, payload.ExpenseTypeVersion!.Value, ct)
                    ?? throw new NotFoundException("Versão do tipo de despesa não encontrada.");
                if (!type.IsActive && previous?.IdExpenseType != type.Id) throw new ConflictException("Escolha um tipo de despesa ativo.");
                if (version.Behavior != behavior)
                    throw new ConflictException(version.Behavior == "card"
                        ? $"O tipo {version.Name} é de compra no cartão; lance pela tela de cartões."
                        : $"O tipo {version.Name} é de {Behaviors[version.Behavior]}; use o formulário de {Behaviors[version.Behavior]}.");
                CustomFields.ValidateValues(JsonSerializer.Deserialize<CustomFieldDefinition[]>(version.FieldsJson)!, fields);
                (idType, typeVersion) = (type.Id, version.Version);
                fieldsJson = JsonSerializer.Serialize(fields);
                categoryCode ??= version.DefaultCategoryCode;
            }
            else if (fields.Count > 0) throw new InputValidationException(new Dictionary<string, string[]> { ["Fields"] = ["Campos adicionais exigem um tipo de despesa."] });
            if (categoryCode is Guid category) idCategory = await CatalogIdAsync(work, CatalogKind.Categories, idTenant, category, previous?.IdCategory, "Categoria", ct);
            var idCreditor = payload.CreditorCode is Guid creditor ? await CatalogIdAsync(work, CatalogKind.Creditors, idTenant, creditor, previous?.IdCreditor, "Credor", ct) : (int?)null;
            var idMethod = payload.PaymentMethodCode is Guid method ? await CatalogIdAsync(work, CatalogKind.PaymentMethods, idTenant, method, previous?.IdPaymentMethod, "Forma de pagamento", ct) : (int?)null;
            int? idPayer = null;
            if (payload.PayerPersonCode is Guid payerCode)
                idPayer = (await PersonAsync(work, idTenant, payerCode, previous?.IdPayerPerson, ct)).Id;
            var shares = new List<ShareInput>();
            foreach (var share in payload.Shares ?? [])
            {
                var person = await PersonAsync(work, idTenant, share.PersonCode, null, ct);
                shares.Add(new ShareInput(person.Code, decimal.Parse(share.Value, CultureInfo.InvariantCulture)));
            }
            var mode = shares.Count > 0 ? payload.SplitMode : null;
            return new ResolvedDetails(new ObligationDetails(payload.Description, idType, typeVersion, idCategory, idCreditor, idMethod, idPayer, fieldsJson, mode), shares);
        }
        private static async Task<int> CatalogIdAsync(IUnitOfWork work, CatalogKind kind, int idTenant, Guid code, int? previous, string label, CancellationToken ct)
        {
            var item = await work.CatalogRepository.FindAsync(kind, idTenant, code, ct) ?? throw new NotFoundException($"{label} não encontrado nesta casa.");
            if (!item.IsActive && previous != item.Id) throw new ConflictException($"{label} está inativo; escolha um item ativo.");
            return item.Id;
        }
        private static async Task<Person> PersonAsync(IUnitOfWork work, int idTenant, Guid code, int? previous, CancellationToken ct)
        {
            var person = await work.HouseholdRepository.FindPersonAsync(idTenant, code, ct) ?? throw new NotFoundException("Morador não encontrado nesta casa.");
            if (!person.IsActive && previous != person.Id) throw new ConflictException($"{person.Name} está inativo; escolha um morador ativo.");
            return person;
        }
        public static string SharesJson(IReadOnlyList<ShareInput> shares) =>
            JsonSerializer.Serialize(shares.Select(x => new SharePayload(x.PersonCode, x.Value.ToString(CultureInfo.InvariantCulture))));
        public static IReadOnlyList<ShareInput> ParseShares(string json) =>
            (JsonSerializer.Deserialize<SharePayload[]>(json) ?? []).Select(x => new ShareInput(x.PersonCode, decimal.Parse(x.Value, CultureInfo.InvariantCulture))).ToArray();
    }

    internal static class ExpenseMapper
    {
        public static JsonElement Fields(string json) => JsonDocument.Parse(json).RootElement.Clone();
        public static ExpenseDto Map(ObligationRecord x, IEnumerable<ShareRecord> shares, DateOnly today)
        {
            var state = ObligationBalance.Of(x.Amount, x.Adjustments, x.Paid, x.Status == "cancelled", DateOnly.FromDateTime(x.DueDate), today);
            return new ExpenseDto(x.Code, x.Description, x.Competence.ToString("yyyy-MM"), DateOnly.FromDateTime(x.DueDate), Money.Format(x.Amount),
                Money.Format(x.Adjustments), Money.Format(x.Paid), Money.Format(state.OpenBalance), state.Status, state.IsOverdue, x.IsEstimated, x.CancelReason,
                x.ExpenseTypeCode, x.ExpenseTypeName, x.ExpenseTypeVersion, x.CategoryCode, x.CategoryName, x.CreditorCode, x.CreditorName, x.PaymentMethodCode,
                x.PayerPersonCode, x.PayerPersonName, x.RecurrenceCode, x.IsDetached, x.InstallmentPlanCode, x.InstallmentNumber, x.InstallmentCount, x.CardInvoiceCode, x.SplitMode,
                shares.Where(s => s.ObligationCode == x.Code).Select(s => new ShareDto(s.PersonCode, s.PersonName, Money.Format(s.Amount),
                    s.Percent is decimal percent ? percent.ToString("0.####", CultureInfo.InvariantCulture) : null)).ToArray(),
                Fields(x.FieldValuesJson), x.Version);
        }
        public static async Task<IReadOnlyList<ExpenseDto>> ReadAsync(IUnitOfWork work, int idTenant, IReadOnlyList<ObligationRecord> records, DateOnly today, CancellationToken ct)
        {
            var shares = await work.ExpenseRepository.ListSharesAsync(idTenant, records.Select(x => x.Code).ToArray(), ct);
            return records.Select(x => Map(x, shares, today)).ToArray();
        }
        public static async Task<ExpenseDto> GetAsync(IUnitOfWork work, int idTenant, Guid code, CancellationToken ct)
        {
            var record = await work.ExpenseRepository.GetAsync(idTenant, code, ct) ?? throw new NotFoundException("Despesa não encontrada.");
            var today = await HouseholdCalendar.TodayAsync(work, idTenant, ct);
            return (await ReadAsync(work, idTenant, [record], today, ct))[0];
        }
        public static PaymentAllocationDto Allocation(PaymentAllocationRecord x) => new(x.PaymentCode, x.ExpenseCode, x.ExpenseDescription, Money.Format(x.Amount),
            DateOnly.FromDateTime(x.PaymentDate), x.AccountName, x.IsReversed);
        public static PaymentDto Payment(PaymentRecord x, IEnumerable<PaymentAllocationRecord> allocations) => new(x.Code, x.AccountCode, x.AccountName,
            x.PayerPersonCode, x.PayerPersonName, x.PaymentMethodCode, DateOnly.FromDateTime(x.PaymentDate), Money.Format(x.Amount), x.Description,
            x.DtReversed is not null, x.ReversalReason, allocations.Where(a => a.PaymentCode == x.Code).Select(Allocation).ToArray());
    }

    /// <summary>Applies the idempotent occurrence plan of one recurring expense inside the buffered transaction.</summary>
    internal static class RecurrenceGenerator
    {
        public static async Task<RecurrencePlanDto> SyncAsync(IUnitOfWork work, RecurringExpense rule, IReadOnlyList<ExpenseTermsVersion> versions, bool isNew,
            DateOnly today, int horizonMonths, string actor, bool apply, CancellationToken ct)
        {
            var (from, to) = Recurrence.Window(today, horizonMonths);
            var expected = ExpenseSchedule.Expected(versions, from, to);
            // An ended recurrence keeps its past occurrences and stops projecting the future.
            if (!rule.IsActive) expected = expected.Where(x => x.Date < today).ToArray();
            var existing = isNew ? [] : (await work.ExpenseRepository.ListOccurrenceStatesAsync(rule.IdTenant, rule.Id, ct)).Select(x => x.ToState()).ToArray();
            var plan = Recurrence.Plan(existing, expected, from, to);
            var estimated = versions.ToDictionary(x => x.Version, x => x.Terms.IsEstimated);
            if (apply)
            {
                var shares = ExpenseDetailsResolver.ParseShares(rule.SharesJson);
                foreach (var occurrence in plan.Inserts)
                {
                    var code = Guid.CreateVersion7();
                    work.ExpenseRepository.InsertOccurrence(rule, code, occurrence, estimated[occurrence.Version], actor);
                    work.ExpenseRepository.ReplaceShares(rule.IdTenant, code, Shares.Resolve(occurrence.Amount!.Value, rule.SplitMode, shares), actor);
                }
                foreach (var (code, occurrence) in plan.Updates)
                {
                    work.ExpenseRepository.UpdateOccurrence(rule, code, occurrence, estimated[occurrence.Version], actor);
                    work.ExpenseRepository.ReplaceShares(rule.IdTenant, code, Shares.Resolve(occurrence.Amount!.Value, rule.SplitMode, shares), actor);
                }
                foreach (var code in plan.Cancels) work.ExpenseRepository.CancelOccurrence(rule.IdTenant, code, actor);
            }
            var changed = plan.Updates.Select(x => x.Code).ToHashSet();
            var lockedDates = existing.Where(x => x.Locked).Select(x => x.Date).ToHashSet();
            var preserved = expected.Count(x => lockedDates.Contains(x.Date) && x.Date >= today);
            var next = expected.Where(x => x.Date >= today).Take(12).Select(x =>
            {
                var current = existing.FirstOrDefault(e => e.Date == x.Date);
                var change = current is null ? "new" : current.Locked ? "preserved" : changed.Contains(current.Code) ? "update" : "keep";
                var amount = current is { Locked: true } ? current.ExpectedAmount ?? x.Amount : x.Amount;
                return new OccurrencePreviewDto(x.Date, Money.Format(amount ?? 0), change);
            }).ToArray();
            return new RecurrencePlanDto(plan.Inserts.Count, plan.Updates.Count, plan.Cancels.Count, preserved, next);
        }
    }
}
