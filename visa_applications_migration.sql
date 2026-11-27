-- ============================================================
-- MedLink Visa Applications Migration
-- Run in Supabase → SQL Editor
-- ============================================================

CREATE TABLE IF NOT EXISTS "VisaApplications" (
    "Id"                  SERIAL PRIMARY KEY,
    "ReferenceId"         VARCHAR(30) NOT NULL UNIQUE,
    "UserId"              TEXT NOT NULL,
    "TourismRequestId"    INTEGER NULL REFERENCES "MedicalTourismRequests"("Id") ON DELETE SET NULL,
    "FullName"            TEXT NOT NULL,
    "Nationality"         TEXT NOT NULL,
    "PassportNumber"      TEXT NOT NULL,
    "PassportExpiry"      TIMESTAMP WITH TIME ZONE NULL,
    "TreatmentType"       TEXT NOT NULL,
    "HospitalName"        TEXT NULL,
    "PlannedArrivalDate"  TIMESTAMP WITH TIME ZONE NULL,
    "MedicalReportsUrl"   TEXT NULL,
    "BankStatementUrl"    TEXT NULL,
    "PassportCopyUrl"     TEXT NULL,
    "AdditionalNotes"     TEXT NULL,
    "Status"              INTEGER NOT NULL DEFAULT 0,
    "AdminNotes"          TEXT NULL,
    "InvitationLetterUrl" TEXT NULL,
    "ProcessedBy"         TEXT NULL,
    "ProcessedAt"         TIMESTAMP WITH TIME ZONE NULL,
    "CreatedAt"           TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt"           TIMESTAMP WITH TIME ZONE NULL
);

CREATE INDEX IF NOT EXISTS "IX_VisaApplications_UserId" ON "VisaApplications"("UserId");
CREATE INDEX IF NOT EXISTS "IX_VisaApplications_Status" ON "VisaApplications"("Status");

-- Verify
SELECT column_name, data_type FROM information_schema.columns
WHERE table_name = 'VisaApplications' ORDER BY ordinal_position;
