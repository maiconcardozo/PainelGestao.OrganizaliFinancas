namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services
{
    /// <summary>Cross-process exclusive lock for background routines, so two API replicas never run the same job together.</summary>
    public interface IJobLock
    {
        /// <summary>Returns null when another process holds the lock; disposing the handle releases it.</summary>
        Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken cancellationToken);
    }
}
