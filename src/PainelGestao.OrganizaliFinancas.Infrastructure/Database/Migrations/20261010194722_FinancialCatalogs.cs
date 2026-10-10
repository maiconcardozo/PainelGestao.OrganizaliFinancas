using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class FinancialCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogRevision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    Catalog = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    ItemCode = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    ParentCode = table.Column<Guid>(type: "char(36)", nullable: true),
                    ItemActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_CatalogRevision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogRevision_Tenant_IdTenant",
                        column: x => x.IdTenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdParentCategory = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
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
                    table.PrimaryKey("PK_Category", x => x.Id);
                    table.UniqueConstraint("AK_Category_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_Category_Category_IdTenant_IdParentCategory",
                        columns: x => new { x.IdTenant, x.IdParentCategory },
                        principalTable: "Category",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Category_Tenant_IdTenant",
                        column: x => x.IdTenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Creditor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
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
                    table.PrimaryKey("PK_Creditor", x => x.Id);
                    table.UniqueConstraint("AK_Creditor_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_Creditor_Tenant_IdTenant",
                        column: x => x.IdTenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IncomeType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
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
                    table.PrimaryKey("PK_IncomeType", x => x.Id);
                    table.UniqueConstraint("AK_IncomeType_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_IncomeType_Tenant_IdTenant",
                        column: x => x.IdTenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PaymentMethod",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
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
                    table.PrimaryKey("PK_PaymentMethod", x => x.Id);
                    table.UniqueConstraint("AK_PaymentMethod_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_PaymentMethod_Tenant_IdTenant",
                        column: x => x.IdTenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogRevision_Code",
                table: "CatalogRevision",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogRevision_IdTenant_Catalog_ItemCode_Version",
                table: "CatalogRevision",
                columns: new[] { "IdTenant", "Catalog", "ItemCode", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Category_Code",
                table: "Category",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Category_IdTenant_IdParentCategory",
                table: "Category",
                columns: new[] { "IdTenant", "IdParentCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_Creditor_Code",
                table: "Creditor",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeType_Code",
                table: "IncomeType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethod_Code",
                table: "PaymentMethod",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogRevision");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Creditor");

            migrationBuilder.DropTable(
                name: "IncomeType");

            migrationBuilder.DropTable(
                name: "PaymentMethod");
        }
    }
}
