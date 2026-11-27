# Railway Environment Variables

Set these in Railway → Project → Variables before deploying.  
**Never commit real values to this file or appsettings.json.**

## Required

| Variable | Description |
|---|---|
| `DATABASE_URL` | Full Postgres connection string (Railway injects this automatically if you provision a Postgres plugin) |
| `JWT_SECRET` | Random string ≥ 32 chars. Generate: `openssl rand -base64 48` |
| `ENCRYPTION_KEY` | 32-char AES key. Generate: `openssl rand -base64 32` |
| `CORS_ALLOWED_ORIGINS` | Comma-separated list of allowed origins e.g. `https://yourapp.railway.app,https://yourdomain.com` |

## Email (pick one)

| Variable | Description |
|---|---|
| `RESEND_API_KEY` | From resend.dev dashboard |
| `GMAIL_USERNAME` | Gmail address for SMTP |
| `GMAIL_APP_PASSWORD` | Gmail App Password (not account password) |

## Third-party Services

| Variable | Description |
|---|---|
| `GOOGLE_CLIENT_ID` | Google OAuth client ID |
| `GOOGLE_CLIENT_SECRET` | Google OAuth client secret |
| `GOOGLE_MAPS_API_KEY` | Google Maps API key |
| `STRIPE_SECRET_KEY` | Stripe secret key (sk_live_...) |
| `STRIPE_PUBLISHABLE_KEY` | Stripe publishable key (pk_live_...) |
| `CLOUDINARY_CLOUD_NAME` | Cloudinary cloud name |
| `CLOUDINARY_API_KEY` | Cloudinary API key |
| `CLOUDINARY_API_SECRET` | Cloudinary API secret |
| `VAPID_PUBLIC_KEY` | Web Push VAPID public key |
| `VAPID_PRIVATE_KEY` | Web Push VAPID private key |
| `GEMINI_API_KEY` | Google Gemini API key |
| `GROQ_API_KEY` | Groq API key |
| `AGORA_APP_ID` | Agora video app ID |
| `AGORA_APP_CERTIFICATE` | Agora app certificate |
| `FIREBASE_SERVICE_ACCOUNT` | Full Firebase service account JSON (as a single-line string) |
| `SENTRY_DSN` | Sentry DSN for error tracking |
| `REDIS_CONNECTION_STRING` | Redis connection string for distributed cache & SignalR backplane |
| `ENABLE_INITIAL_SEED` | Set `true` only for first-time DB user seeding (`Admin`, `Doctor`, `LabAdmin`, `Pharmacist`), then remove/set `false` |
| `SEED_ADMIN_PASSWORD` | Password for initial Admin seed (if `ENABLE_INITIAL_SEED=true`) |
| `SEED_DOCTOR_PASSWORD` | Password for initial Doctor seed (if `ENABLE_INITIAL_SEED=true`) |
| `SEED_LABADMIN_PASSWORD` | Password for initial LabAdmin seed (if `ENABLE_INITIAL_SEED=true`) |
| `SEED_PHARMACIST_PASSWORD` | Password for initial Pharmacist seed (if `ENABLE_INITIAL_SEED=true`) |

## How to Update Program.cs to Read These

The pattern already used in the codebase is:

```csharp
var value = Environment.GetEnvironmentVariable("ENV_VAR_NAME")
         ?? builder.Configuration["Config:Path"];
```

Apply this to any service that currently reads directly from `builder.Configuration` only.
