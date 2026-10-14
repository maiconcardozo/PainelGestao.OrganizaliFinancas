using System.Security.Cryptography;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Attachments
{
    public sealed record AttachmentDto(Guid Code, string Kind, Guid TargetCode, string FileName, string ContentType, long Size, DateTime CreatedAt);
    public sealed record AttachmentContentDto(Stream Content, string ContentType, string FileName, long Size);
    public sealed record ListAttachmentsQuery(SessionIdentity Identity, string Kind, Guid Target) : IRequest<IReadOnlyList<AttachmentDto>>;
    /// <summary>The stream is read once; it is hashed and stored before the row is written, and removed again if the row fails.</summary>
    public sealed record UploadAttachmentCommand(SessionIdentity Identity, string Kind, Guid Target, string FileName, string ContentType, long Length, Stream Content) : IRequest<AttachmentDto>;
    public sealed record GetAttachmentContentQuery(SessionIdentity Identity, Guid Code) : IRequest<AttachmentContentDto>;
    public sealed record DeleteAttachmentCommand(SessionIdentity Identity, Guid Code) : IRequest;

    internal static class AttachmentTargets
    {
        public const int MaxPerTarget = 20;
        /// <summary>The target must belong to the household; otherwise a code of another household would accept files.</summary>
        public static async Task EnsureExistsAsync(IUnitOfWork work, int idTenant, string kind, Guid target, CancellationToken ct)
        {
            var found = kind switch
            {
                "expense" => await work.ExpenseRepository.GetAsync(idTenant, target, ct) is not null,
                "payment" => await work.ExpenseRepository.GetPaymentAsync(idTenant, target, ct) is not null,
                "receipt" => await work.IncomeRepository.GetReceiptAsync(idTenant, target, ct) is not null,
                _ => throw new InputValidationException(new Dictionary<string, string[]> { ["Kind"] = ["Comprovantes aceitam despesa, pagamento ou recebimento."] })
            };
            if (!found) throw new NotFoundException("Lançamento não encontrado nesta casa.");
        }
        public static AttachmentDto Map(Domain.Interfaces.Repositories.AttachmentRecord x) => new(x.Code, x.TargetKind, x.TargetCode, x.FileName, x.ContentType, x.Size, x.DtCreated);
        public static string NormalizeContentType(string contentType) => contentType.Trim().ToLowerInvariant() switch
        {
            "image/jpg" or "image/pjpeg" => "image/jpeg",
            "image/heif" => "image/heic",
            var other => other
        };
    }

    public class ListAttachmentsHandler(IUnitOfWork work) : IRequestHandler<ListAttachmentsQuery, IReadOnlyList<AttachmentDto>>
    {
        public async Task<IReadOnlyList<AttachmentDto>> Handle(ListAttachmentsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            if (!Attachment.Kinds.Contains(request.Kind)) return [];
            return (await work.AttachmentRepository.ListAsync(request.Identity.IdTenant, request.Kind, request.Target, ct)).Select(AttachmentTargets.Map).ToArray();
        }
    }

    public class UploadAttachmentHandler(IUnitOfWork work, IAttachmentStorage storage) : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
    {
        public async Task<AttachmentDto> Handle(UploadAttachmentCommand request, CancellationToken ct)
        {
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            await AttachmentTargets.EnsureExistsAsync(work, user.IdTenant, request.Kind, request.Target, ct);
            var existing = await work.AttachmentRepository.ListAsync(user.IdTenant, request.Kind, request.Target, ct);
            if (existing.Count >= AttachmentTargets.MaxPerTarget)
                throw new ConflictException($"Cada lançamento aceita até {AttachmentTargets.MaxPerTarget} comprovantes.");
            // Validation runs on the declared size before anything is written; the real size is checked while copying.
            var attachment = Attachment.Create(user.IdTenant, request.Kind, request.Target, request.FileName, AttachmentTargets.NormalizeContentType(request.ContentType),
                request.Length, "pending", user.Code.ToString());
            string sha256; long size;
            await using (var buffer = new MemoryStream())
            {
                await CopyLimitedAsync(request.Content, buffer, Attachment.MaxSize, ct);
                size = buffer.Length;
                buffer.Position = 0;
                sha256 = Convert.ToHexString(await SHA256.HashDataAsync(buffer, ct));
                buffer.Position = 0;
                attachment = Attachment.Create(user.IdTenant, request.Kind, request.Target, request.FileName, attachment.ContentType, size, sha256, user.Code.ToString());
                await storage.SaveAsync(attachment.StorageKey, buffer, ct);
            }
            try
            {
                work.AttachmentRepository.Add(attachment);
                await work.CommitAsync(ct);
            }
            catch
            {
                try { await storage.DeleteAsync(attachment.StorageKey, CancellationToken.None); } catch { /* the row was not written; the orphan file is removed on a best-effort basis */ }
                throw;
            }
            return new AttachmentDto(attachment.Code, attachment.TargetKind, attachment.TargetCode, attachment.FileName, attachment.ContentType, attachment.Size, attachment.DtCreated);
        }

        private static async Task CopyLimitedAsync(Stream source, Stream target, long limit, CancellationToken ct)
        {
            var buffer = new byte[81920]; long total = 0; int read;
            while ((read = await source.ReadAsync(buffer, ct)) > 0)
            {
                total += read;
                if (total > limit) throw new InputValidationException(new Dictionary<string, string[]> { ["File"] = ["O arquivo precisa ter até 10 MB."] });
                await target.WriteAsync(buffer.AsMemory(0, read), ct);
            }
            if (total == 0) throw new InputValidationException(new Dictionary<string, string[]> { ["File"] = ["O arquivo está vazio."] });
        }
    }

    public class GetAttachmentContentHandler(IUnitOfWork work, IAttachmentStorage storage) : IRequestHandler<GetAttachmentContentQuery, AttachmentContentDto>
    {
        public async Task<AttachmentContentDto> Handle(GetAttachmentContentQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var record = await work.AttachmentRepository.FindAsync(request.Identity.IdTenant, request.Code, ct) ?? throw new NotFoundException("Comprovante não encontrado.");
            return new AttachmentContentDto(await storage.OpenAsync(record.StorageKey, ct), record.ContentType, record.FileName, record.Size);
        }
    }

    public class DeleteAttachmentHandler(IUnitOfWork work, IAttachmentStorage storage) : IRequestHandler<DeleteAttachmentCommand>
    {
        public async Task Handle(DeleteAttachmentCommand request, CancellationToken ct)
        {
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var record = await work.AttachmentRepository.FindAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Comprovante não encontrado.");
            work.AttachmentRepository.Remove(user.IdTenant, record.Code, user.Code.ToString());
            await work.CommitAsync(ct);
            // The row is the source of truth; a file that survives a failed delete is unreachable and can be cleaned later.
            try { await storage.DeleteAsync(record.StorageKey, ct); } catch { /* see above */ }
        }
    }
}
