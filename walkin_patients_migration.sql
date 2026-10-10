-- WalkInPatients table migration
-- Run this on your PostgreSQL database

CREATE TABLE IF NOT EXISTS "Doc_WalkInPatients" (
    "Id"           SERIAL PRIMARY KEY,
    "DoctorUserId" TEXT          NOT NULL,
    "FullName"     VARCHAR(150)  NOT NULL,
    "Phone"        VARCHAR(20)   NULL,
    "AgeYears"     INTEGER       NULL,
    "Gender"       VARCHAR(10)   NULL,
    "VisitReason"  VARCHAR(300)  NULL,
    "Diagnosis"    VARCHAR(1000) NULL,
    "Prescription" VARCHAR(2000) NULL,
    "Notes"        VARCHAR(1000) NULL,
    "Vitals"       VARCHAR(200)  NULL,
    "Allergies"    VARCHAR(300)  NULL,
    "HasRecord"    BOOLEAN       NOT NULL DEFAULT FALSE,
    "VisitDate"    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedAt"    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS "IX_Doc_WalkInPatients_DoctorUserId" ON "Doc_WalkInPatients" ("DoctorUserId");
CREATE INDEX IF NOT EXISTS "IX_Doc_WalkInPatients_VisitDate"    ON "Doc_WalkInPatients" ("VisitDate");
