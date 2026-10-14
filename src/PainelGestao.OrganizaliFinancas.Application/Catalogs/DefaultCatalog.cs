using System.Text.Json;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Application.Catalogs
{
    public sealed record DefaultCatalogResultDto(int Categories, int IncomeTypes, int Creditors, int PaymentMethods, int ExpenseTypes);
    /// <summary>Loads the standard household catalog into the caller's household. Idempotent by name; never changes or removes existing rows.</summary>
    public sealed record SeedDefaultCatalogCommand(SessionIdentity Identity) : IRequest<DefaultCatalogResultDto>;

    public class SeedDefaultCatalogHandler(IUnitOfWork work) : IRequestHandler<SeedDefaultCatalogCommand, DefaultCatalogResultDto>
    {
        public async Task<DefaultCatalogResultDto> Handle(SeedDefaultCatalogCommand request, CancellationToken ct)
        {
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            return await DefaultCatalogSeeder.SeedAsync(work, user.IdTenant, user.Code.ToString(), ct);
        }
    }

    /// <summary>
    /// The standard catalog of a Brazilian household: categories with subcategories, income types, creditors, payment methods
    /// and parameterized expense types. Shared by provisioning, the operator command and the "load defaults" action of the screens.
    /// </summary>
    public static class DefaultCatalogSeeder
    {
        private sealed record TypeSeed(string Name, string Behavior, string? Category, CustomFieldDefinition[] Fields);
        private static CustomFieldDefinition F(string key, string label, string type, bool required = false, int? maxLength = null, decimal? min = null, decimal? max = null, string[]? options = null) =>
            new(key, label, type, required, maxLength, min, max, options);
        private static readonly CustomFieldDefinition Reference = F("referencia", "Mês de referência (MM/AAAA)", "text", false, 7);

        public static readonly IReadOnlyDictionary<string, string[]> Categories = new Dictionary<string, string[]>
        {
            ["Moradia"] = ["Aluguel", "Condomínio", "Energia elétrica", "Água e esgoto", "Gás", "Internet e TV", "Telefone celular", "IPTU", "Manutenção e reparos", "Móveis e eletrodomésticos", "Limpeza e produtos de casa"],
            ["Alimentação"] = ["Supermercado", "Feira e hortifrúti", "Padaria e açougue", "Restaurantes e delivery"],
            ["Transporte"] = ["Combustível", "Transporte por aplicativo e ônibus", "Manutenção do veículo", "Seguro do veículo", "IPVA e licenciamento", "Estacionamento e pedágio"],
            ["Saúde"] = ["Plano de saúde", "Farmácia", "Consultas e exames", "Dentista", "Academia e esporte"],
            ["Educação"] = ["Escola e faculdade", "Cursos", "Livros e material escolar"],
            ["Lazer e cultura"] = ["Streaming e assinaturas", "Viagens", "Passeios, cinema e eventos", "Hobbies"],
            ["Cuidados pessoais"] = ["Vestuário e calçados", "Salão e barbearia", "Cosméticos e perfumaria"],
            ["Pets"] = ["Ração e petshop", "Veterinário"],
            ["Dívidas e financiamentos"] = ["Empréstimo", "Financiamento", "Juros e tarifas bancárias", "Renegociação"],
            ["Impostos e taxas"] = ["Imposto de renda", "Taxas e documentos"],
            ["Seguros"] = ["Seguro de vida", "Seguro residencial"],
            ["Presentes e doações"] = ["Presentes", "Doações"],
            ["Profissional"] = ["Ferramentas e softwares", "Contador e impostos do negócio", "Equipamentos"],
            ["Reserva e investimentos"] = ["Reserva de emergência", "Investimentos", "Previdência"],
            ["Outros gastos"] = [],
        };
        public static readonly string[] IncomeTypes = ["Salário", "Pró-labore", "Distribuição de lucros", "Freelance e serviços", "Comissões e bônus", "13º salário", "Férias",
            "Aluguel recebido", "Rendimentos de investimentos", "Benefício do INSS", "Pensão", "Reembolso", "Venda de bens", "Presente ou doação recebida", "Outras rendas"];
        public static readonly string[] Creditors = ["Imobiliária ou proprietário", "Administradora do condomínio", "Companhia de energia", "Companhia de água", "Distribuidora de gás",
            "Provedor de internet", "Operadora de celular", "Prefeitura", "Detran", "Receita Federal", "Supermercado", "Padaria", "Farmácia", "Posto de combustível", "Plano de saúde",
            "Dentista", "Escola", "Academia", "Petshop", "Banco", "Cartão de crédito", "Streaming", "Loja online", "Seguradora", "Contador", "Mecânica", "Outro credor"];
        public static readonly string[] PaymentMethods = ["Pix", "Dinheiro", "Débito automático", "Boleto", "Cartão de débito", "Cartão de crédito", "Transferência bancária",
            "Vale-alimentação ou refeição", "Carteira digital"];
        private static readonly TypeSeed[] ExpenseTypes =
        [
            new("Aluguel", "recurring", "Aluguel", [F("contrato", "Número do contrato", "text", false, 60), F("reajuste", "Data do próximo reajuste", "date")]),
            new("Condomínio", "recurring", "Condomínio", [Reference]),
            new("Conta de luz", "recurring", "Energia elétrica", [Reference, F("kwh", "Consumo (kWh)", "decimal", false, null, 0, 100000), F("unidade", "Unidade consumidora", "text", false, 30)]),
            new("Conta de água", "recurring", "Água e esgoto", [Reference, F("m3", "Consumo (m³)", "decimal", false, null, 0, 100000)]),
            new("Gás", "single", "Gás", [F("tipo", "Tipo", "list", true, options: ["Botijão 13 kg", "Gás encanado", "Outro"])]),
            new("Internet e TV", "recurring", "Internet e TV", [F("plano", "Plano contratado", "text", false, 80)]),
            new("Telefone celular", "recurring", "Telefone celular", [F("linha", "Linha (número)", "text", false, 20)]),
            new("IPTU", "installments", "IPTU", [F("exercicio", "Exercício (ano)", "text", true, 4), F("cota_unica", "Pago em cota única", "boolean")]),
            new("IPVA e licenciamento", "installments", "IPVA e licenciamento", [F("placa", "Placa do veículo", "text", true, 8), F("exercicio", "Exercício (ano)", "text", true, 4)]),
            new("Supermercado", "single", "Supermercado", [F("loja", "Loja", "text", false, 80)]),
            new("Feira e hortifrúti", "single", "Feira e hortifrúti", []),
            new("Restaurante e delivery", "single", "Restaurantes e delivery", [F("estabelecimento", "Estabelecimento", "text", false, 80)]),
            new("Combustível", "single", "Combustível", [F("combustivel", "Combustível", "list", true, options: ["Gasolina", "Etanol", "Diesel", "GNV"]),
                F("litros", "Litros", "decimal", false, null, 0, 1000), F("km", "Quilometragem no abastecimento", "decimal", false, null, 0, 9999999)]),
            new("Manutenção do veículo", "single", "Manutenção do veículo", [F("servico", "Serviço realizado", "text", true, 120), F("km", "Quilometragem", "decimal", false, null, 0, 9999999)]),
            new("Transporte por aplicativo", "single", "Transporte por aplicativo e ônibus", [F("app", "Aplicativo", "list", false, options: ["Uber", "99", "Ônibus", "Táxi", "Outro"])]),
            new("Plano de saúde", "recurring", "Plano de saúde", [F("operadora", "Operadora", "text", false, 80), F("beneficiarios", "Beneficiários", "text", false, 160)]),
            new("Farmácia", "single", "Farmácia", [F("com_receita", "Com receita médica", "boolean")]),
            new("Consulta ou exame", "single", "Consultas e exames", [F("profissional", "Profissional ou clínica", "text", false, 120), F("reembolsavel", "Pedir reembolso ao plano", "boolean")]),
            new("Academia", "recurring", "Academia e esporte", []),
            new("Mensalidade escolar", "recurring", "Escola e faculdade", [F("aluno", "Aluno", "text", true, 80), Reference]),
            new("Curso", "installments", "Cursos", [F("curso", "Curso", "text", true, 120), F("aluno", "Aluno", "text", false, 80)]),
            new("Assinatura ou streaming", "recurring", "Streaming e assinaturas", [F("servico", "Serviço", "text", true, 80)]),
            new("Viagem", "single", "Viagens", [F("destino", "Destino", "text", true, 120), F("periodo", "Período", "text", false, 60)]),
            new("Empréstimo", "installments", "Empréstimo", [F("banco", "Banco ou financeira", "text", true, 80), F("contrato", "Número do contrato", "text", false, 60),
                F("juros_mes", "Juros ao mês (%)", "decimal", false, null, 0, 100)]),
            new("Financiamento", "installments", "Financiamento", [F("bem", "Bem financiado", "list", true, options: ["Imóvel", "Veículo", "Outro"]),
                F("banco", "Banco ou financeira", "text", true, 80), F("contrato", "Número do contrato", "text", false, 60)]),
            new("Renegociação de dívida", "installments", "Renegociação", [F("credor_original", "Credor original", "text", true, 120), F("acordo", "Número do acordo", "text", false, 60)]),
            new("Compra parcelada", "installments", null, [F("loja", "Loja", "text", true, 80), F("produto", "Produto", "text", true, 120)]),
            new("Compra no cartão", "card", null, [F("loja", "Loja", "text", false, 80)]),
            new("Seguro", "recurring", "Seguros", [F("tipo", "Tipo de seguro", "list", true, options: ["Vida", "Residencial", "Veículo", "Outro"]), F("apolice", "Apólice", "text", false, 60)]),
            new("Imposto de renda (DARF)", "single", "Imposto de renda", [F("exercicio", "Exercício (ano)", "text", true, 4)]),
            new("Taxa ou documento", "single", "Taxas e documentos", [F("descricao", "Descrição", "text", true, 160)]),
            new("Tarifa bancária ou juros", "single", "Juros e tarifas bancárias", [F("banco", "Banco", "text", false, 80)]),
            new("Manutenção da casa", "single", "Manutenção e reparos", [F("servico", "Serviço", "text", true, 120), F("prestador", "Prestador", "text", false, 80)]),
            new("Móveis e eletrodomésticos", "installments", "Móveis e eletrodomésticos", [F("item", "Item comprado", "text", true, 120), F("garantia_ate", "Garantia até", "date")]),
            new("Vestuário", "single", "Vestuário e calçados", [F("para_quem", "Para quem", "text", false, 80)]),
            new("Salão e beleza", "single", "Salão e barbearia", []),
            new("Pet", "single", "Ração e petshop", [F("tipo", "Tipo", "list", true, options: ["Ração", "Petshop", "Banho e tosa", "Veterinário", "Outro"]), F("animal", "Animal", "text", false, 60)]),
            new("Presente", "single", "Presentes", [F("para_quem", "Para quem", "text", true, 80), F("ocasiao", "Ocasião", "text", false, 80)]),
            new("Doação", "single", "Doações", [F("instituicao", "Instituição ou pessoa", "text", false, 120)]),
            new("Software ou ferramenta de trabalho", "recurring", "Ferramentas e softwares", [F("servico", "Serviço", "text", true, 80)]),
            new("Contador e impostos do negócio", "recurring", "Contador e impostos do negócio", [F("tipo", "Tipo", "list", true, options: ["Honorários do contador", "DAS (MEI/Simples)", "Outro imposto"])]),
            new("Reserva ou investimento", "recurring", "Reserva de emergência", [F("destino", "Onde foi aplicado", "text", false, 120)]),
            new("Despesa avulsa", "single", "Outros gastos", [F("descricao", "Descrição", "text", false, 160)]),
        ];

        /// <summary>Three short transactions: parents and flat catalogs, then subcategories (parent ids), then expense types (category ids).</summary>
        public static async Task<DefaultCatalogResultDto> SeedAsync(IUnitOfWork work, int idTenant, string actor, CancellationToken ct)
        {
            var counts = new int[5];
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(idTenant, ct);
                counts[0] += await AddMissingAsync(work, CatalogKind.Categories, idTenant, Categories.Keys, actor, null, ct);
                counts[1] += await AddMissingAsync(work, CatalogKind.IncomeTypes, idTenant, IncomeTypes, actor, null, ct);
                counts[2] += await AddMissingAsync(work, CatalogKind.Creditors, idTenant, Creditors, actor, null, ct);
                counts[3] += await AddMissingAsync(work, CatalogKind.PaymentMethods, idTenant, PaymentMethods, actor, null, ct);
                return true;
            }, ct);
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(idTenant, ct);
                var parents = (await work.CatalogRepository.ListAsync(CatalogKind.Categories, idTenant, ct)).Cast<Category>().ToArray();
                foreach (var (parentName, children) in Categories)
                {
                    var parent = parents.FirstOrDefault(x => x.Name == parentName);
                    if (parent is null || children.Length == 0) continue;
                    counts[0] += await AddMissingAsync(work, CatalogKind.Categories, idTenant, children, actor, parent, ct);
                }
                return true;
            }, ct);
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(idTenant, ct);
                var categories = (await work.CatalogRepository.ListAsync(CatalogKind.Categories, idTenant, ct)).Cast<Category>().ToArray();
                var existing = (await work.ExpenseTypeRepository.ListAsync(idTenant, ct)).Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var seed in ExpenseTypes)
                {
                    if (existing.Contains(seed.Name)) continue;
                    var category = seed.Category is null ? null : categories.FirstOrDefault(x => x.Name == seed.Category && x.IsActive);
                    var type = ExpenseType.Create(idTenant, seed.Name, actor);
                    work.ExpenseTypeRepository.Save(type, false, seed.Behavior, category?.Id, JsonSerializer.Serialize(seed.Fields), actor);
                    counts[4]++;
                }
                return true;
            }, ct);
            return new DefaultCatalogResultDto(counts[0], counts[1], counts[2], counts[3], counts[4]);
        }

        private static async Task<int> AddMissingAsync(IUnitOfWork work, CatalogKind kind, int idTenant, IEnumerable<string> names, string actor, Category? parent, CancellationToken ct)
        {
            var existing = (await work.CatalogRepository.ListAsync(kind, idTenant, ct)).Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var added = 0;
            foreach (var name in names)
            {
                if (existing.Contains(name)) continue;
                ICatalogItem item = kind switch
                {
                    CatalogKind.Categories => Category.Create(idTenant, name, actor),
                    CatalogKind.IncomeTypes => IncomeType.Create(idTenant, name, actor),
                    CatalogKind.Creditors => Creditor.Create(idTenant, name, actor),
                    CatalogKind.PaymentMethods => PaymentMethod.Create(idTenant, name, actor),
                    _ => throw new ArgumentOutOfRangeException(nameof(kind))
                };
                if (item is Category category && parent is not null) category.SetParent(parent.Id);
                work.CatalogRepository.Save(item, false);
                work.CatalogRepository.AddRevision(CatalogRevision.Create(idTenant, kind.ToString(), item.Code, item.Name, parent?.Code, true, item.Version, actor));
                added++;
            }
            return added;
        }
    }
}
