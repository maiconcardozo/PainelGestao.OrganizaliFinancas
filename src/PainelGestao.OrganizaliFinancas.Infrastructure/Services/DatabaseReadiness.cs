using System.Data.Common;
using Dapper;
using Foundation.Domain.Interfaces.Data;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Services
{
    public class DatabaseReadiness(ISqlConnectionFactory connectionFactory) : IDatabaseReadiness
    {
        public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var connection = connectionFactory.CreateConnection();
                await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT COUNT(*) FROM Tenant WHERE 1 = 0", commandTimeout: 3,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
                return true;
            }
            catch (DbException) { return false; }
        }
    }
}
