# 🚀 CleanArchitecture.ApiTemplate - Deployment Documentation

> *"The journey of a thousand deployments begins with a single container."*  
> — Inspired by DevOps wisdom

**Complete deployment guide for CleanArchitecture.ApiTemplate across multiple platforms and environments.**

---

## 📑 Table of Contents

### **Quick Navigation**
1. [Overview](#-overview)
2. [Quick Start Guides](#-quick-start-guides)
   - [Quick Start: Azure App Service](#quick-start-azure-app-service)
   - [Quick Start: Docker Deployment](#quick-start-docker-deployment)
3. [Deployment Options](#-deployment-options)
   - [Azure App Service (PaaS)](#?-azure-app-service-paas)
   - [Docker Containers](#-docker-containers)
   - [Azure Container Apps](#?-azure-container-apps)
   - [Azure Kubernetes Service (AKS)](#?-azure-kubernetes-service-aks)
   - [Traditional Deployments](#?-traditional-deployments)
4. [Deployment Comparison Matrix](#-deployment-comparison-matrix)
5. [Prerequisites](#-prerequisites)
   - [For Azure Deployments](#for-azure-deployments)
   - [For Docker Deployments](#for-docker-deployments)
   - [For Traditional Deployments](#for-traditional-deployments)
6. [Environment Configuration](#?-environment-configuration)
   - [Required Environment Variables](#required-environment-variables)
   - [Configuration Files](#configuration-files)
   - [Configuration Priority](#configuration-priority)
7. [Security Best Practices](#-security-best-practices)
8. [Monitoring & Logging](#-monitoring--logging)
   - [Azure Application Insights](#azure-application-insights)
   - [Docker Logging](#docker-logging)
   - [Azure Monitor](#azure-monitor)
9. [Troubleshooting](#-troubleshooting)
   - [Common Issues](#common-issues)
   - [Detailed Troubleshooting](#detailed-troubleshooting)
10. [Deployment Documentation](#-deployment-documentation)
    - [Azure App Service Documentation](#azure-app-service-documentation)
    - [Docker Documentation](#docker-documentation)
11. [Additional Resources](#-additional-resources)
12. [Next Steps](#-next-steps)
13. [Support](#-support)

---

## 📑 Overview

This directory contains comprehensive deployment documentation for CleanArchitecture.ApiTemplate across different platforms and technologies. Whether you're deploying to Azure PaaS, using Docker containers, or setting up Kubernetes, you'll find complete guides and quick-start instructions here.

**What's included:**
- ✅ Complete step-by-step deployment guides
- ✅ Quick start guides for rapid deployment
- ✅ Platform comparison and recommendations
- ✅ Security best practices
- ✅ Troubleshooting guides
- ✅ CI/CD automation examples

---

## 📑 Quick Start Guides

### **Quick Start: Azure App Service**

Deploy CleanArchitecture.ApiTemplate to Azure App Service in **5 simple steps**:

#### **1. Prerequisites**
```bash
# Ensure you have:
# - Azure subscription
# - Azure CLI installed (optional)
# - GitHub account
```

#### **2. Create Azure Resources**
```bash
# Using Azure CLI (fastest)
az webapp up --name CleanArchitecture.ApiTemplate \
  --resource-group CleanArchitecture.ApiTemplate-RG \
  --runtime "DOTNETCORE:8.0" \
  --sku B1
```

**Or via Azure Portal:**
- Create Resource → Web App
- Runtime: .NET 8
- Operating System: Linux
- Pricing: Basic B1 or higher

#### **3. Enable Managed Identity**
```bash
# Via Azure CLI
az webapp identity assign \
  --name CleanArchitecture.ApiTemplate \
  --resource-group CleanArchitecture.ApiTemplate-RG
```

**Or via Azure Portal:**
- Your App Service → Identity → System assigned → On

#### **4. Configure GitHub Secrets**

Add these secrets to your GitHub repository (Settings → Secrets and variables → Actions):

| Secret Name | Value | Where to Get It |
|-------------|-------|-----------------|
| `AZURE_WEBAPP_NAME` | `CleanArchitecture.ApiTemplate` | Your App Service name |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Download from Azure Portal | App Service → Get publish profile |
| `THIRDPARTY_API_BASEURL` | Your API URL | Your external API endpoint |

#### **5. Deploy**
```bash
# Push to master branch to trigger deployment
git push origin master

# GitHub Actions automatically:
# ✅ Builds the application
# ✅ Runs tests
# ✅ Deploys to Azure
# ✅ Verifies deployment
```

#### **Verify Deployment**
```bash
# Check your app is live
curl https://CleanArchitecture.ApiTemplate.azurewebsites.net/health
# Expected: Healthy

# Or visit in browser
https://CleanArchitecture.ApiTemplate.azurewebsites.net
```

**⏱️ Total Time: ~10-15 minutes especially if Azure resources are pre-created**

📖 **For complete Azure deployment guide:** See [AzureAppService/DEPLOYMENT_GUIDE.md](./AzureAppService/DEPLOYMENT_GUIDE.md)

---

### **Quick Start: Docker Deployment**

Deploy CleanArchitecture.ApiTemplate using Docker in **4 simple steps**:

#### **1. Prerequisites**
```bash
# Ensure you have:
# - Docker Desktop installed
# - Docker Hub account (for publishing)
```

#### **2. Build Docker Image**
```bash
# Clone repository (if not already)
git clone https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate.git
cd CleanArchitecture.ApiTemplate

# Build the image
docker build -t CleanArchitecture.ApiTemplate:latest .

# Expected: Build succeeds in 2-3 minutes
```

#### **3. Run Locally**
```bash
# Using Docker Compose (easiest)
docker-compose up -d

# Or using Docker CLI
docker run -d -p 8080:8080 --name CleanArchitecture.ApiTemplate CleanArchitecture.ApiTemplate:latest
```

#### **4. Verify & Test**
```bash
# Check container is running
docker ps | grep CleanArchitecture.ApiTemplate

# Test the application
curl http://localhost:8080/health
# Expected: Healthy

# View logs
docker logs -f CleanArchitecture.ApiTemplate

# Access the app
# Browser: http://localhost:8080
```

**⏱️ Total Time: ~5-10 minutes especially if Docker is pre-installed**

#### **Optional: Push to Docker Hub**
```bash
# Login to Docker Hub
docker login

# Tag image
docker tag CleanArchitecture.ApiTemplate:latest yourdockerhubusername/CleanArchitecture.ApiTemplate:latest

# Push to Docker Hub
docker push yourdockerhubusername/CleanArchitecture.ApiTemplate:latest
```

#### **Optional: Deploy to Cloud**

**Azure Container Instances:**
```bash
az container create \
  --resource-group myResourceGroup \
  --name CleanArchitecture.ApiTemplate \
  --image yourdockerhubusername/CleanArchitecture.ApiTemplate:latest \
  --dns-name-label CleanArchitecture.ApiTemplate \
  --ports 8080
```

**Azure Container Apps:**
```bash
az containerapp create \
  --name CleanArchitecture.ApiTemplate \
  --resource-group myResourceGroup \
  --environment myEnvironment \
  --image yourdockerhubusername/CleanArchitecture.ApiTemplate:latest \
  --target-port 8080 \
  --ingress external
```

**Kubernetes:**
```bash
kubectl create deployment CleanArchitecture.ApiTemplate \
  --image=yourdockerhubusername/CleanArchitecture.ApiTemplate:latest
kubectl expose deployment CleanArchitecture.ApiTemplate \
  --type=LoadBalancer --port=80 --target-port=8080
```

📖 **For complete Docker deployment guide:** See [Docker/DOCKER_DEPLOYMENT.md](./Docker/DOCKER_DEPLOYMENT.md)

---

## ?? Deployment Options

### ☁️ **Azure App Service (PaaS)**

**Best for:** Simple web applications, managed infrastructure, quick deployments

**Key Features:**
- ✅ Fully managed platform (no server management)
- ✅ Automatic scaling (vertical and horizontal)
- ✅ Built-in CI/CD with GitHub Actions
- ✅ Free SSL certificates
- ✅ Custom domains
- ✅ Application Insights integration
- ✅ Managed Identity for secure Azure access
- ✅ Deployment slots (blue-green deployments)

**Quick Deploy:**
```bash
az webapp up --name CleanArchitecture.ApiTemplate --resource-group myResourceGroup --runtime "DOTNETCORE:8.0"
```

📖 **Documentation:** [AzureAppService/](./AzureAppService/)

**Quick access:**
- [Complete Deployment Guide](./AzureAppService/DEPLOYMENT_GUIDE.md) - Step-by-step Azure deployment
- [Azure App Service Overview](./AzureAppService/README.md) - Features, architecture, and best practices

---

### 🐳 **Docker Containers**

**Best for:** Portability, cloud-agnostic deployments, microservices, flexibility

**Key Features:**
- ✅ Run anywhere (local, cloud, on-premises)
- ✅ Consistent environments (dev = prod)
- ✅ Easy scaling and orchestration
- ✅ Version control for infrastructure
- ✅ Efficient resource utilization
- ✅ CI/CD automation with GitHub Actions
- ✅ Multi-cloud support (Azure, AWS, GCP)

**Quick Deploy:**
```bash
# Local
docker-compose up -d

# Or
docker build -t CleanArchitecture.ApiTemplate:latest .
docker run -d -p 8080:8080 CleanArchitecture.ApiTemplate:latest
```

📖 **Documentation:** [Docker/](./Docker/)

**Quick access:**
- [Complete Docker Guide](./Docker/DOCKER_DEPLOYMENT.md) - Full deployment instructions
- [Quick Reference](./Docker/DOCKER_QUICK_REFERENCE.md) - Common commands
- [Setup Summary](./Docker/DOCKER_SETUP_SUMMARY.md) - Overview of what was created

**Cloud Platforms:**
- Azure Container Instances (ACI)
- Azure Container Apps
- Azure Kubernetes Service (AKS)
- AWS ECS/Fargate
- Google Cloud Run
- Kubernetes clusters (any provider)

---

### 🚀 **Azure Container Apps**

**Best for:** Serverless containers, microservices, event-driven applications

**Key Features:**
- ✅ Auto-scaling (0 to N instances)
- ✅ KEDA event-driven scaling
- ✅ Dapr integration
- ✅ Multiple revisions/traffic splitting
- ✅ Pay-per-use pricing
- ✅ Built-in service discovery

**Quick Deploy:**
```bash
az containerapp create \
  --name CleanArchitecture.ApiTemplate \
  --resource-group myResourceGroup \
  --environment myEnvironment \
  --image yourdockerhubusername/CleanArchitecture.ApiTemplate:latest \
  --target-port 8080 \
  --ingress external
```

📖 **Prerequisites:** Docker image pushed to registry

---

### ⚙️ **Azure Kubernetes Service (AKS)**

**Best for:** Complex microservices, enterprise deployments, advanced orchestration

**Key Features:**
- ✅ Advanced orchestration
- ✅ Service mesh support
- ✅ Multi-region deployments
- ✅ Advanced networking
- ✅ Auto-healing and auto-scaling
- ✅ Rolling updates and rollbacks

**Quick Deploy:**
```bash
# Create deployment
kubectl create deployment CleanArchitecture.ApiTemplate \
  --image=yourdockerhubusername/CleanArchitecture.ApiTemplate:latest

# Expose service
kubectl expose deployment CleanArchitecture.ApiTemplate \
  --type=LoadBalancer --port=80 --target-port=8080

# Check status
kubectl get deployments
kubectl get services
kubectl get pods
```

📖 **Prerequisites:** Kubernetes manifests, Docker image in registry

---

### 🏗️ **Traditional Deployments**

#### **IIS (Windows Server)**
**Best for:** On-premises Windows environments, legacy infrastructure

**Steps:**
1. Publish: `dotnet publish -c Release`
2. Copy to IIS directory
3. Configure IIS site and app pool
4. Set up bindings and SSL

#### **Linux (Systemd Service)**
**Best for:** On-premises Linux environments, VM deployments

**Steps:**
1. Publish: `dotnet publish -c Release`
2. Copy to `/var/www/CleanArchitecture.ApiTemplate`
3. Create systemd service
4. Configure Nginx/Apache reverse proxy

---

## ?? Deployment Comparison Matrix

| Deployment Option | Complexity | Cost | Scalability | Best Use Case | Time to Deploy |
|------------------|------------|------|-------------|---------------|----------------|
| **Docker Compose** | ✅ Low | Free | Manual | Local development | 5 min |
| **Azure Container Apps** | ⚙️ Medium | $$ Pay-as-you-go | Auto (0-N) | Production apps, microservices | 10 min |
| **Azure App Service** | ✅ Low | $$ Fixed ($13-70/mo) | Auto | Simple web apps, APIs | 15 min |
| **AKS** | 🔧 High | $$$ Complex | Advanced | Enterprise, complex microservices | 30+ min |
| **Docker + VM** | ⚙️ Medium | $ Fixed | Manual | Custom infrastructure | 20 min |
| **IIS** | ⚙️ Medium | Hardware | Manual | On-premises Windows | 15 min |

### **Recommendation by Use Case:**

| Use Case | Recommended Option | Why |
|----------|-------------------|-----|
| **Getting Started** | Docker Compose | Fast, free, works everywhere |
| **Small Production App** | Azure App Service | Managed, easy, cost-effective |
| **Microservices** | Azure Container Apps | Auto-scaling, event-driven |
| **Enterprise** | AKS | Advanced features, scalability |
| **Multi-Cloud** | Docker + Kubernetes | Portable across providers |
| **On-Premises** | Docker + VM or IIS | Control over infrastructure |

---

## ⚙️ Prerequisites

### **For Azure Deployments:**

#### **Required:**
- [ ] Active Azure subscription
- [ ] GitHub account with repository access
- [ ] .NET 8 SDK installed locally (for testing)

#### **Optional (but recommended):**
- [ ] Azure CLI installed ([Download](https://docs.microsoft.com/cli/azure/install-azure-cli))
- [ ] Visual Studio 2022 or VS Code

#### **Azure Resources:**
- [ ] Azure App Service or Container resource
- [ ] Azure Key Vault (for secrets management)
- [ ] Application Insights (for monitoring)

---

### **For Docker Deployments:**

#### **Required:**
- [ ] Docker Desktop (Windows/Mac) or Docker Engine (Linux)
  - Download: https://www.docker.com/products/docker-desktop
- [ ] Docker Hub account (for publishing images)
  - Sign up: https://hub.docker.com/signup

#### **Optional:**
- [ ] Docker Compose (included with Docker Desktop)
- [ ] Azure/AWS/GCP CLI (for cloud deployments)

---

### **For Traditional Deployments:**

#### **Required:**
- [ ] .NET 8 Runtime installed
- [ ] Web server (IIS, Nginx, or Apache)
- [ ] SSL certificate for HTTPS
- [ ] Appropriate server permissions

---

## ?? Environment Configuration

### **Required Environment Variables:**

```bash
# Application Environment
ASPNETCORE_ENVIRONMENT=Production

# JWT Authentication
JwtSettings__SecretKey=your-secure-secret-key-here
JwtSettings__Issuer=CleanArchitecture.ApiTemplate
JwtSettings__Audience=CleanArchitecture.ApiTemplate.Api
JwtSettings__ExpirationMinutes=20

# External API
ThirdPartyApi__ApiKey=your-api-key-here
ThirdPartyApi__BaseUrl=https://api.example.com/
ThirdPartyApi__Timeout=30

# CORS (Production)
Cors__AllowedOrigins__0=https://yourdomain.com

# Rate Limiting
IpRateLimiting__EnableEndpointRateLimiting=true
IpRateLimiting__HttpStatusCode=429

# Azure Key Vault (Optional but recommended for production)
Azure__KeyVaultUri=https://your-vault.vault.azure.net/
```

---

### **Configuration Files:**

| File | Purpose | Environment | Committed to Git |
|------|---------|-------------|------------------|
| `appsettings.json` | Base configuration | All | ✅ Yes |
| `appsettings.Development.json` | Local development | Development | ✅ Yes |
| `appsettings.Production.json` | Production overrides | Production | ✅ Yes (no secrets!) |
| `.env` | Docker Compose secrets | Local Docker | ❌ No (.gitignore) |
| **Azure Key Vault** | Production secrets | Production | N/A (Azure only) |
| **Environment Variables** | Container/cloud config | Production | N/A |

---

### **Configuration Priority:**

Configuration sources are loaded in this order (later overrides earlier):

1. **appsettings.json** - Base configuration
2. **appsettings.{Environment}.json** - Environment-specific overrides
3. **User Secrets** - Development only (not in source control)
4. **Azure Key Vault** - Production secrets (via Managed Identity)
5. **Environment Variables** - Container/cloud configuration
6. **Command-line arguments** - Runtime overrides

**Example:**
```
appsettings.json: JwtSettings:ExpirationMinutes = 60
Environment Variable: JwtSettings__ExpirationMinutes = 20
Result: 20 (environment variable wins)
```

---

## ?? Security Best Practices

Before deploying to production:

### **Secrets Management:**
- [ ] Replace all default secrets with strong values
- [ ] Use Azure Key Vault or equivalent for production
- [ ] Never hardcode secrets in source code
- [ ] Use environment variables for container deployments
- [ ] Rotate secrets regularly (every 90 days recommended)

### **Network Security:**
- [ ] Enable HTTPS/SSL (required for production)
- [ ] Configure CORS properly (no wildcards `*` in production)
- [ ] Set up rate limiting (already configured in app)
- [ ] Use network security groups/firewalls
- [ ] Consider Azure Private Link for sensitive workloads

### **Container Security:**
- [ ] Scan Docker images for vulnerabilities (`docker scan`)
- [ ] Use specific image tags (not `latest`) in production
- [ ] Run containers as non-root user (already configured)
- [ ] Keep base images updated
- [ ] Enable Azure Container Registry scanning

### **Application Security:**
- [ ] Enable authentication/authorization on sensitive endpoints
- [ ] Review security headers (already configured)
- [ ] Implement audit logging
- [ ] Set up security alerts (Azure Security Center)
- [ ] Regular security assessments

### **Access Control:**
- [ ] Use Managed Identity (no passwords)
- [ ] Implement RBAC (Role-Based Access Control)
- [ ] Follow principle of least privilege
- [ ] Enable MFA for Azure Portal access
- [ ] Audit access logs regularly

---

## ?? Monitoring & Logging

### **Azure Application Insights**

**Automatically tracks:**
- HTTP requests and response times
- Dependency calls (APIs, databases)
- Exceptions and errors
- Custom events and metrics
- User behavior and analytics

**Setup:**
1. Create Application Insights resource in Azure
2. Add connection string to App Service configuration
3. Deploy application
4. View telemetry in Azure Portal

**Cost:** 5 GB free per month, then $2.30/GB

---

### **Docker Logging**

**View container logs:**
```bash
# View logs
docker logs CleanArchitecture.ApiTemplate

# Follow logs (real-time)
docker logs -f CleanArchitecture.ApiTemplate

# View last 100 lines
docker logs --tail 100 CleanArchitecture.ApiTemplate

# Docker Compose logs
docker-compose logs -f
```

---

### **Azure Monitor**

**Features:**
- Log Analytics workspace
- Alerts and action groups
- Dashboards and workbooks
- Query logs with KQL (Kusto Query Language)

---

## ?? Troubleshooting

### **Common Issues:**

#### **Port Conflicts:**
```bash
# Check what's using port 8080
netstat -ano | findstr :8080  # Windows
lsof -i :8080                  # Linux/Mac

# Use different port
docker run -d -p 8081:8080 CleanArchitecture.ApiTemplate:latest
```

#### **Environment Variables Not Working:**
```bash
# Verify inside container
docker exec CleanArchitecture.ApiTemplate printenv

# Check Azure App Service
az webapp config appsettings list --name <app-name> --resource-group <rg-name>

# Remember: Use __ (double underscore) for nested config
```

#### **Container Won't Start:**
```bash
# Check logs
docker logs CleanArchitecture.ApiTemplate

# Inspect container
docker inspect CleanArchitecture.ApiTemplate

# Verify health
curl http://localhost:8080/health
```

#### **Build Fails:**
```bash
# Clean cache
docker builder prune

# Build without cache
docker build --no-cache -t CleanArchitecture.ApiTemplate:latest .
```

---

### **Detailed Troubleshooting:**

- **Docker Issues:** [Docker/DOCKER_DEPLOYMENT.md#troubleshooting](./Docker/DOCKER_DEPLOYMENT.md#-troubleshooting)
- **Azure Issues:** [AzureAppService/DEPLOYMENT_GUIDE.md#troubleshooting](./AzureAppService/DEPLOYMENT_GUIDE.md#?-troubleshooting)

---

## ?? Deployment Documentation

### **Azure App Service Documentation**

📖 **[AzureAppService/](./AzureAppService/)**

| Document | Description | Size |
|----------|-------------|------|
| **[README.md](./AzureAppService/README.md)** | Azure App Service overview, features, quick start | 13 KB |
| **[DEPLOYMENT_GUIDE.md](./AzureAppService/DEPLOYMENT_GUIDE.md)** | Complete step-by-step Azure deployment guide | 32 KB |

**Topics covered:**
- Azure resource creation (App Service, Key Vault)
- Managed Identity configuration
- GitHub Secrets setup
- CI/CD with GitHub Actions
- Application Insights integration
- Scaling options
- Deployment slots
- Cost optimization
- Complete troubleshooting guide

---

### **Docker Documentation**

🐳 **[Docker/](./Docker/)**

| Document | Description | Size |
|----------|-------------|------|
| **[DOCKER_README.md](./Docker/README.md)** | Docker documentation hub and navigation | 6 KB |
| **[DOCKER_DEPLOYMENT.md](./Docker/DOCKER_DEPLOYMENT.md)** | Complete Docker deployment guide | 11 KB |
| **[DOCKER_QUICK_REFERENCE.md](./Docker/DOCKER_QUICK_REFERENCE.md)** | Quick command reference | 4 KB |
| **[DOCKER_SETUP_SUMMARY.md](./Docker/DOCKER_SETUP_SUMMARY.md)** | Setup overview and checklist | 7 KB |
| **[DOCKER_DNS_CONFIGURATION.md](./Docker/DOCKER_DNS_CONFIGURATION.md)** | DNS setup for Docker deployments | 5 KB |

**Topics covered:**
- Building Docker images (multi-stage Dockerfile)
- Running containers locally (Docker Compose)
- Publishing to Docker Hub
- Cloud deployment (Azure, AWS, GCP, Kubernetes)
- Environment variables
- CI/CD with GitHub Actions
- Security best practices
- Troubleshooting guide

---

## ?? Additional Resources

### **Official Documentation:**
- [ASP.NET Core Deployment](https://docs.microsoft.com/aspnet/core/host-and-deploy/)
- [Azure App Service](https://docs.microsoft.com/azure/app-service/)
- [Azure Container Apps](https://docs.microsoft.com/azure/container-apps/)
- [Docker Documentation](https://docs.docker.com/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)

### **CleanArchitecture.ApiTemplate Specific:**
- [Project README](../../README.md) - Application overview
- [Clean Architecture](../CleanArchitecture/) - Architecture documentation
- [Security Implementation](../AuthenticationAuthorization/) - Security guides
- [API Documentation](../../README.md#-swaggeropenapi-support) - Swagger/OpenAPI

### **Tools:**
- [Azure CLI](https://docs.microsoft.com/cli/azure/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Azure Portal](https://portal.azure.com)

---

## ⚡ Next Steps

### **1. Test Locally First**
```bash
# Clone repository
git clone https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate.git
cd CleanArchitecture.ApiTemplate

# Run locally
dotnet run

# Or with Docker
docker-compose up -d
```

### **2. Choose Deployment Platform**

**Decision Tree:**
- **New to cloud?** → Start with Azure App Service
- **Need flexibility?** → Use Docker containers
- **Building microservices?** → Try Azure Container Apps
- **Enterprise scale?** → Consider AKS
- **Multi-cloud?** → Use Docker + Kubernetes

### **3. Configure Secrets**

**Azure Key Vault (recommended for production):**
```bash
# Create Key Vault
az keyvault create --name your-vault --resource-group your-rg

# Add secrets
az keyvault secret set --vault-name your-vault --name "JwtSecretKey" --value "your-secret"
```

**Or Environment Variables (Docker):**
```bash
# Create .env file
cat > .env << EOF
JWT_SECRET_KEY=your-secret-key
THIRDPARTY_API_KEY=your-api-key
EOF
```

### **4. 🔧 Set Up CI/CD**

**GitHub Actions workflows included:**
- `.github/workflows/azure-deploy.yml` - Azure App Service deployment
- `.github/workflows/docker-publish.yml` - Docker Hub publishing

**Configure GitHub Secrets** (required):
- Repository → Settings → Secrets and variables → Actions
- Add required secrets (see quick start guides above)

### **5. 🔧 Configure Monitoring**

**Azure Application Insights:**
- Create resource in Azure Portal
- Add connection string to app configuration
- View telemetry and set up alerts

### **6. 🔧 Enable HTTPS**

**Azure App Service:** Free managed certificates (automatic)  
**Docker/Kubernetes:** Configure reverse proxy (Nginx, Traefik)

### **7. 📚 Test Thoroughly**

**Staging environment:**
- Test all features
- Load testing
- Security testing
- Verify monitoring

### **8. 🚀 Deploy to Production**

**Follow deployment guide:**
- Azure: [AzureAppService/DEPLOYMENT_GUIDE.md](./AzureAppService/DEPLOYMENT_GUIDE.md)
- Docker: [Docker/DOCKER_DEPLOYMENT.md](./Docker/DOCKER_DEPLOYMENT.md)

---

## 🔍 Support

**Need Help?**

- 📖 **Documentation:** Start with the deployment guides above
- 🐛 **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues)
- 📧 **Email:** softevolutionsl@gmail.com
- 🐙 **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

**Before asking for help:**
1. ✅ Check the troubleshooting sections REF: above
2. ✅ Review the deployment guides for common pitfalls. REF: above
3. 🔍 Search existing GitHub issues
4. 📜 Include error messages and logs

---

**Ready to deploy? Choose your path above and follow the corresponding guide! Happy deploying SecureClean Team! 🚀**

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate)


