-- Migration: Add Stripe payment tracking fields to PharmacyOrders
-- Run this against your Supabase PostgreSQL database

ALTER TABLE "PharmacyOrders"
    ADD COLUMN IF NOT EXISTS "StripePaymentIntentId" TEXT NULL,
    ADD COLUMN IF NOT EXISTS "StripeChargeId" TEXT NULL,
    ADD COLUMN IF NOT EXISTS "StripeCardBrand" TEXT NULL,
    ADD COLUMN IF NOT EXISTS "StripeCardLast4" TEXT NULL,
    ADD COLUMN IF NOT EXISTS "PaidAt" TIMESTAMP NULL;

-- Index for quick lookup by payment intent
CREATE INDEX IF NOT EXISTS idx_pharmacy_orders_stripe
    ON "PharmacyOrders" ("StripePaymentIntentId")
    WHERE "StripePaymentIntentId" IS NOT NULL;
