# Phase 3 - IMMEDIATE ACTION REQUIRED ⚠️

**🚨 SECURITY ISSUES DETECTED AND FIXED IN CODE**  
**👤 YOUR ACTION REQUIRED TO COMPLETE THE FIX**

---

## ⏰ **DO THIS NOW (15 minutes)**

### Step 1: Setup User Secrets (Copy & Paste)

```bash
# Navigate to API project
cd TravelBridge.API

# Initialize user secrets
dotnet user-secrets init

# REPLACE THE VALUES BELOW WITH YOUR ACTUAL CREDENTIALS
# ⚠️ Use NEW credentials (rotate the old ones that were exposed)

# Database
dotnet user-secrets set "ConnectionStrings:MariaDBConnection" "server=31.97.34.101;database=code_TravelBridge_dev;user=code_travelbridge_dev;password=YOUR_NEW_DB_PASSWORD"

# HereMaps
dotnet user-secrets set "HereMapsApi:ApiKey" "YOUR_NEW_HEREMAPS_KEY"

# MapBox
dotnet user-secrets set "MapBoxApi:ApiKey" "YOUR_NEW_MAPBOX_KEY"

# Viva Payments
dotnet user-secrets set "VivaApi:ApiKey" "YOUR_VIVA_KEY"
dotnet user-secrets set "VivaApi:ApiSecret" "YOUR_NEW_VIVA_SECRET"
dotnet user-secrets set "VivaApi:SourceCode" "9156"
dotnet user-secrets set "VivaApi:SourceCodeTravelProject" "9695"

# WebHotelier
dotnet user-secrets set "WebHotelierApi:Username" "travelproje20666"
dotnet user-secrets set "WebHotelierApi:Password" "YOUR_NEW_WH_PASSWORD"

# SMTP
dotnet user-secrets set "Smtp:Host" "mail.my-diakopes.gr"
dotnet user-secrets set "Smtp:Username" "bookings@my-diakopes.gr"
dotnet user-secrets set "Smtp:Password" "YOUR_NEW_SMTP_PASSWORD"
dotnet user-secrets set "Smtp:From" "bookings@my-diakopes.gr"

# Test Card (for development)
dotnet user-secrets set "TestCard:CardNumber" "5375346200033267"
dotnet user-secrets set "TestCard:CardType" "MC"
dotnet user-secrets set "TestCard:CardName" "Test User"
dotnet user-secrets set "TestCard:CardMonth" "05"
dotnet user-secrets set "TestCard:CardYear" "2026"
dotnet user-secrets set "TestCard:CardCVV" "590"
```

### Step 2: Verify It Works

```bash
# List secrets (values will be masked)
dotnet user-secrets list

# Run the application
dotnet run

# Check that it starts successfully
# You should see: "InventorySeedService starting"
```

### Step 3: Remove Logs from Git

```bash
# From repository root
git rm -r --cached TravelBridge.API/logs/
git commit -m "fix: remove log files from Git tracking"
git push
```

---

## 🔒 **CREDENTIAL ROTATION (Do Within 24 Hours)**

**⚠️ These credentials were exposed in Git and MUST be changed:**

### 1. MariaDB Password
**Exposed:** `Skoupidi@2025`

```sql
-- Connect to your database as admin
ALTER USER 'code_travelbridge_dev'@'%' IDENTIFIED BY 'NewSecurePassword123!';
FLUSH PRIVILEGES;

-- Update in user secrets
dotnet user-secrets set "ConnectionStrings:MariaDBConnection" "server=31.97.34.101;database=code_TravelBridge_dev;user=code_travelbridge_dev;password=NewSecurePassword123!"
```

### 2. HereMaps API Key
**Exposed:** `9wiOSCzvyTGW9uqSW-Q8BOo0Gc34-wIpAzEcrQVfRzo`

1. Go to: https://platform.here.com/admin/apps
2. Regenerate or create new API key
3. Update: `dotnet user-secrets set "HereMapsApi:ApiKey" "NEW_KEY"`

### 3. MapBox API Key
**Exposed:** `pk.eyJ1IjoiYWNoaWxsZWFza2FyIiwiYSI6ImNtNXR6Z3l0czBubW4yanNpa3h0YWszZ3YifQ.t0fSJyVIPkCsl4v93nk0Xw`

1. Go to: https://account.mapbox.com/access-tokens/
2. Revoke old token
3. Create new token
4. Update: `dotnet user-secrets set "MapBoxApi:ApiKey" "NEW_KEY"`

### 4. Viva Payments API Secret
**Exposed:** `neMur6Qc3nX76Ae84P40W7iC315FRY`

1. Contact Viva Payments support or use merchant portal
2. Regenerate API secret
3. Update: `dotnet user-secrets set "VivaApi:ApiSecret" "NEW_SECRET"`

### 5. WebHotelier Password
**Exposed:** `F9FD67BEC99B96C45519D34CB77BAEFEBD445A9B`

1. Log into WebHotelier admin panel
2. Change password for user `travelproje20666`
3. Update: `dotnet user-secrets set "WebHotelierApi:Password" "NEW_PASSWORD"`

### 6. SMTP Password
**Exposed:** `CNG5YYeI4Cd`

1. Access your email hosting control panel (my-diakopes.gr)
2. Change password for `bookings@my-diakopes.gr`
3. Update: `dotnet user-secrets set "Smtp:Password" "NEW_PASSWORD"`

---

## 🧹 **CLEAN GIT HISTORY (Do After Rotation)**

**⚠️ Only do this AFTER you've rotated all credentials above!**

### Option A: BFG Repo Cleaner (Easiest)

```bash
# 1. Download BFG
wget https://repo1.maven.org/maven2/com/madgag/bfg/1.14.0/bfg-1.14.0.jar
# OR visit: https://rtyley.github.io/bfg-repo-cleaner/

# 2. Create a file with exposed passwords
cat > passwords.txt << 'EOF'
Skoupidi@2025
9wiOSCzvyTGW9uqSW-Q8BOo0Gc34-wIpAzEcrQVfRzo
pk.eyJ1IjoiYWNoaWxsZWFza2FyIiwiYSI6ImNtNXR6Z3l0czBubW4yanNpa3h0YWszZ3YifQ.t0fSJyVIPkCsl4v93nk0Xw
neMur6Qc3nX76Ae84P40W7iC315FRY
F9FD67BEC99B96C45519D34CB77BAEFEBD445A9B
CNG5YYeI4Cd
EOF

# 3. Run BFG (from repository root)
java -jar bfg-1.14.0.jar --replace-text passwords.txt --no-blob-protection

# 4. Clean up
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# 5. Force push (⚠️ WARNING: This rewrites Git history!)
git push --force --all

# 6. Notify team members to re-clone the repository
```

### Option B: Manual (If you prefer)

```bash
# Remove appsettings.json from history
git filter-repo --path TravelBridge.API/appsettings.json --invert-paths --force

# Re-add the cleaned version
git add TravelBridge.API/appsettings.json
git commit -m "Add appsettings.json template (secrets removed)"

# Force push
git push --force --all
```

---

## ✅ **VERIFICATION CHECKLIST**

After completing the above, verify:

- [ ] Application starts successfully (`dotnet run`)
- [ ] No errors about missing configuration
- [ ] Database connection works
- [ ] `appsettings.json` contains only empty strings (no real credentials)
- [ ] User secrets configured (`dotnet user-secrets list` shows entries)
- [ ] Logs removed from Git (`git ls-files | grep logs/` returns nothing)
- [ ] Old credentials no longer work (test old DB password - should fail)
- [ ] New credentials work (test new DB password - should succeed)

---

## 📱 **NOTIFY THESE PEOPLE**

If this is a team project:

1. **Team Members:** "We had a security issue. Please pull latest changes and run: `git pull --force`"
2. **Security Team:** "Credentials were exposed in Git history between [dates]. All credentials rotated and Git history cleaned."
3. **Service Providers:** Notify Viva Payments, WebHotelier if their portals have suspicious activity alerts

---

## 🆘 **NEED HELP?**

**Issue:** "Can't connect to database after setup"
```bash
# Test connection manually
mysql -h 31.97.34.101 -u code_travelbridge_dev -p
# Enter your NEW password

# If it works, update user secret:
dotnet user-secrets set "ConnectionStrings:MariaDBConnection" "server=31.97.34.101;database=code_TravelBridge_dev;user=code_travelbridge_dev;password=YOUR_NEW_PASSWORD"
```

**Issue:** "dotnet user-secrets command not found"
```bash
# Ensure you're in the TravelBridge.API directory
cd TravelBridge.API
pwd  # Should show: .../TravelBridge/TravelBridge.API
```

**Issue:** "BFG says 'no blob-ids were specified'"
```bash
# Use --no-blob-protection flag
java -jar bfg-1.14.0.jar --replace-text passwords.txt --no-blob-protection
```

---

## 📚 **FULL DOCUMENTATION**

- **Issue Details:** `docs/PHASE3_CRITICAL_ISSUES.md`
- **Setup Guide:** `docs/PHASE3_SECURITY_SETUP.md`
- **Fixes Summary:** `docs/PHASE3_FIXES_SUMMARY.md`

---

**⏱️ TIME ESTIMATE:**
- Setup user secrets: 5 minutes
- Rotate credentials: 30-60 minutes (varies by service)
- Clean Git history: 10 minutes

**TOTAL: ~1 hour to secure everything**

---

**Status:** 🚨 **ACTION REQUIRED**  
**Priority:** 🔴 **CRITICAL - Do before any deployment**

✅ **Code fixes are done. Now it's your turn to secure the credentials!**
