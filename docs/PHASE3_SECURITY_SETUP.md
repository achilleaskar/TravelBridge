# Phase 3 Security Fixes - Setup Guide

**⚠️ CRITICAL: Complete these steps before running the application**

---

## 🔐 Step 1: Setup User Secrets (Development)

Run these commands from the `TravelBridge.API` directory:

```bash
# Navigate to API project
cd TravelBridge.API

# Initialize user secrets
dotnet user-secrets init

# Set database connection (REPLACE with your actual credentials)
dotnet user-secrets set "ConnectionStrings:MariaDBConnection" "server=31.97.34.101;database=code_TravelBridge_dev;user=code_travelbridge_dev;password=YOUR_NEW_PASSWORD"

# Set HereMaps API key
dotnet user-secrets set "HereMapsApi:ApiKey" "YOUR_HEREMAPS_KEY"

# Set MapBox API key
dotnet user-secrets set "MapBoxApi:ApiKey" "YOUR_MAPBOX_KEY"

# Set Viva Payments credentials
dotnet user-secrets set "VivaApi:ApiKey" "YOUR_VIVA_KEY"
dotnet user-secrets set "VivaApi:ApiSecret" "YOUR_VIVA_SECRET"
dotnet user-secrets set "VivaApi:SourceCode" "YOUR_SOURCE_CODE"
dotnet user-secrets set "VivaApi:SourceCodeTravelProject" "YOUR_PROJECT_SOURCE_CODE"

# Set WebHotelier credentials
dotnet user-secrets set "WebHotelierApi:Username" "YOUR_WH_USERNAME"
dotnet user-secrets set "WebHotelierApi:Password" "YOUR_WH_PASSWORD"

# Set SMTP credentials
dotnet user-secrets set "Smtp:Host" "mail.my-diakopes.gr"
dotnet user-secrets set "Smtp:Username" "YOUR_SMTP_USERNAME"
dotnet user-secrets set "Smtp:Password" "YOUR_SMTP_PASSWORD"
dotnet user-secrets set "Smtp:From" "YOUR_FROM_EMAIL"

# Set test card (for development only)
dotnet user-secrets set "TestCard:CardNumber" "5375346200033267"
dotnet user-secrets set "TestCard:CardType" "MC"
dotnet user-secrets set "TestCard:CardName" "Test User"
dotnet user-secrets set "TestCard:CardMonth" "05"
dotnet user-secrets set "TestCard:CardYear" "2026"
dotnet user-secrets set "TestCard:CardCVV" "590"
```

---

## 📝 Step 2: Verify User Secrets

```bash
# List all secrets (values are masked)
dotnet user-secrets list

# You should see output like:
# ConnectionStrings:MariaDBConnection = server=...;password=***
# VivaApi:ApiKey = ***
# etc.
```

---

## 🔒 Step 3: Production Environment Variables

For production deployment, set these as environment variables (Azure, AWS, Docker, etc.):

### **Azure App Service:**
```bash
az webapp config appsettings set --name YourAppName --resource-group YourRG --settings \
  "ConnectionStrings__MariaDBConnection=server=...;password=..." \
  "VivaApi__ApiSecret=..." \
  "WebHotelierApi__Password=..."
```

### **Docker:**
Create a `.env` file (DO NOT commit):
```env
ConnectionStrings__MariaDBConnection=server=...;password=...
VivaApi__ApiSecret=...
WebHotelierApi__Password=...
```

Then use in `docker-compose.yml`:
```yaml
version: '3.8'
services:
  api:
    build: .
    env_file:
      - .env
```

### **AWS ECS/Fargate:**
Use Parameter Store or Secrets Manager, reference in task definition.

---

## 🧹 Step 4: Clean Git History (If Already Committed)

**⚠️ IMPORTANT: Do this AFTER rotating credentials**

### Option A: BFG Repo Cleaner (Recommended)

```bash
# Download BFG: https://rtyley.github.io/bfg-repo-cleaner/

# Create passwords.txt with the old passwords
cat > passwords.txt << EOF
Skoupidi@2025
9wiOSCzvyTGW9uqSW-Q8BOo0Gc34-wIpAzEcrQVfRzo
pk.eyJ1IjoiYWNoaWxsZWFza2FyIiwiYSI6ImNtNXR6Z3l0czBubW4yanNpa3h0YWszZ3YifQ.t0fSJyVIPkCsl4v93nk0Xw
neMur6Qc3nX76Ae84P40W7iC315FRY
F9FD67BEC99B96C45519D34CB77BAEFEBD445A9B
CNG5YYeI4Cd
EOF

# Run BFG
java -jar bfg.jar --replace-text passwords.txt --no-blob-protection

# Clean up
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# Force push (⚠️ WARNING: Rewrites history!)
git push --force --all
```

### Option B: Git Filter-Repo

```bash
# Install: pip install git-filter-repo

# Remove appsettings.json from history
git filter-repo --path TravelBridge.API/appsettings.json --invert-paths --force

# Re-add the cleaned version
git add TravelBridge.API/appsettings.json
git commit -m "Add appsettings.json template (secrets removed)"

# Force push
git push --force --all
```

---

## 📂 Step 5: Update .gitignore

Add to `.gitignore` (if not already present):

```gitignore
# Secrets
**/appsettings.Development.json
**/appsettings.Production.json
**/secrets.json

# Logs
logs/
*.log

# User-specific files
*.user
*.suo
*.userosscache
*.sln.docstates

# Environment files
.env
.env.local
.env.*.local
```

---

## 🧪 Step 6: Test the Setup

```bash
# Navigate to API project
cd TravelBridge.API

# Run the application
dotnet run

# Verify it starts without errors
# Check logs for: "InventorySeedService starting"

# Test a public endpoint
curl http://localhost:5000/health

# Verify admin endpoints are NOT accessible in Production
# (Set ASPNETCORE_ENVIRONMENT=Production first)
export ASPNETCORE_ENVIRONMENT=Production
dotnet run
# Try: curl http://localhost:5000/admin/owned/inventory/roomtype/1
# Should return 404 (endpoints not registered)
```

---

## 🔐 Step 7: Credential Rotation Checklist

**⚠️ MUST DO if credentials were committed to Git:**

- [ ] MariaDB password (`Skoupidi@2025`)
  - Change via: `ALTER USER 'code_travelbridge_dev'@'%' IDENTIFIED BY 'NewPassword123';`
  
- [ ] HereMaps API key
  - Regenerate at: https://platform.here.com/admin/apps
  
- [ ] MapBox API key
  - Regenerate at: https://account.mapbox.com/access-tokens/
  
- [ ] Viva Payments API secret
  - Contact Viva support or regenerate in merchant portal
  
- [ ] WebHotelier password
  - Change via WebHotelier admin panel
  
- [ ] SMTP password
  - Change via email host control panel

---

## 📋 Verification Checklist

Before deployment, verify:

- [ ] `appsettings.json` contains NO hardcoded secrets (all empty strings)
- [ ] User secrets configured for local development (`dotnet user-secrets list` shows values)
- [ ] Environment variables configured for production
- [ ] `.gitignore` includes `logs/`, `appsettings.*.json`, `.env`
- [ ] No log files in Git (`git ls-files | grep logs/`)
- [ ] Git history cleaned (if secrets were committed)
- [ ] Application starts successfully in Development mode
- [ ] Admin endpoints NOT accessible in Production mode
- [ ] Database connection works with new credentials

---

## 🆘 Troubleshooting

### "Configuration value not found"
- Check user secrets: `dotnet user-secrets list`
- Verify secret key matches appsettings structure (e.g., `ConnectionStrings:MariaDBConnection`)

### "Cannot connect to database"
- Verify connection string in user secrets
- Test connection: `mysql -h server -u user -p`
- Check firewall rules

### "Admin endpoints returning 404"
- Expected in Production (security feature)
- Use Development environment: `export ASPNETCORE_ENVIRONMENT=Development`

### "Inventory seed service failing"
- Check database schema: `dotnet ef database update`
- Verify OwnedRoomTypes table exists
- Check logs in `logs/travelbridge-*.log`

---

## 📚 Additional Resources

- **User Secrets:** https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
- **Environment Variables:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
- **BFG Repo Cleaner:** https://rtyley.github.io/bfg-repo-cleaner/
- **Git Filter-Repo:** https://github.com/newren/git-filter-repo

---

**Status:** ✅ Follow these steps to secure your Phase 3 deployment  
**Priority:** 🚨 CRITICAL - Complete before any deployment
