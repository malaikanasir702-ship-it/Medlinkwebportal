using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedLinkPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorInConsultationField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guard all PharmacyOrders column additions (columns may already exist in DB)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PharmacyOrders' AND column_name='PaidAt') THEN
                        ALTER TABLE ""PharmacyOrders"" ADD ""PaidAt"" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PharmacyOrders' AND column_name='StripeCardBrand') THEN
                        ALTER TABLE ""PharmacyOrders"" ADD ""StripeCardBrand"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PharmacyOrders' AND column_name='StripeCardLast4') THEN
                        ALTER TABLE ""PharmacyOrders"" ADD ""StripeCardLast4"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PharmacyOrders' AND column_name='StripeChargeId') THEN
                        ALTER TABLE ""PharmacyOrders"" ADD ""StripeChargeId"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PharmacyOrders' AND column_name='StripePaymentIntentId') THEN
                        ALTER TABLE ""PharmacyOrders"" ADD ""StripePaymentIntentId"" text;
                    END IF;
                END
                $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='MedicalTourismPackages' AND column_name='IsCustomProposal') THEN
                        ALTER TABLE ""MedicalTourismPackages"" ADD ""IsCustomProposal"" boolean NOT NULL DEFAULT false;
                    END IF;
                END
                $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Doctors' AND column_name='IsInConsultation') THEN
                        ALTER TABLE ""Doctors"" ADD ""IsInConsultation"" boolean NOT NULL DEFAULT false;
                    END IF;
                END
                $$;
            ");

            migrationBuilder.CreateTable(
                name: "AnonymizedCaseData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaseHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AgeGroup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryDiagnosis = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DiagnosesJson = table.Column<string>(type: "text", nullable: false),
                    ProceduresJson = table.Column<string>(type: "text", nullable: false),
                    MedicationsJson = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClinicalHours = table.Column<double>(type: "double precision", nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymizedCaseData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<string>(type: "text", nullable: true),
                    DoctorUserId = table.Column<string>(type: "text", nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    OriginalHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UploadDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExtractedJson = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalDocuments_AspNetUsers_DoctorUserId",
                        column: x => x.DoctorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClinicalDocuments_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConsentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientUserId = table.Column<string>(type: "text", nullable: false),
                    ConsentType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsGranted = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsentVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentRecords_AspNetUsers_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataAccessLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccessedByUserId = table.Column<string>(type: "text", nullable: true),
                    AccessedByRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientUserId = table.Column<string>(type: "text", nullable: true),
                    ResourceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AccessType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataAccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataAccessLogs_AspNetUsers_AccessedByUserId",
                        column: x => x.AccessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DataAccessLogs_AspNetUsers_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Doc_WalkInPatients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorUserId = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AgeYears = table.Column<int>(type: "integer", nullable: true),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    VisitReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Diagnosis = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Prescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Vitals = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Allergies = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    HasRecord = table.Column<bool>(type: "boolean", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doc_WalkInPatients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployerRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HospitalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiredSpecialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinExperienceYears = table.Column<int>(type: "integer", nullable: false),
                    MinCasesCount = table.Column<int>(type: "integer", nullable: false),
                    RequiredProceduresJson = table.Column<string>(type: "text", nullable: false),
                    RequiredCertificationsJson = table.Column<string>(type: "text", nullable: false),
                    SalaryRange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VisaSponsorship = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationPartners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartnerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WebhookUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PartnerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PermissionsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeGraphSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalEntities = table.Column<int>(type: "integer", nullable: false),
                    TotalRelationships = table.Column<int>(type: "integer", nullable: false),
                    InsightsJson = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeGraphSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientUserId = table.Column<string>(type: "text", nullable: true),
                    Symptoms = table.Column<string>(type: "text", nullable: false),
                    Urgency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientAge = table.Column<int>(type: "integer", nullable: true),
                    PatientGender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PatientLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TargetSpecialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResultsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchingRequests_AspNetUsers_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicalClassificationTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorUserId = table.Column<string>(type: "text", nullable: true),
                    TagType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TagName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TagCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    LastObservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalClassificationTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalClassificationTags_AspNetUsers_DoctorUserId",
                        column: x => x.DoctorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicalEntities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PropertiesJson = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhysicianCredentialProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorUserId = table.Column<string>(type: "text", nullable: false),
                    TotalCasesProcessed = table.Column<int>(type: "integer", nullable: false),
                    TotalProceduresPerformed = table.Column<int>(type: "integer", nullable: false),
                    TotalClinicalHours = table.Column<double>(type: "double precision", nullable: false),
                    TotalConsultations = table.Column<int>(type: "integer", nullable: false),
                    TotalPrescriptions = table.Column<int>(type: "integer", nullable: false),
                    TotalReferralsMade = table.Column<int>(type: "integer", nullable: false),
                    CpdPoints = table.Column<int>(type: "integer", nullable: false),
                    PrimarySpecialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SecondarySpecialties = table.Column<string>(type: "text", nullable: false),
                    TopProcedures = table.Column<string>(type: "text", nullable: false),
                    ExpertiseTags = table.Column<string>(type: "text", nullable: false),
                    CredentialScore = table.Column<double>(type: "double precision", nullable: false),
                    TierLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PmcVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PmcNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MdcVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProfileGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicianCredentialProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicianCredentialProfiles_AspNetUsers_DoctorUserId",
                        column: x => x.DoctorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassifiedRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClinicalDocumentId = table.Column<int>(type: "integer", nullable: true),
                    DoctorUserId = table.Column<string>(type: "text", nullable: true),
                    PatientUserId = table.Column<string>(type: "text", nullable: true),
                    Specialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ICD10Codes = table.Column<string>(type: "text", nullable: false),
                    CPTCodes = table.Column<string>(type: "text", nullable: false),
                    ProceduresPerformed = table.Column<string>(type: "text", nullable: false),
                    DiagnosisTerms = table.Column<string>(type: "text", nullable: false),
                    ClinicalHoursContributed = table.Column<double>(type: "double precision", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AccuracyScore = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassifiedRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassifiedRecords_AspNetUsers_DoctorUserId",
                        column: x => x.DoctorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassifiedRecords_AspNetUsers_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassifiedRecords_ClinicalDocuments_ClinicalDocumentId",
                        column: x => x.ClinicalDocumentId,
                        principalTable: "ClinicalDocuments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExtractionJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClinicalDocumentId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractionJobs_ClinicalDocuments_ClinicalDocumentId",
                        column: x => x.ClinicalDocumentId,
                        principalTable: "ClinicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceEntityId = table.Column<int>(type: "integer", nullable: false),
                    TargetEntityId = table.Column<int>(type: "integer", nullable: false),
                    RelationshipType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    EvidenceCount = table.Column<int>(type: "integer", nullable: false),
                    PropertiesJson = table.Column<string>(type: "text", nullable: false),
                    LastObservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityRelationships_MedicalEntities_SourceEntityId",
                        column: x => x.SourceEntityId,
                        principalTable: "MedicalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntityRelationships_MedicalEntities_TargetEntityId",
                        column: x => x.TargetEntityId,
                        principalTable: "MedicalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 101,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 102,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 103,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 104,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 105,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 106,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 107,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 108,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 109,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 110,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 111,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 112,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 113,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 114,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 115,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 116,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 117,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 118,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 119,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 120,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 121,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 122,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 123,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 124,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 125,
                column: "IsInConsultation",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_AnonymizedCaseData_CaseHash",
                table: "AnonymizedCaseData",
                column: "CaseHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedRecords_ClinicalDocumentId",
                table: "ClassifiedRecords",
                column: "ClinicalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedRecords_DoctorUserId",
                table: "ClassifiedRecords",
                column: "DoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedRecords_PatientUserId",
                table: "ClassifiedRecords",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedRecords_Specialty",
                table: "ClassifiedRecords",
                column: "Specialty");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalDocuments_DoctorUserId",
                table: "ClinicalDocuments",
                column: "DoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalDocuments_PatientId",
                table: "ClinicalDocuments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalDocuments_Status",
                table: "ClinicalDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_ConsentType",
                table: "ConsentRecords",
                column: "ConsentType");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_PatientUserId",
                table: "ConsentRecords",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataAccessLogs_AccessedByUserId",
                table: "DataAccessLogs",
                column: "AccessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataAccessLogs_PatientUserId",
                table: "DataAccessLogs",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataAccessLogs_Timestamp",
                table: "DataAccessLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_WalkInPatients_DoctorUserId",
                table: "Doc_WalkInPatients",
                column: "DoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Doc_WalkInPatients_VisitDate",
                table: "Doc_WalkInPatients",
                column: "VisitDate");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelationships_RelationshipType",
                table: "EntityRelationships",
                column: "RelationshipType");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelationships_SourceEntityId",
                table: "EntityRelationships",
                column: "SourceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelationships_TargetEntityId",
                table: "EntityRelationships",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionJobs_ClinicalDocumentId",
                table: "ExtractionJobs",
                column: "ClinicalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionJobs_Status",
                table: "ExtractionJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationPartners_ApiKey",
                table: "IntegrationPartners",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchingRequests_CreatedDate",
                table: "MatchingRequests",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingRequests_PatientUserId",
                table: "MatchingRequests",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalClassificationTags_DoctorUserId",
                table: "MedicalClassificationTags",
                column: "DoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalClassificationTags_TagType",
                table: "MedicalClassificationTags",
                column: "TagType");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalEntities_EntityType",
                table: "MedicalEntities",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalEntities_Name",
                table: "MedicalEntities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicianCredentialProfiles_CredentialScore",
                table: "PhysicianCredentialProfiles",
                column: "CredentialScore");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicianCredentialProfiles_DoctorUserId",
                table: "PhysicianCredentialProfiles",
                column: "DoctorUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicianCredentialProfiles_TierLevel",
                table: "PhysicianCredentialProfiles",
                column: "TierLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnonymizedCaseData");

            migrationBuilder.DropTable(
                name: "ClassifiedRecords");

            migrationBuilder.DropTable(
                name: "ConsentRecords");

            migrationBuilder.DropTable(
                name: "DataAccessLogs");

            migrationBuilder.DropTable(
                name: "Doc_WalkInPatients");

            migrationBuilder.DropTable(
                name: "EmployerRequirements");

            migrationBuilder.DropTable(
                name: "EntityRelationships");

            migrationBuilder.DropTable(
                name: "ExtractionJobs");

            migrationBuilder.DropTable(
                name: "IntegrationPartners");

            migrationBuilder.DropTable(
                name: "KnowledgeGraphSnapshots");

            migrationBuilder.DropTable(
                name: "MatchingRequests");

            migrationBuilder.DropTable(
                name: "MedicalClassificationTags");

            migrationBuilder.DropTable(
                name: "PhysicianCredentialProfiles");

            migrationBuilder.DropTable(
                name: "MedicalEntities");

            migrationBuilder.DropTable(
                name: "ClinicalDocuments");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "PharmacyOrders");

            migrationBuilder.DropColumn(
                name: "StripeCardBrand",
                table: "PharmacyOrders");

            migrationBuilder.DropColumn(
                name: "StripeCardLast4",
                table: "PharmacyOrders");

            migrationBuilder.DropColumn(
                name: "StripeChargeId",
                table: "PharmacyOrders");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "PharmacyOrders");

            migrationBuilder.DropColumn(
                name: "IsCustomProposal",
                table: "MedicalTourismPackages");

            migrationBuilder.DropColumn(
                name: "IsInConsultation",
                table: "Doctors");
        }
    }
}
