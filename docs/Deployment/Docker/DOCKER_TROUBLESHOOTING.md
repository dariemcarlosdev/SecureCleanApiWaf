# 🔧 Docker Troubleshooting Guide - CleanArchitecture.ApiTemplate

> *"Every error is a lesson in disguise; troubleshooting is the path to mastery."*  
> — Engineering Wisdom

This guide helps you build and run the CleanArchitecture.ApiTemplate Blazor application in Docker.

---

## ✅ **Setup Instructions (Step-by-Step)**

### **Step 1: Build and Publish Locally (REQUIRED FIRST)**

Before building the Docker image, you **must** build your .NET application locally. This avoids all Docker network and certificate issues.

**Open PowerShell in your project root and run:**

```powershell
# Restore NuGet packages
dotnet restore

# Build the application in Release mode
dotnet build -c Release

# Publish the application to a 'publish' folder
dotnet publish -c Release -o ./publish
```

**Expected Result:**
- A `publish` folder is created in your project root
- Contains compiled files: `CleanArchitecture.ApiTemplate.dll`, `appsettings.json`, static files, etc.

**Verify:**
```powershell
dir ./publish
```

### **Step 2: Build the Docker Image**

Once the `publish` folder exists, build the Docker image:

```powershell
docker-compose build
```

### **Step 3: Run the Container**

```powershell
docker-compose up -d
```

**Verify it's running:**
```powershell
docker ps
```

You should see `CleanArchitecture.ApiTemplate` in the list with status `Up`.

### **Step 4: Access the Application**

Open your browser and navigate to:
```
http://localhost:8080
```

---

## ?? **Common Docker Commands**

### **Building and Running**
```powershell
# Build the image from pre-published app
docker-compose build

# Start the container
docker-compose up -d

# Stop the container
docker-compose down

# View container logs in real-time
docker-compose logs -f
```

### **Cleanup and Reset**
```powershell
# Stop and remove the container
docker-compose down

# Remove the image
docker rmi CleanArchitecture.ApiTemplate:latest

# Clean up unused Docker resources
docker system prune -a -f
```

---

## ?? **Troubleshooting**

### **Error: "COPY ./publish .: not found"**

**Cause:** You tried to build the Docker image before publishing the application locally.

**Solution:**
1. Run `dotnet publish -c Release -o ./publish` locally first
2. Verify the `publish` folder exists: `dir ./publish`
3. Then run `docker-compose build`

---

## ?? **FINAL RESOLUTION: "failed to compute cache key: /publish not found"**

### **Problem**
Even after the `publish` folder exists, Docker build fails with:
```
failed to solve: failed to compute cache key: failed to calculate checksum of ref: "/publish": not found
```

### **Root Cause**
The `.dockerignore` file was excluding the `publish` folder with the line:
```
**/publish/
```

This prevented Docker from including the `publish` folder in the build context, even though the files physically existed on disk.

### **✅ Final Solution**

**Edit `.dockerignore` and remove or comment out the `**/publish/` line:**

```
# Before (WRONG):
**/bin/
**/obj/
**/out/
**/publish/    # ⚠️ This was blocking the build!
**/build/

# After (CORRECT):
**/bin/
**/obj/
**/out/
# NOTE: /publish/ is NOT ignored because we use pre-published apps in the Dockerfile
**/build/
```

### **Why This Works**
- The new Dockerfile uses a **runtime-only** approach that requires the `publish` folder
- The `.dockerignore` file was created for the old multi-stage build approach (which built inside Docker)
- Removing `**/publish/` from `.dockerignore` allows Docker to access the pre-published files

### **Verification**
After updating `.dockerignore`, run:
```powershell
docker-compose build
docker-compose up -d
docker ps
```

The container should now build and run successfully! ✅

---

### **Error: "Address already in use" or "port is already allocated"**

Port 8080 is already in use. Either:
1. Stop the other application using port 8080
2. Change the port in `docker-compose.yml`:
   ```yaml
   ports:
     - "8081:8080"  # Use 8081 instead
   ```

### **Container starts but application doesn't respond**

```powershell
# Check the logs
docker-compose logs -f

# Verify the container is running
docker ps

# Check if port is listening
netstat -ano | findstr :8080
```

### **"Application Not Accessible at http://localhost:8080"**

1. **Check if the container is running:**
   ```powershell
   docker ps
   ```
   *If it's not listed, check the logs: `docker-compose logs`*

2. **Check if the port is already in use:**
   ```powershell
   netstat -ano | findstr :8080
   ```
   *If another process is using port 8080, stop it or change the port in `docker-compose.yml`.*

---

## ?? **Why This Approach?**

This Dockerfile uses a **runtime-only** image that expects pre-published application files:

| Benefit | Why |
|---------|-----|
| ✅ No network issues during build | NuGet restore happens locally, not in Docker |
| ✅ No certificate/proxy problems | Local build uses your system's network settings |
| ✅ Smaller, faster container images | Only runtime files, no SDK or build tools |
| ✅ Works in corporate networks | Avoids Docker's SSL inspection issues |
| ✅ Industry standard practice | How enterprises handle Docker in restricted networks |

---

## Additional Resources

- [Docker Official Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [ASP.NET Core Docker Documentation](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)

---

**Last Updated:** 2025-01-14
**For:** CleanArchitecture.ApiTemplate (Blazor .NET 8)
**Approach:** Local build + Docker containerization
**Status:** ✅ Fully Working and Tested

## ?? Support

**Need Help?**

- 📖 **Documentation:** Start with the deployment guides above
- 🐛 **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues)
- 📧 **Email:** softevolutionsl@gmail.com
- 🐙 **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

**Before asking for help:**
1. Check the troubleshooting sections REF: above
2. Review the deployment guides for common pitfalls. REF: above
3. Search existing GitHub issues
4. Include error messages and logs

---

**Ready to troubleshoot and deploy your CleanArchitecture.ApiTemplate in Docker? Follow this guide step-by-step to ensure a smooth experience!**

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate)
