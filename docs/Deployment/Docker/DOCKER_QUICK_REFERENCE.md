# 📋 Docker Quick Reference - SecureCleanApiWaf

> *"Quick references are the lighthouses in the sea of commands."*  
> — Developer's Wisdom

## 📑 Table of Contents

### **Quick Navigation**
1. [Quick Commands](#quick-commands)
   - [Build](#build)
   - [Run](#run)
   - [View Logs](#view-logs)
   - [Stop & Remove](#stop--remove)
   - [Push to Docker Hub](#push-to-docker-hub)
2. [Docker Compose](#docker-compose)
   - [Start](#start)
   - [Stop](#stop)
   - [Rebuild](#rebuild)
   - [View Logs (Compose)](#view-logs-1)
3. [Production Deployment](#production-deployment)
   - [Azure Container Instances](#azure-container-instances)
   - [Azure Container Apps](#azure-container-apps)
   - [Kubernetes](#kubernetes)
4. [Environment Variables](#environment-variables)
   - [Required](#required)
   - [Optional](#optional)
5. [Troubleshooting](#troubleshooting)
   - [Check Container Status](#check-container-status)
   - [View Container Logs](#view-container-logs)
   - [Execute Shell Inside Container](#execute-shell-inside-container)
   - [Check Environment Variables](#check-environment-variables)
   - [Check Health](#check-health)
   - [Port Already in Use](#port-already-in-use)
6. [Automated Scripts](#automated-scripts)
   - [Linux/Mac](#linuxmac)
   - [Windows PowerShell](#windows-powershell)
7. [Access URLs](#access-urls)
8. [Files Overview](#files-overview)
9. [Before Publishing](#before-publishing)
10. [Security Checklist](#security-checklist)
11. [Resources](#resources)

---

## Quick Commands

### Build
```bash
docker build -t SecureCleanApiWaf:latest .
```

### Run
```bash
docker run -d -p 8080:8080 --name SecureCleanApiWaf SecureCleanApiWaf:latest
```

### View Logs
```bash
docker logs -f SecureCleanApiWaf
```

### Stop & Remove
```bash
docker stop SecureCleanApiWaf && docker rm SecureCleanApiWaf
```

### Push to Docker Hub
```bash
# Replace 'yourdockerhubusername' with your actual username
docker tag SecureCleanApiWaf:latest yourdockerhubusername/SecureCleanApiWaf:latest
docker push yourdockerhubusername/SecureCleanApiWaf:latest
```

## Docker Compose

### Start
```bash
docker-compose up -d
```

### Stop
```bash
docker-compose down
```

### Rebuild
```bash
docker-compose up -d --build
```

### View Logs (Compose)
```bash
docker-compose logs -f
```

## Production Deployment

### Azure Container Instances
```bash
az container create \
  --resource-group myResourceGroup \
  --name SecureCleanApiWaf \
  --image yourdockerhubusername/SecureCleanApiWaf:latest \
  --dns-name-label SecureCleanApiWaf \
  --ports 8080
```

### Azure Container Apps
```bash
az containerapp create \
  --name SecureCleanApiWaf \
  --resource-group myResourceGroup \
  --environment myEnvironment \
  --image yourdockerhubusername/SecureCleanApiWaf:latest \
  --target-port 8080 \
  --ingress external
```

### Kubernetes
```bash
kubectl create deployment SecureCleanApiWaf --image=yourdockerhubusername/SecureCleanApiWaf:latest
kubectl expose deployment SecureCleanApiWaf --type=LoadBalancer --port=80 --target-port=8080
```

## Environment Variables

### Required
- `ASPNETCORE_ENVIRONMENT`: Production/Development
- `JwtSettings__SecretKey`: Your JWT secret key
- `ThirdPartyApi__ApiKey`: Your API key

### Optional
- `Azure__KeyVaultUri`: Azure Key Vault URL
- `JwtSettings__ExpirationMinutes`: Token expiration (default: 20)
- `ThirdPartyApi__BaseUrl`: External API URL

## Troubleshooting

### Check Container Status
```bash
docker ps -a | grep SecureCleanApiWaf
```

### View Container Logs
```bash
docker logs SecureCleanApiWaf
```

### Execute Shell Inside Container
```bash
docker exec -it SecureCleanApiWaf sh
```

### Check Environment Variables
```bash
docker exec SecureCleanApiWaf printenv
```

### Check Health
```bash
curl http://localhost:8080/health
```

### Port Already in Use
```bash
# Use different port
docker run -d -p 8081:8080 --name SecureCleanApiWaf SecureCleanApiWaf:latest
```

## Automated Scripts

### Linux/Mac
```bash
chmod +x docker-build-push.sh
./docker-build-push.sh v1.0.0
```

### Windows PowerShell
```powershell
.\docker-build-push.ps1 v1.0.0
```

## Access URLs

- **Local**: http://localhost:8080
- **API Docs (Swagger)**: http://localhost:8080/swagger
- **Health Check**: http://localhost:8080/health

## Files Overview

| File | Purpose |
|------|---------|
| `Dockerfile` | Multi-stage build configuration |
| `.dockerignore` | Excludes unnecessary files |
| `docker-compose.yml` | Local development setup |
| `.github/workflows/docker-publish.yml` | CI/CD workflow |
| `DOCKER_DEPLOYMENT.md` | Complete documentation |

## Before Publishing

1. ✅ Update Docker Hub username in all files
2. ✅ Test build locally: `docker build -t SecureCleanApiWaf:latest .`
3. ✅ Test run locally: `docker run -d -p 8080:8080 SecureCleanApiWaf:latest`
4. ✅ Verify app works: http://localhost:8080
5. ✅ Login to Docker Hub: `docker login`
6. ✅ Push image: `docker push yourdockerhubusername/SecureCleanApiWaf:latest`

## Security Checklist

- [ ] Replace `yourdockerhubusername` with actual username
- [ ] Use strong JWT secret keys (not default values)
- [ ] Store secrets in Azure Key Vault or environment variables
- [ ] Enable HTTPS in production
- [ ] Scan images for vulnerabilities
- [ ] Keep base images updated
- [ ] Implement rate limiting (already configured)
- [ ] Set up monitoring and logging

## Resources

- [Full Documentation](./DOCKER_DEPLOYMENT.md)
- [Docker Documentation](https://docs.docker.com/)
- [Docker Hub](https://hub.docker.com/)
- [Azure Container Docs](https://docs.microsoft.com/azure/container-instances/)

---

**Need Help?** See [DOCKER_DEPLOYMENT.md](./DOCKER_DEPLOYMENT.md) for detailed instructions.

## ?? Support

**Need Help?**

- 📖 **Documentation:** Start with the deployment guides above
- 🐛 **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/SecureCleanApiWaf/issues)
- 📧 **Email:** softevolutionsl@gmail.com
- 🐙 **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

**Before asking for help:**
1. Check the troubleshooting sections REF: above
2. Review the deployment guides for common pitfalls. REF: above
3. Search existing GitHub issues
4. Include error messages and logs

---

** Enjoying SecureCleanApiWaf? ⭐ Star the repo on GitHub to support continued development! **

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [SecureCleanApiWaf](https://github.com/dariemcarlosdev/SecureCleanApiWaf)
