using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class GoalsAttachmentsImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attachment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    TargetKind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    TargetCode = table.Column<Guid>(type: "char(36)", nullable: false),
                    FileName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Sha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_Attachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachment_Tenant_IdTenant",
                        column: x => x.IdTenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportBatch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdFinancialAccount = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Format = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    LineCount = table.Column<int>(type: "int", nullable: false),
                    AppliedCount = table.Column<int>(type: "int", nullable: false),
                    IgnoredCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ImportBatch", x => x.Id);
                    table.UniqueConstraint("AK_ImportBatch_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_ImportBatch_FinancialAccount_IdTenant_IdFinancialAccount",
                        columns: x => new { x.IdTenant, x.IdFinancialAccount },
                        principalTable: "FinancialAccount",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SavingsGoal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdFinancialAccount = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    TargetAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    TargetDate = table.Column<DateTime>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_SavingsGoal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingsGoal_FinancialAccount_IdTenant_IdFinancialAccount",
                        columns: x => new { x.IdTenant, x.IdFinancialAccount },
                        principalTable: "FinancialAccount",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdImportBatch = table.Column<int>(type: "int", nullable: false),
                    Fingerprint = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    LineDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ExternalId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ResultCode = table.Column<Guid>(type: "char(36)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DtCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportLine_ImportBatch_IdTenant_IdImportBatch",
                        columns: x => new { x.IdTenant, x.IdImportBatch },
                        principalTable: "ImportBatch",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_Code",
                table: "Attachment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_IdTenant_TargetKind_TargetCode",
                table: "Attachment",
                columns: new[] { "IdTenant", "TargetKind", "TargetCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatch_Code",
                table: "ImportBatch",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatch_IdTenant_IdFinancialAccount",
                table: "ImportBatch",
                columns: new[] { "IdTenant", "IdFinancialAccount" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportLine_Code",
                table: "ImportLine",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportLine_IdTenant_Fingerprint",
                table: "ImportLine",
                columns: new[] { "IdTenant", "Fingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportLine_IdTenant_IdImportBatch",
                table: "ImportLine",
                columns: new[] { "IdTenant", "IdImportBatch" });

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoal_Code",
                table: "SavingsGoal",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoal_IdTenant_IdFinancialAccount",
                table: "SavingsGoal",
                columns: new[] { "IdTenant", "IdFinancialAccount" });

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoal_IdTenant_IsActive",
                table: "SavingsGoal",
                columns: new[] { "IdTenant", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachment");

            migrationBuilder.DropTable(
                name: "ImportLine");

            migrationBuilder.DropTable(
                name: "SavingsGoal");

            migrationBuilder.DropTable(
                name: "ImportBatch");
        }
    }
}
