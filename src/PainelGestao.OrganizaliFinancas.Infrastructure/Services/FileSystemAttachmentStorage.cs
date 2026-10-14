using System.Text.RegularExpressions;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services;
using PainelGestao.OrganizaliFinancas.Shared.Settings;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Services
{
    /// <summary>Files on a private volume of the API container. Written to a temporary name and moved, so a reader never sees a partial file.</summary>
    public sealed partial class FileSystemAttachmentStorage(OrganizaliFinancasSettings settings) : IAttachmentStorage
    {
        [GeneratedRegex("^[0-9]+/[0-9a-f]{32}$")]
        private static partial Regex ValidKey();

        private string PathOf(string key)
        {
            if (!ValidKey().IsMatch(key)) throw new ArgumentException("Chave de arquivo inválida.", nameof(key));
            var root = Path.GetFullPath(string.IsNullOrWhiteSpace(settings.AttachmentsPath) ? "attachments" : settings.AttachmentsPath);
            var full = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(root, StringComparison.Ordinal)) throw new ArgumentException("Chave de arquivo inválida.", nameof(key));
            return full;
        }

        public async Task SaveAsync(string key, Stream content, CancellationToken ct)
        {
            var path = PathOf(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".part";
            await using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await content.CopyToAsync(file, ct);
            File.Move(temporary, path, overwrite: true);
        }

        public Task<Stream> OpenAsync(string key, CancellationToken ct)
        {
            var path = PathOf(key);
            if (!File.Exists(path)) throw new FileNotFoundException("Arquivo do comprovante não encontrado.", path);
            return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
        }

        public Task DeleteAsync(string key, CancellationToken ct)
        {
            var path = PathOf(key);
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }
    }
}
