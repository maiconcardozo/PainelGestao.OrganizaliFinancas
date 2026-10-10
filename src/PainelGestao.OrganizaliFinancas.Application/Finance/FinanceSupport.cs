using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Finance
{
    public sealed record ReversalPayload(string Reason, DateOnly? ReversalDate);

    /// <summary>Civil dates follow the household time zone (D19), never the server clock.</summary>
    public static class HouseholdCalendar
    {
        public static async Task<DateOnly> TodayAsync(IUnitOfWork work, int idTenant, CancellationToken ct)
        {
            var zone = (await work.HouseholdRepository.GetSettingsAsync(idTenant, ct))?.TimeZone ?? "America/Sao_Paulo";
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, zone));
        }
        public static void EnsureNotFuture(DateOnly date, DateOnly today, string field)
        {
            if (date > today) throw new InputValidationException(new Dictionary<string, string[]> { [field] = ["Movimentos efetivos não podem ter data futura."] });
        }
        public static DateTime Day(DateOnly date) => date.ToDateTime(TimeOnly.MinValue);
    }

    /// <summary>Per household/operation/key with a payload hash; repeating the same request returns the original result.</summary>
    public static class Idempotency
    {
        public static string Hash(params object?[] parts) =>
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(parts)));
        public static async Task<Guid?> ReplayAsync(IUnitOfWork work, int idTenant, string operation, Guid key, string hash, CancellationToken ct)
        {
            var record = await work.IdempotencyRepository.FindAsync(idTenant, operation, key, ct);
            if (record is null) return null;
            if (record.RequestHash != hash) throw new ConflictException("Esta chave de idempotência já foi usada com outros dados.");
            return record.ResultCode;
        }
        public static void Record(IUnitOfWork work, int idTenant, string operation, Guid key, string hash, Guid result, string actor) =>
            work.IdempotencyRepository.Add(IdempotencyRecord.Create(idTenant, operation, key, hash, result, actor));
    }

    public static class MoneyRules
    {
        public static IRuleBuilderOptions<T, string?> PositiveMoney<T>(this IRuleBuilder<T, string?> rule) =>
            rule.Must(Money.IsPositive).WithMessage("Informe um valor positivo com até duas casas decimais, por exemplo 1250.50.");
        public static IRuleBuilderOptions<T, string?> SignedMoney<T>(this IRuleBuilder<T, string?> rule) =>
            rule.Must(value => Money.TryParse(value, out _)).WithMessage("Informe um valor com até duas casas decimais, por exemplo -150.00.");
        public static string? Format(decimal? value) => value is decimal amount ? Money.Format(amount) : null;
    }
}
