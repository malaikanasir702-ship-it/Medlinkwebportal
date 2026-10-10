-- ============================================================
-- MedLink AI Enterprise Layer — Database Migration
-- Run on: Supabase PostgreSQL (Railway production)
-- Tables: 13 new tables for Phase 1-7 AI features
-- Safe to run multiple times (CREATE TABLE IF NOT EXISTS)
-- ============================================================

-- ─────────────────────────────────────────────────────────────
-- 1. ClinicalDocuments (Phase 1: Document Ingestion)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "ClinicalDocuments" (
    "Id"             SERIAL PRIMARY KEY,
    "PatientId"      TEXT NULL,
    "DoctorUserId"   TEXT NULL,
    "DocumentType"   VARCHAR(100) NOT NULL DEFAULT 'Prescription',
    "FileName"       VARCHAR(255) NOT NULL DEFAULT '',
    "FileUrl"        TEXT NOT NULL DEFAULT '',
    "OriginalHash"   VARCHAR(128) NULL,
    "UploadDate"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ExtractedJson"  TEXT NULL,
    "Status"         VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "ErrorMessage"   TEXT NULL,
    "Confidence"     DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    CONSTRAINT "FK_ClinicalDocuments_Patient"
        FOREIGN KEY ("PatientId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL,
    CONSTRAINT "FK_ClinicalDocuments_Doctor"
        FOREIGN KEY ("DoctorUserId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_ClinicalDocuments_PatientId"
    ON "ClinicalDocuments" ("PatientId");

CREATE INDEX IF NOT EXISTS "IX_ClinicalDocuments_DoctorUserId"
    ON "ClinicalDocuments" ("DoctorUserId");

CREATE INDEX IF NOT EXISTS "IX_ClinicalDocuments_Status"
    ON "ClinicalDocuments" ("Status");

CREATE INDEX IF NOT EXISTS "IX_ClinicalDocuments_UploadDate"
    ON "ClinicalDocuments" ("UploadDate");

-- ─────────────────────────────────────────────────────────────
-- 2. ExtractionJobs (Phase 1: Processing Queue)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "ExtractionJobs" (
    "Id"                   SERIAL PRIMARY KEY,
    "ClinicalDocumentId"   INT NOT NULL,
    "Status"               VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "RetryCount"           INT NOT NULL DEFAULT 0,
    "MaxRetries"           INT NOT NULL DEFAULT 3,
    "CreatedAt"            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ProcessedAt"          TIMESTAMPTZ NULL,
    "ErrorMessage"         TEXT NULL,
    CONSTRAINT "FK_ExtractionJobs_ClinicalDocument"
        FOREIGN KEY ("ClinicalDocumentId")
        REFERENCES "ClinicalDocuments"("Id")
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_ExtractionJobs_ClinicalDocumentId"
    ON "ExtractionJobs" ("ClinicalDocumentId");

CREATE INDEX IF NOT EXISTS "IX_ExtractionJobs_Status"
    ON "ExtractionJobs" ("Status");

-- ─────────────────────────────────────────────────────────────
-- 3. ClassifiedRecords (Phase 2: AI Classification)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "ClassifiedRecords" (
    "Id"                        SERIAL PRIMARY KEY,
    "ClinicalDocumentId"        INT NULL,
    "DoctorUserId"              TEXT NULL,
    "PatientUserId"             TEXT NULL,
    "Specialty"                 VARCHAR(100) NOT NULL DEFAULT 'General Practice',
    "ICD10Codes"                TEXT NOT NULL DEFAULT '[]',
    "CPTCodes"                  TEXT NOT NULL DEFAULT '[]',
    "ProceduresPerformed"       TEXT NOT NULL DEFAULT '[]',
    "DiagnosisTerms"            TEXT NOT NULL DEFAULT '[]',
    "ClinicalHoursContributed"  DOUBLE PRECISION NOT NULL DEFAULT 1.0,
    "ProcessedAt"               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "AccuracyScore"             DOUBLE PRECISION NOT NULL DEFAULT 0.95,
    "Notes"                     TEXT NULL,
    "Status"                    VARCHAR(50) NOT NULL DEFAULT 'Verified',
    CONSTRAINT "FK_ClassifiedRecords_ClinicalDocument"
        FOREIGN KEY ("ClinicalDocumentId")
        REFERENCES "ClinicalDocuments"("Id")
        ON DELETE SET NULL,
    CONSTRAINT "FK_ClassifiedRecords_Doctor"
        FOREIGN KEY ("DoctorUserId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL,
    CONSTRAINT "FK_ClassifiedRecords_Patient"
        FOREIGN KEY ("PatientUserId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_ClassifiedRecords_DoctorUserId"
    ON "ClassifiedRecords" ("DoctorUserId");

CREATE INDEX IF NOT EXISTS "IX_ClassifiedRecords_PatientUserId"
    ON "ClassifiedRecords" ("PatientUserId");

CREATE INDEX IF NOT EXISTS "IX_ClassifiedRecords_Specialty"
    ON "ClassifiedRecords" ("Specialty");

CREATE INDEX IF NOT EXISTS "IX_ClassifiedRecords_Status"
    ON "ClassifiedRecords" ("Status");

-- ─────────────────────────────────────────────────────────────
-- 4. MedicalClassificationTags (Phase 2: Tag Cloud per Doctor)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "MedicalClassificationTags" (
    "Id"               SERIAL PRIMARY KEY,
    "DoctorUserId"     TEXT NULL,
    "TagType"          VARCHAR(50) NOT NULL DEFAULT 'Specialty',
    "TagName"          VARCHAR(150) NOT NULL DEFAULT '',
    "TagCode"          VARCHAR(50) NULL,
    "UsageCount"       INT NOT NULL DEFAULT 1,
    "LastObservedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_MedicalClassificationTags_Doctor"
        FOREIGN KEY ("DoctorUserId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_MedicalClassificationTags_DoctorUserId"
    ON "MedicalClassificationTags" ("DoctorUserId");

CREATE INDEX IF NOT EXISTS "IX_MedicalClassificationTags_TagType"
    ON "MedicalClassificationTags" ("TagType");

-- ─────────────────────────────────────────────────────────────
-- 5. PhysicianCredentialProfiles (Phase 3: Dynamic Doctor Profiles)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "PhysicianCredentialProfiles" (
    "Id"                        SERIAL PRIMARY KEY,
    "DoctorUserId"              TEXT NOT NULL,
    "TotalCasesProcessed"       INT NOT NULL DEFAULT 0,
    "TotalProceduresPerformed"  INT NOT NULL DEFAULT 0,
    "TotalClinicalHours"        DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    "TotalConsultations"        INT NOT NULL DEFAULT 0,
    "TotalPrescriptions"        INT NOT NULL DEFAULT 0,
    "TotalReferralsMade"        INT NOT NULL DEFAULT 0,
    "CpdPoints"                 INT NOT NULL DEFAULT 0,
    "PrimarySpecialty"          VARCHAR(100) NOT NULL DEFAULT 'General Practice',
    "SecondarySpecialties"      TEXT NOT NULL DEFAULT '[]',
    "TopProcedures"             TEXT NOT NULL DEFAULT '[]',
    "ExpertiseTags"             TEXT NOT NULL DEFAULT '[]',
    "CredentialScore"           DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    "TierLevel"                 VARCHAR(50) NOT NULL DEFAULT 'Junior',
    "PmcVerified"               BOOLEAN NOT NULL DEFAULT FALSE,
    "PmcNumber"                 VARCHAR(100) NULL,
    "MdcVerified"               BOOLEAN NOT NULL DEFAULT FALSE,
    "LastUpdated"               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ProfileGeneratedAt"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_PhysicianCredentialProfiles_Doctor"
        FOREIGN KEY ("DoctorUserId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PhysicianCredentialProfiles_DoctorUserId"
    ON "PhysicianCredentialProfiles" ("DoctorUserId");

CREATE INDEX IF NOT EXISTS "IX_PhysicianCredentialProfiles_TierLevel"
    ON "PhysicianCredentialProfiles" ("TierLevel");

CREATE INDEX IF NOT EXISTS "IX_PhysicianCredentialProfiles_CredentialScore"
    ON "PhysicianCredentialProfiles" ("CredentialScore");

-- ─────────────────────────────────────────────────────────────
-- 6. MatchingRequests (Phase 4: AI Matching Engine — Patient→Doctor)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "MatchingRequests" (
    "Id"               SERIAL PRIMARY KEY,
    "PatientUserId"    TEXT NULL,
    "Symptoms"         TEXT NOT NULL DEFAULT '',
    "Urgency"          VARCHAR(50) NOT NULL DEFAULT 'Normal',
    "PatientAge"       INT NULL,
    "PatientGender"    VARCHAR(20) NULL,
    "PatientLocation"  VARCHAR(100) NULL,
    "TargetSpecialty"  VARCHAR(100) NULL,
    "CreatedDate"      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Status"           VARCHAR(50) NOT NULL DEFAULT 'Completed',
    "ResultsJson"      TEXT NULL,
    CONSTRAINT "FK_MatchingRequests_Patient"
        FOREIGN KEY ("PatientUserId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_MatchingRequests_PatientUserId"
    ON "MatchingRequests" ("PatientUserId");

CREATE INDEX IF NOT EXISTS "IX_MatchingRequests_CreatedDate"
    ON "MatchingRequests" ("CreatedDate");

CREATE INDEX IF NOT EXISTS "IX_MatchingRequests_Status"
    ON "MatchingRequests" ("Status");

-- ─────────────────────────────────────────────────────────────
-- 7. EmployerRequirements (Phase 4: Doctor→International Employer Matching)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "EmployerRequirements" (
    "Id"                        SERIAL PRIMARY KEY,
    "HospitalName"              VARCHAR(200) NOT NULL DEFAULT '',
    "Country"                   VARCHAR(100) NOT NULL DEFAULT 'United Kingdom',
    "RequiredSpecialty"         VARCHAR(100) NOT NULL DEFAULT 'Cardiology',
    "MinExperienceYears"        INT NOT NULL DEFAULT 5,
    "MinCasesCount"             INT NOT NULL DEFAULT 500,
    "RequiredProceduresJson"    TEXT NOT NULL DEFAULT '[]',
    "RequiredCertificationsJson" TEXT NOT NULL DEFAULT '[]',
    "SalaryRange"               VARCHAR(100) NULL DEFAULT '£90,000 - £120,000 / yr',
    "VisaSponsorship"           BOOLEAN NOT NULL DEFAULT TRUE,
    "IsActive"                  BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedDate"               TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS "IX_EmployerRequirements_IsActive"
    ON "EmployerRequirements" ("IsActive");

CREATE INDEX IF NOT EXISTS "IX_EmployerRequirements_RequiredSpecialty"
    ON "EmployerRequirements" ("RequiredSpecialty");

-- ─────────────────────────────────────────────────────────────
-- 8. MedicalEntities (Phase 6: Knowledge Graph — Nodes)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "MedicalEntities" (
    "Id"             SERIAL PRIMARY KEY,
    "EntityType"     VARCHAR(50) NOT NULL DEFAULT 'Disease',
    "Name"           VARCHAR(200) NOT NULL DEFAULT '',
    "Code"           VARCHAR(50) NULL,
    "PropertiesJson" TEXT NOT NULL DEFAULT '{}',
    "Confidence"     DOUBLE PRECISION NOT NULL DEFAULT 1.0,
    "CreatedAt"      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS "IX_MedicalEntities_EntityType"
    ON "MedicalEntities" ("EntityType");

CREATE INDEX IF NOT EXISTS "IX_MedicalEntities_Name"
    ON "MedicalEntities" ("Name");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_MedicalEntities_Name_EntityType"
    ON "MedicalEntities" ("Name", "EntityType");

-- ─────────────────────────────────────────────────────────────
-- 9. EntityRelationships (Phase 6: Knowledge Graph — Edges)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "EntityRelationships" (
    "Id"                 SERIAL PRIMARY KEY,
    "SourceEntityId"     INT NOT NULL,
    "TargetEntityId"     INT NOT NULL,
    "RelationshipType"   VARCHAR(60) NOT NULL DEFAULT 'indicates',
    "Weight"             DOUBLE PRECISION NOT NULL DEFAULT 1.0,
    "EvidenceCount"      INT NOT NULL DEFAULT 1,
    "PropertiesJson"     TEXT NOT NULL DEFAULT '{}',
    "LastObservedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_EntityRelationships_Source"
        FOREIGN KEY ("SourceEntityId")
        REFERENCES "MedicalEntities"("Id")
        ON DELETE CASCADE,
    CONSTRAINT "FK_EntityRelationships_Target"
        FOREIGN KEY ("TargetEntityId")
        REFERENCES "MedicalEntities"("Id")
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_EntityRelationships_SourceEntityId"
    ON "EntityRelationships" ("SourceEntityId");

CREATE INDEX IF NOT EXISTS "IX_EntityRelationships_TargetEntityId"
    ON "EntityRelationships" ("TargetEntityId");

CREATE INDEX IF NOT EXISTS "IX_EntityRelationships_RelationshipType"
    ON "EntityRelationships" ("RelationshipType");

-- ─────────────────────────────────────────────────────────────
-- 10. AnonymizedCaseData (Phase 6: PII-free Knowledge Repository)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "AnonymizedCaseData" (
    "Id"               SERIAL PRIMARY KEY,
    "CaseHash"         VARCHAR(128) NOT NULL DEFAULT '',
    "AgeGroup"         VARCHAR(50) NOT NULL DEFAULT '30-39',
    "Gender"           VARCHAR(20) NOT NULL DEFAULT 'Unknown',
    "City"             VARCHAR(100) NOT NULL DEFAULT 'Karachi',
    "PrimaryDiagnosis" VARCHAR(200) NOT NULL DEFAULT '',
    "DiagnosesJson"    TEXT NOT NULL DEFAULT '[]',
    "ProceduresJson"   TEXT NOT NULL DEFAULT '[]',
    "MedicationsJson"  TEXT NOT NULL DEFAULT '[]',
    "Outcome"          VARCHAR(100) NOT NULL DEFAULT 'Recovered',
    "ClinicalHours"    DOUBLE PRECISION NOT NULL DEFAULT 1.0,
    "IngestedAt"       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_AnonymizedCaseData_CaseHash"
    ON "AnonymizedCaseData" ("CaseHash");

CREATE INDEX IF NOT EXISTS "IX_AnonymizedCaseData_PrimaryDiagnosis"
    ON "AnonymizedCaseData" ("PrimaryDiagnosis");

-- ─────────────────────────────────────────────────────────────
-- 11. KnowledgeGraphSnapshots (Phase 6: Periodic Graph State)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "KnowledgeGraphSnapshots" (
    "Id"                   SERIAL PRIMARY KEY,
    "SnapshotDate"         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "TotalEntities"        INT NOT NULL DEFAULT 0,
    "TotalRelationships"   INT NOT NULL DEFAULT 0,
    "InsightsJson"         TEXT NOT NULL DEFAULT '[]',
    "Summary"              VARCHAR(500) NOT NULL DEFAULT ''
);

-- ─────────────────────────────────────────────────────────────
-- 12. DataAccessLogs (Phase 7: Privacy & Governance Audit)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "DataAccessLogs" (
    "Id"                SERIAL PRIMARY KEY,
    "AccessedByUserId"  TEXT NULL,
    "AccessedByRole"    VARCHAR(50) NOT NULL DEFAULT 'Doctor',
    "PatientUserId"     TEXT NULL,
    "ResourceName"      VARCHAR(100) NOT NULL DEFAULT 'MedicalRecord',
    "RecordId"          VARCHAR(100) NULL,
    "AccessType"        VARCHAR(50) NOT NULL DEFAULT 'Read',
    "IpAddress"         VARCHAR(60) NULL,
    "Timestamp"         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Purpose"           VARCHAR(250) NULL DEFAULT 'Clinical Consultation',
    CONSTRAINT "FK_DataAccessLogs_AccessedBy"
        FOREIGN KEY ("AccessedByUserId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL,
    CONSTRAINT "FK_DataAccessLogs_Patient"
        FOREIGN KEY ("PatientUserId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_DataAccessLogs_AccessedByUserId"
    ON "DataAccessLogs" ("AccessedByUserId");

CREATE INDEX IF NOT EXISTS "IX_DataAccessLogs_PatientUserId"
    ON "DataAccessLogs" ("PatientUserId");

CREATE INDEX IF NOT EXISTS "IX_DataAccessLogs_Timestamp"
    ON "DataAccessLogs" ("Timestamp");

CREATE INDEX IF NOT EXISTS "IX_DataAccessLogs_AccessType"
    ON "DataAccessLogs" ("AccessType");

-- ─────────────────────────────────────────────────────────────
-- 13. ConsentRecords (Phase 7: Patient Consent Management)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "ConsentRecords" (
    "Id"               SERIAL PRIMARY KEY,
    "PatientUserId"    TEXT NOT NULL,
    "ConsentType"      VARCHAR(60) NOT NULL DEFAULT 'Research',
    "IsGranted"        BOOLEAN NOT NULL DEFAULT TRUE,
    "GrantedAt"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "RevokedAt"        TIMESTAMPTZ NULL,
    "ConsentVersion"   VARCHAR(20) NOT NULL DEFAULT '1.0',
    "IpAddress"        VARCHAR(60) NULL,
    CONSTRAINT "FK_ConsentRecords_Patient"
        FOREIGN KEY ("PatientUserId")
        REFERENCES "AspNetUsers"("Id")
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_ConsentRecords_PatientUserId"
    ON "ConsentRecords" ("PatientUserId");

CREATE INDEX IF NOT EXISTS "IX_ConsentRecords_ConsentType"
    ON "ConsentRecords" ("ConsentType");

CREATE INDEX IF NOT EXISTS "IX_ConsentRecords_IsGranted"
    ON "ConsentRecords" ("IsGranted");

-- ─────────────────────────────────────────────────────────────
-- 14. IntegrationPartners (Phase 7: Hospital/Lab API Integrations)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "IntegrationPartners" (
    "Id"               SERIAL PRIMARY KEY,
    "PartnerName"      VARCHAR(150) NOT NULL DEFAULT '',
    "ApiKey"           VARCHAR(100) NOT NULL DEFAULT '',
    "WebhookUrl"       VARCHAR(255) NULL,
    "PartnerType"      VARCHAR(50) NOT NULL DEFAULT 'Hospital',
    "IsActive"         BOOLEAN NOT NULL DEFAULT TRUE,
    "PermissionsJson"  TEXT NOT NULL DEFAULT '["lab-result", "discharge-summary"]',
    "CreatedAt"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastActiveAt"     TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS "IX_IntegrationPartners_IsActive"
    ON "IntegrationPartners" ("IsActive");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_IntegrationPartners_ApiKey"
    ON "IntegrationPartners" ("ApiKey");

-- ─────────────────────────────────────────────────────────────
-- VERIFICATION QUERY — Run after migration to confirm
-- ─────────────────────────────────────────────────────────────
SELECT
    tablename,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = t.tablename) AS column_count
FROM (
    VALUES
        ('ClinicalDocuments'),
        ('ExtractionJobs'),
        ('ClassifiedRecords'),
        ('MedicalClassificationTags'),
        ('PhysicianCredentialProfiles'),
        ('MatchingRequests'),
        ('EmployerRequirements'),
        ('MedicalEntities'),
        ('EntityRelationships'),
        ('AnonymizedCaseData'),
        ('KnowledgeGraphSnapshots'),
        ('DataAccessLogs'),
        ('ConsentRecords'),
        ('IntegrationPartners')
) AS t(tablename)
ORDER BY tablename;
