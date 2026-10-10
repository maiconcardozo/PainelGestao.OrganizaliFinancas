using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreditCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdCardInvoice",
                table: "Obligation",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 11);

            migrationBuilder.CreateTable(
                name: "CreditCard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdHolderPerson = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    ClosingDay = table.Column<int>(type: "int", nullable: false),
                    DueDay = table.Column<int>(type: "int", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: true),
                    ClosingDayRule = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DtCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    DtUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    DtDeleted = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCard", x => x.Id);
                    table.UniqueConstraint("AK_CreditCard_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_CreditCard_Person_IdTenant_IdHolderPerson",
                        columns: x => new { x.IdTenant, x.IdHolderPerson },
                        principalTable: "Person",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CardInvoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdCreditCard = table.Column<int>(type: "int", nullable: false),
                    ReferenceMonth = table.Column<DateTime>(type: "date", nullable: false),
                    ClosingDate = table.Column<DateTime>(type: "date", nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DtCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    DtUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    DtDeleted = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardInvoice", x => x.Id);
                    table.UniqueConstraint("AK_CardInvoice_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_CardInvoice_CreditCard_IdTenant_IdCreditCard",
                        columns: x => new { x.IdTenant, x.IdCreditCard },
                        principalTable: "CreditCard",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CardPurchase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdCreditCard = table.Column<int>(type: "int", nullable: false),
                    IdBuyerPerson = table.Column<int>(type: "int", nullable: true),
                    IdExpenseType = table.Column<int>(type: "int", nullable: true),
                    IdCategory = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ExpenseTypeVersion = table.Column<int>(type: "int", nullable: true),
                    FieldValuesJson = table.Column<string>(type: "longtext", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    InstallmentCount = table.Column<int>(type: "int", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DtCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    DtUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    DtDeleted = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPurchase", x => x.Id);
                    table.UniqueConstraint("AK_CardPurchase_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_CardPurchase_Category_IdTenant_IdCategory",
                        columns: x => new { x.IdTenant, x.IdCategory },
                        principalTable: "Category",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardPurchase_CreditCard_IdTenant_IdCreditCard",
                        columns: x => new { x.IdTenant, x.IdCreditCard },
                        principalTable: "CreditCard",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardPurchase_ExpenseType_IdTenant_IdExpenseType",
                        columns: x => new { x.IdTenant, x.IdExpenseType },
                        principalTable: "ExpenseType",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardPurchase_Person_IdTenant_IdBuyerPerson",
                        columns: x => new { x.IdTenant, x.IdBuyerPerson },
                        principalTable: "Person",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CardItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdCardPurchase = table.Column<int>(type: "int", nullable: false),
                    IdCardInvoice = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    Competence = table.Column<DateTime>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DtCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    DtUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    DtDeleted = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardItem_CardInvoice_IdTenant_IdCardInvoice",
                        columns: x => new { x.IdTenant, x.IdCardInvoice },
                        principalTable: "CardInvoice",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardItem_CardPurchase_IdTenant_IdCardPurchase",
                        columns: x => new { x.IdTenant, x.IdCardPurchase },
                        principalTable: "CardPurchase",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_IdCardInvoice",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdCardInvoice" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardInvoice_Code",
                table: "CardInvoice",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardInvoice_IdTenant_IdCreditCard_ReferenceMonth",
                table: "CardInvoice",
                columns: new[] { "IdTenant", "IdCreditCard", "ReferenceMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardItem_Code",
                table: "CardItem",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardItem_IdTenant_Competence",
                table: "CardItem",
                columns: new[] { "IdTenant", "Competence" });

            migrationBuilder.CreateIndex(
                name: "IX_CardItem_IdTenant_IdCardInvoice_IsActive",
                table: "CardItem",
                columns: new[] { "IdTenant", "IdCardInvoice", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CardItem_IdTenant_IdCardPurchase",
                table: "CardItem",
                columns: new[] { "IdTenant", "IdCardPurchase" });

            migrationBuilder.CreateIndex(
                name: "IX_CardPurchase_Code",
                table: "CardPurchase",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardPurchase_IdTenant_IdBuyerPerson",
                table: "CardPurchase",
                columns: new[] { "IdTenant", "IdBuyerPerson" });

            migrationBuilder.CreateIndex(
                name: "IX_CardPurchase_IdTenant_IdCategory",
                table: "CardPurchase",
                columns: new[] { "IdTenant", "IdCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_CardPurchase_IdTenant_IdCreditCard",
                table: "CardPurchase",
                columns: new[] { "IdTenant", "IdCreditCard" });

            migrationBuilder.CreateIndex(
                name: "IX_CardPurchase_IdTenant_IdExpenseType",
                table: "CardPurchase",
                columns: new[] { "IdTenant", "IdExpenseType" });

            migrationBuilder.CreateIndex(
                name: "IX_CardPurchase_IdTenant_PurchaseDate",
                table: "CardPurchase",
                columns: new[] { "IdTenant", "PurchaseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCard_Code",
                table: "CreditCard",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditCard_IdTenant_IdHolderPerson",
                table: "CreditCard",
                columns: new[] { "IdTenant", "IdHolderPerson" });

            migrationBuilder.AddForeignKey(
                name: "FK_Obligation_CardInvoice_IdTenant_IdCardInvoice",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdCardInvoice" },
                principalTable: "CardInvoice",
                principalColumns: new[] { "IdTenant", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Obligation_CardInvoice_IdTenant_IdCardInvoice",
                table: "Obligation");

            migrationBuilder.DropTable(
                name: "CardItem");

            migrationBuilder.DropTable(
                name: "CardInvoice");

            migrationBuilder.DropTable(
                name: "CardPurchase");

            migrationBuilder.DropTable(
                name: "CreditCard");

            migrationBuilder.DropIndex(
                name: "IX_Obligation_IdTenant_IdCardInvoice",
                table: "Obligation");

            migrationBuilder.DropColumn(
                name: "IdCardInvoice",
                table: "Obligation");

        }
    }
}
