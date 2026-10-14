using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;

namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    /// <summary>RF44: OFX (SGML) and CSV statements from Brazilian banks are read without writing anything; the fingerprint identifies a line again.</summary>
    public sealed class StatementParserTests
    {
        private const string Ofx = """
            OFXHEADER:100
            DATA:OFXSGML
            <OFX>
            <BANKMSGSRSV1><STMTTRNRS><STMTRS>
            <BANKTRANLIST>
            <STMTTRN>
            <TRNTYPE>DEBIT
            <DTPOSTED>20261005120000[-3:BRT]
            <TRNAMT>-280.45
            <FITID>2026100500123
            <MEMO>CELESC DISTRIBUICAO
            </STMTTRN>
            <STMTTRN>
            <TRNTYPE>CREDIT
            <DTPOSTED>20261006
            <TRNAMT>14000,00
            <FITID>2026100600777
            <NAME>PIX RECEBIDO CP COMUNICA
            </STMTTRN>
            </BANKTRANLIST>
            </STMTRS></STMTTRNRS></BANKMSGSRSV1>
            </OFX>
            """;

        [Fact]
        public void Ofx_ReadsDebitAndCreditWithIdsAndDescriptions()
        {
            StatementParser.DetectFormat("extrato.txt", Ofx).Should().Be("ofx");
            var lines = StatementParser.Parse("ofx", Ofx);
            lines.Should().HaveCount(2);
            lines[0].Should().Be(new ParsedTransaction(new DateOnly(2026, 10, 5), -280.45m, "CELESC DISTRIBUICAO", "2026100500123"));
            lines[1].Should().Be(new ParsedTransaction(new DateOnly(2026, 10, 6), 14000.00m, "PIX RECEBIDO CP COMUNICA", "2026100600777"));
        }

        [Fact]
        public void Csv_WithPortugueseHeaderAndBrazilianNumbers()
        {
            const string csv = """
                Data;Descrição;Valor;Documento
                05/10/2026;"Supermercado Angeloni";-1.234,56;9001
                06/10/2026;Salário;R$ 5.000,00;9002
                07/10/2026;Tarifa;(12,30);
                """;
            StatementParser.DetectFormat("extrato.csv", csv).Should().Be("csv");
            var lines = StatementParser.Parse("csv", csv);
            lines.Should().HaveCount(3);
            lines[0].Should().Be(new ParsedTransaction(new DateOnly(2026, 10, 5), -1234.56m, "Supermercado Angeloni", "9001"));
            lines[1].Amount.Should().Be(5000.00m);
            lines[2].Should().Be(new ParsedTransaction(new DateOnly(2026, 10, 7), -12.30m, "Tarifa", null));
        }

        [Fact]
        public void Csv_WithSeparateCreditAndDebitColumnsAndNoIds()
        {
            const string csv = """
                data,historico,credito,debito
                2026-10-01,Pix recebido,150.00,
                2026-10-02,Conta de luz,,280.45
                """;
            var lines = StatementParser.Parse("csv", csv);
            lines.Select(x => x.Amount).Should().Equal(150.00m, -280.45m);
            lines.Select(x => x.Description).Should().Equal("Pix recebido", "Conta de luz");
        }

        [Fact]
        public void Csv_WithoutHeader_UsesDateMoneyAndLongestTextColumns()
        {
            const string csv = """
                10/10/2026;Padaria do bairro;-25,90
                11/10/2026;Transferência recebida;300,00
                """;
            var lines = StatementParser.Parse("csv", csv);
            lines.Should().HaveCount(2);
            lines[0].Description.Should().Be("Padaria do bairro");
            lines[1].Amount.Should().Be(300.00m);
        }

        [Fact]
        public void EmptyOrUnreadableFile_IsRejectedWithGuidance()
        {
            var act = () => StatementParser.Parse("csv", "isto não é um extrato\nsó texto");
            act.Should().Throw<InputValidationException>();
        }

        [Fact]
        public void Fingerprint_IsStableAndUsesBankIdWhenPresent()
        {
            var account = Guid.NewGuid();
            var withId = StatementParser.Fingerprint(account, new DateOnly(2026, 10, 5), -280.45m, "CELESC", "123");
            StatementParser.Fingerprint(account, new DateOnly(2026, 10, 5), -280.45m, "celesc distribuicao", "123").Should().Be(withId, "the bank id wins over the description");
            StatementParser.Fingerprint(account, new DateOnly(2026, 10, 5), -280.45m, "CELESC", "124").Should().NotBe(withId);
            var byText = StatementParser.Fingerprint(account, new DateOnly(2026, 10, 5), -280.45m, "Celesc  Distribuição", null);
            StatementParser.Fingerprint(account, new DateOnly(2026, 10, 5), -280.45m, "celesc distribuicao", null).Should().NotBe(byText, "accents are part of the text key; only spacing and case are normalized");
            StatementParser.Fingerprint(account, new DateOnly(2026, 10, 5), -280.45m, "CELESC DISTRIBUIÇÃO", null).Should().Be(byText);
            withId.Should().HaveLength(40);
        }
    }
}
