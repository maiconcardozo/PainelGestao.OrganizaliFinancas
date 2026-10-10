using Foundation.Dapper;
using Foundation.Domain.Interfaces.Data;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.UnitOfWork
{
    /// <summary>Uses the transaction implementation supplied by Foundation.Base.</summary>
    public class OrganizaliFinancasUnitOfWork(ISqlConnectionFactory connectionFactory)
        : DapperUnitOfWork(connectionFactory), IUnitOfWork
    {
    }
}
