# Deploy to Fly (free tier)

This file contains quick commands to deploy the app to Fly.io using the Dockerfile already included in the `Web` project.

Prerequisites:
- `flyctl` installed and logged in: https://fly.io/docs/hands-on/install-flyctl/
- A Fly account (free tier available)
- GitHub repo connected (optional, for GitHub Actions)

Local quick deploy (recommended for first deploy):

```bash
# from repo root
cd "$(dirname "$0")"
# create app (replace <name> with your chosen name)
flyctl apps create <app-name>

# Set secrets (use your Supabase connection string). Do NOT commit secrets to repo.
flyctl secrets set ConnectionStrings__DefaultConnection='Host=...;Username=...;Password=...;Database=...;SSL Mode=Require;Trust Server Certificate=true;'

# Deploy (uses Dockerfile in Web/)
flyctl deploy --config fly.toml
```

Deploy via GitHub Actions:

1. Add `FLY_API_TOKEN` to your GitHub repository secrets (Settings → Secrets).
2. Optionally add `ConnectionStrings__DefaultConnection` as a repository secret (or set it via `flyctl secrets` after app creation).
3. Push to `main` branch — the workflow `.github/workflows/deploy-fly.yml` will build and call `flyctl deploy`.

Notes:
- The Dockerfile builds the `Web` project and exposes port 80.
- Keep production secrets out of source. Use `flyctl secrets` or GitHub secrets.
- If you prefer Render / Railway / Vercel, tell me and I can add specific config.
