# ?? SecureCleanApiWaf - Docker Deployment Guide

**Status:** ? Current & Maintained  
**Repository:** https://github.com/dariemcarlosdev/SecureCleanApiWaf (Branch: Dev)  
**Application:** Blazor Web Application (.NET 8)  
**Last Updated:** November 2025  

This comprehensive guide provides step-by-step instructions for building, running, and publishing the SecureCleanApiWaf Blazor application using Docker.

---

## ?? Table of Contents

1. [Overview](#-overview)
2. [Prerequisites](#-prerequisites)
3. [Quick Start (5 Minutes)](#-quick-start-5-minutes)
4. [Building the Docker Image](#-building-the-docker-image)
5. [Running the Container](#-running-the-container)
6. [Docker Compose](#-docker-compose)
7. [Publishing to Docker Hub](#-publishing-to-docker-hub)
8. [Environment Variables](#-environment-variables)
9. [Production Deployment](#-production-deployment)
10. [Security Best Practices](#-security-best-practices)
11. [Docker Commands Cheat Sheet](#-docker-commands-cheat-sheet)
12. [Troubleshooting](#-troubleshooting)
13. [Support](#-support)

---

## ?? Overview

SecureCleanApiWaf is a **Blazor Web Application** built with **.NET 8** that has been fully containerized with production-ready Docker configuration.

**Key Features:**
- ? Multi-stage optimized Dockerfile
- ? .NET 8 ASP.NET Core runtime (~215 MB)
- ? Non-root user security (appuser, UID 1000)
- ? Docker Compose for local development
- ? Environment variable configuration
- ? Pre-build approach (optimized for corporate environments)
- ? Ready for Azure, AWS, GCP, Kubernetes deployment

---

## ?? Prerequisites

### Required Software

Ensure you have the following installed:

**Docker Desktop or Docker Engine:**
- **Windows/Mac:** [Download Docker Desktop](https://www.docker.com/products/docker-desktop)
- **Linux:** Install Docker Engine and Docker Compose
- **Verify Installation:**
  ```bash
  docker --version
  docker-compose --version
  ```

**Docker Hub Account (for publishing):**
- Sign up: https://hub.docker.com/signup
- Keep your username and password handy

**(.NET 8 SDK - for local builds):**
- [Download .NET 8 SDK](https://dotnet.microsoft.com/download)
- **Verify:**
  ```bash
  dotnet --version
  # Expected: 8.0.x
  ```

### Verify Prerequisites

```bash
# Check Docker
docker --version
# Expected: Docker version 20.10 or higher

# Check Docker Compose
docker-compose --version
# Expected: Docker Compose version 1.29 or higher

# Check .NET SDK
dotnet --version
# Expected: 8.0.x
```

---

## ?? Quick Start (5 Minutes)

### Step 1: Prepare the Application

```bash
# Navigate to project directory
cd "C:\DATA\MYSTUFFS\PROFESSIONAL STUFF\TECH CHALLENGE\SecureCleanApiWaf"

# Restore packages
dotnet restore

# Build in Release mode
dotnet build -c Release

# Publish for containerization
dotnet publish -c Release -o ./publish

# Verify publish folder
ls ./publish
# Should show: SecureCleanApiWaf.dll, appsettings.json, wwwroot/, etc.
```

### Step 2: Build Docker Image

```bash
# Build the image
docker build -t SecureCleanApiWaf:latest .

# Verify the build
docker images | grep SecureCleanApiWaf
# Should show: SecureCleanApiWaf  latest  <IMAGE_ID>  <SIZE>
```

### Step 3: Run with Docker Compose

```bash
# Start the application stack
docker-compose up -d

# Verify container is running
docker-compose ps
# Should show: SecureCleanApiWaf  Yes  Up

# View logs (optional)
docker-compose logs -f SecureCleanApiWaf
```

### Step 4: Test the Application

```bash
# Access in browser
# Open: http://localhost:8080

# Or test with curl
curl http://localhost:8080

# View logs
docker-compose logs SecureCleanApiWaf
```

### Step 5: Stop When Done

```bash
# Stop containers
docker-compose stop

# Or remove containers
docker-compose down

# Remove images (optional)
docker rmi SecureCleanApiWaf:latest
```

**?? Total Time: 5-10 minutes (first time may take longer)**

---

## ??? Building the Docker Image

### Understand the Build Process

The SecureCleanApiWaf Dockerfile uses a **pre-built approach**, which means:

1. **Application is built locally** on your development machine
2. **Only the runtime** is included in the Docker image
3. **Image size is minimal** (~215 MB instead of 700+ MB)
4. **Works with corporate proxies** - avoids DNS issues during build

### Build Commands

**Basic Build:**
```bash
docker build -t SecureCleanApiWaf:latest .
```

**Build with Version Tag:**
```bash
docker build -t SecureCleanApiWaf:v1.0.0 .
```

**Build with Docker Hub Username (for publishing later):**
```bash
docker build -t yourdockerhubusername/SecureCleanApiWaf:latest .
```

**Build with Custom Dockerfile:**
```bash
# If you have multiple Dockerfiles
docker build -f Dockerfile.production -t SecureCleanApiWaf:latest .
```

**Build Without Cache (clean rebuild):**
```bash
docker build --no-cache -t SecureCleanApiWaf:latest .
```

### Verify the Build

```bash
# List all local images
docker images

# Get specific image info
docker images SecureCleanApiWaf

# View image layers and size
docker history SecureCleanApiWaf:latest

# Inspect detailed image information
docker inspect SecureCleanApiWaf:latest

# Check image labels and configuration
docker inspect --format='{{json .Config}}' SecureCleanApiWaf:latest | jq
```

### Build Troubleshooting

**If build fails:**
```bash
# Check if publish folder exists
ls -la ./publish

# Check if SecureCleanApiWaf.dll exists
ls ./publish/*.dll

# Rebuild with verbose output
docker build -t SecureCleanApiWaf:latest . --progress=plain
```

---

## ?? Running the Container

### Basic Run

```bash
docker run -d \
  -p 8080:8080 \
  --name SecureCleanApiWaf \
  SecureCleanApiWaf:latest
```

**What this does:**
- `-d` = Detached mode (runs in background)
- `-p 8080:8080` = Map port 8080 from container to host
- `--name SecureCleanApiWaf` = Give container a friendly name
- `SecureCleanApiWaf:latest` = Image to run

### Run with Custom Port

```bash
docker run -d \
  -p 8081:8080 \
  --name SecureCleanApiWaf \
  SecureCleanApiWaf:latest

# Access at: http://localhost:8081
```

### Run with Environment Variables

```bash
docker run -d \
  -p 8080:8080 \
  --name SecureCleanApiWaf \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e ThirdPartyApi__BaseUrl=https://api.example.com/ \
  SecureCleanApiWaf:latest
```

### Run with Resource Limits

```bash
docker run -d \
  -p 8080:8080 \
  --name SecureCleanApiWaf \
  --memory=512m \
  --cpus=1 \
  SecureCleanApiWaf:latest
```

### Run with Volume Mounts

```bash
docker run -d \
  -p 8080:8080 \
  --name SecureCleanApiWaf \
  -v $(pwd)/logs:/app/logs \
  -v $(pwd)/config:/app/config \
  SecureCleanApiWaf:latest
```

### Run Interactive (for debugging)

```bash
# Run with interactive terminal
docker run -it \
  -p 8080:8080 \
  --name SecureCleanApiWaf \
  SecureCleanApiWaf:latest

# Press Ctrl+C to stop
```

---

## ?? Docker Compose

### Why Use Docker Compose?

Docker Compose simplifies running multi-container applications:
- ? One command to start everything
- ? Easy environment configuration
- ? Service networking built-in
- ? Perfect for local development

### Current docker-compose.yml Configuration

**File Location:** `../../docker-compose.yml` (root directory)

**Current Settings:**
```yaml
services:
  SecureCleanApiWaf:
    build:
      context: .
      dockerfile: Dockerfile
      network: host
    container_name: SecureCleanApiWaf
    image: SecureCleanApiWaf:latest
    ports:
      - "8080:8080"
    dns:
      - 8.8.8.8
      - 8.8.4.4
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ThirdPartyApi__BaseUrl=https://jsonplaceholder.typicode.com/
    restart: unless-stopped
```

### Docker Compose Commands

**Start the Application:**
```bash
# Start in detached mode (background)
docker-compose up -d

# Start and watch logs
docker-compose up

# Build and start
docker-compose up -d --build
```

**View Status:**
```bash
# List running services
docker-compose ps

# View logs
docker-compose logs

# Follow logs (real-time)
docker-compose logs -f SecureCleanApiWaf

# View last 100 lines
docker-compose logs --tail 100 SecureCleanApiWaf
```

**Stop/Remove:**
```bash
# Stop containers (keeps them)
docker-compose stop

# Start stopped containers
docker-compose start

# Remove containers
docker-compose down

# Remove containers and volumes
docker-compose down -v

# Remove everything
docker-compose down -v --rmi all
```

**Execute Commands:**
```bash
# Execute command in running container
docker-compose exec SecureCleanApiWaf ls -la /app

# Open shell inside container
docker-compose exec SecureCleanApiWaf bash
```

### Create .env File for Secrets

Create a `.env` file in the same directory as `docker-compose.yml`:

```env
# .env (add to .gitignore!)
ASPNETCORE_ENVIRONMENT=Production
JWT_SECRET_KEY=YourSuperSecureKeyHere123456789!
THIRDPARTY_API_KEY=your-actual-api-key
THIRDPARTY_API_URL=https://api.example.com/
AZURE_KEYVAULT_URI=https://your-vault.vault.azure.net/
```

Then reference in docker-compose.yml:
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
  - JwtSettings__SecretKey=${JWT_SECRET_KEY}
```

---

## ?? Publishing to Docker Hub

### Step 1: Create Docker Hub Repository

1. Go to https://hub.docker.com
2. Click "Create Repository"
3. Name: `SecureCleanApiWaf`
4. Visibility: Public or Private
5. Click "Create"

### Step 2: Login to Docker Hub

```bash
docker login

# Enter your Docker Hub username and password
# or use access token for security

# Verify login succeeded
docker ps  # Should work without errors
```

### Step 3: Tag Your Image

```bash
# Single tag (latest)
docker tag SecureCleanApiWaf:latest yourdockerhubusername/SecureCleanApiWaf:latest

# Version tag
docker tag SecureCleanApiWaf:latest yourdockerhubusername/SecureCleanApiWaf:v1.0.0

# Tag all versions
docker tag SecureCleanApiWaf:latest yourdockerhubusername/SecureCleanApiWaf:latest
docker tag SecureCleanApiWaf:latest yourdockerhubusername/SecureCleanApiWaf:v1.0.0
docker tag SecureCleanApiWaf:latest yourdockerhubusername/SecureCleanApiWaf:dev
```

### Step 4: Push to Docker Hub

```bash
# Push specific tag
docker push yourdockerhubusername/SecureCleanApiWaf:latest

# Push version tag
docker push yourdockerhubusername/SecureCleanApiWaf:v1.0.0

# Push all tags at once
docker push yourdockerhubusername/SecureCleanApiWaf --all-tags
```

### Step 5: Verify on Docker Hub

Visit: `https://hub.docker.com/r/yourdockerhubusername/SecureCleanApiWaf`

You should see:
- ? Latest tag available
- ? Version tags listed
- ? Image size shown
- ? Pull instructions displayed

### Step 6: Pull and Run from Docker Hub

```bash
# Pull the image
docker pull yourdockerhubusername/SecureCleanApiWaf:latest

# Run the pulled image
docker run -d -p 8080:8080 yourdockerhubusername/SecureCleanApiWaf:latest

# Verify it works
curl http://localhost:8080
```

### Using Automation Script (Optional)

**For Windows (PowerShell):**
```powershell
# Make sure you're in the Docker directory
cd docs\Deployment\Docker

# Run the script
.\docker-build-push.ps1 v1.0.0
```

**For Linux/Mac (Bash):**
```bash
# Navigate to Docker directory
cd docs/Deployment/Docker

# Make executable (first time only)
chmod +x docker-build-push.sh

# Run the script
./docker-build-push.sh v1.0.0
```

---

## ?? Environment Variables

### Required Variables

| Variable | Purpose | Example | Where Set |
|----------|---------|---------|-----------|
| `ASPNETCORE_ENVIRONMENT` | App environment | `Production` | docker-compose.yml or .env |
| `ASPNETCORE_URLS` | Listening URL | `http://+:8080` | Dockerfile (ENV) |
| `ThirdPartyApi__BaseUrl` | API endpoint | `https://jsonplaceholder.typicode.com/` | docker-compose.yml or .env |

### Optional Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `JwtSettings__SecretKey` | JWT signing key | None (should be set) |
| `JwtSettings__Issuer` | Token issuer | `SecureCleanApiWaf` |
| `JwtSettings__Audience` | Token audience | `SecureCleanApiWaf.Api` |
| `JwtSettings__ExpirationMinutes` | Token lifetime | `20` |

### Setting Variables in Docker Compose

**Method 1: Inline in docker-compose.yml**
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ThirdPartyApi__BaseUrl=https://api.example.com/
```

**Method 2: Using .env file**
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
```

Create `.env` file:
```env
ASPNETCORE_ENVIRONMENT=Production
ThirdPartyApi__BaseUrl=https://api.example.com/
```

**Method 3: Using --env-file**
```bash
docker run --env-file .env -p 8080:8080 SecureCleanApiWaf:latest
```

### Setting Variables in Docker Run

```bash
docker run -d \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ThirdPartyApi__BaseUrl=https://api.example.com/ \
  SecureCleanApiWaf:latest
```

### Configuration Priority

Variables are loaded in this order (later overrides earlier):

1. **appsettings.json** - Base configuration
2. **appsettings.{Environment}.json** - Environment-specific overrides
3. **Environment Variables** - Container configuration
4. **.env file** - Docker Compose variables

---

## ?? Production Deployment

### Azure Container Instances (ACI)

**Deploy via Azure CLI:**
```bash
az container create \
  --resource-group myResourceGroup \
  --name SecureCleanApiWaf \
  --image yourdockerhubusername/SecureCleanApiWaf:latest \
  --dns-name-label SecureCleanApiWaf \
  --ports 8080 \
  --cpu 1 \
  --memory 1.5 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    ThirdPartyApi__BaseUrl=https://api.example.com/
```

**Verify Deployment:**
```bash
az container show \
  --resource-group myResourceGroup \
  --name SecureCleanApiWaf \
  --query ipAddress.fqdn

# Access at: http://<FQDN>:8080
```

### Azure Container Apps

**Deploy via Azure CLI:**
```bash
az containerapp create \
  --name SecureCleanApiWaf \
  --resource-group myResourceGroup \
  --environment myEnvironment \
  --image yourdockerhubusername/SecureCleanApiWaf:latest \
  --target-port 8080 \
  --ingress external \
  --cpu 0.5 \
  --memory 1.0Gi \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ThirdPartyApi__BaseUrl=https://api.example.com/
```

### Azure Kubernetes Service (AKS)

**Create Deployment:**
```bash
# Create deployment
kubectl create deployment SecureCleanApiWaf \
  --image=yourdockerhubusername/SecureCleanApiWaf:latest

# Check deployment status
kubectl get deployments
kubectl get pods

# Expose the service
kubectl expose deployment SecureCleanApiWaf \
  --type=LoadBalancer \
  --port=80 \
  --target-port=8080

# Get external IP
kubectl get services
```

**Apply Configuration:**
```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: SecureCleanApiWaf
spec:
  replicas: 3
  selector:
    matchLabels:
      app: SecureCleanApiWaf
  template:
    metadata:
      labels:
        app: SecureCleanApiWaf
    spec:
      containers:
      - name: SecureCleanApiWaf
        image: yourdockerhubusername/SecureCleanApiWaf:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        - name: ThirdPartyApi__BaseUrl
          value: https://api.example.com/
```

```bash
# Apply the manifest
kubectl apply -f deployment.yaml

# Monitor deployment
kubectl rollout status deployment/SecureCleanApiWaf

# View pods
kubectl get pods -l app=SecureCleanApiWaf
```

### AWS ECS/Fargate

1. Push image to AWS ECR or Docker Hub
2. Create ECS Task Definition:
   ```json
   {
     "name": "SecureCleanApiWaf",
     "image": "yourdockerhubusername/SecureCleanApiWaf:latest",
     "portMappings": [{
       "containerPort": 8080,
       "hostPort": 8080
     }],
     "environment": [{
       "name": "ASPNETCORE_ENVIRONMENT",
       "value": "Production"
     }]
   }
   ```
3. Create ECS Service with the task definition
4. Configure load balancer and target group

### Google Cloud Run

```bash
gcloud run deploy SecureCleanApiWaf \
  --image yourdockerhubusername/SecureCleanApiWaf:latest \
  --platform managed \
  --region us-central1 \
  --port 8080 \
  --allow-unauthenticated \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production
```

---

## ?? Security Best Practices

### 1. Never Hardcode Secrets

? **Bad:**
```dockerfile
ENV JWT_SECRET=mysecretkey123
```

? **Good:**
```dockerfile
# Use environment variables at runtime
# Don't embed in image
```

### 2. Use Azure Key Vault

```bash
docker run -d \
  -p 8080:8080 \
  -e Azure__KeyVaultUri=https://your-vault.vault.azure.net/ \
  -e AZURE_CLIENT_ID=<client-id> \
  -e AZURE_CLIENT_SECRET=<client-secret> \
  SecureCleanApiWaf:latest
```

### 3. Use Specific Image Tags

? **Bad:**
```bash
docker run SecureCleanApiWaf:latest  # Latest can change unexpectedly
```

? **Good:**
```bash
docker run SecureCleanApiWaf:v1.0.0  # Specific version
```

### 4. Run as Non-Root User

? **Already configured in Dockerfile:**
```dockerfile
RUN adduser -u 1000 --disabled-password --gecos "" appuser
USER appuser
```

### 5. Scan for Vulnerabilities

```bash
# Scan image for security issues
docker scan SecureCleanApiWaf:latest

# Update base image regularly
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker build --no-cache -t SecureCleanApiWaf:latest .
```

### 6. Use HTTPS in Production

Configure a reverse proxy (Nginx, Traefik, or Azure Front Door):
```nginx
server {
    listen 443 ssl;
    server_name example.com;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    
    location / {
        proxy_pass http://SecureCleanApiWaf:8080;
    }
}
```

### 7. Implement Rate Limiting

Already configured in SecureCleanApiWaf - ensure it's enabled:
```bash
# Verify in environment
docker exec SecureCleanApiWaf printenv | grep RateLimiting
```

### 8. Enable Logging and Monitoring

**For Azure:**
```bash
# Add Application Insights connection string
docker run -d \
  -p 8080:8080 \
  -e ApplicationInsights__InstrumentationKey=<your-key> \
  SecureCleanApiWaf:latest
```

---

## ?? Docker Commands Cheat Sheet

### Container Management

```bash
# List running containers
docker ps

# List all containers (including stopped)
docker ps -a

# Start a stopped container
docker start SecureCleanApiWaf

# Stop a running container
docker stop SecureCleanApiWaf

# Restart a container
docker restart SecureCleanApiWaf

# Remove a container
docker rm SecureCleanApiWaf

# Remove a running container (force)
docker rm -f SecureCleanApiWaf

# Rename a container
docker rename SecureCleanApiWaf SecureCleanApiWaf-old
```

### Image Management

```bash
# List all images
docker images

# Remove an image
docker rmi SecureCleanApiWaf:latest

# Remove unused images
docker image prune

# Remove all unused images
docker image prune -a

# Tag an image
docker tag SecureCleanApiWaf:latest yourusername/SecureCleanApiWaf:latest

# Show image history
docker history SecureCleanApiWaf:latest
```

### Logs and Monitoring

```bash
# View container logs
docker logs SecureCleanApiWaf

# Follow logs (real-time)
docker logs -f SecureCleanApiWaf

# View last 50 lines
docker logs --tail 50 SecureCleanApiWaf

# Show timestamps in logs
docker logs -t SecureCleanApiWaf

# View container stats
docker stats SecureCleanApiWaf

# Inspect container
docker inspect SecureCleanApiWaf
```

### Interactive Shell

```bash
# Execute bash shell
docker exec -it SecureCleanApiWaf bash

# Execute sh shell
docker exec -it SecureCleanApiWaf sh

# Run a specific command
docker exec SecureCleanApiWaf curl http://localhost:8080

# Copy file from container
docker cp SecureCleanApiWaf:/app/appsettings.json ./appsettings.json
```

### Network and Port

```bash
# View port mappings
docker port SecureCleanApiWaf

# View network connections
docker network ls

# Inspect network
docker network inspect bridge
```

### Resource Management

```bash
# View disk usage
docker system df

# Remove unused images, containers, and networks
docker system prune

# Remove everything (including volumes)
docker system prune -a --volumes

# View container resource limits
docker inspect SecureCleanApiWaf | grep -A 5 '"HostConfig"'
```

---

## ?? Troubleshooting

### Issue: Container Won't Start

**Symptoms:** Container exits immediately or shows error

**Diagnosis:**
```bash
# Check container status
docker ps -a | grep SecureCleanApiWaf

# View error logs
docker logs SecureCleanApiWaf

# View last 100 lines
docker logs --tail 100 SecureCleanApiWaf

# Inspect container
docker inspect SecureCleanApiWaf
```

**Solutions:**
1. Check if `./publish` folder exists
2. Verify `SecureCleanApiWaf.dll` is in the publish folder
3. Check Dockerfile for correct DLL name
4. Review environment variables

---

### Issue: Port 8080 Already in Use

**Symptoms:** `Error: Address already in use`

**Diagnosis:**
```bash
# Windows
netstat -ano | findstr :8080

# Linux/Mac
lsof -i :8080
```

**Solutions:**
```bash
# Option 1: Stop other container
docker stop <container-id>

# Option 2: Use different port
docker run -p 8081:8080 SecureCleanApiWaf:latest

# Option 3: Find and kill process
# Windows: taskkill /PID <PID> /F
# Linux/Mac: kill -9 <PID>
```

---

### Issue: Image Build Fails

**Symptoms:** `docker build` command fails with error

**Diagnosis:**
```bash
# Build with verbose output
docker build -t SecureCleanApiWaf:latest . --progress=plain

# Check if publish folder exists
ls -la ./publish

# Verify DLL exists
ls ./publish/*.dll
```

**Solutions:**
```bash
# Step 1: Run dotnet publish
dotnet publish -c Release -o ./publish

# Step 2: Verify publish contents
ls ./publish/SecureCleanApiWaf.dll

# Step 3: Rebuild Docker image
docker build -t SecureCleanApiWaf:latest . --no-cache
```

---

### Issue: Environment Variables Not Working

**Symptoms:** Application uses default values instead of custom values

**Diagnosis:**
```bash
# Check variables inside container
docker exec -it SecureCleanApiWaf printenv

# Check specific variable
docker exec SecureCleanApiWaf printenv ThirdPartyApi__BaseUrl
```

**Solutions:**
```bash
# Remember: Use double underscore (__) for nested config
# appsettings.json: "ThirdPartyApi": { "BaseUrl": "..." }
# Environment: ThirdPartyApi__BaseUrl=...

# Verify in docker-compose.yml
cat docker-compose.yml | grep ThirdPartyApi

# For docker run, use -e flag
docker run -e ThirdPartyApi__BaseUrl=https://api.example.com/ -p 8080:8080 SecureCleanApiWaf:latest
```

---

### Issue: Cannot Access Application

**Symptoms:** `curl: Failed to connect to 127.0.0.1:8080`

**Diagnosis:**
```bash
# Check if container is running
docker ps | grep SecureCleanApiWaf

# Check port mapping
docker port SecureCleanApiWaf

# Check if port is listening
netstat -tulpn | grep 8080

# Test inside container
docker exec SecureCleanApiWaf curl http://localhost:8080
```

**Solutions:**
```bash
# Ensure container is running
docker-compose up -d

# Check logs for errors
docker-compose logs -f

# Verify port mapping
docker port SecureCleanApiWaf

# Test the container
curl http://localhost:8080
```

---

### Issue: Push to Docker Hub Fails

**Symptoms:** `denied: requested access to the resource is denied`

**Diagnosis:**
```bash
# Check Docker login status
docker info | grep Username

# Check image tag format
docker images | grep SecureCleanApiWaf
```

**Solutions:**
```bash
# Re-login to Docker Hub
docker logout
docker login

# Verify tag format (must include username)
# Format: yourusername/SecureCleanApiWaf:tag
docker tag SecureCleanApiWaf:latest yourusername/SecureCleanApiWaf:latest

# Try push again
docker push yourusername/SecureCleanApiWaf:latest
```

---

### Issue: Out of Memory

**Symptoms:** Container restarts or crashes with memory error

**Solutions:**
```bash
# Increase Docker memory allocation
# Windows/Mac: Docker Desktop ? Settings ? Resources
# Linux: Configure in docker daemon config

# Or limit memory for specific container
docker run -m 512m -p 8080:8080 SecureCleanApiWaf:latest

# Check current memory usage
docker stats SecureCleanApiWaf
```

---

## ?? Additional Resources

**Official Documentation:**
- [Docker Documentation](https://docs.docker.com/) - Complete Docker reference
- [Docker Compose Docs](https://docs.docker.com/compose/) - Compose specification
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet-aspnet) - Microsoft official images
- [ASP.NET Core Docker](https://learn.microsoft.com/aspnet/core/host-and-deploy/docker/) - Microsoft guide

**Azure Resources:**
- [Azure Container Registry](https://learn.microsoft.com/azure/container-registry/) - Private registry
- [Azure Container Instances](https://learn.microsoft.com/azure/container-instances/) - Simple container hosting
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/) - Serverless containers
- [Azure Kubernetes Service](https://learn.microsoft.com/azure/aks/) - Kubernetes orchestration

**Learning Resources:**
- [Docker Getting Started](https://docs.docker.com/get-started/) - Official tutorial
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/) - Best practices guide
- [Dockerfile Reference](https://docs.docker.com/engine/reference/builder/) - Dockerfile syntax

---

## ?? Support

**Need Help?**

- ?? **Documentation:** Start with the deployment guides in this directory
- ?? **Quick Ref:** Use [DOCKER_QUICK_REFERENCE.md](./DOCKER_QUICK_REFERENCE.md) for commands
- ?? **Troubleshoot:** Check the [Troubleshooting](#-troubleshooting) section above
- ?? **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/SecureCleanApiWaf/issues)
- ?? **Email:** softevolutionsl@gmail.com
- ?? **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

**Before asking for help:**
1. ? Check the [Troubleshooting](#-troubleshooting) section above
2. ? Review the [Quick Start](#-quick-start-5-minutes) for common issues
3. ? Search [existing GitHub issues](https://github.com/dariemcarlosdev/SecureCleanApiWaf/issues)
4. ? Include error messages and logs in your issue

**Getting Help:**
- ?? **First time with Docker?** ? Start with [Quick Start](#-quick-start-5-minutes)
- ? **Need quick commands?** ? Check [Docker Commands Cheat Sheet](#-docker-commands-cheat-sheet)
- ??? **Building for production?** ? See [Production Deployment](#-production-deployment)
- ?? **Something's broken?** ? Go to [Troubleshooting](#-troubleshooting)

---

**Ready to deploy? Follow the Quick Start above and get containerizing! Happy deploying SecureCleanApiWaf Team! ??**

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [SecureCleanApiWaf](https://github.com/dariemcarlosdev/SecureCleanApiWaf)  
**Status:** ? Current & Maintained
