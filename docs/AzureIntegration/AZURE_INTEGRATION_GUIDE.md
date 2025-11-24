# Azure Integration Guide - CleanArchitecture.ApiTemplate

> "Seamless Azure integration empowers CleanArchitecture.ApiTemplate to deliver secure, scalable, and resilient experiences�where cloud best practices meet modern .NET innovation."

## 📖 Overview

This guide provides step-by-step instructions for integrating CleanArchitecture.ApiTemplate with Azure services for production deployment. It covers Azure App Service, Azure Key Vault, managed identity configuration, and best practices for secure cloud deployment.

---

## 📑 Table of Contents

1. [Azure Services Overview](#azure-services-overview)
2. [Azure App Service Setup](#azure-app-service-setup)
3. [Azure Key Vault Configuration](#azure-key-vault-configuration)
4. [Managed Identity Setup](#managed-identity-setup)
5. [Configuration Management](#configuration-management)
6. [Deployment Process](#deployment-process)
7. [Monitoring & Diagnostics](#monitoring--diagnostics)
8. [Troubleshooting](#troubleshooting)

---

## ☁️ Azure Services Overview

CleanArchitecture.ApiTemplate integrates with the following Azure services:

| Service | Purpose | Usage |
|---------|---------|-------|
| **Azure App Service** | Web application hosting | Hosts Blazor Server application and REST APIs |
| **Azure Key Vault** | Secrets management | Stores API keys, connection strings, JWT secrets |
| **Azure Monitor** | Application monitoring | Application Insights for telemetry and diagnostics |
| **GitHub Actions** | CI/CD pipeline | Automated build, test, and deployment |

**Architecture Diagram:**

```
GitHub Repository
        ?
    GitHub Actions (CI/CD)
        ?
Azure App Service (Web App)
        ?
Azure Key Vault (Secrets) ? Managed Identity
        ?
Azure Monitor (Telemetry)
```

---

## 🚀 Azure App Service Setup

### **📝 Step 1: Create App Service**

#### **Using Azure Portal:**

1. **Navigate to Azure Portal**: [https://portal.azure.com](https://portal.azure.com)

2. **Create Resource**:
   - Click "Create a resource"
   - Search for "App Service"
   - Click "Create"

3. **Configure Basics**:
   ```
   Subscription: [Your Subscription]
   Resource Group: [Create new or select existing]
   Name: CleanArchitecture.ApiTemplate-prod
   Publish: Code
   Runtime Stack: .NET 8 (LTS)
   Operating System: Linux (recommended) or Windows
   Region: [Choose nearest region]
   ```

4. **App Service Plan**:
   ```
   Plan: [Create new or select existing]
   Sku: B1 (Basic) or higher for production
   ```

5. **Review + Create**: Click "Review + create" ? "Create"

#### **Using Azure CLI:**

```bash
# Login to Azure
az login

# Create resource group
az group create --name CleanArchitecture.ApiTemplate-rg --location eastus

# Create App Service Plan
az appservice plan create \
  --name CleanArchitecture.ApiTemplate-plan \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --plan CleanArchitecture.ApiTemplate-plan \
  --runtime "DOTNET|8.0"
```

---

### **📝 Step 2: Get Publish Profile**

#### **Using Azure Portal:**

1. Navigate to your App Service
2. Click "Overview" ? "Get publish profile"
3. Download the `.PublishSettings` file
4. **Important**: Store this file securely (needed for GitHub Actions)

#### **Using Azure CLI:**

```bash
az webapp deployment list-publishing-profiles \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --xml
```

---

### **📝 Step 3: Configure App Settings**

#### **Using Azure Portal:**

1. Navigate to your App Service
2. Click "Configuration" (under Settings)
3. Click "+ New application setting"

**Add these settings:**

| Name | Value | Description |
|------|-------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment name |
| `KeyVault__Url` | `https://{vault-name}.vault.azure.net/` | Key Vault URL |
| `ASPNETCORE_HTTPS_PORTS` | `443` | HTTPS port |

4. Click "Save"

#### **Using Azure CLI:**

```bash
az webapp config appsettings set \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    KeyVault__Url=https://CleanArchitecture.ApiTemplate-kv.vault.azure.net/
```

---

### **🚀 App Service Configuration Best Practices**

? **Always On**: Enable for production (prevents cold starts)  
? **HTTPS Only**: Enforce HTTPS for all requests  
? **Minimum TLS Version**: Set to 1.2 or higher  
? **ARR Affinity**: Disable for stateless apps (better scalability)  
? **Health Check**: Configure health check endpoint (`/health`)  

**Configure in Portal:**
- **Always On**: Configuration ? General settings ? Always On: On
- **HTTPS Only**: Configuration ? General settings ? HTTPS Only: On
- **TLS**: Configuration ? General settings ? Minimum TLS Version: 1.2
- **Health Check**: Monitoring ? Health check ? Path: `/health`

---

## 🔐 Azure Key Vault Configuration

### **📝 Step 1: Create Key Vault**

#### **Using Azure Portal:**

1. **Navigate to Azure Portal**
2. **Create Resource**:
   - Search for "Key Vault"
   - Click "Create"

3. **Configure Basics**:
   ```
   Subscription: [Your Subscription]
   Resource Group: CleanArchitecture.ApiTemplate-rg (same as App Service)
   Key Vault Name: CleanArchitecture.ApiTemplate-kv
   Region: [Same as App Service]
   Pricing Tier: Standard
   ```

4. **Access Configuration**:
   ```
   Permission Model: Vault access policy
   ```

5. **Review + Create**: Click "Review + create" ? "Create"

#### **Using Azure CLI:**

```bash
az keyvault create \
  --name CleanArchitecture.ApiTemplate-kv \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --location eastus
```

---

### **📝 Step 2: Add Secrets to Key Vault**

#### **Using Azure Portal:**

1. Navigate to your Key Vault
2. Click "Secrets" (under Settings)
3. Click "+ Generate/Import"

**Add these secrets:**

| Secret Name | Example Value | Description |
|-------------|---------------|-------------|
| `ThirdPartyApi--ApiKey` | `your-api-key-here` | External API key |
| `JwtSettings--SecretKey` | `your-jwt-secret-key` | JWT signing key (min 32 chars) |
| `JwtSettings--Issuer` | `https://CleanArchitecture.ApiTemplate.azurewebsites.net` | JWT issuer |
| `JwtSettings--Audience` | `https://CleanArchitecture.ApiTemplate.azurewebsites.net` | JWT audience |

4. Click "Create" for each secret

**Important Naming Convention:**
- Use `--` (double dash) instead of `:` for nested configuration
- Example: `ThirdPartyApi--ApiKey` maps to `ThirdPartyApi:ApiKey` in appsettings.json

#### **Using Azure CLI:**

```bash
# Add Third-Party API Key
az keyvault secret set \
  --vault-name CleanArchitecture.ApiTemplate-kv \
  --name "ThirdPartyApi--ApiKey" \
  --value "your-api-key-here"

# Add JWT Secret Key
az keyvault secret set \
  --vault-name CleanArchitecture.ApiTemplate-kv \
  --name "JwtSettings--SecretKey" \
  --value "your-jwt-secret-key-min-32-chars"
```

---

### **📝 Step 3: Assign Access to App Service**

#### **Using Azure Portal:**

1. Navigate to your Key Vault
2. Click "Access policies" (under Settings)
3. Click "+ Add Access Policy"
4. **Configure**:
   ```
   Secret permissions: Get, List
   Select principal: CleanArchitecture.ApiTemplate-prod (your App Service)
   ```
5. Click "Add" ? "Save"

#### **Using Azure CLI:**

```bash
# Get App Service Principal ID
APP_ID=$(az webapp identity show \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --query principalId \
  --output tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name CleanArchitecture.ApiTemplate-kv \
  --object-id $APP_ID \
  --secret-permissions get list
```

---

## 🔑 Managed Identity Setup

### **🔑 What is Managed Identity?**

Managed Identity allows Azure services to authenticate to other Azure services without storing credentials in code. It's the **recommended** way to access Azure Key Vault.

**✅ Benefits:**
- ✅ No credentials in code or configuration files
- ✅ Automatic credential rotation
- ✅ Simplified access management
- ✅ Enhanced security

---

### **⚙️ Enable System-Assigned Managed Identity**

#### **Using Azure Portal:**

1. Navigate to your App Service
2. Click "Identity" (under Settings)
3. **System assigned** tab:
   ```
   Status: On
   ```
4. Click "Save"
5. **Copy Object (principal) ID** (needed for Key Vault access)

#### **Using Azure CLI:**

```bash
az webapp identity assign \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg
```

---

### **💻 Application Code Integration**

CleanArchitecture.ApiTemplate is already configured to use Managed Identity. See `Program.cs`:

```csharp
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

**How it works:**
1. App Service has system-assigned managed identity enabled
2. `DefaultAzureCredential` automatically uses the managed identity
3. App Service authenticates to Key Vault without any credentials
4. Secrets are loaded from Key Vault and available via `IConfiguration`

---

## ⚙️ Configuration Management

### **🌍 Environment-Based Configuration**

| Environment | Secret Source | Configuration File |
|-------------|---------------|-------------------|
| **Development** | `appsettings.Development.json` | Local file (never commit production secrets) |
| **Production** | Azure Key Vault | Managed Identity authentication |
| **CI/CD** | GitHub Secrets | GitHub Actions environment variables |

---

### **📊 Configuration Priority Order**

Later sources override earlier ones:

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific)
3. **Azure Key Vault** (production secrets)
4. Environment Variables (container/cloud overrides)
5. Command-line arguments (runtime overrides)

---

### **📋 Example Configuration**

**appsettings.json (checked into source control):**
```json
{
  "ThirdPartyApi": {
    "BaseUrl": "https://api.thirdparty.com",
    "ApiKey": "placeholder" // Overridden by Key Vault
  },
  "JwtSettings": {
    "Issuer": "https://localhost:7178",
    "Audience": "https://localhost:7178",
    "ExpirationMinutes": 60,
    "SecretKey": "placeholder" // Overridden by Key Vault
  }
}
```

**Azure Key Vault (production):**
- `ThirdPartyApi--ApiKey`: `actual-production-api-key`
- `JwtSettings--SecretKey`: `actual-production-jwt-secret`

**Result in Production:**
- All settings from `appsettings.json` are loaded
- Secrets from Key Vault override placeholder values
- App uses production secrets without them being in source control

---

## 📦 Deployment Process

### **📦 Manual Deployment (ZIP Deploy)**

```bash
# Publish app locally
dotnet publish -c Release -o ./publish

# Create ZIP file
cd publish
zip -r ../app.zip .
cd ..

# Deploy to App Service
az webapp deploy \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --name CleanArchitecture.ApiTemplate-prod \
  --src-path app.zip \
  --type zip
```

---

### **🔄 GitHub Actions CI/CD**

**See complete guide:** [`docs/CICD/`](../CICD/)

**Quick Setup:**

1. **Add GitHub Secrets**:
   - `AZURE_WEBAPP_NAME`: `CleanArchitecture.ApiTemplate-prod`
   - `AZURE_WEBAPP_PUBLISH_PROFILE`: (contents of publish profile)

2. **GitHub Actions Workflow** (`.github/workflows/azure-deploy.yml`):
```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

---

## 📊 Monitoring & Diagnostics

### **📊 Application Insights (Recommended)**

#### **Create Application Insights:**

```bash
az monitor app-insights component create \
  --app CleanArchitecture.ApiTemplate-insights \
  --location eastus \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --application-type web
```

#### **Link to App Service:**

1. Navigate to App Service
2. Click "Application Insights" (under Settings)
3. Click "Turn on Application Insights"
4. Select existing resource or create new
5. Click "Apply"

---

### **📝 App Service Logs**

#### **Enable Logging:**

```bash
az webapp log config \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --application-logging filesystem \
  --level information
```

#### **View Logs:**

```bash
# Stream logs in real-time
az webapp log tail \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg
```

---

### **💚 Health Check Endpoint**

CleanArchitecture.ApiTemplate includes a health check endpoint: `/health`

**Test:**
```bash
curl https://CleanArchitecture.ApiTemplate-prod.azurewebsites.net/health
```

**Expected Response:**
```json
{
  "status": "Healthy"
}
```

---

## 🔧 Troubleshooting

### **⚠️ Common Issues**

#### **1. App Won't Start**

**Symptoms:** HTTP 500 errors, "Application Error" page

**Solutions:**
- Check App Service logs: `az webapp log tail`
- Verify .NET 8 runtime is installed
- Check `ASPNETCORE_ENVIRONMENT` is set to `Production`
- Verify all required secrets are in Key Vault

---

#### **2. Key Vault Access Denied**

**Symptoms:** `Azure.RequestFailedException: Access denied`

**Solutions:**
- Verify Managed Identity is enabled on App Service
- Check Key Vault access policy grants App Service Get/List permissions
- Verify `KeyVault__Url` is correct in App Settings
- Check App Service can reach Key Vault (no firewall blocking)

---

#### **3. Configuration Values Not Loading**

**Symptoms:** Application uses placeholder values instead of Key Vault secrets

**Solutions:**
- Check secret naming uses `--` instead of `:`
- Verify `KeyVault__Url` environment variable is set
- Ensure secrets exist in Key Vault
- Check logs for Key Vault authentication errors

---

#### **4. Deployment Fails**

**Symptoms:** GitHub Actions deployment fails

**Solutions:**
- Verify `AZURE_WEBAPP_PUBLISH_PROFILE` secret is current
- Check App Service is running
- Verify .NET 8 SDK is configured in workflow
- Review GitHub Actions logs for specific errors

---

### **🔍 Diagnostic Commands**

```bash
# Check App Service status
az webapp show \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg \
  --query "state"

# View App Service configuration
az webapp config appsettings list \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg

# Test Key Vault connectivity
az keyvault secret show \
  --vault-name CleanArchitecture.ApiTemplate-kv \
  --name "ThirdPartyApi--ApiKey"

# Check Managed Identity
az webapp identity show \
  --name CleanArchitecture.ApiTemplate-prod \
  --resource-group CleanArchitecture.ApiTemplate-rg
```

---

## 🔒 Security Best Practices

### **🔐 Key Vault**
- ✅ Use Managed Identity (never store credentials)
- ✅ Grant minimum required permissions (Get, List only)
- ✅ Enable soft delete and purge protection
- ✅ Use separate Key Vaults per environment (dev, staging, prod)
- ✅ Regularly rotate secrets

### **🚀 App Service**
- ✅ Enforce HTTPS only
- ✅ Use minimum TLS 1.2
- ✅ Enable diagnostics logging
- ✅ Configure custom domain with SSL certificate
- ✅ Restrict access with IP restrictions (if applicable)

### **⚙️ Configuration**
- ❌ **Never commit secrets to source control**
- ✅ Use environment variables for environment-specific settings
- ✅ Use Key Vault for sensitive data
- ✅ Validate all configuration on startup

---

## 📚 Reference Files

**Application Code:**
- 📄 [`Program.cs`](../../Program.cs) - Azure Key Vault integration
- ⚙️ [`appsettings.json`](../../appsettings.json) - Base configuration
- ⚙️ [`appsettings.Development.json`](../../appsettings.Development.json) - Development overrides

**Documentation:**
- 📖 [`DEPLOYMENT_GUIDE.md`](../../DEPLOYMENT_GUIDE.md) - Complete deployment guide
- 📖 [`docs/CICD/`](../CICD/) - CI/CD pipeline documentation

---


## 🆘 Contact & Support

### **Project Information**
- **Project Name:** CleanArchitecture.ApiTemplate - Clean Architecture Demo with Azure Integration
- **Version:** 1.0.0 (Azure Integration Complete)
- **Framework:** .NET 8
- **Cloud Platform:** Microsoft Azure
- **Key Services:** App Service, Key Vault, Application Insights, Managed Identity
- **Repository:** [https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate)

### **Author & Maintainer**
- **Name:** Dariem Carlos
- **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)
- **Email:** softevolutionsl@gmail.com
- **Branch:** Dev
- **Location:** Professional Tech Challenge Submission

### **Getting Help**

#### ☁️ **Azure Configuration Issues**
If you encounter issues with Azure setup:
1. Review the [Troubleshooting](#troubleshooting) section above for common issues
2. Check [Azure Service Health](https://status.azure.com/) for any service outages
3. Verify all prerequisites are met (Azure subscription, resource group, permissions)
4. Check [existing issues](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues?q=label%3Aazure)
5. Create a new issue with:
   - Azure service affected (App Service, Key Vault, etc.)
   - Error message from Azure Portal or CLI
   - Steps to reproduce
   - Your Azure region and subscription tier

#### 🔐 **Key Vault Access Problems**
For Key Vault authentication and access issues:
1. Verify Managed Identity is enabled on App Service
2. Check Key Vault access policies grant proper permissions (Get, List)
3. Confirm `KeyVault__Url` is correctly set in App Settings
4. Review Key Vault firewall and network settings
5. Check Application Insights logs for authentication errors
6. Consult [Azure Key Vault documentation](https://learn.microsoft.com/en-us/azure/key-vault/)

#### 🚀 **Deployment Issues**
For App Service deployment problems:
1. Verify publish profile is current and complete
2. Check App Service runtime stack matches project (.NET 8)
3. Review deployment logs in Azure Portal (Deployment Center)
4. Ensure all required secrets are in Key Vault
5. Check App Service configuration settings
6. Review [`docs/CICD/CICD_PIPELINE_GUIDE.md`](../CICD/CICD_PIPELINE_GUIDE.md) for CI/CD troubleshooting

#### 📖 **Documentation Questions**
To improve this Azure Integration documentation:
1. Open a [discussion](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/discussions) with tag `azure`
2. Submit a pull request with corrections or enhancements
3. Include screenshots for complex Azure Portal steps
4. Update related deployment documentation

#### 🔒 **Security Concerns**
For Azure security-related issues:
1. **DO NOT** post secrets, connection strings, or credentials in public issues
2. Use GitHub's private vulnerability reporting
3. Email directly: softevolutionsl@gmail.com with subject "Security - CleanArchitecture.ApiTemplate Azure"
4. Review [Azure Security Best Practices](https://learn.microsoft.com/en-us/azure/security/fundamentals/best-practices-and-patterns)
5. Check Key Vault audit logs for suspicious activity

### **Support Channels**

#### 📧 **Direct Contact**
For private inquiries or urgent Azure issues:
- **Email:** softevolutionsl@gmail.com
- **Subject Format:** `[CleanArchitecture.ApiTemplate Azure] Your Issue`
- **Response Time:** 24-48 hours (typically)
- **Include:** Azure service name, error messages, subscription region

#### 💬 **Community Discussions**
For general Azure questions and best practices:
- Use [GitHub Discussions](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/discussions)
- Tag with: `azure`, `app-service`, `key-vault`, `managed-identity`
- Search existing discussions before posting
- Share your Azure configuration experiences

#### 🐙 **GitHub Issues**
For bug reports and feature requests:
- **Bug Reports:** Use template, include Azure Portal screenshots
- **Feature Requests:** Describe Azure service integration needs
- **Labels:** `azure`, `deployment`, `key-vault`, `app-service`, `security`

### **Useful Links**

#### 📚 **Related Documentation**
- 📖 [CI/CD Pipeline Guide](../CICD/CICD_PIPELINE_GUIDE.md) - GitHub Actions automation
- 📖 [Deployment Guide](../../DEPLOYMENT_GUIDE.md) - Manual deployment instructions
- 📖 [API Documentation](../API/API_DESIGN_GUIDE.md) - REST API endpoints
- 📖 [Testing Guide](../Testing/TEST_INDEX.md) - Testing strategies

#### ☁️ **Azure Resources**
- [Azure App Service Documentation](https://learn.microsoft.com/en-us/azure/app-service/)
- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Managed Identity Documentation](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [Application Insights Documentation](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Azure CLI Reference](https://learn.microsoft.com/en-us/cli/azure/)

#### 🔗 **External Tools**
- [Azure Portal](https://portal.azure.com)
- [Azure CLI Download](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)
- [Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/)

### **Contributing to Azure Integration**

#### 🤝 **How to Contribute**
Contributions to improve Azure integration are welcome!

1. **Fork the repository**
2. **Create a feature branch** from `Dev`
3. **Make your Azure-related changes**:
   - Update `Program.cs` for new Azure services
   - Add configuration in `appsettings.json`
   - Update Key Vault secret names if changed
4. **Test your changes** in your own Azure subscription
5. **Submit a pull request** with:
   - Clear description of Azure integration changes
   - Configuration requirements
   - Azure CLI commands for setup
   - Screenshots of Azure Portal configuration

#### ✅ **Contribution Guidelines**
- Follow Azure naming conventions (lowercase, hyphens)
- Use Managed Identity over connection strings
- Document all new Azure resources required
- Update cost estimates if adding expensive services
- Test in both development and production-like environments
- Maintain backward compatibility with existing Azure setup

### **Azure Services Status**

#### 🚦 **Current Integration Status**
| Azure Service | Status | Purpose |
|---------------|--------|---------|
| **App Service** | ✅ Implemented | Web application hosting |
| **Key Vault** | ✅ Implemented | Secrets management |
| **Managed Identity** | ✅ Implemented | Passwordless authentication |
| **Application Insights** | ✅ Implemented | Monitoring and diagnostics |
| **Azure SQL Database** | 🔧 Optional | Data persistence |
| **Azure Storage** | 🔧 Optional | File storage |
| **Azure Service Bus** | 🔧 Optional | Messaging |

#### 💰 **Estimated Azure Costs** (USD/month)
Based on basic tier and moderate usage:
- **App Service (B1):** ~$13.14/month
- **Key Vault:** ~$0.03/month (per 10k operations)
- **Application Insights:** ~$2.30/month (1GB free tier)
- **Total Estimated:** ~$15-20/month

**Note:** Costs vary by region, usage, and tier. Use [Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/) for accurate estimates.

### **Azure Best Practices Checklist**

#### ✅ **Security**
- ✅ Use Managed Identity (no credentials in code)
- ✅ Store all secrets in Key Vault
- ✅ Enable HTTPS only on App Service
- ✅ Use minimum TLS 1.2
- ✅ Enable Key Vault soft delete
- ✅ Configure Key Vault firewall (if applicable)
- ✅ Regularly rotate secrets

#### ✅ **Reliability**
- ✅ Enable App Service Always On
- ✅ Configure health check endpoint
- ✅ Set up Application Insights alerts
- ✅ Use deployment slots for staging
- ✅ Enable automatic backups (if critical)

#### ✅ **Cost Optimization**
- ✅ Use appropriate pricing tiers (B1 for basic production)
- ✅ Monitor resource usage with Azure Monitor
- ✅ Set up budget alerts
- ✅ Use Azure Advisor recommendations
- ✅ Clean up unused resources

#### ✅ **Monitoring**
- ✅ Enable Application Insights
- ✅ Configure diagnostic logs
- ✅ Set up availability tests
- ✅ Create custom dashboards
- ✅ Configure alert rules for failures

### **Quick Reference Commands**

#### 🔍 **Azure CLI Essentials**
```bash
# Login to Azure
az login

# Set default subscription
az account set --subscription "Your Subscription Name"

# List all App Services
az webapp list --output table

# View App Service logs
az webapp log tail --name CleanArchitecture.ApiTemplate-prod --resource-group CleanArchitecture.ApiTemplate-rg

# List Key Vault secrets
az keyvault secret list --vault-name CleanArchitecture.ApiTemplate-kv --output table

# Check Managed Identity status
az webapp identity show --name CleanArchitecture.ApiTemplate-prod --resource-group CleanArchitecture.ApiTemplate-rg

# View App Service configuration
az webapp config appsettings list --name CleanArchitecture.ApiTemplate-prod --resource-group CleanArchitecture.ApiTemplate-rg
```

### **Version History**

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0.0 | Nov 2025 | Initial Azure integration implementation | Dariem Carlos |
| 1.1.0 | Nov 2025 | Added Managed Identity support | Dariem Carlos |
| 1.2.0 | Nov 2025 | Enhanced Key Vault integration | Dariem Carlos |
| 1.3.0 | Nov 2025 | Added Application Insights monitoring | Dariem Carlos |

---

**Last Updated:** November 2025  
**Document Status:** ✅ Complete and Production-Ready  
**Review Status:** Approved for Tech Challenge Submission  
**Maintainer:** Dariemcarlos  
**Azure Integration Status:** 🟢 Active and Verified

---

*This Azure Integration guide is maintained as part of the CleanArchitecture.ApiTemplate project.*  
*For the latest updates, visit the [GitHub repository](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate).*  
*For Azure-specific issues, consult [Microsoft Azure Support](https://azure.microsoft.com/en-us/support/).*
