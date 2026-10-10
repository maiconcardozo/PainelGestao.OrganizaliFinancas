using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Application.Expenses
{
    /// <summary>Value is money ("50.00") when splitting by value or a percentage ("33.3333") when splitting by percent.</summary>
    public sealed record SharePayload(Guid PersonCode, string Value);
    /// <summary>Descriptive data common to a single bill, a recurrence and an installment purchase.</summary>
    public sealed record ExpenseDetailsPayload(string Description, Guid? ExpenseTypeCode, int? ExpenseTypeVersion, Guid? CategoryCode, Guid? CreditorCode,
        Guid? PaymentMethodCode, Guid? PayerPersonCode, Dictionary<string, JsonElement>? Fields, string? SplitMode, SharePayload[]? Shares);
    public sealed record ImmediatePaymentPayload(Guid AccountCode, DateOnly PaymentDate, Guid? PayerPersonCode);
    public sealed record ExpensePayload(ExpenseDetailsPayload Details, DateOnly? Competence, DateOnly DueDate, string Amount, int Version, ImmediatePaymentPayload? Payment);
    public sealed record CancelExpensePayload(string Reason, int Version);
    public sealed record AdjustmentPayload(string Kind, string Amount, string Reason, DateOnly? Date);
    public sealed record PaymentAllocationPayload(Guid ExpenseCode, string Amount, string? Interest, string? Fine, string? Discount);
    public sealed record PaymentPayload(Guid AccountCode, DateOnly PaymentDate, Guid? PayerPersonCode, Guid? PaymentMethodCode, string? Description,
        PaymentAllocationPayload[] Allocations, string? AdjustmentReason);
    public sealed record RecurrenceTermsPayload(DateOnly ValidFrom, DateOnly? EndDate, string Frequency, string Amount, bool IsEstimated, bool IsActive);
    public sealed record RecurrencePayload(ExpenseDetailsPayload Details, RecurrenceTermsPayload Terms, int Version);
    public sealed record InstallmentPreviewPayload(string Total, int Count, DateOnly FirstDueDate, string Frequency);
    public sealed record InstallmentPayload(ExpenseDetailsPayload Details, InstallmentPreviewPayload Plan);

    public sealed record ShareDto(Guid PersonCode, string PersonName, string Amount, string? Percent);
    public sealed record ExpenseDto(Guid Code, string Description, string Competence, DateOnly DueDate, string Amount, string Adjustments, string Paid,
        string OpenBalance, string Status, bool IsOverdue, bool IsEstimated, string? CancelReason, Guid? ExpenseTypeCode, string? ExpenseTypeName,
        int? ExpenseTypeVersion, Guid? CategoryCode, string? CategoryName, Guid? CreditorCode, string? CreditorName, Guid? PaymentMethodCode,
        Guid? PayerPersonCode, string? PayerPersonName, Guid? RecurrenceCode, bool IsDetached, Guid? InstallmentPlanCode, int? InstallmentNumber,
        int? InstallmentCount, string? SplitMode, IReadOnlyList<ShareDto> Shares, JsonElement Fields, int Version);
    /// <summary>Totals cover the obligations due in the period; overdue items from earlier periods are listed and summed separately.</summary>
    public sealed record ExpenseListDto(DateOnly From, DateOnly To, DateOnly Today, string Total, string Paid, string Open, string Overdue, IReadOnlyList<ExpenseDto> Items);
    public sealed record AdjustmentDto(Guid Code, string Kind, string Amount, DateOnly Date, string Reason);
    public sealed record PaymentAllocationDto(Guid PaymentCode, Guid ExpenseCode, string ExpenseDescription, string Amount, DateOnly PaymentDate, string AccountName, bool IsReversed);
    public sealed record ExpenseDetailDto(ExpenseDto Expense, IReadOnlyList<AdjustmentDto> Adjustments, IReadOnlyList<PaymentAllocationDto> Payments);
    public sealed record PaymentDto(Guid Code, Guid AccountCode, string AccountName, Guid? PayerPersonCode, string? PayerPersonName, Guid? PaymentMethodCode,
        DateOnly PaymentDate, string Amount, string Description, bool IsReversed, string? ReversalReason, IReadOnlyList<PaymentAllocationDto> Allocations);
    public sealed record RecurrenceDto(Guid Code, string Description, bool IsActive, int Version, DateOnly ValidFrom, DateOnly? EndDate, string Frequency,
        string Amount, bool IsEstimated, Guid? ExpenseTypeCode, int? ExpenseTypeVersion, Guid? CategoryCode, string? CategoryName, Guid? CreditorCode,
        Guid? PaymentMethodCode, Guid? PayerPersonCode, string? PayerPersonName, string? SplitMode, IReadOnlyList<SharePayload> Shares, JsonElement Fields);
    public sealed record OccurrencePreviewDto(DateOnly Date, string Amount, string Change);
    /// <summary>Preserved occurrences were paid, adjusted, cancelled or edited individually and keep their values.</summary>
    public sealed record RecurrencePlanDto(int Inserted, int Updated, int Cancelled, int Preserved, IReadOnlyList<OccurrencePreviewDto> Next);
    public sealed record RecurrenceSaveDto(RecurrenceDto Recurrence, RecurrencePlanDto Plan);
    public sealed record InstallmentPreviewItemDto(int Number, DateOnly DueDate, string Competence, string Amount);
    public sealed record InstallmentPreviewDto(string Total, int Count, IReadOnlyList<InstallmentPreviewItemDto> Items);
    public sealed record InstallmentPlanDto(Guid Code, string Description, string Total, int Count, IReadOnlyList<ExpenseDto> Installments);
    public sealed record ContributionDto(Guid PersonCode, string PersonName, string Responsible, string Paid, string Balance);
    public sealed record SettlementDto(Guid FromPersonCode, string FromPersonName, Guid ToPersonCode, string ToPersonName, string Amount);
    public sealed record ContributionsDto(string Month, IReadOnlyList<ContributionDto> People, IReadOnlyList<SettlementDto> Settlements);

    public sealed record ListExpensesQuery(SessionIdentity Identity, DateOnly? From, DateOnly? To, bool IncludeOverdue) : IRequest<ExpenseListDto>;
    public sealed record GetExpenseQuery(SessionIdentity Identity, Guid Code) : IRequest<ExpenseDetailDto>;
    public sealed record CreateExpenseCommand(SessionIdentity Identity, Guid Key, ExpensePayload Payload) : IRequest<ExpenseDto>;
    public sealed record UpdateExpenseCommand(SessionIdentity Identity, Guid Code, ExpensePayload Payload) : IRequest<ExpenseDto>;
    public sealed record CancelExpenseCommand(SessionIdentity Identity, Guid Code, CancelExpensePayload Payload) : IRequest<ExpenseDto>;
    public sealed record AddAdjustmentCommand(SessionIdentity Identity, Guid Code, AdjustmentPayload Payload) : IRequest<ExpenseDetailDto>;
    public sealed record ListPaymentsQuery(SessionIdentity Identity, DateOnly? From, DateOnly? To) : IRequest<IReadOnlyList<PaymentDto>>;
    public sealed record CreatePaymentCommand(SessionIdentity Identity, Guid Key, PaymentPayload Payload) : IRequest<PaymentDto>;
    public sealed record ReversePaymentCommand(SessionIdentity Identity, Guid Key, Guid Code, ReversalPayload Payload) : IRequest<PaymentDto>;
    public sealed record ListRecurrencesQuery(SessionIdentity Identity) : IRequest<IReadOnlyList<RecurrenceDto>>;
    public sealed record PreviewRecurrenceCommand(SessionIdentity Identity, Guid? Code, RecurrenceTermsPayload Terms) : IRequest<RecurrencePlanDto>;
    public sealed record CreateRecurrenceCommand(SessionIdentity Identity, Guid Key, RecurrencePayload Payload) : IRequest<RecurrenceSaveDto>;
    public sealed record UpdateRecurrenceCommand(SessionIdentity Identity, Guid Code, RecurrencePayload Payload) : IRequest<RecurrenceSaveDto>;
    public sealed record GenerateRecurrencesCommand(SessionIdentity Identity) : IRequest<RecurrencePlanDto>;
    public sealed record PreviewInstallmentsQuery(SessionIdentity Identity, InstallmentPreviewPayload Payload) : IRequest<InstallmentPreviewDto>;
    public sealed record CreateInstallmentsCommand(SessionIdentity Identity, Guid Key, InstallmentPayload Payload) : IRequest<InstallmentPlanDto>;
    public sealed record GetContributionsQuery(SessionIdentity Identity, DateOnly? Month) : IRequest<ContributionsDto>;

    public static partial class ShareRules
    {
        [GeneratedRegex(@"^\d{1,3}(\.\d{1,4})?$", RegexOptions.CultureInvariant)]
        public static partial Regex Percent();
        public static bool IsValidValue(string? mode, string? value) =>
            mode == "percent" ? value is not null && Percent().IsMatch(value) : Money.IsPositive(value);
    }
    /// <summary>Abstract so assembly scanning registers only the concrete variants below.</summary>
    public abstract class ExpenseDetailsValidator : AbstractValidator<ExpenseDetailsPayload>
    {
        protected ExpenseDetailsValidator(bool percentOnly)
        {
            RuleFor(x => x.Description).NotEmpty().MaximumLength(200).WithMessage("Informe a descrição com até 200 caracteres.");
            RuleFor(x => x.ExpenseTypeVersion).NotNull().GreaterThan(0).When(x => x.ExpenseTypeCode is not null).WithMessage("Informe a versão do tipo de despesa.");
            RuleFor(x => x.Fields).Must(fields => fields is null || fields.Count <= 30).WithMessage("Use até 30 campos.");
            RuleFor(x => x.Shares).Must(items => items is null || items.Length <= 20).WithMessage("Divida entre até 20 moradores.");
            RuleFor(x => x.SplitMode).Must(Shares.IsMode).When(x => x.Shares is { Length: > 0 }).WithMessage("Escolha dividir por valor ou por percentual.");
            if (percentOnly)
                RuleFor(x => x.SplitMode).Equal("percent").When(x => x.Shares is { Length: > 0 })
                    .WithMessage("Recorrências e parcelamentos dividem por percentual, pois o valor de cada ocorrência pode mudar.");
            RuleForEach(x => x.Shares).Must((details, share) => ShareRules.IsValidValue(details.SplitMode, share.Value))
                .WithMessage("Informe cada parte como valor positivo (50.00) ou percentual (até 4 casas, ex.: 33.3333).");
        }
    }
    public sealed class SingleExpenseDetailsValidator() : ExpenseDetailsValidator(false);
    public sealed class TemplateExpenseDetailsValidator() : ExpenseDetailsValidator(true);
    public class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
    {
        public CreateExpenseValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Details).NotNull().SetValidator(new SingleExpenseDetailsValidator());
            RuleFor(x => x.Payload.Amount).PositiveMoney();
            RuleFor(x => x.Payload.DueDate).Must(date => date.Year >= 2000).WithMessage("Vencimento inválido.");
        }
    }
    public class UpdateExpenseValidator : AbstractValidator<UpdateExpenseCommand>
    {
        public UpdateExpenseValidator()
        {
            RuleFor(x => x.Payload.Details).NotNull().SetValidator(new SingleExpenseDetailsValidator());
            RuleFor(x => x.Payload.Amount).PositiveMoney();
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Payload.Payment).Null().WithMessage("Registre o pagamento pela tela de pagamento.");
        }
    }
    public class CancelExpenseValidator : AbstractValidator<CancelExpenseCommand>
    {
        public CancelExpenseValidator() => RuleFor(x => x.Payload.Reason).NotEmpty().MaximumLength(300).WithMessage("Informe o motivo do cancelamento.");
    }
    public class AddAdjustmentValidator : AbstractValidator<AddAdjustmentCommand>
    {
        public AddAdjustmentValidator()
        {
            RuleFor(x => x.Payload.Kind).Must(kind => Domain.Entities.ObligationAdjustment.Kinds.Contains(kind)).WithMessage("Escolha juros, multa, desconto ou ajuste.");
            RuleFor(x => x.Payload.Amount).SignedMoney().Must(value => value != "0" && value != "0.00").WithMessage("Informe um valor diferente de zero.");
            RuleFor(x => x.Payload.Reason).NotEmpty().MaximumLength(300).WithMessage("Informe a justificativa.");
        }
    }
    public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
    {
        public CreatePaymentValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Allocations).NotEmpty().WithMessage("Escolha ao menos uma despesa.")
                .Must(items => items is null || items.Length <= 50).WithMessage("Pague até 50 despesas por vez.")
                .Must(items => items is null || items.Select(x => x.ExpenseCode).Distinct().Count() == items.Length).WithMessage("Cada despesa aparece uma única vez.");
            RuleForEach(x => x.Payload.Allocations).ChildRules(item =>
            {
                item.RuleFor(x => x.Amount).PositiveMoney();
                item.RuleFor(x => x.Interest).PositiveMoney().When(x => x.Interest is not null);
                item.RuleFor(x => x.Fine).PositiveMoney().When(x => x.Fine is not null);
                item.RuleFor(x => x.Discount).PositiveMoney().When(x => x.Discount is not null);
            });
            RuleFor(x => x.Payload.AdjustmentReason).NotEmpty().MaximumLength(300)
                .When(x => x.Payload.Allocations?.Any(a => a.Interest is not null || a.Fine is not null || a.Discount is not null) == true)
                .WithMessage("Informe a justificativa dos juros, multa ou desconto.");
            RuleFor(x => x.Payload.Description).MaximumLength(200);
        }
    }
    public class ReversePaymentValidator : AbstractValidator<ReversePaymentCommand>
    {
        public ReversePaymentValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Reason).NotEmpty().MaximumLength(300).WithMessage("Informe a justificativa do estorno.");
        }
    }
    public class RecurrenceTermsValidator : AbstractValidator<RecurrenceTermsPayload>
    {
        public RecurrenceTermsValidator()
        {
            RuleFor(x => x.Frequency).Must(IncomeFrequency.IsValid).WithMessage("Periodicidade inválida.");
            RuleFor(x => x.Amount).PositiveMoney();
        }
    }
    public class PreviewRecurrenceValidator : AbstractValidator<PreviewRecurrenceCommand>
    {
        public PreviewRecurrenceValidator() => RuleFor(x => x.Terms).NotNull().SetValidator(new RecurrenceTermsValidator());
    }
    public class CreateRecurrenceValidator : AbstractValidator<CreateRecurrenceCommand>
    {
        public CreateRecurrenceValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Details).NotNull().SetValidator(new TemplateExpenseDetailsValidator());
            RuleFor(x => x.Payload.Terms).NotNull().SetValidator(new RecurrenceTermsValidator());
            RuleFor(x => x.Payload.Terms.IsActive).Equal(true).When(x => x.Payload.Terms is not null).WithMessage("Uma nova recorrência deve estar ativa.");
        }
    }
    public class UpdateRecurrenceValidator : AbstractValidator<UpdateRecurrenceCommand>
    {
        public UpdateRecurrenceValidator()
        {
            RuleFor(x => x.Payload.Details).NotNull().SetValidator(new TemplateExpenseDetailsValidator());
            RuleFor(x => x.Payload.Terms).NotNull().SetValidator(new RecurrenceTermsValidator());
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
        }
    }
    public class InstallmentPreviewValidator : AbstractValidator<InstallmentPreviewPayload>
    {
        public InstallmentPreviewValidator()
        {
            RuleFor(x => x.Total).PositiveMoney();
            RuleFor(x => x.Count).InclusiveBetween(2, Installments.MaxCount).WithMessage($"Informe de 2 a {Installments.MaxCount} parcelas.");
            RuleFor(x => x.Frequency).Must(IncomeFrequency.IsValid).WithMessage("Periodicidade inválida.");
            RuleFor(x => x.FirstDueDate).Must(date => date.Year >= 2000).WithMessage("Primeiro vencimento inválido.");
        }
    }
    public class PreviewInstallmentsValidator : AbstractValidator<PreviewInstallmentsQuery>
    {
        public PreviewInstallmentsValidator() => RuleFor(x => x.Payload).NotNull().SetValidator(new InstallmentPreviewValidator());
    }
    public class CreateInstallmentsValidator : AbstractValidator<CreateInstallmentsCommand>
    {
        public CreateInstallmentsValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Details).NotNull().SetValidator(new TemplateExpenseDetailsValidator());
            RuleFor(x => x.Payload.Plan).NotNull().SetValidator(new InstallmentPreviewValidator());
        }
    }
}
