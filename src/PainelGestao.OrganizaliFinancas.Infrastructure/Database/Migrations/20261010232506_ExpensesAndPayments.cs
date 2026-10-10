using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ExpensesAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdPayment",
                table: "AccountMovement",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 7);

            migrationBuilder.CreateTable(
                name: "InstallmentPlan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    InstallmentCount = table.Column<int>(type: "int", nullable: false),
                    FirstDueDate = table.Column<DateTime>(type: "date", nullable: false),
                    Frequency = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_InstallmentPlan", x => x.Id);
                    table.UniqueConstraint("AK_InstallmentPlan_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_InstallmentPlan_Tenant_IdTenant",
                        column: x => x.IdTenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdFinancialAccount = table.Column<int>(type: "int", nullable: false),
                    IdPayerPerson = table.Column<int>(type: "int", nullable: true),
                    IdPaymentMethod = table.Column<int>(type: "int", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "date", nullable: false),
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
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.UniqueConstraint("AK_Payment_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_Payment_FinancialAccount_IdTenant_IdFinancialAccount",
                        columns: x => new { x.IdTenant, x.IdFinancialAccount },
                        principalTable: "FinancialAccount",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_PaymentMethod_IdTenant_IdPaymentMethod",
                        columns: x => new { x.IdTenant, x.IdPaymentMethod },
                        principalTable: "PaymentMethod",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_Person_IdTenant_IdPayerPerson",
                        columns: x => new { x.IdTenant, x.IdPayerPerson },
                        principalTable: "Person",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RecurringExpense",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdExpenseType = table.Column<int>(type: "int", nullable: true),
                    IdCategory = table.Column<int>(type: "int", nullable: true),
                    IdCreditor = table.Column<int>(type: "int", nullable: true),
                    IdPaymentMethod = table.Column<int>(type: "int", nullable: true),
                    IdPayerPerson = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ExpenseTypeVersion = table.Column<int>(type: "int", nullable: true),
                    FieldValuesJson = table.Column<string>(type: "longtext", nullable: false),
                    SplitMode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    SharesJson = table.Column<string>(type: "longtext", nullable: false),
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
                    table.PrimaryKey("PK_RecurringExpense", x => x.Id);
                    table.UniqueConstraint("AK_RecurringExpense_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_RecurringExpense_Category_IdTenant_IdCategory",
                        columns: x => new { x.IdTenant, x.IdCategory },
                        principalTable: "Category",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringExpense_Creditor_IdTenant_IdCreditor",
                        columns: x => new { x.IdTenant, x.IdCreditor },
                        principalTable: "Creditor",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringExpense_ExpenseType_IdTenant_IdExpenseType",
                        columns: x => new { x.IdTenant, x.IdExpenseType },
                        principalTable: "ExpenseType",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringExpense_PaymentMethod_IdTenant_IdPaymentMethod",
                        columns: x => new { x.IdTenant, x.IdPaymentMethod },
                        principalTable: "PaymentMethod",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringExpense_Person_IdTenant_IdPayerPerson",
                        columns: x => new { x.IdTenant, x.IdPayerPerson },
                        principalTable: "Person",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Obligation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdExpenseType = table.Column<int>(type: "int", nullable: true),
                    IdCategory = table.Column<int>(type: "int", nullable: true),
                    IdCreditor = table.Column<int>(type: "int", nullable: true),
                    IdPaymentMethod = table.Column<int>(type: "int", nullable: true),
                    IdPayerPerson = table.Column<int>(type: "int", nullable: true),
                    IdRecurringExpense = table.Column<int>(type: "int", nullable: true),
                    IdInstallmentPlan = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ExpenseTypeVersion = table.Column<int>(type: "int", nullable: true),
                    FieldValuesJson = table.Column<string>(type: "longtext", nullable: false),
                    SplitMode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    Competence = table.Column<DateTime>(type: "date", nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    IsEstimated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    CancelReason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    RecurrenceVersion = table.Column<int>(type: "int", nullable: true),
                    OccurrenceDate = table.Column<DateTime>(type: "date", nullable: true),
                    IsDetached = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: true),
                    InstallmentCount = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Obligation", x => x.Id);
                    table.UniqueConstraint("AK_Obligation_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_Obligation_Category_IdTenant_IdCategory",
                        columns: x => new { x.IdTenant, x.IdCategory },
                        principalTable: "Category",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Obligation_Creditor_IdTenant_IdCreditor",
                        columns: x => new { x.IdTenant, x.IdCreditor },
                        principalTable: "Creditor",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Obligation_ExpenseType_IdTenant_IdExpenseType",
                        columns: x => new { x.IdTenant, x.IdExpenseType },
                        principalTable: "ExpenseType",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Obligation_InstallmentPlan_IdTenant_IdInstallmentPlan",
                        columns: x => new { x.IdTenant, x.IdInstallmentPlan },
                        principalTable: "InstallmentPlan",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Obligation_PaymentMethod_IdTenant_IdPaymentMethod",
                        columns: x => new { x.IdTenant, x.IdPaymentMethod },
                        principalTable: "PaymentMethod",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Obligation_Person_IdTenant_IdPayerPerson",
                        columns: x => new { x.IdTenant, x.IdPayerPerson },
                        principalTable: "Person",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Obligation_RecurringExpense_IdTenant_IdRecurringExpense",
                        columns: x => new { x.IdTenant, x.IdRecurringExpense },
                        principalTable: "RecurringExpense",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RecurringExpenseVersion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdRecurringExpense = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    Frequency = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    IsEstimated = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_RecurringExpenseVersion", x => x.Id);
                    table.UniqueConstraint("AK_RecurringExpenseVersion_IdTenant_Id", x => new { x.IdTenant, x.Id });
                    table.ForeignKey(
                        name: "FK_RecurringExpenseVersion_RecurringExpense_IdTenant_IdRecurrin~",
                        columns: x => new { x.IdTenant, x.IdRecurringExpense },
                        principalTable: "RecurringExpense",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ObligationAdjustment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdObligation = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
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
                    table.PrimaryKey("PK_ObligationAdjustment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObligationAdjustment_Obligation_IdTenant_IdObligation",
                        columns: x => new { x.IdTenant, x.IdObligation },
                        principalTable: "Obligation",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ObligationShare",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdObligation = table.Column<int>(type: "int", nullable: false),
                    IdPerson = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
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
                    table.PrimaryKey("PK_ObligationShare", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObligationShare_Obligation_IdTenant_IdObligation",
                        columns: x => new { x.IdTenant, x.IdObligation },
                        principalTable: "Obligation",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObligationShare_Person_IdTenant_IdPerson",
                        columns: x => new { x.IdTenant, x.IdPerson },
                        principalTable: "Person",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PaymentAllocation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdTenant = table.Column<int>(type: "int", nullable: false),
                    IdPayment = table.Column<int>(type: "int", nullable: false),
                    IdObligation = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PaymentAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocation_Obligation_IdTenant_IdObligation",
                        columns: x => new { x.IdTenant, x.IdObligation },
                        principalTable: "Obligation",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocation_Payment_IdTenant_IdPayment",
                        columns: x => new { x.IdTenant, x.IdPayment },
                        principalTable: "Payment",
                        principalColumns: new[] { "IdTenant", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AccountMovement_IdTenant_IdPayment",
                table: "AccountMovement",
                columns: new[] { "IdTenant", "IdPayment" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlan_Code",
                table: "InstallmentPlan",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_Code",
                table: "Obligation",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_Competence",
                table: "Obligation",
                columns: new[] { "IdTenant", "Competence" });

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_DueDate",
                table: "Obligation",
                columns: new[] { "IdTenant", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_IdCategory",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_IdCreditor",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdCreditor" });

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_IdExpenseType",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdExpenseType" });

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_IdInstallmentPlan",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdInstallmentPlan" });

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_IdPayerPerson",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdPayerPerson" });

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_IdPaymentMethod",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdPaymentMethod" });

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_IdRecurringExpense_OccurrenceDate",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdRecurringExpense", "OccurrenceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObligationAdjustment_Code",
                table: "ObligationAdjustment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObligationAdjustment_IdTenant_IdObligation",
                table: "ObligationAdjustment",
                columns: new[] { "IdTenant", "IdObligation" });

            migrationBuilder.CreateIndex(
                name: "IX_ObligationShare_Code",
                table: "ObligationShare",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObligationShare_IdTenant_IdObligation_IsActive",
                table: "ObligationShare",
                columns: new[] { "IdTenant", "IdObligation", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ObligationShare_IdTenant_IdPerson",
                table: "ObligationShare",
                columns: new[] { "IdTenant", "IdPerson" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Code",
                table: "Payment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_IdTenant_IdFinancialAccount",
                table: "Payment",
                columns: new[] { "IdTenant", "IdFinancialAccount" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_IdTenant_IdPayerPerson",
                table: "Payment",
                columns: new[] { "IdTenant", "IdPayerPerson" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_IdTenant_IdPaymentMethod",
                table: "Payment",
                columns: new[] { "IdTenant", "IdPaymentMethod" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_IdTenant_PaymentDate",
                table: "Payment",
                columns: new[] { "IdTenant", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_Code",
                table: "PaymentAllocation",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_IdTenant_IdObligation",
                table: "PaymentAllocation",
                columns: new[] { "IdTenant", "IdObligation" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_IdTenant_IdPayment_IdObligation",
                table: "PaymentAllocation",
                columns: new[] { "IdTenant", "IdPayment", "IdObligation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_Code",
                table: "RecurringExpense",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_IdTenant_IdCategory",
                table: "RecurringExpense",
                columns: new[] { "IdTenant", "IdCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_IdTenant_IdCreditor",
                table: "RecurringExpense",
                columns: new[] { "IdTenant", "IdCreditor" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_IdTenant_IdExpenseType",
                table: "RecurringExpense",
                columns: new[] { "IdTenant", "IdExpenseType" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_IdTenant_IdPayerPerson",
                table: "RecurringExpense",
                columns: new[] { "IdTenant", "IdPayerPerson" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_IdTenant_IdPaymentMethod",
                table: "RecurringExpense",
                columns: new[] { "IdTenant", "IdPaymentMethod" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenseVersion_Code",
                table: "RecurringExpenseVersion",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenseVersion_IdTenant_IdRecurringExpense_Version",
                table: "RecurringExpenseVersion",
                columns: new[] { "IdTenant", "IdRecurringExpense", "Version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountMovement_Payment_IdTenant_IdPayment",
                table: "AccountMovement",
                columns: new[] { "IdTenant", "IdPayment" },
                principalTable: "Payment",
                principalColumns: new[] { "IdTenant", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountMovement_Payment_IdTenant_IdPayment",
                table: "AccountMovement");

            migrationBuilder.DropTable(
                name: "ObligationAdjustment");

            migrationBuilder.DropTable(
                name: "ObligationShare");

            migrationBuilder.DropTable(
                name: "PaymentAllocation");

            migrationBuilder.DropTable(
                name: "RecurringExpenseVersion");

            migrationBuilder.DropTable(
                name: "Obligation");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "InstallmentPlan");

            migrationBuilder.DropTable(
                name: "RecurringExpense");

            migrationBuilder.DropIndex(
                name: "IX_AccountMovement_IdTenant_IdPayment",
                table: "AccountMovement");

            migrationBuilder.DropColumn(
                name: "IdPayment",
                table: "AccountMovement");

        }
    }
}
