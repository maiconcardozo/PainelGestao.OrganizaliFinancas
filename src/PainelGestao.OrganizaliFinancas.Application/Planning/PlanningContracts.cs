using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Application.Planning
{
    public sealed record BudgetLineDto(Guid? CategoryCode, string Name, Guid? ParentCode, int Depth, string? Limit, string Committed, string Paid, string Card,
        string? UsedPercent, string Band, string Excess, bool CountsInTotal);
    public sealed record BudgetDto(string Month, int WarningPercent, int LimitPercent, string TotalLimit, string TotalCommitted, string TotalPaid, string Unbudgeted,
        IReadOnlyList<BudgetLineDto> Lines);
    /// <summary>Amount null removes the limit ("Sem orçamento"); "0.00" is a valid zero limit.</summary>
    public sealed record BudgetItemPayload(Guid CategoryCode, string? Amount);
    public sealed record SaveBudgetsPayload(BudgetItemPayload[] Items);
    public sealed record AlertPreferencePayload(int WarningPercent, int LimitPercent, int DueSoonDays, int Version);
    public sealed record AlertPreferenceDto(int WarningPercent, int LimitPercent, int DueSoonDays, int Version);
    public sealed record AlertDto(string Key, string Kind, string Severity, string Title, string Detail, string Link, DateOnly Date, string Status);
    public sealed record AlertActionPayload(string Key, string Action, int? Days);

    public sealed record CashEventDto(string Kind, string Description, string Amount, bool IsEstimate, Guid? Code);
    public sealed record ProjectionDayDto(DateOnly Date, string Inflow, string Outflow, string Balance, IReadOnlyList<CashEventDto> Events);
    /// <summary>Starts from today's real balance; overdue items are placed on today. Variable income without an expected amount is left out (A09).</summary>
    public sealed record ProjectionDto(DateOnly From, DateOnly To, string Start, string End, DateOnly? FirstDeficit, string Lowest, DateOnly LowestDate,
        IReadOnlyList<CashEventDto> DeficitCauses, IReadOnlyList<ProjectionDayDto> Days);
    public sealed record DayFlowDto(DateOnly Date, string Inflow, string Outflow, string Balance, bool IsProjected);
    public sealed record MonthFlowDto(string Month, string Opening, string Inflow, string Outflow, string Other, string Closing, bool IsProjected);
    public sealed record CashFlowDto(string Month, DateOnly Today, string Opening, string Closing, IReadOnlyList<DayFlowDto> Days, IReadOnlyList<MonthFlowDto> Months);
    public sealed record AgendaItemDto(DateOnly Date, string Kind, string Description, string Amount, bool IsEstimate, bool IsOverdue, Guid Code, string Link);
    public sealed record AgendaDto(IReadOnlyList<AgendaItemDto> Overdue, IReadOnlyList<AgendaItemDto> Today, IReadOnlyList<AgendaItemDto> Next7,
        IReadOnlyList<AgendaItemDto> Next30, IReadOnlyList<AgendaItemDto> Later);
    public sealed record MonthIncomeDto(string Expected, string Received, string Pending);
    public sealed record MonthExpenseDto(string Total, string Paid, string Open, string Overdue);
    public sealed record DashboardDto(string Month, DateOnly Today, string Balance, MonthIncomeDto Income, MonthExpenseDto Expenses, ProjectionDto Projection,
        BudgetDto Budget, AgendaDto Agenda, IReadOnlyList<AlertDto> Alerts, int UnreadAlerts);
    public sealed record AnalysisLineDto(string Source, Guid Code, string Description, Guid? CategoryCode, string? CategoryName, DateOnly DueDate, string Amount,
        string Paid, string Status, string? CardName);
    public sealed record CategoryTotalDto(Guid? CategoryCode, string Name, string Amount);
    public sealed record AnalysisDto(string Month, Guid? CategoryCode, Guid? PersonCode, Guid? AccountCode, string? Status, string Total, string Paid,
        IReadOnlyList<CategoryTotalDto> ByCategory, IReadOnlyList<AnalysisLineDto> Lines);
    /// <summary>A22: bills of a month the household pays with a card, grouped by category (or description), with the invoice their cash leaves on.</summary>
    public sealed record MarginGroupDto(string Name, int Count, string Amount, string CardName, string InvoiceMonth, DateOnly InvoiceDueDate);
    /// <summary>
    /// One month of "Margem do mês": Opening + Inflow + Outflow + Other = Closing; Bills and Invoices split the still-open part of Outflow; Margin = Closing − reserve;
    /// Status green/yellow/red (Shortfall says how far below the reserve); ToCard is what leaves later through a card invoice, listed in ToCardGroups.
    /// </summary>
    public sealed record MarginMonthDto(string Month, string Opening, string Inflow, string Outflow, string Bills, string Invoices, string Other, string Closing, string Margin,
        string Status, string Shortfall, string ToCard, IReadOnlyList<MarginGroupDto> ToCardGroups);
    /// <summary>"Hoje, em qual fatura cai?": a purchase made today on the card lands in ReferenceMonth (closes ClosingDate, due DueDate) until LastDay; after that, NextReferenceMonth.</summary>
    public sealed record CardLandingDto(Guid CardCode, string CardName, int ClosingDay, int DueDay, string ReferenceMonth, DateOnly ClosingDate, DateOnly DueDate, DateOnly LastDay,
        string NextReferenceMonth);
    /// <summary>The same months recomputed with a spend of Amount today: in cash (Date = today) or on a card (Date = the invoice due date it lands in).</summary>
    public sealed record MarginSimulationDto(string Amount, DateOnly Date, string? CardName, string? InvoiceMonth, DateOnly? InvoiceDueDate, IReadOnlyList<MarginMonthDto> Months);
    public sealed record MarginDto(DateOnly Today, string Reserve, string AttentionThreshold, IReadOnlyList<MarginMonthDto> Months, IReadOnlyList<CardLandingDto> Cards,
        MarginSimulationDto? Simulation);

    public sealed record GetBudgetQuery(SessionIdentity Identity, DateOnly? Month) : IRequest<BudgetDto>;
    public sealed record SaveBudgetsCommand(SessionIdentity Identity, DateOnly Month, SaveBudgetsPayload Payload) : IRequest<BudgetDto>;
    public sealed record GetAlertPreferenceQuery(SessionIdentity Identity) : IRequest<AlertPreferenceDto>;
    public sealed record SaveAlertPreferenceCommand(SessionIdentity Identity, AlertPreferencePayload Payload) : IRequest<AlertPreferenceDto>;
    public sealed record ListAlertsQuery(SessionIdentity Identity) : IRequest<IReadOnlyList<AlertDto>>;
    public sealed record AlertActionCommand(SessionIdentity Identity, AlertActionPayload Payload) : IRequest<IReadOnlyList<AlertDto>>;
    public sealed record GetCashFlowQuery(SessionIdentity Identity, DateOnly? Month) : IRequest<CashFlowDto>;
    public sealed record GetProjectionQuery(SessionIdentity Identity, int? Days) : IRequest<ProjectionDto>;
    public sealed record GetAgendaQuery(SessionIdentity Identity) : IRequest<AgendaDto>;
    public sealed record GetDashboardQuery(SessionIdentity Identity, DateOnly? Month) : IRequest<DashboardDto>;
    public sealed record GetAnalysisQuery(SessionIdentity Identity, DateOnly? Month, Guid? CategoryCode, Guid? PersonCode, Guid? AccountCode, string? Status) : IRequest<AnalysisDto>;
    /// <summary>Amount (optional) simulates a spend today; CardCode puts it on that card's invoice instead of leaving the account now.</summary>
    public sealed record GetMarginQuery(SessionIdentity Identity, string? Amount, Guid? CardCode) : IRequest<MarginDto>;

    public class SaveBudgetsValidator : AbstractValidator<SaveBudgetsCommand>
    {
        public SaveBudgetsValidator()
        {
            RuleFor(x => x.Payload.Items).NotNull().Must(items => items is null || items.Length <= 300).WithMessage("Envie até 300 categorias.")
                .Must(items => items is null || items.Select(x => x.CategoryCode).Distinct().Count() == items.Length).WithMessage("Cada categoria aparece uma única vez.");
            RuleForEach(x => x.Payload.Items).ChildRules(item => item.RuleFor(x => x.Amount)
                .Must(value => value is null || Money.TryParse(value, out var amount) && amount >= 0).WithMessage("Informe o limite como valor maior ou igual a zero, por exemplo 1000.00."));
        }
    }
    public class SaveAlertPreferenceValidator : AbstractValidator<SaveAlertPreferenceCommand>
    {
        public SaveAlertPreferenceValidator()
        {
            RuleFor(x => x.Payload.WarningPercent).InclusiveBetween(1, 100);
            RuleFor(x => x.Payload.LimitPercent).InclusiveBetween(1, 200).GreaterThanOrEqualTo(x => x.Payload.WarningPercent)
                .WithMessage("O limite deve ser maior ou igual à aproximação.");
            RuleFor(x => x.Payload.DueSoonDays).InclusiveBetween(0, 30);
        }
    }
    public class AlertActionValidator : AbstractValidator<AlertActionCommand>
    {
        public AlertActionValidator()
        {
            RuleFor(x => x.Payload.Key).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Payload.Action).Must(action => action is "read" or "snooze" or "resolve").WithMessage("Ação inválida: use read, snooze ou resolve.");
            RuleFor(x => x.Payload.Days).InclusiveBetween(1, 90).When(x => x.Payload.Action == "snooze").WithMessage("Adie por 1 a 90 dias.");
        }
    }
    public class GetAnalysisValidator : AbstractValidator<GetAnalysisQuery>
    {
        public GetAnalysisValidator() =>
            RuleFor(x => x.Status).Must(status => status is null or "open" or "paid" or "overdue" or "card").WithMessage("Situação inválida.");
    }
    public class GetProjectionValidator : AbstractValidator<GetProjectionQuery>
    {
        public GetProjectionValidator() => RuleFor(x => x.Days).InclusiveBetween(7, 400).When(x => x.Days is not null);
    }
    public class GetMarginValidator : AbstractValidator<GetMarginQuery>
    {
        public GetMarginValidator()
        {
            RuleFor(x => x.Amount).Must(value => value is null || Money.IsPositive(value)).WithMessage("Informe o valor a simular como número positivo, por exemplo 1000.00.");
            RuleFor(x => x.CardCode).Null().When(x => x.Amount is null).WithMessage("Informe o valor a simular no cartão.");
        }
    }
}
