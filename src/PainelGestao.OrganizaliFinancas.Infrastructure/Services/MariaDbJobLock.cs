using System.Data;
using Dapper;
using Foundation.Domain.Interfaces.Data;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Services
{
    /// <summary>MariaDB named lock held by a dedicated connection; it is released explicitly or when the connection closes.</summary>
    public class MariaDbJobLock(ISqlConnectionFactory connectionFactory) : IJobLock
    {
        public async Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken cancellationToken)
        {
            var connection = connectionFactory.CreateConnection();
            try
            {
                // The lock belongs to the database session: the connection must stay open (Dapper would close and pool it after the query).
                if (connection is System.Data.Common.DbConnection db) await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                else connection.Open();
                var acquired = await connection.ExecuteScalarAsync<long?>(new CommandDefinition("SELECT GET_LOCK(@Name, 0)", new { Name = name },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
                if (acquired == 1) return new Handle(connection, name);
            }
            catch { connection.Dispose(); throw; }
            connection.Dispose();
            return null;
        }

        private sealed class Handle(IDbConnection connection, string name) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                try { await connection.ExecuteAsync("SELECT RELEASE_LOCK(@Name)", new { Name = name }).ConfigureAwait(false); }
                finally { connection.Dispose(); }
            }
        }
    }
}
