using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedLinkPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddTourismPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WantsHotel",
                table: "MedicalTourismRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HotelPreference",
                table: "MedicalTourismRequests",
                type: "text",
                nullable: true,
                defaultValue: "Not Required");

            migrationBuilder.AddColumn<int>(
                name: "HotelNights",
                table: "MedicalTourismRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WantsAirportPickup",
                table: "MedicalTourismRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TransportPreference",
                table: "MedicalTourismRequests",
                type: "text",
                nullable: true,
                defaultValue: "Not Required");

            migrationBuilder.AddColumn<int>(
                name: "PreArrivalAppointmentId",
                table: "MedicalTourismRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalTourismRequests_PreArrivalAppointmentId",
                table: "MedicalTourismRequests",
                column: "PreArrivalAppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalTourismRequests_Appointments_PreArrivalAppointmentId",
                table: "MedicalTourismRequests",
                column: "PreArrivalAppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalTourismRequests_Appointments_PreArrivalAppointmentId",
                table: "MedicalTourismRequests");

            migrationBuilder.DropIndex(
                name: "IX_MedicalTourismRequests_PreArrivalAppointmentId",
                table: "MedicalTourismRequests");

            migrationBuilder.DropColumn(name: "WantsHotel", table: "MedicalTourismRequests");
            migrationBuilder.DropColumn(name: "HotelPreference", table: "MedicalTourismRequests");
            migrationBuilder.DropColumn(name: "HotelNights", table: "MedicalTourismRequests");
            migrationBuilder.DropColumn(name: "WantsAirportPickup", table: "MedicalTourismRequests");
            migrationBuilder.DropColumn(name: "TransportPreference", table: "MedicalTourismRequests");
            migrationBuilder.DropColumn(name: "PreArrivalAppointmentId", table: "MedicalTourismRequests");
        }
    }
}
