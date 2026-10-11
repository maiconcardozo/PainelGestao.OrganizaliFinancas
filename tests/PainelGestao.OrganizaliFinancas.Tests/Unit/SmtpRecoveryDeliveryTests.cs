using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Infrastructure.Security;
using PainelGestao.OrganizaliFinancas.Shared.Settings;
namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    /// <summary>Uses an in-process SMTP listener on loopback: no message ever leaves the machine.</summary>
    public class SmtpRecoveryDeliveryTests
    {
        [Fact]
        public void Settings_RequireHostSenderAndOrigin()
        {
            new MailSettings().IsConfigured.Should().BeFalse();
            new MailSettings { Host = "smtp.example.test", From = "nao-responda@example.test" }.IsConfigured.Should().BeFalse("the link needs the public origin");
            new MailSettings { Host = "smtp.example.test", From = "nao-responda@example.test", AppOrigin = "https://financas.example.test" }.IsConfigured.Should().BeTrue();
        }

        [Fact]
        public async Task Delivery_SendsOneTimeLinkWithTokenInFragment()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var received = FakeSmtpAsync(listener);
            var settings = new MailSettings { Host = "127.0.0.1", Port = port, EnableSsl = false, From = "nao-responda@example.test", AppOrigin = "https://financas.example.test/" };
            await new SmtpRecoveryDelivery(settings).DeliverAsync("titular@example.test", "token+/=", CancellationToken.None);
            var data = await received.WaitAsync(TimeSpan.FromSeconds(10));
            data.Should().Contain("RCPT TO:<titular@example.test>");
            data.Should().Contain("https://financas.example.test/reset-password#token=3Dtoken%2B%2F%3D", "the token is URL-escaped (quoted-printable keeps '=' as =3D)");
            data.Should().NotContain("R$", "the message carries no financial data");
        }

        private static async Task<string> FakeSmtpAsync(TcpListener listener)
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            await using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };
            var log = new StringBuilder();
            await writer.WriteLineAsync("220 fake.local ESMTP");
            var inData = false;
            while (await reader.ReadLineAsync() is { } line)
            {
                log.AppendLine(line);
                if (inData)
                {
                    if (line == ".") { inData = false; await writer.WriteLineAsync("250 OK"); }
                    continue;
                }
                var command = line.Split(' ')[0].ToUpperInvariant();
                if (command is "EHLO" or "HELO") await writer.WriteLineAsync("250 fake.local");
                else if (command == "DATA") { inData = true; await writer.WriteLineAsync("354 End with <CRLF>.<CRLF>"); }
                else if (command == "QUIT") { await writer.WriteLineAsync("221 Bye"); break; }
                else await writer.WriteLineAsync("250 OK");
            }
            // Quoted-printable bodies wrap long lines with "=" soft breaks; join them before asserting.
            return log.ToString().Replace("=\r\n", string.Empty);
        }
    }
}
