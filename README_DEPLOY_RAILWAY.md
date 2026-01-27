# Deploy to Railway (Free - $5/month credit)

Railway is a simple, free hosting platform with no credit card required to start.

## Quick Steps:

1. **Sign up** (no card required):
   - Go to https://railway.app
   - Sign up with GitHub

2. **Create a new project**:
   - Click "Create Project"
   - Select "Deploy from GitHub repo"
   - Authorize Railway to access your GitHub account
   - Select this repo

3. **Configure environment variables**:
   - In Railway dashboard, click your project
   - Go to "Variables"
   - Add your Supabase connection string:
     ```
     ConnectionStrings__DefaultConnection=Host=aws-1-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;User Id=postgres.ihhuucgionsifkxnrrvo;Password=<YOUR_PASSWORD>;SSL Mode=Require;Trust Server Certificate=true;
     ```

4. **Deploy**:
   - Railway auto-deploys when you push to `main`
   - Or manually trigger from dashboard

5. **Get public URL**:
   - Dashboard → "Deployments" tab
   - Your app URL will be displayed (e.g., `https://app-name.railway.app`)

## Notes:
- Free tier includes $5/month credit (enough for small ASP.NET app + database)
- Automatic deployments on git push
- SSL/HTTPS included
- No card required initially
- GitHub integration is seamless

If you already have a GitHub repo, just push to `main` and Railway will auto-detect the Dockerfile and deploy.
