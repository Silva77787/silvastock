# Set up Google sign-in (OAuth client)

The back office signs administrators in with Google (ADR-0004). Visitors never sign in. This is done once per environment by the owner. It is free.

## 1. Google Cloud project
1. Open [console.cloud.google.com](https://console.cloud.google.com) and create a project named **SilvaStock**.
2. Go to **Google Auth Platform**.

## 2. Consent screen (Branding / Audience / Data access)
- **User type:** External.
- **App name:** SilvaStock, with your support email. Do **not** upload a logo yet: a logo requires Google's brand verification.
- **Scopes:** only `openid`, `email` and `profile`. They are non-sensitive, so no verification is needed.
- **Publishing status:** keep **Testing** during development, and add the administrators' Google emails as **test users**. Only those accounts can even reach the Google sign-in screen, an extra layer on top of the `admin_users` allowlist. Switch to **In production** before launch (Phase 7).

## 3. OAuth client (Clients → Create client)
- **Application type:** Web application. **Name:** SilvaStock API.
- **Authorized redirect URIs** — exactly:
  - local: `http://localhost:5080/api/auth/callback/google`
  - production (Phase 7): `https://api.<domain>/api/auth/callback/google`
- **Authorized JavaScript origins:** leave empty. The login runs through the API, not in the browser.
- Create the client and copy the **Client ID** and **Client secret**.

## 4. Store the credentials (never in Git, files or chats)
Locally, from the repository root. Type the values only in your own terminal:
```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>" --project src/SilvaStock.Api
```
```bash
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>" --project src/SilvaStock.Api
```
Production (Phase 7): Azure Container Apps secrets exposed as the environment variables `Authentication__Google__ClientId`, `Authentication__Google__ClientSecret` and `Site__BaseUrl`.

## 5. Allow yourself in and test
1. Add your email to the allowlist: `dotnet run --project src/SilvaStock.Cli -- add-admin you@gmail.com --name "Your Name"` (see `recover-admin-access.md`).
2. Start the local services and the API: `docker compose up -d`, apply the migrations, then `dotnet run --project src/SilvaStock.Api`.
3. Open `http://localhost:5080/api/auth/login/google` in Chrome or Firefox. Safari does not accept `Secure` cookies on `http://localhost`.
4. After signing in with Google you are redirected to the site (`Site:BaseUrl`, `http://localhost:3000` in Development).

## Troubleshooting
| Symptom | Cause |
|---|---|
| Google shows `redirect_uri_mismatch` | The redirect URI in step 3 does not match exactly (scheme, port, path). |
| Google says "access blocked … not completed verification" | Your account is not a test user (step 2). |
| `/admin/login?error=not_allowed` | The email is not active in `admin_users`, is not verified, or is bound to a different Google account (`reset-google`). |
| `/admin/login?error=login_failed` | Sign-in was cancelled at Google, it took longer than 10 minutes, or the database was unreachable. |
| The API does not start ("Missing required configuration") | The user-secrets from step 4 are missing. |
