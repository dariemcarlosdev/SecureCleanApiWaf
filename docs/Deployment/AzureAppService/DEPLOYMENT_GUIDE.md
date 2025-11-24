# ?? CleanArchitecture.ApiTemplate Deployment Guide

> *"Deployment is not the end of development; it's the beginning of value delivery."*  
> — DevOps Principles

## ?? Table of Contents

### **Quick Navigation**
1. [Overview](#-overview)
2. [Architecture Overview](#-architecture-overview)
3. [Prerequisites](#-prerequisites)
   - [Local Development](#local-development)
   - [Azure Resources](#azure-resources)
   - [GitHub](#github)
4. [Step 1: Clone the Repository](#-step-1-clone-the-repository)
   - [Option 1: Fresh Clone](#option-1-fresh-clone)
   - [Option 2: Update Existing Clone](#option-2-update-existing-clone)
5. [Step 2: Build and Test Locally](#-step-2-build-and-test-locally)
   - [2.1 Clean the Solution](#21-clean-the-solution)
   - [2.2 Restore Dependencies](#22-restore-dependencies)
   - [2.3 Build the Solution](#23-build-the-solution)
   - [2.4 Run Tests](#24-run-tests-optional)
   - [2.5 Run the Application Locally](#25-run-the-application-locally)
6. [Step 3: Configure Azure Resources](#-step-3-configure-azure-resources)
   - [3.1 Create Azure App Service](#31-create-azure-app-service)
   - [3.2 Download Publish Profile](#32-download-publish-profile)
   - [3.3 Enable Managed Identity](#33-enable-managed-identity-recommended)
   - [3.4 Configure App Service Settings](#34-configure-app-service-settings)
7. [Step 4: Configure Azure Key Vault](#-step-4-configure-azure-key-vault-optional-but-recommended)
   - [4.1 Create Key Vault](#41-create-key-vault)
   - [4.2 Add Secrets to Key Vault](#42-add-secrets-to-key-vault)
   - [4.3 Grant App Service Access](#43-grant-app-service-access-to-key-vault)
   - [4.4 Configure Application](#44-configure-application-to-use-key-vault)
8. [Step 5: Configure GitHub Secrets](#-step-5-configure-github-secrets)
   - [5.1 Navigate to Repository Secrets](#51-navigate-to-repository-secrets)
   - [5.2 Add Required Secrets](#52-add-required-secrets)
   - [5.3 Optional: Add Azure Service Principal](#53-optional-add-azure-service-principal-for-app-settings-automation)
   - [5.4 Verify Secrets](#54-verify-secrets)
9. [Step 6: Deploy via GitHub Actions](#-step-6-deploy-via-github-actions)
   - [6.1 Understanding the Workflow](#61-understanding-the-workflow)
   - [6.2 Deploy to Production](#62-deploy-to-production-master-branch)
   - [6.3 Monitor Deployment](#63-monitor-deployment)
   - [6.4 Verify Deployment in Azure](#64-verify-deployment-in-azure)
10. [Step 7: Health Check Endpoint](#-step-7-health-check-endpoint-optional-enhancement)
    - [7.1 Add Health Check](#71-add-health-check-to-programcs)
    - [7.2 Test Health Endpoint Locally](#72-test-health-endpoint-locally)
11. [Step 8: Monitor Your Application](#-step-8-monitor-your-application)
    - [8.1 Application Insights](#81-application-insights-optional)
12. [Troubleshooting](#-troubleshooting)
    - [Issue 1: Deployment Fails](#issue-1-deployment-fails---publish-profile-error)
    - [Issue 2: Application Shows 500 Error](#issue-2-application-shows-500-error)
13. [Additional Resources](#-additional-resources)
14. [Deployment Checklist](#-deployment-checklist)
15. [Summary](#-summary)
16. [Support](#-support)
17. [Contact](#contact)

---

## ?? Overview

This comprehensive guide walks you through deploying CleanArchitecture.ApiTemplate to Azure App Service using modern CI/CD practices with GitHub Actions. The application follows Clean Architecture principles and is built with .NET 8 and Blazor Server.

---

## ??? Architecture Overview

**CleanArchitecture.ApiTemplate** is a production-ready application featuring:
- ? **Clean Architecture** (Single-project with clear layer separation)
- ? **CQRS Pattern** (MediatR for command/query separation)
- ? **Interface Abstractions** (Dependency Inversion Principle)
- ? **Distributed Caching** (In-memory with Redis support)
- ? **Azure Integration** (Key Vault, App Service, Application Insights)
- ? **CI/CD Pipeline** (Automated builds, tests, and deployments)

**Technology Stack:**
- .NET 8.0
- Blazor Server
- MediatR (CQRS)
- ASP.NET Core Web API
- Azure App Service
- Azure Key Vault
- GitHub Actions

---

## ? Prerequisites

Before starting, ensure you have:

### **Local Development**
- [ ] .NET 8 SDK installed ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- [ ] Git installed ([Download](https://git-scm.com/downloads))
- [ ] Visual Studio 2022 or VS Code with C# extension
- [ ] Azure CLI (optional, for manual Azure operations)

### **Azure Resources**
- [ ] Active Azure subscription
- [ ] Azure App Service (Linux or Windows, .NET 8 runtime)
- [ ] Azure Key Vault (optional but recommended)
- [ ] Azure Application Insights (optional, for monitoring)

### **GitHub**
- [ ] GitHub account with repository access
- [ ] Repository cloned locally

---

## ?? Step 1: Clone the Repository

### **Option 1: Fresh Clone**

```bash
# Clone the repository
git clone https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate.git

# Navigate to project directory
cd CleanArchitecture.ApiTemplate

# Verify you're on the correct branch
git branch
```

### **Option 2: Update Existing Clone**

```bash
# Fetch latest changes
git fetch origin

# Switch to Master (production) or Dev (development)
git checkout Master
# OR
git checkout Dev

# Pull latest changes
git pull origin Master
```

---

## ?? Step 2: Build and Test Locally

### **2.1 Clean the Solution**

```bash
# Remove previous build artifacts
dotnet clean

# Remove bin and obj folders (optional, thorough clean)
Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue
```

### **2.2 Restore Dependencies**

```bash
# Restore NuGet packages
dotnet restore

# Verify packages restored successfully
dotnet list package
```

### **2.3 Build the Solution**

```bash
# Build in Release mode
dotnet build --configuration Release --no-restore

# Expected output: Build succeeded. 0 Warning(s). 0 Error(s).
```

### **2.4 Run Tests (Optional)**

```bash
# Run unit tests (when test projects exist)
dotnet test --configuration Release --no-build --verbosity normal

# Note: Currently no test projects - this is a future enhancement
```

### **2.5 Run the Application Locally**

```bash
# Run the application
dotnet run --project CleanArchitecture.ApiTemplate.csproj

# Application should start on:
# - HTTP: http://localhost:5006
# - HTTPS: https://localhost:7178
```

**Verify Local Installation:**
1. Open browser to: `http://localhost:5006`
2. Check Swagger UI: `http://localhost:5006/swagger`
3. Test a sample API endpoint
4. Verify Blazor pages load correctly

---

## ?? Step 3: Configure Azure Resources

### **3.1 Create Azure App Service**

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** (top left)
3. Search for **"Web App"** and select it
4. Click **Create**

#### **Configure Basic Settings**

**Basics Tab:**

| Setting | Value | Notes |
|---------|-------|-------|
| **Subscription** | Your subscription | Select your active subscription |
| **Resource Group** | `CleanArchitecture.ApiTemplate-RG` | Click **Create new** if it doesn't exist |
| **Name** | `CleanArchitecture.ApiTemplate` (must be unique) | This becomes `CleanArchitecture.ApiTemplate.azurewebsites.net` |
| **Publish** | **Code** | Select "Code" (not "Docker Container") |
| **Runtime stack** | **.NET 8 (LTS)** | Must match your project target framework |
| **Operating System** | **Linux** | Recommended for .NET 8 (lower cost, better performance) |
| **Region** | **East US** (or nearest) | Choose the region closest to your users |

**Pricing Plans Tab:**

| Setting | Value | Notes |
|---------|-------|-------|
| **Pricing Plan** | **Basic B1** | $13/month - Good for testing/small apps |
| | or **Free F1** | Free tier (limited resources, no custom domains) |
| | or **Standard S1** | $70/month - Production workloads |

5. Click **Review + Create**
6. Review your settings
7. Click **Create**
8. Wait for deployment to complete (~2-3 minutes)
9. Click **Go to resource** when deployment finishes

**Your App Service is now created!** ?

---

### **3.2 Download Publish Profile**

The publish profile contains deployment credentials needed for GitHub Actions.

**Steps:**

1. In your App Service page, find the top menu bar
2. Click **Get publish profile** button (looks like a download icon)
3. A file named `CleanArchitecture.ApiTemplate.PublishSettings` will download
4. **Save this file securely** - it contains deployment credentials
5. **Do NOT commit this file to Git**

**Important Security Notes:**
- This file contains passwords and deployment keys
- Keep it in a secure location
- You'll copy its contents to GitHub Secrets (next steps)
- If compromised, regenerate from Azure Portal

---

### **3.3 Enable Managed Identity (Recommended)**

Managed Identity allows your app to securely access other Azure resources (like Key Vault) without storing passwords.

**Steps:**
1. In your App Service, scroll down the left menu
2. Under **Settings**, click **Identity**
3. You'll see the **System assigned** tab
4. Toggle **Status** to **On**
5. Click **Save** at the top
6. A confirmation dialog appears ? Click **Yes**
7. Wait a moment for Azure to create the identity
8. **Copy the Object (principal) ID** (you'll need this for Key Vault access)

**What This Does:**
- Creates a secure identity for your app in Azure Active Directory
- No passwords or keys to manage
- The app can now authenticate to Azure services automatically

---

### **3.4 Configure App Service Settings**

Set initial configuration values in your App Service. These will be updated automatically by the CI/CD pipeline later.

**Steps:**
1. In your App Service, scroll down the left menu
2. Under **Settings**, click **Configuration**
3. Click the **Application settings** tab
4. Click **+ New application setting**

**Add Setting 1: Environment**

| Field | Value |
|-------|-------|
| **Name** | `ASPNETCORE_ENVIRONMENT` |
| **Value** | `Production` |
| **Deployment slot setting** | ? Unchecked |

Click **OK**

**Add Setting 2: API Base URL**

| Field | Value |
|-------|-------|
| **Name** | `ThirdPartyApi__BaseUrl` |
| **Value** | `https://api.example.com/` (replace with your actual API URL) |
| **Deployment slot setting** | ? Unchecked |

Click **OK**

5. Click **Save** at the top of the Configuration page
6. Click **Continue** to confirm restart

**Note:** The double underscore `__` in `ThirdPartyApi__BaseUrl` is important - it represents nested configuration in .NET (equivalent to JSON `"ThirdPartyApi": { "BaseUrl": "..." }`).

**Important:** The GitHub Actions CI/CD pipeline will automatically update these settings during deployment. This manual configuration is just for initial setup.

---

## ?? Step 4: Configure Azure Key Vault (Optional but Recommended)

Azure Key Vault provides secure secret management for passwords, API keys, and certificates.

### **4.1 Create Key Vault**

**Steps:**

1. In [Azure Portal](https://portal.azure.com), click **Create a resource**
2. Search for **"Key Vault"** and select it
3. Click **Create**

**Basics Tab:**

| Setting | Value | Notes |
|---------|-------|-------|
| **Subscription** | Your subscription | Same as App Service |
| **Resource Group** | `CleanArchitecture.ApiTemplate-RG` | Use the same resource group |
| **Key vault name** | `CleanArchitecture.ApiTemplate-kv` (must be unique) | Lowercase, hyphens allowed |
| **Region** | **East US** | Same region as App Service for best performance |
| **Pricing tier** | **Standard** | Sufficient for most apps ($0.03 per 10,000 operations) |

**Access configuration Tab:**

| Setting | Value | Notes |
|---------|-------|-------|
| **Permission model** | **Vault access policy** | Recommended for App Services |
| **Enable access to** | ?? Azure Virtual Machines for deployment | Check if using VMs |
| | ?? Azure Resource Manager for template deployment | Recommended |

**Networking Tab:**

| Setting | Value |
|---------|-------|
| **Connectivity method** | **Public endpoint (all networks)** |

*Note: For production, consider "Private endpoint" for enhanced security*

**Advanced Tab:**

| Setting | Value | Importance |
|---------|-------|-----------|
| **Soft-delete** | **90 days** | ? Keeps deleted secrets recoverable |
| **Purge protection** | **Enabled** | ? Prevents permanent deletion (recommended for production) |

4. Click **Review + create**
5. Click **Create**
6. Wait for deployment (~1-2 minutes)
7. Click **Go to resource**

**Your Key Vault is now created!** ?

---

### **4.2 Add Secrets to Key Vault**

Now let's store sensitive values securely in Key Vault.

**Steps:**

1. In your Key Vault, scroll down the left menu
2. Under **Objects**, click **Secrets**
3. Click **+ Generate/Import** at the top

**Add Secret 1: API Key**

| Field | Value |
|-------|-------|
| **Upload options** | **Manual** |
| **Name** | `ThirdPartyApiKey` |
| **Value** | `your-actual-api-key-here` (paste your real API key) |
| **Content type** | `text/plain` (optional description) |
| **Set activation date** | ? Unchecked (or set if needed) |
| **Set expiration date** | ? Unchecked (or set if needed) |
| **Enabled** | ?? Yes |

Click **Create**

**Add Secret 2: Connection String (if using database)**

Repeat the steps above with:

| Field | Value |
|-------|-------|
| **Name** | `ConnectionStrings--DefaultConnection` |
| **Value** | `Server=...;Database=...;User Id=...;Password=...;` |

*Note: Use double hyphens `--` to represent nested JSON structure in .NET configuration*

Click **Create**

**Your secrets are now securely stored!** ?

---

### **4.3 Grant App Service Access to Key Vault**

Allow your App Service to read secrets from Key Vault using Managed Identity.

**Steps:**

1. In your Key Vault, scroll down the left menu
2. Under **Settings**, click **Access policies**
3. Click **+ Create** (top of the page)

**Permissions Tab:**

Under **Secret permissions**, check:
- ?? **Get** (read secrets)
- ?? **List** (enumerate secrets)

Click **Next**

**Principal Tab:**

1. In the search box, type your App Service name: `CleanArchitecture.ApiTemplate`
2. Select your App Service from the results (look for the System Assigned Identity)
3. Click **Next**

**Review + create Tab:**

1. Review the permissions
2. Click **Create**

**Verify Access Policy:**

You should now see your App Service listed in the Access policies table with:
- **Principal:** CleanArchitecture.ApiTemplate
- **Secret permissions:** Get, List

**Your App Service can now securely access Key Vault secrets!** ??

---

### **4.4 Configure Application to Use Key Vault**

The application is already configured to use Key Vault in production. You just need to tell it where your Key Vault is located.

**Steps:**

1. **Create** or **update** `appsettings.Production.json` in your project root
2. Add the Key Vault URL:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "KeyVault": {
    "Url": "https://CleanArchitecture.ApiTemplate-kv.vault.azure.net/"
  },
  "AllowedHosts": "*"
}
```

**Important:** Replace `CleanArchitecture.ApiTemplate-kv` with your actual Key Vault name!

**How It Works:**

The application already has this code in `Program.cs`:

```csharp
// Automatically loads secrets from Key Vault in Production
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential()); // Uses Managed Identity
    }
}
```

**What This Does:**
1. When deployed to Azure, the app detects it's in Production
2. Reads the Key Vault URL from configuration
3. Connects to Key Vault using Managed Identity (no passwords needed!)
4. Automatically loads all secrets into app configuration
5. You can access secrets using `configuration["SecretName"]`

**Example Usage:**

```csharp
// In your code, access Key Vault secrets like any other config value
var apiKey = configuration["ThirdPartyApiKey"];
var connectionString = configuration["ConnectionStrings:DefaultConnection"];
```

---

## ?? Step 5: Configure GitHub Secrets

GitHub Secrets securely store sensitive data for CI/CD workflows.

### **5.1 Navigate to Repository Secrets**

1. Go to your GitHub repository: `https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate`
2. Click **Settings** (top menu)
3. Expand **Secrets and variables** (left menu) ? **Actions**
4. Click **New repository secret**

### **5.2 Add Required Secrets**

Add each secret individually:

#### **Secret 1: AZURE_WEBAPP_NAME**
- **Name:** `AZURE_WEBAPP_NAME`
- **Value:** `CleanArchitecture.ApiTemplate` (your App Service name, without `.azurewebsites.net`)
- Click **Add secret**

#### **Secret 2: AZURE_WEBAPP_PUBLISH_PROFILE**
- **Name:** `AZURE_WEBAPP_PUBLISH_PROFILE`
- **Value:** (Paste entire contents of `.PublishSettings` file downloaded in Step 3.2)
- Click **Add secret**

#### **Secret 3: THIRDPARTY_API_BASEURL**
- **Name:** `THIRDPARTY_API_BASEURL`
- **Value:** `https://api.example.com/` (your actual API base URL)
- Click **Add secret**

### **5.3 Optional: Add Azure Service Principal (For App Settings Automation)**

To enable automatic App Settings configuration during deployment, create a Service Principal.

**What is a Service Principal?**
A Service Principal is like a "service account" that allows GitHub Actions to authenticate to Azure and make changes (like updating App Settings) during deployment.

**Steps to Create Service Principal via Azure Portal:**

1. In [Azure Portal](https://portal.azure.com), click the search bar at the top
2. Search for **"App registrations"** and select it
3. Click **+ New registration**

**Register an application:**

| Field | Value |
|-------|-------|
| **Name** | `GitHubActions-CleanArchitecture.ApiTemplate` |
| **Supported account types** | **Accounts in this organizational directory only** |
| **Redirect URI** | Leave blank |

4. Click **Register**
5. **Copy and save** these values (you'll need them for GitHub Secret):
   - **Application (client) ID**
   - **Directory (tenant) ID**

**Create Client Secret:**

6. In your new App registration, click **Certificates & secrets** (left menu)
7. Click **+ New client secret**
8. Fill in:
   - **Description:** `GitHub Actions Deployment`
   - **Expires:** `730 days (24 months)` (or your preference)
9. Click **Add**
10. **Immediately copy the Value** (client secret) - you can't see it again!

**Assign Permissions to Resource Group:**

11. Navigate to your Resource Group (`CleanArchitecture.ApiTemplate-RG`)
12. In the Resource Group, click **Access control (IAM)** (left menu)
13. Click **+ Add** ? **Add role assignment**
14. **Role Tab:**
    - Search for and select **Contributor**
    - Click **Next**
15. **Members Tab:**
    - Select **User, group, or service principal**
    - Click **+ Select members**
    - Search for `GitHubActions-CleanArchitecture.ApiTemplate`
    - Select it
    - Click **Select**
    - Click **Next**
16. **Review + assign Tab:**
    - Click **Review + assign**

**Create GitHub Secret JSON:**

17. Create a JSON file with this format (use the values you copied):

```json
{
  "clientId": "<Application (client) ID>",
  "clientSecret": "<Client secret Value>",
  "subscriptionId": "<Your subscription ID>",
  "tenantId": "<Directory (tenant) ID>"
}
```

**To get your Subscription ID:**
- Azure Portal ? Search "Subscriptions" ? Copy your Subscription ID

**Add to GitHub:**

18. Go to GitHub repository ? **Settings** ? **Secrets and variables** ? **Actions**
19. Click **New repository secret**
20. Name: `AZURE_CREDENTIALS`
21. Value: Paste the entire JSON (from step 17)
22. Click **Add secret**

**Verification:**

Your Service Principal is now configured and GitHub Actions can:
- ? Authenticate to Azure
- ? Update App Service settings
- ? Access resources in your Resource Group

**Security Notes:**
- The client secret expires (24 months by default)
- Create a calendar reminder to renew before expiration
- Store the secret securely (password manager)
- Never commit to Git

### **5.4 Verify Secrets**

After adding all secrets, you should see:

| Secret Name | Status |
|-------------|--------|
| `AZURE_WEBAPP_NAME` | ? Set |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | ? Set |
| `THIRDPARTY_API_BASEURL` | ? Set |
| `AZURE_CREDENTIALS` | ?? Set (optional) |

---

## ?? Step 6: Deploy via GitHub Actions

The CI/CD pipeline is fully automated. Deployments trigger on push to Master branch.

### **6.1 Understanding the Workflow**

**Workflow File:** `.github/workflows/azure-deploy.yml`

**Trigger Behavior:**

| Event | Build | Test | Deploy | Environment |
|-------|-------|------|--------|-------------|
| **Push to Master** | ? | ? | ? | Production |
| **Push to Dev** | ? | ? | ? | N/A |
| **Pull Request** | ? | ? | ? | N/A |

**Workflow Jobs:**
1. **Build Job:** Compiles, tests, and publishes the application
2. **Deploy Job:** Deploys to Azure (Master only)
3. **Security Scan Job:** Scans for vulnerabilities (PRs only)

### **6.2 Deploy to Production (Master Branch)**

#### **Option 1: Direct Push to Master**

```bash
# Ensure you're on Dev branch with latest changes
git checkout Dev
git pull origin Dev

# Merge Dev into Master
git checkout Master
git merge Dev

# Push to Master (triggers deployment)
git push origin Master
```

#### **Option 2: Pull Request (Recommended)**

1. Create a branch for your changes:
```bash
git checkout Dev
git checkout -b feature/my-changes
```

2. Make changes, commit, and push:
```bash
git add .
git commit -m "feat: implement new feature"
git push origin feature/my-changes
```

3. Open Pull Request on GitHub:
   - Base: `Master`
   - Compare: `feature/my-changes`
   - Review changes
   - Wait for CI checks to pass (build + security scan)
   - Merge PR (triggers deployment)

### **6.3 Monitor Deployment**

**GitHub Actions UI:**
1. Go to repository ? **Actions** tab
2. Click on the running workflow
3. Watch real-time logs for each job:
   - Build and Test
   - Deploy to Azure
   - Health Check

**Typical Deployment Timeline:**
- Build job: ~2-3 minutes
- Deploy job: ~1-2 minutes
- **Total:** ~3-5 minutes

**Expected Output:**
```
? Build and Test
   ? Checkout code
   ? Setup .NET
   ? Cache NuGet packages (cache hit - 50% faster!)
   ? Restore dependencies
   ? Build
   ? Test (optional)
   ? Publish
   ? Upload artifact

? Deploy to Azure
   ? Download artifact
   ? Deploy to Azure Web App
   ? Azure Login (optional)
   ? Set Azure App Settings (optional)
   ? Health Check
```

### **6.4 Verify Deployment in Azure**

1. **Azure Portal Verification:**
   - Go to your App Service in Azure Portal
   - Check **Overview** ? **Status** should be "Running"
   - Check **Deployment Center** ? Recent deployments should show success

2. **Application URL Verification:**
   ```
   https://CleanArchitecture.ApiTemplate.azurewebsites.net
   ```
   - Blazor UI should load
   - Check Swagger: `https://CleanArchitecture.ApiTemplate.azurewebsites.net/swagger`

3. **Health Endpoint Verification:**
   ```bash
   curl https://CleanArchitecture.ApiTemplate.azurewebsites.net/health
   ```
   - Should return: `Healthy`

---

## ?? Step 7: Health Check Endpoint (Optional Enhancement)

The CI/CD workflow includes a health check step. Add a health endpoint to your application:

### **7.1 Add Health Check to Program.cs**

```csharp
// Already implemented in Program.cs via PresentationServiceExtensions
builder.Services.AddHealthChecks();

// In pipeline configuration
app.MapHealthChecks("/health");
```

### **7.2 Test Health Endpoint Locally**

```bash
# Run application
dotnet run

# Test health endpoint
curl http://localhost:5006/health
# Expected output: Healthy
```

---

## ?? Step 8: Monitor Your Application

### **8.1 Application Insights (Optional)**

Enable Application Insights for advanced monitoring, performance tracking, and diagnostics.

**Steps to Create Application Insights:**

1. In [Azure Portal](https://portal.azure.com), click **Create a resource**
2. Search for **"Application Insights"** and select it
3. Click **Create**

**Project Details:**

| Setting | Value | Notes |
|---------|-------|-------|
| **Subscription** | Your subscription | Same as App Service |
| **Resource Group** | `CleanArchitecture.ApiTemplate-RG` | Use the same resource group |
| **Name** | `CleanArchitecture.ApiTemplate-insights` | Descriptive name |
| **Region** | **East US** | Same region as App Service |
| **Resource Mode** | **Workspace-based** | Recommended (uses Log Analytics) |

**Workspace Details:**

| Setting | Value |
|---------|-------|
| **Log Analytics Workspace** | Create new or select existing |
| **Workspace name** | `CleanArchitecture.ApiTemplate-workspace` (if creating new) |

4. Click **Review + create**
5. Click **Create**
6. Wait for deployment (~1 minute)
7. Click **Go to resource**

**Get Connection String:**

8. In your Application Insights resource, look for **Overview** (left menu)
9. At the top right, you'll see **Connection String**
10. Click the **Copy to clipboard** icon next to the connection string
11. **Save this value** - you'll add it to App Service configuration

**Configure App Service to Use Application Insights:**

12. Go back to your **App Service** (`CleanArchitecture.ApiTemplate`)
13. Scroll down to **Settings** ? **Configuration**
14. Click **+ New application setting**

| Field | Value |
|-------|-------|
| **Name** | `ApplicationInsights__ConnectionString` |
| **Value** | Paste the connection string you copied |
| **Deployment slot setting** | ? Unchecked |

15. Click **OK**
16. Click **Save** at the top
17. Click **Continue** to restart

**Alternative: Enable from App Service**

Azure App Service has a shortcut to enable Application Insights:

1. In your App Service, scroll to **Settings** ? **Application Insights**
2. Click **Turn on Application Insights**
3. Select **Create new resource** or **Select existing resource**
4. If creating new:
   - **Application Insights name:** `CleanArchitecture.ApiTemplate-insights`
   - **Location:** Same as App Service
5. Click **Apply**
6. Click **Yes** to confirm

**Verify Application Insights:**

1. Go to your Application Insights resource
2. Click **Live Metrics** (left menu)
3. Deploy your app or make requests
4. You should see real-time telemetry appearing

**What You Get with Application Insights:**

- ? **Request tracking** - See all HTTP requests, response times
- ? **Dependency tracking** - Monitor calls to databases, APIs
- ? **Exception tracking** - Automatic logging of all exceptions
- ? **Performance monitoring** - CPU, memory, request duration
- ? **Custom events** - Track your own business metrics
- ? **Application Map** - Visualize dependencies
- ? **Smart Detection** - AI-powered anomaly detection

**Access Your Telemetry:**

In Application Insights, explore:
- **Live Metrics** - Real-time monitoring
- **Failures** - Exceptions and failed requests
- **Performance** - Slow requests and dependencies
- **Metrics** - Charts and graphs
- **Logs** - Query telemetry with KQL

**Cost:** Application Insights offers 5 GB of data ingestion free per month, then pay-as-you-go ($2.30 per GB).

---

## ?? Troubleshooting

### **Issue 1: Deployment Fails - Publish Profile Error**

**Symptom:** GitHub Actions shows "Error: Publish profile not found"

**Solution:**
1. Re-download publish profile from Azure Portal
2. Update `AZURE_WEBAPP_PUBLISH_PROFILE` secret in GitHub
3. Ensure no extra whitespace in secret value
4. Re-run workflow

### **Issue 2: Application Shows 500 Error**

**Symptom:** Azure app loads but shows HTTP 500 error

**Solution:**

**Check App Service Logs:**

1. Go to your App Service in Azure Portal
2. Scroll down to **Monitoring** ? **Log stream** (left menu)
3. Click **Application Logs** tab
4. Look for error messages in red

**Enable Detailed Logging:**

1. In App Service, go to **Monitoring** ? **App Service logs**
2. Turn on:
   - **Application Logging (Filesystem):** Error level
   - **Detailed error messages:** On
   - **Failed request tracing:** On
3. Click **Save**
4. Go back to **Log stream** to see more detailed logs

**Common Causes & Fixes:**

**Cause 1: Wrong Environment Variable**
1. Go to **Configuration** ? **Application settings**
2. Verify `ASPNETCORE_ENVIRONMENT` is set to `Production`
3. Save and restart if changed

**Cause 2: Missing Key Vault Access**
1. Go to your Key Vault
2. Check **Access policies**
3. Verify your App Service is listed with Get/List permissions
4. If missing, add access policy (see Step 4.3)

**Cause 3: Missing NuGet Packages**
1. Check **Deployment Center** ? **Logs**
2. Look for "File not found" or assembly errors
3. Verify all required packages are in `.csproj`
4. Redeploy the application

**Cause 4: Configuration Missing**
1. Check if required configuration is present
2. Go to **Configuration** ? **Application settings**
3. Verify all expected settings are there
4. Add any missing settings and restart

---

## ?? Additional Resources

### **Documentation**
- [CleanArchitecture.ApiTemplate README](README.md) - Project overview
- [Clean Architecture Docs](docs/CleanArchitecture/README.md) - Architecture details
- [CI/CD Workflow Documentation](docs/CI-CD-WORKFLOW-UPDATE.md) - Pipeline details
- [Interface Abstractions](docs/CleanArchitecture/INTERFACE_ABSTRACTIONS_SUMMARY.md) - DI setup

### **Azure Documentation**
- [Azure App Service](https://docs.microsoft.com/azure/app-service/)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault/)
- [Managed Identity](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)

### **GitHub Actions**
- [GitHub Actions Documentation](https://docs.github.com/actions)
- [Azure Web Apps Deploy Action](https://github.com/Azure/webapps-deploy)

---

## ? Deployment Checklist

### **Pre-Deployment**
- [ ] .NET 8 SDK installed
- [ ] Repository cloned and up to date
- [ ] Application builds locally without errors
- [ ] Application runs locally successfully

### **Azure Setup**
- [ ] Azure App Service created
- [ ] Runtime set to .NET 8
- [ ] Publish profile downloaded
- [ ] Managed Identity enabled
- [ ] Azure Key Vault created (optional)
- [ ] App Service has Key Vault access (optional)

### **GitHub Configuration**
- [ ] `AZURE_WEBAPP_NAME` secret added
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE` secret added
- [ ] `THIRDPARTY_API_BASEURL` secret added
- [ ] `AZURE_CREDENTIALS` secret added (optional)

### **Deployment**
- [ ] Changes committed to branch
- [ ] Pull request created (if using PR workflow)
- [ ] CI checks passed (build, test, security scan)
- [ ] Merged to Master (triggers deployment)
- [ ] GitHub Actions workflow completed successfully
- [ ] Health check passed

### **Verification**
- [ ] Application loads at Azure URL
- [ ] Blazor UI works correctly
- [ ] Swagger UI accessible
- [ ] API endpoints respond correctly
- [ ] Health endpoint returns "Healthy"
- [ ] Azure Portal shows "Running" status

---

## ?? Summary

You now have a **production-ready deployment pipeline** for CleanArchitecture.ApiTemplate:

? **Clean Architecture** - Organized, maintainable codebase  
? **Automated CI/CD** - Push to Master ? Automatic deployment  
? **Secure Configuration** - GitHub Secrets + Azure Key Vault  
? **Environment Separation** - Dev for testing, Master for production  
? **Health Monitoring** - Automated health checks  
? **Security Scanning** - Vulnerability detection on PRs  

**Deployment Time:** ~3-5 minutes from push to live  
**Reliability:** Automated tests and health checks  
**Security:** Managed Identity + Key Vault + GitHub Secrets  

---

## ?? Support & Contact

**Issues or Questions?**
- ?? **Documentation:** Start with the deployment guides above
- ?? **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues)
- ?? **Email:** softevolutionsl@gmail.com
- ?? **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)sl@gmail.com

---

**Congratulations! Your CleanArchitecture.ApiTemplate is now deployed to Azure with a professional CI/CD pipeline!** ??

---

## Contact

For questions, open an issue or contact the maintainer at softevolutionsl@gmail.com or via GitHub: https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate

**Last Updated:** 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate)

*Documentation created: November 2025*  
*For: CleanArchitecture.ApiTemplate - Deployment Guide*

