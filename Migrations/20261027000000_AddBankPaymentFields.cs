using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedLinkPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddBankPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add BankAccountTitle (stored encrypted)
            migrationBuilder.AddColumn<string>(
                name: "BankAccountTitle",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            // Add BankName (plain text)
            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountTitle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "AspNetUsers");
        }
    }
}
