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
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='MedicalTourismRequests' AND column_name='WantsHotel') THEN
                        ALTER TABLE ""MedicalTourismRequests"" ADD ""WantsHotel"" boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='MedicalTourismRequests' AND column_name='HotelPreference') THEN
                        ALTER TABLE ""MedicalTourismRequests"" ADD ""HotelPreference"" text DEFAULT 'Not Required';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='MedicalTourismRequests' AND column_name='HotelNights') THEN
                        ALTER TABLE ""MedicalTourismRequests"" ADD ""HotelNights"" integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='MedicalTourismRequests' AND column_name='WantsAirportPickup') THEN
                        ALTER TABLE ""MedicalTourismRequests"" ADD ""WantsAirportPickup"" boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='MedicalTourismRequests' AND column_name='TransportPreference') THEN
                        ALTER TABLE ""MedicalTourismRequests"" ADD ""TransportPreference"" text DEFAULT 'Not Required';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='MedicalTourismRequests' AND column_name='PreArrivalAppointmentId') THEN
                        ALTER TABLE ""MedicalTourismRequests"" ADD ""PreArrivalAppointmentId"" integer;
                    END IF;
                END
                $$;

                CREATE INDEX IF NOT EXISTS ""IX_MedicalTourismRequests_PreArrivalAppointmentId"" ON ""MedicalTourismRequests"" (""PreArrivalAppointmentId"");

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_MedicalTourismRequests_Appointments_PreArrivalAppointmentId') THEN
                        ALTER TABLE ""MedicalTourismRequests"" ADD CONSTRAINT ""FK_MedicalTourismRequests_Appointments_PreArrivalAppointmentId"" FOREIGN KEY (""PreArrivalAppointmentId"") REFERENCES ""Appointments"" (""Id"") ON DELETE SET NULL;
                    END IF;
                END
                $$;
            ");
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
