using System.Data;
using Foundation.Domain.Interfaces.Data;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Shared.Settings;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Data.MariaDb
{
    /// <summary>
    /// Creates MariaDB connections for the Dapper data access path.
    ///
    /// Same shape as SqlConnectionFactory in Autenticacao.Global, with the MySQL driver:
    /// the caller owns the connection, and the unit of work is the one that keeps it open
    /// for the scope.
    /// </summary>
    public class MySqlConnectionFactory(IOptions<OrganizaliFinancasSettings> settings) : ISqlConnectionFactory
    {
        private readonly string _connectionString = settings.Value.ConnectionString;

        /// <inheritdoc/>
        public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
    }
}
