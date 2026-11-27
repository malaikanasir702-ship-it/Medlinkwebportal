-- ============================================================
-- Medical Tourism Patient Preferences Migration
-- Adds hotel & transport preference fields to MedicalTourismRequests
-- ============================================================

ALTER TABLE "MedicalTourismRequests"
    ADD COLUMN IF NOT EXISTS "WantsHotel" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "HotelPreference" text NULL DEFAULT 'Not Required',
    ADD COLUMN IF NOT EXISTS "HotelNights" integer NULL,
    ADD COLUMN IF NOT EXISTS "WantsAirportPickup" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "TransportPreference" text NULL DEFAULT 'Not Required',
    ADD COLUMN IF NOT EXISTS "PreArrivalAppointmentId" integer NULL;

-- Verify
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'MedicalTourismRequests'
  AND column_name IN ('WantsHotel','HotelPreference','HotelNights','WantsAirportPickup','TransportPreference','PreArrivalAppointmentId');
