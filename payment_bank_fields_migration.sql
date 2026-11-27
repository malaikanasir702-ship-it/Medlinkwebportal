-- ============================================================
-- MedLink Payment Flow Migration
-- Adds BankAccountTitle and BankName to AspNetUsers
-- Run this SQL directly on your PostgreSQL database
-- ============================================================

-- Add BankAccountTitle (encrypted via app, stored as text)
ALTER TABLE "AspNetUsers"
    ADD COLUMN IF NOT EXISTS "BankAccountTitle" text NULL;

-- Add BankName (plain text, e.g. HBL, UBL, Meezan)
ALTER TABLE "AspNetUsers"
    ADD COLUMN IF NOT EXISTS "BankName" text NULL;

-- Confirm columns added
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'AspNetUsers'
  AND column_name IN ('BankAccountNumber', 'BankAccountTitle', 'BankName');
