# 🌐 Docker Desktop DNS Configuration - Visual Guide

> *"In the world of containers, DNS is the compass that guides your packets home."*  
> — DevOps Wisdom

This document provides step-by-step visual instructions for configuring DNS in Docker Desktop to fix NuGet connectivity issues.

---

## 🔍 Problem You're Experiencing

```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json
failed to solve: process "/bin/sh -c dotnet restore..." did not complete successfully: exit code: 1
```

---

## Solution: Configure DNS in Docker Desktop

### Step 1: Open Docker Desktop

1. Look for the **Docker icon** in your system tray (bottom-right of Windows taskbar)
   - Should be a **whale icon** 🐳
   - Icon should be **GREEN** ✅ (if gray or orange, Docker is not fully running)

2. **Right-click** the Docker icon
3. Select **"Dashboard"** or **"Settings"**

---

### Step 2: Navigate to Docker Engine Settings

```
Docker Desktop Window Layout:
+-------------------------------------------------+
�  Docker Desktop                              ?? �
�                                                 �
�  +--------------+  +-------------------------+  �
�  � General      �  �                         �  �
�  � Resources    �  �   Settings Content      �  �
�  � Docker Engine�?-�   (JSON Configuration)  �  �
�  � Kubernetes   �  �                         �  �
�  � Software     �  �                         �  �
�  � Updates      �  �                         �  �
�  +--------------+  +-------------------------+  �
+-------------------------------------------------+
```

1. Click **Settings** (gear icon ⚙️) in top-right corner
2. In the left sidebar, click **"Docker Engine"**

---

### Step 3: Edit Docker Engine JSON Configuration

You'll see a **JSON editor** with configuration like this:

#### BEFORE (Your current config might look like this):
```json
{
  "builder": {
    "gc": {
      "defaultKeepStorage": "20GB",
      "enabled": true
    }
  },
  "experimental": false,
  "features": {
    "buildkit": true
  }
}
```

#### AFTER (Add the DNS configuration):
```json
{
  "builder": {
    "gc": {
      "defaultKeepStorage": "20GB",
      "enabled": true
    }
  },
  "dns": ["8.8.8.8", "8.8.4.4"],
  "experimental": false,
  "features": {
    "buildkit": true
  }
}
```

**⚠️ IMPORTANT:**
- Add the line: `"dns": ["8.8.8.8", "8.8.4.4"],` 
- Make sure to add a **comma** after the previous line (see the comma after `}` in "builder" section)
- JSON syntax matters - one missing comma or bracket will cause errors

---

### Step 4: Apply Changes

```
Bottom of Settings Window:
+------------------------------------------+
�                                          �
�   [Cancel]  [Apply & Restart]            �
+------------------------------------------+
```

1. Click **"Apply & Restart"** button at bottom-right
2. Docker Desktop will restart (this takes 30-60 seconds)
3. **WAIT** for the Docker icon in system tray to turn **GREEN**
4. Do NOT proceed until Docker is fully restarted

---

### Step 5: Verify DNS Configuration

Open PowerShell and run:

```powershell
# Test if Docker can resolve DNS
docker run --rm alpine nslookup api.nuget.org

# Expected output:
# Server:    8.8.8.8
# Address:   8.8.8.8#53
# 
# Name:      api.nuget.org
# Address:   <some IP address>
```

If you see the output above, DNS is configured correctly! ✅

---

### Step 6: Rebuild Your Application

Now that DNS is configured, rebuild:

```powershell
# Clear old build cache
docker builder prune -a
# Type 'y' to confirm

# Rebuild from scratch
docker-compose build --no-cache

# Start the application
docker-compose up -d
```

---

## Alternative: Use the Automated Script

Instead of manual steps, run the provided PowerShell script:

```powershell
# Navigate to project directory
cd "C:\DATA\MYSTUFFS\PROFESSIONAL STUFF\TECH CHALLENGE\SecureCleanApiWaf"

# Run the fix script
.\Fix-DockerBuild.ps1
```

The script will:
1. ✅ Check if Docker is running
2. ✅ Test network connectivity
3. ✅ Guide you through DNS configuration
4. ✅ Clear build cache
5. ✅ Rebuild the application

---

## What the DNS Configuration Does

**Google Public DNS (8.8.8.8 and 8.8.4.4):**
- Provides **reliable** DNS resolution
- Works from anywhere in the world
- Not blocked by most corporate networks
- Fast and reliable

**Why is this needed?**
- Docker containers by default use Docker's internal DNS
- Sometimes Docker's DNS cannot reach external services like NuGet.org
- By setting explicit DNS servers, containers can reliably resolve domain names

---

## Troubleshooting

### ❌ "Apply & Restart" button is grayed out
**Cause:** JSON syntax error
**Solution:** 
- Check for missing commas
- Check for matching brackets `{}`
- Copy the example JSON exactly as shown above

### ❌ Docker won't restart after applying
**Cause:** Invalid configuration
**Solution:**
1. Re-open Docker Desktop settings
2. Revert to previous configuration
3. Try again with correct JSON syntax

### ❌ Still getting NuGet errors after DNS configuration
**Solutions:**
1. Verify Docker fully restarted (green icon)
2. Run `docker info` and check DNS section
3. Try disabling VPN temporarily
4. Check Windows Firewall settings
5. Restart your computer

### ❌ Cannot find "Docker Engine" in settings
**Cause:** Using old version of Docker Desktop
**Solution:**
- Update Docker Desktop to latest version
- Download from: https://www.docker.com/products/docker-desktop

---

## Additional DNS Servers (Alternatives)

If Google DNS (8.8.8.8) doesn't work, try these alternatives:

### Cloudflare DNS (Very fast):
```json
"dns": ["1.1.1.1", "1.0.0.1"]
```

### OpenDNS:
```json
"dns": ["208.67.222.222", "208.67.220.220"]
```

### Multiple DNS servers (recommended):
```json
"dns": ["8.8.8.8", "8.8.4.4", "1.1.1.1", "1.0.0.1"]
```

---

## Quick Reference Card

### Where to Configure DNS:
```
Docker Desktop → Settings → Docker Engine → JSON Editor
```

### What to Add:
```json
"dns": ["8.8.8.8", "8.8.4.4"],
```

### What to Click:
```
"Apply & Restart" button
```

### How Long to Wait:
```
30-60 seconds for Docker to restart
```

### What to Run Next:
```powershell
docker builder prune -a
docker-compose build --no-cache
docker-compose up -d
```

---

## Success Indicators

✅ Docker icon in system tray is **GREEN**
✅ Command `docker version` shows both Client and Server
✅ Command `docker run --rm alpine nslookup api.nuget.org` succeeds
✅ Build completes without NuGet errors
✅ Application starts at http://localhost:8080

---

## Related Documentation

- **Quick Fix Guide**: `DOCKER_QUICKFIX.md`
- **Full Troubleshooting**: `docs/Deployment/Docker/DOCKER_TROUBLESHOOTING.md`
- **Automated Script**: `Fix-DockerBuild.ps1`
- **Docker Compose**: `docker-compose.yml`
- **Dockerfile**: `Dockerfile`

---


**Last Updated**: 2025
**For**: SecureCleanApiWaf - .NET 8 Blazor Application

---

## 🆘 Support & Contact

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

**Remember**: 90% of Docker NuGet issues are resolved by configuring DNS! 🎉

---

**Last Updated:** November 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [SecureCleanApiWaf](https://github.com/dariemcarlosdev/SecureCleanApiWaf)

