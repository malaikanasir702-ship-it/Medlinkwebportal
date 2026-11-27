using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedLinkPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddVisaApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisaApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferenceId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TourismRequestId = table.Column<int>(type: "integer", nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Nationality = table.Column<string>(type: "text", nullable: false),
                    PassportNumber = table.Column<string>(type: "text", nullable: false),
                    PassportExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TreatmentType = table.Column<string>(type: "text", nullable: false),
                    HospitalName = table.Column<string>(type: "text", nullable: true),
                    PlannedArrivalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MedicalReportsUrl = table.Column<string>(type: "text", nullable: true),
                    BankStatementUrl = table.Column<string>(type: "text", nullable: true),
                    PassportCopyUrl = table.Column<string>(type: "text", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    InvitationLetterUrl = table.Column<string>(type: "text", nullable: true),
                    ProcessedBy = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisaApplications_MedicalTourismRequests_TourismRequestId",
                        column: x => x.TourismRequestId,
                        principalTable: "MedicalTourismRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_ReferenceId",
                table: "VisaApplications",
                column: "ReferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_UserId",
                table: "VisaApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_TourismRequestId",
                table: "VisaApplications",
                column: "TourismRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VisaApplications");
        }
    }
}
