using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountService.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionBalanceAndTransferFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvailableBalance",
                table: "Transaction",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DestinationAccountId",
                table: "Transaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationAccountIdentification",
                table: "Transaction",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedBalance",
                table: "Transaction",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_DestinationAccountIdentification",
                table: "Transaction",
                column: "DestinationAccountIdentification");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Accounts_DestinationAccountIdentification",
                table: "Transaction",
                column: "DestinationAccountIdentification",
                principalTable: "Accounts",
                principalColumn: "Identification",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Accounts_DestinationAccountIdentification",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_DestinationAccountIdentification",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "AvailableBalance",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "DestinationAccountId",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "DestinationAccountIdentification",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "ReservedBalance",
                table: "Transaction");
        }
    }
}
