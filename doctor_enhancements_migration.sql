-- ============================================================
-- MedLink Doctor Enhancements Migration
-- Apply this to Supabase PostgreSQL database
-- Generated from: 20260825060250_AddDoctorEnhancements
-- Tables: PrescriptionTemplates, PatientFollowUpReminders,
--         WaitingRoomEntries, DoctorReferrals, CpdActivities,
--         ClinicStaff, ReviewReplies, DoctorVoiceNotes
-- ============================================================

-- 1. PrescriptionTemplates
CREATE TABLE IF NOT EXISTS "PrescriptionTemplates" (
    "Id" SERIAL PRIMARY KEY,
    "DoctorId" TEXT NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Diagnosis" TEXT NOT NULL,
    "MedicationsJson" TEXT NOT NULL DEFAULT '[]',
    "Notes" VARCHAR(2000),
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_PrescriptionTemplates_AspNetUsers_DoctorId"
        FOREIGN KEY ("DoctorId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_PrescriptionTemplates_DoctorId"
    ON "PrescriptionTemplates"("DoctorId");

-- 2. PatientFollowUpReminders
CREATE TABLE IF NOT EXISTS "PatientFollowUpReminders" (
    "Id" SERIAL PRIMARY KEY,
    "DoctorId" TEXT NOT NULL,
    "PatientId" TEXT NOT NULL,
    "AppointmentId" INTEGER,
    "Message" VARCHAR(500) NOT NULL,
    "ScheduledAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IsSent" BOOLEAN NOT NULL DEFAULT FALSE,
    "SentAt" TIMESTAMP WITH TIME ZONE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_PatientFollowUpReminders_DoctorId"
        FOREIGN KEY ("DoctorId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PatientFollowUpReminders_PatientId"
        FOREIGN KEY ("PatientId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PatientFollowUpReminders_AppointmentId"
        FOREIGN KEY ("AppointmentId") REFERENCES "Appointments"("Id") ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS "IX_PatientFollowUpReminders_DoctorId"
    ON "PatientFollowUpReminders"("DoctorId");
CREATE INDEX IF NOT EXISTS "IX_PatientFollowUpReminders_PatientId"
    ON "PatientFollowUpReminders"("PatientId");
CREATE INDEX IF NOT EXISTS "IX_PatientFollowUpReminders_IsSent"
    ON "PatientFollowUpReminders"("IsSent");

-- 3. WaitingRoomEntries
CREATE TABLE IF NOT EXISTS "WaitingRoomEntries" (
    "Id" SERIAL PRIMARY KEY,
    "DoctorDbId" INTEGER NOT NULL,
    "PatientId" TEXT NOT NULL,
    "AppointmentId" INTEGER NOT NULL,
    "QueuePosition" INTEGER NOT NULL DEFAULT 1,
    "EstimatedWaitMinutes" INTEGER NOT NULL DEFAULT 0,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Waiting',
    "JoinedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CalledAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_WaitingRoomEntries_Doctors_DoctorDbId"
        FOREIGN KEY ("DoctorDbId") REFERENCES "Doctors"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WaitingRoomEntries_AspNetUsers_PatientId"
        FOREIGN KEY ("PatientId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WaitingRoomEntries_Appointments_AppointmentId"
        FOREIGN KEY ("AppointmentId") REFERENCES "Appointments"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_WaitingRoomEntries_DoctorDbId"
    ON "WaitingRoomEntries"("DoctorDbId");
CREATE INDEX IF NOT EXISTS "IX_WaitingRoomEntries_Status"
    ON "WaitingRoomEntries"("Status");
CREATE INDEX IF NOT EXISTS "IX_WaitingRoomEntries_PatientId"
    ON "WaitingRoomEntries"("PatientId");

-- 4. DoctorReferrals
CREATE TABLE IF NOT EXISTS "DoctorReferrals" (
    "Id" SERIAL PRIMARY KEY,
    "ReferringDoctorUserId" TEXT NOT NULL,
    "ReceivingDoctorUserId" TEXT NOT NULL,
    "PatientId" TEXT NOT NULL,
    "AppointmentId" INTEGER,
    "Reason" VARCHAR(500) NOT NULL,
    "ClinicalNotes" VARCHAR(2000),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "RespondedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_DoctorReferrals_Referring"
        FOREIGN KEY ("ReferringDoctorUserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DoctorReferrals_Receiving"
        FOREIGN KEY ("ReceivingDoctorUserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DoctorReferrals_Patient"
        FOREIGN KEY ("PatientId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_DoctorReferrals_ReferringDoctorUserId"
    ON "DoctorReferrals"("ReferringDoctorUserId");
CREATE INDEX IF NOT EXISTS "IX_DoctorReferrals_ReceivingDoctorUserId"
    ON "DoctorReferrals"("ReceivingDoctorUserId");
CREATE INDEX IF NOT EXISTS "IX_DoctorReferrals_PatientId"
    ON "DoctorReferrals"("PatientId");

-- 5. CpdActivities
CREATE TABLE IF NOT EXISTS "CpdActivities" (
    "Id" SERIAL PRIMARY KEY,
    "DoctorId" TEXT NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Provider" VARCHAR(100),
    "ActivityType" VARCHAR(50) NOT NULL DEFAULT 'Webinar',
    "CreditPoints" INTEGER NOT NULL DEFAULT 1,
    "ActivityDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CertificateUrl" TEXT,
    "Description" VARCHAR(500),
    "IsVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_CpdActivities_AspNetUsers_DoctorId"
        FOREIGN KEY ("DoctorId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_CpdActivities_DoctorId"
    ON "CpdActivities"("DoctorId");

-- 6. ClinicStaff
CREATE TABLE IF NOT EXISTS "ClinicStaff" (
    "Id" SERIAL PRIMARY KEY,
    "DoctorId" TEXT NOT NULL,
    "StaffUserId" TEXT NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(200) NOT NULL,
    "Phone" VARCHAR(20) NOT NULL DEFAULT '',
    "Role" VARCHAR(50) NOT NULL DEFAULT 'Receptionist',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "AddedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_ClinicStaff_AspNetUsers_DoctorId"
        FOREIGN KEY ("DoctorId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ClinicStaff_AspNetUsers_StaffUserId"
        FOREIGN KEY ("StaffUserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_ClinicStaff_DoctorId"
    ON "ClinicStaff"("DoctorId");

-- 7. ReviewReplies
CREATE TABLE IF NOT EXISTS "ReviewReplies" (
    "Id" SERIAL PRIMARY KEY,
    "ReviewId" INTEGER NOT NULL,
    "DoctorId" TEXT NOT NULL,
    "ReplyText" VARCHAR(1000) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_ReviewReplies_Reviews_ReviewId"
        FOREIGN KEY ("ReviewId") REFERENCES "Reviews"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ReviewReplies_AspNetUsers_DoctorId"
        FOREIGN KEY ("DoctorId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_ReviewReplies_ReviewId"
    ON "ReviewReplies"("ReviewId");
CREATE INDEX IF NOT EXISTS "IX_ReviewReplies_DoctorId"
    ON "ReviewReplies"("DoctorId");

-- 8. DoctorVoiceNotes
CREATE TABLE IF NOT EXISTS "DoctorVoiceNotes" (
    "Id" SERIAL PRIMARY KEY,
    "DoctorId" TEXT NOT NULL,
    "PatientId" TEXT,
    "AppointmentId" INTEGER,
    "TranscribedText" TEXT NOT NULL,
    "AudioUrl" VARCHAR(200),
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_DoctorVoiceNotes_AspNetUsers_DoctorId"
        FOREIGN KEY ("DoctorId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DoctorVoiceNotes_AspNetUsers_PatientId"
        FOREIGN KEY ("PatientId") REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_DoctorVoiceNotes_Appointments_AppointmentId"
        FOREIGN KEY ("AppointmentId") REFERENCES "Appointments"("Id") ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS "IX_DoctorVoiceNotes_DoctorId"
    ON "DoctorVoiceNotes"("DoctorId");

-- EF Core migration history record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260825060250_AddDoctorEnhancements', '10.0.11')
ON CONFLICT DO NOTHING;

-- Done
