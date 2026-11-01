using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Database.Migrations
{
    /// <summary>A17: a payment may come from a credit card instead of a bank account; it then points to the purchase it created in the invoice.</summary>
    public partial class CardPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "IdFinancialAccount",
                table: "Payment",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "IdCardPurchase",
                table: "Payment",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_IdTenant_IdCardPurchase",
                table: "Payment",
                columns: new[] { "IdTenant", "IdCardPurchase" });

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_CardPurchase_IdTenant_IdCardPurchase",
                table: "Payment",
                columns: new[] { "IdTenant", "IdCardPurchase" },
                principalTable: "CardPurchase",
                principalColumns: new[] { "IdTenant", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_CardPurchase_IdTenant_IdCardPurchase",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_IdTenant_IdCardPurchase",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "IdCardPurchase",
                table: "Payment");

            migrationBuilder.AlterColumn<int>(
                name: "IdFinancialAccount",
                table: "Payment",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
