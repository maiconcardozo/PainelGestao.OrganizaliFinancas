using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>
    /// Reserve goal (RF38): a target amount, an optional deadline and the account where the money is kept.
    /// Progress is the balance of that account; saving never creates income or expense.
    /// </summary>
    public class SavingsGoal : TenantEntity
    {
        private SavingsGoal() { }
        private SavingsGoal(int idTenant) : base(idTenant) { }
        public string Name { get; private set; } = string.Empty;
        public decimal TargetAmount { get; private set; }
        public DateTime? TargetDate { get; private set; }
        public int IdFinancialAccount { get; private set; }
        public string? Note { get; private set; }
        public int Version { get; private set; }
        public static SavingsGoal Create(int idTenant, string name, decimal target, DateOnly? targetDate, FinancialAccount account, string? note, string actor)
        {
            var goal = new SavingsGoal(idTenant);
            goal.Apply(name, target, targetDate, account, note);
            goal.SetCreatedBy(actor);
            return goal;
        }
        public void Update(string name, decimal target, DateOnly? targetDate, FinancialAccount account, string? note, bool active, string actor)
        {
            Apply(name, target, targetDate, account, note);
            if (active) Activate(actor); else SoftDelete(actor);
            Version++;
            UpdateAuditInfo(actor);
        }
        private void Apply(string name, decimal target, DateOnly? targetDate, FinancialAccount account, string? note)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name.Trim().Length > 160) throw new ArgumentException("Nome excede 160 caracteres.", nameof(name));
            if (target <= 0 || decimal.Round(target, 2) != target) throw new ArgumentException("Valor alvo deve ser positivo, em centavos.", nameof(target));
            if (account.IdTenant != IdTenant) throw new ArgumentException("Conta de outra casa.", nameof(account));
            Name = name.Trim();
            TargetAmount = target;
            TargetDate = targetDate?.ToDateTime(TimeOnly.MinValue);
            IdFinancialAccount = account.Id;
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(300, note.Trim().Length)];
        }
        public DateOnly? TargetDay() => TargetDate is DateTime date ? DateOnly.FromDateTime(date) : null;
    }

    /// <summary>Private receipt or document (RF34) attached to an expense, a payment or an income receipt of the household.</summary>
    public class Attachment : TenantEntity
    {
        public static readonly string[] Kinds = ["expense", "payment", "receipt"];
        public static readonly string[] ContentTypes = ["application/pdf", "image/jpeg", "image/png", "image/webp", "image/heic"];
        public const long MaxSize = 10 * 1024 * 1024;
        private Attachment() { }
        private Attachment(int idTenant) : base(idTenant) { }
        public string TargetKind { get; private set; } = string.Empty;
        public Guid TargetCode { get; private set; }
        public string FileName { get; private set; } = string.Empty;
        public string ContentType { get; private set; } = string.Empty;
        public long Size { get; private set; }
        public string StorageKey { get; private set; } = string.Empty;
        public string Sha256 { get; private set; } = string.Empty;
        public static Attachment Create(int idTenant, string kind, Guid target, string fileName, string contentType, long size, string sha256, string actor)
        {
            if (!Kinds.Contains(kind)) throw new ArgumentException("Destino do comprovante inválido.", nameof(kind));
            if (!ContentTypes.Contains(contentType)) throw new InputValidationException(new Dictionary<string, string[]> { ["File"] = ["Envie PDF ou imagem (JPG, PNG, WEBP ou HEIC)."] });
            if (size <= 0 || size > MaxSize) throw new InputValidationException(new Dictionary<string, string[]> { ["File"] = ["O arquivo precisa ter até 10 MB."] });
            var name = Path.GetFileName(fileName.Trim());
            if (string.IsNullOrWhiteSpace(name)) name = "comprovante";
            var attachment = new Attachment(idTenant)
            {
                TargetKind = kind, TargetCode = target, FileName = name.Length > 200 ? name[..200] : name, ContentType = contentType, Size = size, Sha256 = sha256
            };
            attachment.StorageKey = $"{idTenant}/{attachment.Code:N}";
            attachment.SetCreatedBy(actor);
            return attachment;
        }
    }

    /// <summary>One statement file imported into one account (RF44). Lines are kept so a repeated file is recognised.</summary>
    public class ImportBatch : TenantEntity
    {
        private ImportBatch() { }
        private ImportBatch(int idTenant) : base(idTenant) { }
        public int IdFinancialAccount { get; private set; }
        public string FileName { get; private set; } = string.Empty;
        public string Format { get; private set; } = string.Empty;
        public int LineCount { get; private set; }
        public int AppliedCount { get; private set; }
        public int IgnoredCount { get; private set; }
        public static ImportBatch Create(FinancialAccount account, string fileName, string format, int lines, int applied, int ignored, string actor)
        {
            var name = Path.GetFileName(fileName.Trim());
            var batch = new ImportBatch(account.IdTenant)
            {
                IdFinancialAccount = account.Id, FileName = name.Length > 200 ? name[..200] : name, Format = format, LineCount = lines, AppliedCount = applied, IgnoredCount = ignored
            };
            batch.SetCreatedBy(actor);
            return batch;
        }
    }

    /// <summary>What was decided for one statement line. The fingerprint is how the same line is recognised next time.</summary>
    public sealed record ImportedLine(Guid Code, string Fingerprint, DateOnly Date, decimal Amount, string Description, string? ExternalId, string Action, Guid? ResultCode);
}
