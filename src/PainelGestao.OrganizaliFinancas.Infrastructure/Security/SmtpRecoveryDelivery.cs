using System.Net;
using System.Net.Mail;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Security;
using PainelGestao.OrganizaliFinancas.Shared.Settings;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Security
{
    /// <summary>
    /// Sends the one-time recovery link by SMTP. The token travels in the URL fragment, so it never reaches server logs,
    /// and the message carries no financial data.
    /// </summary>
    public class SmtpRecoveryDelivery(MailSettings settings) : IRecoveryDelivery
    {
        public bool IsConfigured => settings.IsConfigured;

        public static MailMessage Compose(MailSettings settings, string email, string token)
        {
            var link = $"{settings.AppOrigin.TrimEnd('/')}/reset-password#token={Uri.EscapeDataString(token)}";
            return new MailMessage(settings.From, email)
            {
                Subject = "Redefinir sua senha do Organizali",
                Body = $"""
                    Recebemos um pedido para redefinir a senha do seu acesso ao Organizali.

                    Abra o link abaixo em até 15 minutos para escolher uma nova senha. Ele só pode ser usado uma vez e encerra as sessões abertas.

                    {link}

                    Se você não pediu a redefinição, ignore esta mensagem: sua senha continua a mesma.
                    """,
                IsBodyHtml = false,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,
                BodyTransferEncoding = System.Net.Mime.TransferEncoding.QuotedPrintable
            };
        }

        public async Task DeliverAsync(string email, string token, CancellationToken cancellationToken)
        {
            if (!IsConfigured) throw new InvalidOperationException("Configure o transporte de recuperação antes de usuários externos.");
            using var message = Compose(settings, email, token);
            using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl, DeliveryMethod = SmtpDeliveryMethod.Network, Timeout = 15000 };
            if (!string.IsNullOrEmpty(settings.User)) client.Credentials = new NetworkCredential(settings.User, settings.Password);
            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
