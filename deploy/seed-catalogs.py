# -*- coding: utf-8 -*-
"""Carrega os cadastros iniciais de uma casa no Organizali Finanças (categorias, tipos de renda,
credores, formas de pagamento e tipos de despesa) pela API, passando pelo proxy do Next.

Idempotente: compara pelo nome e só cria o que falta. Nunca apaga nem inativa nada.

Uso (Python 3.10+, sem dependências):
  set ORGANIZALI_ORIGIN=https://192.168.1.7:3300   (endereço público exato, usado também como Origin)
  set ORGANIZALI_EMAIL=titular@exemplo.com
  set ORGANIZALI_PASSWORD=...
  set ORGANIZALI_INSECURE=1                        (só para certificado interno da LAN)
  python -I deploy/seed-catalogs.py            -> sonda: lista o que falta, não grava
  python -I deploy/seed-catalogs.py --apply    -> cria o que falta
"""
import json, os, ssl, sys, http.cookiejar, urllib.request, urllib.error

ORIGIN = os.environ.get("ORGANIZALI_ORIGIN", "").rstrip("/")
EMAIL = os.environ.get("ORGANIZALI_EMAIL", "")
PASSWORD = os.environ.get("ORGANIZALI_PASSWORD", "")
APPLY = "--apply" in sys.argv
if not (ORIGIN and EMAIL and PASSWORD):
    sys.exit("Defina ORGANIZALI_ORIGIN, ORGANIZALI_EMAIL e ORGANIZALI_PASSWORD.")

ctx = ssl.create_default_context()
if os.environ.get("ORGANIZALI_INSECURE") == "1":
    ctx.check_hostname = False
    ctx.verify_mode = ssl.CERT_NONE
jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPSHandler(context=ctx), urllib.request.HTTPCookieProcessor(jar))


def call(method, path, body=None):
    data = json.dumps(body).encode("utf-8") if body is not None else None
    req = urllib.request.Request(ORIGIN + path, data=data, method=method)
    req.add_header("Origin", ORIGIN)
    req.add_header("Accept", "application/json")
    if data is not None:
        req.add_header("Content-Type", "application/json")
    try:
        with opener.open(req, timeout=30) as resp:
            raw = resp.read().decode("utf-8")
            return resp.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", "replace")
        try:
            return e.code, json.loads(raw)
        except Exception:
            return e.code, raw


def unwrap(payload):
    if isinstance(payload, dict) and "data" in payload:
        return payload["data"]
    return payload


status, body = call("POST", "/api/auth/login", {"email": EMAIL, "password": PASSWORD})
print("login:", status, "" if status == 200 else body)
if status != 200:
    sys.exit(1)

KINDS = ["categories", "income-types", "creditors", "payment-methods"]
existing = {}
for kind in KINDS:
    status, body = call("GET", f"/api/backend/catalogs/{kind}")
    items = unwrap(body) if status == 200 else []
    existing[kind] = {i["name"]: i for i in items} if isinstance(items, list) else {}
    print(f"{kind}: {status} -> {len(existing[kind])} registro(s)")
    if status != 200:
        print("   ", body)
status, body = call("GET", "/api/backend/expense-types")
types = unwrap(body) if status == 200 else []
existing_types = {t["name"]: t for t in types} if isinstance(types, list) else {}
print(f"expense-types: {status} -> {len(existing_types)} registro(s)")

# ----------------------------------------------------------------------------- dados
CATEGORIES = {
    "Moradia": ["Aluguel", "Condomínio", "Energia elétrica", "Água e esgoto", "Gás", "Internet e TV", "Telefone celular",
                "IPTU", "Manutenção e reparos", "Móveis e eletrodomésticos", "Limpeza e produtos de casa"],
    "Alimentação": ["Supermercado", "Feira e hortifrúti", "Padaria e açougue", "Restaurantes e delivery"],
    "Transporte": ["Combustível", "Transporte por aplicativo e ônibus", "Manutenção do veículo", "Seguro do veículo",
                   "IPVA e licenciamento", "Estacionamento e pedágio"],
    "Saúde": ["Plano de saúde", "Farmácia", "Consultas e exames", "Dentista", "Academia e esporte"],
    "Educação": ["Escola e faculdade", "Cursos", "Livros e material escolar"],
    "Lazer e cultura": ["Streaming e assinaturas", "Viagens", "Passeios, cinema e eventos", "Hobbies"],
    "Cuidados pessoais": ["Vestuário e calçados", "Salão e barbearia", "Cosméticos e perfumaria"],
    "Pets": ["Ração e petshop", "Veterinário"],
    "Dívidas e financiamentos": ["Empréstimo", "Financiamento", "Juros e tarifas bancárias", "Renegociação"],
    "Impostos e taxas": ["Imposto de renda", "Taxas e documentos"],
    "Seguros": ["Seguro de vida", "Seguro residencial"],
    "Presentes e doações": ["Presentes", "Doações"],
    "Profissional": ["Ferramentas e softwares", "Contador e impostos do negócio", "Equipamentos"],
    "Reserva e investimentos": ["Reserva de emergência", "Investimentos", "Previdência"],
    "Outros gastos": [],
}
INCOME_TYPES = ["Salário", "Pró-labore", "Distribuição de lucros", "Freelance e serviços", "Comissões e bônus",
                "13º salário", "Férias", "Aluguel recebido", "Rendimentos de investimentos", "Benefício do INSS",
                "Pensão", "Reembolso", "Venda de bens", "Presente ou doação recebida", "Outras rendas"]
CREDITORS = ["Imobiliária ou proprietário", "Administradora do condomínio", "Celesc (energia)", "Águas de Joinville",
             "Distribuidora de gás", "Provedor de internet", "Operadora de celular", "Prefeitura de Joinville (IPTU)",
             "Detran SC", "Receita Federal", "Supermercado", "Padaria", "Farmácia", "Posto de combustível",
             "Plano de saúde", "Dentista", "Escola", "Academia", "Petshop", "Banco", "Cartão de crédito",
             "Netflix", "Spotify", "Amazon", "Mercado Livre", "Seguradora", "Contador", "Mecânica", "Outro credor"]
PAYMENT_METHODS = ["Pix", "Dinheiro", "Débito automático", "Boleto", "Cartão de débito", "Cartão de crédito",
                   "Transferência bancária", "Vale-alimentação ou refeição", "Carteira digital"]


def f(key, label, type_, required=False, max_length=None, min_=None, max_=None, options=None):
    return {"key": key, "label": label, "type": type_, "required": required, "maxLength": max_length,
            "min": min_, "max": max_, "options": options}


REFERENCIA = f("referencia", "Mês de referência (MM/AAAA)", "text", False, 7)
EXPENSE_TYPES = [
    # (nome, comportamento, categoria padrão, campos)
    ("Aluguel", "recurring", "Aluguel", [f("contrato", "Número do contrato", "text", False, 60),
                                        f("reajuste", "Data do próximo reajuste", "date")]),
    ("Condomínio", "recurring", "Condomínio", [REFERENCIA]),
    ("Conta de luz", "recurring", "Energia elétrica", [REFERENCIA, f("kwh", "Consumo (kWh)", "decimal", False, None, 0, 100000),
                                                       f("unidade", "Unidade consumidora", "text", False, 30)]),
    ("Conta de água", "recurring", "Água e esgoto", [REFERENCIA, f("m3", "Consumo (m³)", "decimal", False, None, 0, 100000)]),
    ("Gás", "single", "Gás", [f("tipo", "Tipo", "list", True, options=["Botijão 13 kg", "Gás encanado", "Outro"])]),
    ("Internet e TV", "recurring", "Internet e TV", [f("plano", "Plano contratado", "text", False, 80)]),
    ("Telefone celular", "recurring", "Telefone celular", [f("linha", "Linha (número)", "text", False, 20)]),
    ("IPTU", "installments", "IPTU", [f("exercicio", "Exercício (ano)", "text", True, 4),
                                      f("cota_unica", "Pago em cota única", "boolean")]),
    ("IPVA e licenciamento", "installments", "IPVA e licenciamento", [f("placa", "Placa do veículo", "text", True, 8),
                                                                      f("exercicio", "Exercício (ano)", "text", True, 4)]),
    ("Supermercado", "single", "Supermercado", [f("loja", "Loja", "text", False, 80)]),
    ("Feira e hortifrúti", "single", "Feira e hortifrúti", []),
    ("Restaurante e delivery", "single", "Restaurantes e delivery", [f("estabelecimento", "Estabelecimento", "text", False, 80)]),
    ("Combustível", "single", "Combustível", [f("combustivel", "Combustível", "list", True, options=["Gasolina", "Etanol", "Diesel", "GNV"]),
                                              f("litros", "Litros", "decimal", False, None, 0, 1000),
                                              f("km", "Quilometragem no abastecimento", "decimal", False, None, 0, 9999999)]),
    ("Manutenção do veículo", "single", "Manutenção do veículo", [f("servico", "Serviço realizado", "text", True, 120),
                                                                 f("km", "Quilometragem", "decimal", False, None, 0, 9999999)]),
    ("Transporte por aplicativo", "single", "Transporte por aplicativo e ônibus", [f("app", "Aplicativo", "list", False, options=["Uber", "99", "Ônibus", "Táxi", "Outro"])]),
    ("Plano de saúde", "recurring", "Plano de saúde", [f("operadora", "Operadora", "text", False, 80),
                                                       f("beneficiarios", "Beneficiários", "text", False, 160)]),
    ("Farmácia", "single", "Farmácia", [f("com_receita", "Com receita médica", "boolean")]),
    ("Consulta ou exame", "single", "Consultas e exames", [f("profissional", "Profissional ou clínica", "text", False, 120),
                                                          f("reembolsavel", "Pedir reembolso ao plano", "boolean")]),
    ("Academia", "recurring", "Academia e esporte", []),
    ("Mensalidade escolar", "recurring", "Escola e faculdade", [f("aluno", "Aluno", "text", True, 80), REFERENCIA]),
    ("Curso", "installments", "Cursos", [f("curso", "Curso", "text", True, 120), f("aluno", "Aluno", "text", False, 80)]),
    ("Assinatura ou streaming", "recurring", "Streaming e assinaturas", [f("servico", "Serviço", "text", True, 80)]),
    ("Viagem", "single", "Viagens", [f("destino", "Destino", "text", True, 120), f("periodo", "Período", "text", False, 60)]),
    ("Empréstimo", "installments", "Empréstimo", [f("banco", "Banco ou financeira", "text", True, 80),
                                                 f("contrato", "Número do contrato", "text", False, 60),
                                                 f("juros_mes", "Juros ao mês (%)", "decimal", False, None, 0, 100)]),
    ("Financiamento", "installments", "Financiamento", [f("bem", "Bem financiado", "list", True, options=["Imóvel", "Veículo", "Outro"]),
                                                       f("banco", "Banco ou financeira", "text", True, 80),
                                                       f("contrato", "Número do contrato", "text", False, 60)]),
    ("Renegociação de dívida", "installments", "Renegociação", [f("credor_original", "Credor original", "text", True, 120),
                                                               f("acordo", "Número do acordo", "text", False, 60)]),
    ("Compra parcelada", "installments", None, [f("loja", "Loja", "text", True, 80), f("produto", "Produto", "text", True, 120)]),
    ("Compra no cartão", "card", None, [f("loja", "Loja", "text", False, 80)]),
    ("Seguro", "recurring", "Seguros", [f("tipo", "Tipo de seguro", "list", True, options=["Vida", "Residencial", "Veículo", "Outro"]),
                                        f("apolice", "Apólice", "text", False, 60)]),
    ("Imposto de renda (DARF)", "single", "Imposto de renda", [f("exercicio", "Exercício (ano)", "text", True, 4)]),
    ("Taxa ou documento", "single", "Taxas e documentos", [f("descricao", "Descrição", "text", True, 160)]),
    ("Tarifa bancária ou juros", "single", "Juros e tarifas bancárias", [f("banco", "Banco", "text", False, 80)]),
    ("Manutenção da casa", "single", "Manutenção e reparos", [f("servico", "Serviço", "text", True, 120),
                                                             f("prestador", "Prestador", "text", False, 80)]),
    ("Móveis e eletrodomésticos", "installments", "Móveis e eletrodomésticos", [f("item", "Item comprado", "text", True, 120),
                                                                              f("garantia_ate", "Garantia até", "date")]),
    ("Vestuário", "single", "Vestuário e calçados", [f("para_quem", "Para quem", "text", False, 80)]),
    ("Salão e beleza", "single", "Salão e barbearia", []),
    ("Pet", "single", "Ração e petshop", [f("tipo", "Tipo", "list", True, options=["Ração", "Petshop", "Banho e tosa", "Veterinário", "Outro"]),
                                         f("animal", "Animal", "text", False, 60)]),
    ("Presente", "single", "Presentes", [f("para_quem", "Para quem", "text", True, 80), f("ocasiao", "Ocasião", "text", False, 80)]),
    ("Doação", "single", "Doações", [f("instituicao", "Instituição ou pessoa", "text", False, 120)]),
    ("Software ou ferramenta de trabalho", "recurring", "Ferramentas e softwares", [f("servico", "Serviço", "text", True, 80)]),
    ("Contador e impostos do negócio", "recurring", "Contador e impostos do negócio", [f("tipo", "Tipo", "list", True, options=["Honorários do contador", "DAS (MEI/Simples)", "Outro imposto"])]),
    ("Reserva ou investimento", "recurring", "Reserva de emergência", [f("destino", "Onde foi aplicado", "text", False, 120)]),
    ("Despesa avulsa", "single", "Outros gastos", [f("descricao", "Descrição", "text", False, 160)]),
]

# ----------------------------------------------------------------------------- aplicação
created = {"categories": 0, "income-types": 0, "creditors": 0, "payment-methods": 0, "expense-types": 0}
errors = []


def ensure(kind, name, parent_code=None):
    if name in existing[kind]:
        return existing[kind][name]["code"]
    if not APPLY:
        print(f"  [faltando] {kind}: {name}")
        return None
    status, body = call("POST", f"/api/backend/catalogs/{kind}",
                        {"name": name, "isActive": True, "version": 0, "parentCode": parent_code})
    if status == 201:
        item = unwrap(body)
        existing[kind][name] = item
        created[kind] += 1
        return item["code"]
    errors.append((kind, name, status, body))
    print(f"  ERRO {kind} {name}: {status} {body}")
    return None


for parent, children in CATEGORIES.items():
    parent_code = ensure("categories", parent)
    for child in children:
        ensure("categories", child, parent_code)
for name in INCOME_TYPES:
    ensure("income-types", name)
for name in CREDITORS:
    ensure("creditors", name)
for name in PAYMENT_METHODS:
    ensure("payment-methods", name)

for name, behavior, category, fields in EXPENSE_TYPES:
    if name in existing_types:
        continue
    cat_code = existing["categories"].get(category, {}).get("code") if category else None
    if category and not cat_code:
        print(f"  [aviso] categoria '{category}' não encontrada para o tipo '{name}'")
    if not APPLY:
        print(f"  [faltando] expense-types: {name}")
        continue
    status, body = call("POST", "/api/backend/expense-types",
                        {"name": name, "isActive": True, "version": 0, "behavior": behavior,
                         "defaultCategoryCode": cat_code, "fields": fields})
    if status == 201:
        created["expense-types"] += 1
        existing_types[name] = unwrap(body)
    else:
        errors.append(("expense-types", name, status, body))
        print(f"  ERRO expense-types {name}: {status} {body}")

print("\nResumo:", "criados" if APPLY else "faltando (sonda, nada gravado)")
for kind in KINDS:
    print(f"  {kind}: {created[kind]} criado(s); total agora {len(existing[kind])}")
print(f"  expense-types: {created['expense-types']} criado(s); total agora {len(existing_types)}")
print(f"  erros: {len(errors)}")
sys.exit(1 if errors else 0)
