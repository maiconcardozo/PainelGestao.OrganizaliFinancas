using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Database.Migrations
{
    /// <summary>
    /// A22: the household keeps a minimum reserve (initial R$ 15.000) and a bill or recurring expense may name the credit card it is usually
    /// paid with, so the cash leaves on that card's invoice due date in the margin, projection and cash flow.
    /// </summary>
    public partial class ReserveAndDefaultCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinimumReserve",
                table: "HouseholdSettings",
                type: "decimal(19,2)",
                precision: 19,
                scale: 2,
                nullable: false,
                defaultValue: 15000m);

            migrationBuilder.AddColumn<int>(
                name: "IdCreditCard",
                table: "Obligation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdCreditCard",
                table: "RecurringExpense",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Obligation_IdTenant_IdCreditCard",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdCreditCard" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpense_IdTenant_IdCreditCard",
                table: "RecurringExpense",
                columns: new[] { "IdTenant", "IdCreditCard" });

            migrationBuilder.AddForeignKey(
                name: "FK_Obligation_CreditCard_IdTenant_IdCreditCard",
                table: "Obligation",
                columns: new[] { "IdTenant", "IdCreditCard" },
                principalTable: "CreditCard",
                principalColumns: new[] { "IdTenant", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringExpense_CreditCard_IdTenant_IdCreditCard",
                table: "RecurringExpense",
                columns: new[] { "IdTenant", "IdCreditCard" },
                principalTable: "CreditCard",
                principalColumns: new[] { "IdTenant", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringExpense_CreditCard_IdTenant_IdCreditCard",
                table: "RecurringExpense");

            migrationBuilder.DropForeignKey(
                name: "FK_Obligation_CreditCard_IdTenant_IdCreditCard",
                table: "Obligation");

            migrationBuilder.DropIndex(
                name: "IX_RecurringExpense_IdTenant_IdCreditCard",
                table: "RecurringExpense");

            migrationBuilder.DropIndex(
                name: "IX_Obligation_IdTenant_IdCreditCard",
                table: "Obligation");

            migrationBuilder.DropColumn(
                name: "IdCreditCard",
                table: "RecurringExpense");

            migrationBuilder.DropColumn(
                name: "IdCreditCard",
                table: "Obligation");

            migrationBuilder.DropColumn(
                name: "MinimumReserve",
                table: "HouseholdSettings");
        }
    }
}
