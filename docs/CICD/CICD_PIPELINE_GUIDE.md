# CI/CD Pipeline Guide - SecureCleanApiWaf

Remember Automation in Action! 

## Overview

This guide documents the complete CI/CD pipeline implementation for SecureCleanApiWaf using GitHub Actions. The pipeline automates building, testing, and deploying the application to Azure App Service, ensuring consistent and reliable deployments.

---

## Table of Contents

1. [Pipeline Architecture](#pipeline-architecture)
2. [Workflow Triggers](#workflow-triggers)
3. [GitHub Secrets Configuration](#github-secrets-configuration)
4. [Job 1: Build and Test](#job-1-build-and-test)
5. [Job 2: Deploy to Azure](#job-2-deploy-to-azure)
6. [Job 3: Security Scan](#job-3-security-scan)
7. [Environment Variables](#environment-variables)
8. [Deployment Environments](#deployment-environments)
9. [Troubleshooting](#troubleshooting)

---

## Pipeline Architecture

### **Three-Job Workflow**

```
+---------------------------------------------------------+
¦                    WORKFLOW TRIGGER                     ¦
¦   (Push to Master/Dev or Pull Request)                 ¦
+---------------------------------------------------------+
                 ¦
                 ?
+---------------------------------------------------------+
¦               JOB 1: BUILD AND TEST                     ¦
¦   • Checkout code                                       ¦
¦   • Setup .NET 8                                        ¦
¦   • Cache NuGet packages                                ¦
¦   • Restore dependencies                                ¦
¦   • Build (Release configuration)                       ¦
¦   • Run tests                                           ¦
¦   • Publish application                                 ¦
¦   • Upload artifact                                     ¦
+---------------------------------------------------------+
                 ¦
        +-----------------+
        ¦                 ¦
        ?                 ?
+--------------+   +--------------+
¦  JOB 2:      ¦   ¦  JOB 3:      ¦
¦  DEPLOY      ¦   ¦  SECURITY    ¦
¦              ¦   ¦  SCAN        ¦
¦ (Master only)¦   ¦  (PRs only)  ¦
+--------------+   +--------------+
```

### **Workflow File Location**

?? `.github/workflows/azure-deploy.yml`

---

## Workflow Triggers

### **Trigger Configuration**

The workflow triggers on three events:

```yaml
on:
  push:
    branches:
      - Master  # Production deployment
      - Dev     # Build/test only
  pull_request:
    branches:
      - Master
      - Dev
```

### **Trigger Behavior Table**

| Trigger | Build | Test | Deploy | Security Scan |
|---------|-------|------|--------|---------------|
| **Push to Master** | ? | ? | ? | ? |
| **Push to Dev** | ? | ? | ? | ? |
| **PR to Master** | ? | ? | ? | ? |
| **PR to Dev** | ? | ? | ? | ? |

**Key Points:**
- ? **Production Deployment** - Only Master branch pushes deploy to Azure
- ? **Build Validation** - All branches and PRs are built and tested
- ? **Security Scanning** - PRs run security vulnerability scans
- ? **Artifact Preservation** - Build artifacts available for debugging

---

## GitHub Secrets Configuration

### **Required Secrets**

Navigate to: **GitHub Repository ? Settings ? Secrets and variables ? Actions**

#### **1. AZURE_WEBAPP_NAME**

**Value:** Your Azure App Service name  
**Example:** `SecureCleanApiWaf-prod`

**How to find:**
```bash
# Azure CLI
az webapp list --query "[].name" -o table

# Azure Portal
App Service ? Overview ? Name
```

---

#### **2. AZURE_WEBAPP_PUBLISH_PROFILE**

**Value:** Complete XML content of publish profile

**How to get:**

**Using Azure Portal:**
1. Navigate to your App Service
2. Click **"Get publish profile"** (in Overview or Deployment Center)
3. Download `.PublishSettings` file
4. Open file in text editor
5. Copy **entire XML content**
6. Paste into GitHub secret

**Using Azure CLI:**
```bash
az webapp deployment list-publishing-profiles \
  --name SecureCleanApiWaf-prod \
  --resource-group SecureCleanApiWaf-rg \
  --xml
```

**Important:** Store the **complete XML**, including `<publishData>` tags.

---

#### **3. THIRDPARTY_API_BASEURL**

**Value:** Base URL for external API  
**Example:** `https://api.thirdparty.com`

**Purpose:** Configured as App Setting in Azure during deployment

---

#### **4. AZURE_CREDENTIALS** (Optional)

**Value:** Azure Service Principal credentials (JSON)

**Required for:** Azure Login step (setting App Settings via CLI)

**Format:**
```json
{
  "clientId": "<service-principal-app-id>",
  "clientSecret": "<service-principal-password>",
  "subscriptionId": "<azure-subscription-id>",
  "tenantId": "<azure-tenant-id>"
}
```

**How to create:**

```bash
# Create service principal
az ad sp create-for-rbac \
  --name "SecureCleanApiWaf-deploy" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
  --sdk-auth
```

**Output:** Copy entire JSON output to GitHub secret

---

### **Adding Secrets to GitHub**

1. **Navigate to Secrets**:
   - Go to your GitHub repository
   - Click **Settings**
   - Select **Secrets and variables ? Actions**

2. **Add New Secret**:
   - Click **"New repository secret"**
   - Enter **Name** (e.g., `AZURE_WEBAPP_NAME`)
   - Enter **Value**
   - Click **"Add secret"**

3. **Repeat for all required secrets**

---

## Job 1: Build and Test

### **Purpose**

Compiles the application, runs tests, and creates deployment artifacts.

### **Runs On**

- ? All branch pushes (Master, Dev)
- ? All pull requests

### **Steps Breakdown**

#### **1. Checkout Code**

```yaml
- name: Checkout code
  uses: actions/checkout@v4
```

**What it does:** Clones repository to GitHub Actions runner

---

#### **2. Setup .NET**

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'
```

**What it does:** Installs .NET 8 SDK on runner

---

#### **3. Cache NuGet Packages**

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

**What it does:** Caches NuGet packages to speed up builds  
**Performance:** Reduces build time by ~50% (3min ? 1.5min)

---

#### **4. Restore Dependencies**

```yaml
- name: Restore dependencies
  run: dotnet restore
```

**What it does:** Downloads NuGet packages specified in project file

---

#### **5. Build**

```yaml
- name: Build
  run: dotnet build --configuration Release --no-restore
```

**What it does:** Compiles application in Release mode  
**Configuration:** Optimized for production (Release build)

---

#### **6. Test**

```yaml
- name: Test
  run: dotnet test --no-build --verbosity normal --configuration Release
  continue-on-error: true
```

**What it does:** Runs unit tests  
**Note:** `continue-on-error: true` allows workflow to succeed without tests  
**TODO:** Remove flag once tests are implemented to make tests required

---

#### **7. Publish**

```yaml
- name: Publish
  run: dotnet publish -c Release -o ./publish
```

**What it does:** Creates deployment package in `./publish` folder  
**Output:** Self-contained application ready for Azure

---

#### **8. Upload Artifact**

```yaml
- name: Upload artifact for deployment job
  uses: actions/upload-artifact@v3
  with:
    name: dotnet-app
    path: ./publish
```

**What it does:** Stores build output for deployment job  
**Access:** Available in GitHub Actions UI for debugging

---

## Job 2: Deploy to Azure

### **Purpose**

Deploys application to Azure App Service (Production only).

### **Runs On**

- ? Push to Master **ONLY**
- ? Dev pushes (no deployment)
- ? Pull requests (no deployment)

### **Conditional Execution**

```yaml
if: github.event_name == 'push' && github.ref == 'refs/heads/Master'
```

**Logic:**
- `github.event_name == 'push'` ? Not a pull request
- `github.ref == 'refs/heads/Master'` ? Master branch only

---

### **Environment Configuration**

```yaml
environment:
  name: 'production'
  url: ${{ steps.deploy-to-webapp.outputs.webapp-url }}
```

**Features:**
- ? **Manual Approval** - Can require approval before deployment (GitHub settings)
- ? **Deployment History** - Track all deployments in GitHub UI
- ? **Live URL** - Direct link to deployed application

---

### **Steps Breakdown**

#### **1. Download Artifact**

```yaml
- name: Download artifact from build job
  uses: actions/download-artifact@v3
  with:
    name: dotnet-app
    path: ./publish
```

**What it does:** Retrieves build output from Job 1

---

#### **2. Deploy to Azure Web App**

```yaml
- name: Deploy to Azure Web App
  id: deploy-to-webapp
  uses: azure/webapps-deploy@v3
  with:
    app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
    publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
    package: ./publish
```

**What it does:** Deploys application to Azure App Service  
**Authentication:** Uses publish profile for secure deployment

---

#### **3. Azure Login** (Optional)

```yaml
- name: Azure Login
  uses: azure/login@v1
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}
  continue-on-error: true
```

**What it does:** Authenticates with Azure using Service Principal  
**Required for:** Setting App Settings via Azure CLI  
**Note:** Optional if service principal not configured

---

#### **4. Set Azure App Settings**

```yaml
- name: Set Azure App Settings
  uses: azure/appservice-settings@v1
  with:
    app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
    app-settings-json: |
      [
        {
          "name": "ThirdPartyApi__BaseUrl",
          "value": "${{ secrets.THIRDPARTY_API_BASEURL }}",
          "slotSetting": false
        },
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production",
          "slotSetting": false
        }
      ]
  continue-on-error: true
```

**What it does:** Configures runtime settings in Azure  
**Key Settings:**
- `ThirdPartyApi__BaseUrl` - External API URL
- `ASPNETCORE_ENVIRONMENT` - Sets to "Production"

**Effect:**
- Loads `appsettings.Production.json`
- Enables Azure Key Vault integration
- Production logging and error handling

---

#### **5. Health Check**

```yaml
- name: Health Check
  run: |
    echo "Waiting 30 seconds for app to start..."
    sleep 30
    curl -f https://${{ secrets.AZURE_WEBAPP_NAME }}.azurewebsites.net/health || echo "Health check failed"
  continue-on-error: true
```

**What it does:** Verifies deployment success  
**Endpoint:** `/health` (implemented in application)  
**Wait Time:** 30 seconds for application startup

---

## Job 3: Security Scan

### **Purpose**

Scans for security vulnerabilities using Trivy scanner.

### **Runs On**

- ? Pull requests **ONLY**
- ? Direct pushes (no scan)

### **Conditional Execution**

```yaml
if: github.event_name == 'pull_request'
```

---

### **Steps Breakdown**

#### **1. Checkout Code**

```yaml
- name: Checkout code
  uses: actions/checkout@v4
```

---

#### **2. Run Trivy Scanner**

```yaml
- name: Run Trivy vulnerability scanner
  uses: aquasecurity/trivy-action@master
  with:
    scan-type: 'fs'
    scan-ref: '.'
    format: 'sarif'
    output: 'trivy-results.sarif'
  continue-on-error: true
```

**What it does:** Scans for vulnerabilities in:
- Dependencies (NuGet packages)
- Docker images (if applicable)
- Configuration files
- Infrastructure as Code

---

#### **3. Upload Results**

```yaml
- name: Upload Trivy results to GitHub Security tab
  uses: github/codeql-action/upload-sarif@v2
  with:
    sarif_file: 'trivy-results.sarif'
  continue-on-error: true
```

**What it does:** Uploads scan results to GitHub Security tab  
**Access:** View in **Security ? Code scanning alerts**

---

## Environment Variables

### **Workflow-Level Variables**

```yaml
env:
  DOTNET_VERSION: '8.0.x'
  AZURE_WEBAPP_PACKAGE_PATH: './publish'
```

| Variable | Value | Purpose |
|----------|-------|---------|
| `DOTNET_VERSION` | `8.0.x` | .NET SDK version to install |
| `AZURE_WEBAPP_PACKAGE_PATH` | `./publish` | Deployment package location |

---

### **Azure App Settings**

Set during deployment in Azure App Service:

| Setting | Value | Source |
|---------|-------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Hardcoded in workflow |
| `ThirdPartyApi__BaseUrl` | Variable | GitHub secret |
| `KeyVault__Url` | Variable | Azure App Service configuration |

---

## Deployment Environments

### **Production Environment**

**Configuration:**
- **Branch:** Master only
- **Environment Name:** `production`
- **App Service:** As specified in `AZURE_WEBAPP_NAME`
- **Manual Approval:** Configurable in GitHub settings

**To Enable Manual Approval:**
1. Go to **Settings ? Environments**
2. Click **production**
3. Enable **Required reviewers**
4. Add reviewers
5. Save protection rules

---

### **Development Environment**

**Configuration:**
- **Branch:** Dev
- **Build:** Yes
- **Test:** Yes
- **Deploy:** No (manual deployment if needed)

---

## Troubleshooting

### **Common Issues**

#### **1. Deployment Fails with "Publish Profile Error"**

**Symptoms:** 
```
Error: Publish profile is invalid
```

**Solutions:**
- Re-download publish profile from Azure Portal
- Ensure **entire XML** is copied (including `<publishData>` tags)
- Check for extra spaces or line breaks in secret
- Verify App Service name matches secret

---

#### **2. Build Fails with "Package Not Found"**

**Symptoms:**
```
error NU1101: Unable to find package
```

**Solutions:**
- Clear NuGet cache: Add step `dotnet nuget locals all --clear`
- Check package sources in `NuGet.config`
- Verify package versions exist on NuGet.org
- Check for typos in package references

---

#### **3. Tests Fail**

**Symptoms:**
```
Test run failed
```

**Solutions:**
- Review test output in GitHub Actions logs
- Run tests locally: `dotnet test`
- Check test dependencies are restored
- Verify test project configuration

---

#### **4. Health Check Fails**

**Symptoms:**
```
curl: (22) The requested URL returned error: 404
```

**Solutions:**
- Verify `/health` endpoint exists in application
- Check App Service is running: `az webapp show`
- Increase wait time in health check step
- Review Application Insights logs

---

#### **5. Azure Login Fails**

**Symptoms:**
```
Error: Login failed with Error: Unable to authenticate
```

**Solutions:**
- Verify `AZURE_CREDENTIALS` secret format is correct JSON
- Check service principal has Contributor role
- Ensure subscription ID is correct
- Verify tenant ID matches Azure AD

---

### **Debugging Tips**

#### **View Workflow Logs**

1. Go to **Actions** tab in GitHub
2. Click on workflow run
3. Click on failed job
4. Expand failed step to view logs

#### **Download Build Artifacts**

1. Go to workflow run
2. Scroll to **Artifacts** section
3. Download `dotnet-app` artifact
4. Inspect files locally

#### **Enable Debug Logging**

Add to workflow file:

```yaml
env:
  ACTIONS_STEP_DEBUG: true
  ACTIONS_RUNNER_DEBUG: true
```

#### **Test Locally**

Simulate workflow steps locally:

```bash
# Restore
dotnet restore

# Build
dotnet build --configuration Release

# Test
dotnet test --configuration Release

# Publish
dotnet publish -c Release -o ./publish
```

---

## Workflow Behavior Summary

### **Master Branch Push**

```
1. Developer pushes to Master
2. Build job: Restore ? Build ? Test ? Publish ? Upload
3. Deploy job: Download ? Deploy ? Configure ? Health Check
4. Application runs with ASPNETCORE_ENVIRONMENT=Production
5. Deployment URL available in GitHub Actions UI
```

### **Dev Branch Push**

```
1. Developer pushes to Dev
2. Build job: Restore ? Build ? Test ? Publish ? Upload
3. Deploy job: SKIPPED (does not run)
4. Artifact available for manual inspection
```

### **Pull Request**

```
1. Developer opens PR
2. Build job: Restore ? Build ? Test ? Publish
3. Security scan job: Trivy vulnerability scan
4. Deploy job: SKIPPED (does not run)
5. Results available for review before merging
```

---

## Best Practices

### **? Do's**

- ? Use semantic versioning for releases
- ? Tag production deployments with version numbers
- ? Enable manual approval for production
- ? Monitor deployment health checks
- ? Review security scan results before merging PRs
- ? Keep secrets up-to-date
- ? Use separate environments for dev/staging/prod

### **? Don'ts**

- ? Don't commit secrets to repository
- ? Don't disable tests without review
- ? Don't skip security scans
- ? Don't deploy directly to production without testing
- ? Don't ignore failed health checks
- ? Don't use production credentials in non-production environments

---

## Reference Files

**Workflow File:**
- ?? [`.github/workflows/azure-deploy.yml`](../../.github/workflows/azure-deploy.yml) - Complete workflow definition

**Related Documentation:**
- ?? [`docs/AzureIntegration/AZURE_INTEGRATION_GUIDE.md`](../AzureIntegration/AZURE_INTEGRATION_GUIDE.md) - Azure setup and configuration
- ?? [`DEPLOYMENT_GUIDE.md`](../../DEPLOYMENT_GUIDE.md) - Complete deployment guide

**Application Code:**
- ?? [`Program.cs`](../../Program.cs) - Application entry point with Key Vault configuration
- ?? [`appsettings.json`](../../appsettings.json) - Base configuration
- ?? [`appsettings.Production.json`](../../appsettings.Production.json) - Production configuration

---

## ?? Contact

**Need Help?**

- ?? **Documentation:** Start with the deployment guides above
- ?? **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/SecureCleanApiWaf/issues)
- ?? **Email:** softevolutionsl@gmail.com
- ?? **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [SecureCleanApiWaf](https://github.com/dariemcarlosdev/SecureCleanApiWaf)
