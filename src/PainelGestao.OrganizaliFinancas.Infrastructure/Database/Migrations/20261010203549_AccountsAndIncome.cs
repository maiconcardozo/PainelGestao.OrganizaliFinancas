using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AccountsAndIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    OpeningDate = table.Column<DateTime>(type: "date", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_FinancialAccount", x => x.Id);
                    table.UniqueConstraint("AK_FinancialAccount_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_FinancialAccount_Tenant_IdTenant",
                        column: x => x.IdTenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IdempotencyRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    RequestKey = table.Column<Guid>(type: "char(36)", nullable: false),
                    RequestHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    ResultCode = table.Column<Guid>(type: "char(36)", nullable: false),
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
                    table.PrimaryKey("PK_IdempotencyRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdempotencyRecord_Tenant_IdTenant",
                        column: x => x.IdTenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IncomeSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdPerson = table.Column<int>(type: "int", nullable: false),
                    IdIncomeType = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_IncomeSource", x => x.Id);
                    table.UniqueConstraint("AK_IncomeSource_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_IncomeSource_IncomeType_IdTenant_IdIncomeType",
                        columns: x => new { x.IdTenant, x.IdIncomeType },
                        principalTable: "IncomeType",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncomeSource_Person_IdTenant_IdPerson",
                        columns: x => new { x.IdTenant, x.IdPerson },
                        principalTable: "Person",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Transfer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdSourceAccount = table.Column<int>(type: "int", nullable: false),
                    IdTargetAccount = table.Column<int>(type: "int", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    DtReversed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReversedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    ReversalReason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_Transfer", x => x.Id);
                    table.UniqueConstraint("AK_Transfer_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_Transfer_FinancialAccount_IdTenant_IdSourceAccount",
                        columns: x => new { x.IdTenant, x.IdSourceAccount },
                        principalTable: "FinancialAccount",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfer_FinancialAccount_IdTenant_IdTargetAccount",
                        columns: x => new { x.IdTenant, x.IdTargetAccount },
                        principalTable: "FinancialAccount",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IncomeSourceVersion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdIncomeSource = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    Frequency = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    AmountMode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    ExpectedNetAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_IncomeSourceVersion", x => x.Id);
                    table.UniqueConstraint("AK_IncomeSourceVersion_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_IncomeSourceVersion_IncomeSource_IdTenant_IdIncomeSource",
                        columns: x => new { x.IdTenant, x.IdIncomeSource },
                        principalTable: "IncomeSource",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Receipt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdFinancialAccount = table.Column<int>(type: "int", nullable: false),
                    IdPerson = table.Column<int>(type: "int", nullable: false),
                    IdIncomeSource = table.Column<int>(type: "int", nullable: true),
                    IdIncomeType = table.Column<int>(type: "int", nullable: true),
                    ReceiptDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    DtReversed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReversedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    ReversalReason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_Receipt", x => x.Id);
                    table.UniqueConstraint("AK_Receipt_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_Receipt_FinancialAccount_IdTenant_IdFinancialAccount",
                        columns: x => new { x.IdTenant, x.IdFinancialAccount },
                        principalTable: "FinancialAccount",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipt_IncomeSource_IdTenant_IdIncomeSource",
                        columns: x => new { x.IdTenant, x.IdIncomeSource },
                        principalTable: "IncomeSource",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipt_IncomeType_IdTenant_IdIncomeType",
                        columns: x => new { x.IdTenant, x.IdIncomeType },
                        principalTable: "IncomeType",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipt_Person_IdTenant_IdPerson",
                        columns: x => new { x.IdTenant, x.IdPerson },
                        principalTable: "Person",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IncomeForecast",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdIncomeSource = table.Column<int>(type: "int", nullable: false),
                    IdIncomeSourceVersion = table.Column<int>(type: "int", nullable: false),
                    SourceVersion = table.Column<int>(type: "int", nullable: false),
                    OccurrenceDate = table.Column<DateTime>(type: "date", nullable: false),
                    Competence = table.Column<DateTime>(type: "date", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    CloseReason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_IncomeForecast", x => x.Id);
                    table.UniqueConstraint("AK_IncomeForecast_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_IncomeForecast_IncomeSourceVersion_IdTenant_IdIncomeSourceVe~",
                        columns: x => new { x.IdTenant, x.IdIncomeSourceVersion },
                        principalTable: "IncomeSourceVersion",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncomeForecast_IncomeSource_IdTenant_IdIncomeSource",
                        columns: x => new { x.IdTenant, x.IdIncomeSource },
                        principalTable: "IncomeSource",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AccountMovement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdFinancialAccount = table.Column<int>(type: "int", nullable: false),
                    IdTransfer = table.Column<int>(type: "int", nullable: true),
                    IdReceipt = table.Column<int>(type: "int", nullable: true),
                    IdReversedMovement = table.Column<int>(type: "int", nullable: true),
                    MovementDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_AccountMovement", x => x.Id);
                    table.UniqueConstraint("AK_AccountMovement_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_AccountMovement_AccountMovement_IdTenant_IdReversedMovement",
                        columns: x => new { x.IdTenant, x.IdReversedMovement },
                        principalTable: "AccountMovement",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountMovement_FinancialAccount_IdTenant_IdFinancialAccount",
                        columns: x => new { x.IdTenant, x.IdFinancialAccount },
                        principalTable: "FinancialAccount",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountMovement_Receipt_IdTenant_IdReceipt",
                        columns: x => new { x.IdTenant, x.IdReceipt },
                        principalTable: "Receipt",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountMovement_Transfer_IdTenant_IdTransfer",
                        columns: x => new { x.IdTenant, x.IdTransfer },
                        principalTable: "Transfer",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReceiptAllocation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdReceipt = table.Column<int>(type: "int", nullable: false),
                    IdIncomeForecast = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_ReceiptAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocation_IncomeForecast_IdTenant_IdIncomeForecast",
                        columns: x => new { x.IdTenant, x.IdIncomeForecast },
                        principalTable: "IncomeForecast",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocation_Receipt_IdTenant_IdReceipt",
                        columns: x => new { x.IdTenant, x.IdReceipt },
                        principalTable: "Receipt",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AccountMovement_Code",
                table: "AccountMovement",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountMovement_IdTenant_IdFinancialAccount_MovementDate",
                table: "AccountMovement",
                columns: new[] { "IdTenant", "IdFinancialAccount", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountMovement_IdTenant_IdReceipt",
                table: "AccountMovement",
                columns: new[] { "IdTenant", "IdReceipt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountMovement_IdTenant_IdReversedMovement",
                table: "AccountMovement",
                columns: new[] { "IdTenant", "IdReversedMovement" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountMovement_IdTenant_IdTransfer",
                table: "AccountMovement",
                columns: new[] { "IdTenant", "IdTransfer" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccount_Code",
                table: "FinancialAccount",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_Code",
                table: "IdempotencyRecord",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_IdTenant_Operation_RequestKey",
                table: "IdempotencyRecord",
                columns: new[] { "IdTenant", "Operation", "RequestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeForecast_Code",
                table: "IncomeForecast",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeForecast_IdTenant_Competence",
                table: "IncomeForecast",
                columns: new[] { "IdTenant", "Competence" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomeForecast_IdTenant_IdIncomeSource_OccurrenceDate",
                table: "IncomeForecast",
                columns: new[] { "IdTenant", "IdIncomeSource", "OccurrenceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeForecast_IdTenant_IdIncomeSourceVersion",
                table: "IncomeForecast",
                columns: new[] { "IdTenant", "IdIncomeSourceVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomeSource_Code",
                table: "IncomeSource",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeSource_IdTenant_IdIncomeType",
                table: "IncomeSource",
                columns: new[] { "IdTenant", "IdIncomeType" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomeSource_IdTenant_IdPerson",
                table: "IncomeSource",
                columns: new[] { "IdTenant", "IdPerson" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomeSourceVersion_Code",
                table: "IncomeSourceVersion",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeSourceVersion_IdTenant_IdIncomeSource_Version",
                table: "IncomeSourceVersion",
                columns: new[] { "IdTenant", "IdIncomeSource", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_Code",
                table: "Receipt",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_IdTenant_IdFinancialAccount",
                table: "Receipt",
                columns: new[] { "IdTenant", "IdFinancialAccount" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_IdTenant_IdIncomeSource",
                table: "Receipt",
                columns: new[] { "IdTenant", "IdIncomeSource" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_IdTenant_IdIncomeType",
                table: "Receipt",
                columns: new[] { "IdTenant", "IdIncomeType" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_IdTenant_IdPerson_ReceiptDate",
                table: "Receipt",
                columns: new[] { "IdTenant", "IdPerson", "ReceiptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_IdTenant_ReceiptDate",
                table: "Receipt",
                columns: new[] { "IdTenant", "ReceiptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocation_Code",
                table: "ReceiptAllocation",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocation_IdTenant_IdIncomeForecast",
                table: "ReceiptAllocation",
                columns: new[] { "IdTenant", "IdIncomeForecast" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocation_IdTenant_IdReceipt_IdIncomeForecast",
                table: "ReceiptAllocation",
                columns: new[] { "IdTenant", "IdReceipt", "IdIncomeForecast" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_Code",
                table: "Transfer",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_IdTenant_IdSourceAccount",
                table: "Transfer",
                columns: new[] { "IdTenant", "IdSourceAccount" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_IdTenant_IdTargetAccount",
                table: "Transfer",
                columns: new[] { "IdTenant", "IdTargetAccount" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_IdTenant_TransferDate",
                table: "Transfer",
                columns: new[] { "IdTenant", "TransferDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountMovement");

            migrationBuilder.DropTable(
                name: "IdempotencyRecord");

            migrationBuilder.DropTable(
                name: "ReceiptAllocation");

            migrationBuilder.DropTable(
                name: "Transfer");

            migrationBuilder.DropTable(
                name: "IncomeForecast");

            migrationBuilder.DropTable(
                name: "Receipt");

            migrationBuilder.DropTable(
                name: "IncomeSourceVersion");

            migrationBuilder.DropTable(
                name: "FinancialAccount");

            migrationBuilder.DropTable(
                name: "IncomeSource");
        }
    }
}
