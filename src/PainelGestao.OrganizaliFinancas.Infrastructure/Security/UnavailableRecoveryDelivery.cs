using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Security;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Security
{
    public class UnavailableRecoveryDelivery : IRecoveryDelivery
    {
        public bool IsConfigured => false;
        public Task DeliverAsync(string email, string token, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Configure o transporte de recuperação antes de usuários externos.");
    }
}
