namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services
{
    /// <summary>Private file store for receipts. Keys are "{tenant}/{code}", so one household can never reach another's files.</summary>
    public interface IAttachmentStorage
    {
        Task SaveAsync(string key, Stream content, CancellationToken ct);
        Task<Stream> OpenAsync(string key, CancellationToken ct);
        Task DeleteAsync(string key, CancellationToken ct);
    }
}
