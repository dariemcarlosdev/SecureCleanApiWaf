# ================================================================================
# DOCKERFILE FOR SECURECLEANAPIWAF (BLAZOR APPLICATION)
# For more details, see README.md and documentation at: Docs/Deployment/Docker/DEPLOYMENT_README.md
# ================================================================================
# This Dockerfile uses a PRE-BUILT approach, meaning the application is built
# locally on your development machine BEFORE being containerized.
#
# ADVANTAGES of this approach:
#   ✅ Works in corporate environments with complex SSL/proxy configurations
#   ✅ Faster builds if you're developing locally frequently
#   ✅ Better control over the build process
#   ✅ Useful when CI/CD handles the build separately
#
# DISADVANTAGES:
#   ❌ Requires manual publish step before docker build
#   ❌ Larger image size (includes all dependencies)
#   ❌ Less reproducible (depends on local environment)
#
# ALTERNATIVE: Multi-stage build approach (see comments below)
#
# ================================================================================

# ================================================================================
# STAGE 1: RUNTIME ENVIRONMENT
# ================================================================================
# Purpose: Create a lightweight container with only the runtime dependencies
#          needed to run the pre-published .NET 8 application
#
# Base Image: mcr.microsoft.com/dotnet/aspnet:8.0
#   - Official Microsoft .NET 8 ASP.NET runtime image
#   - Optimized for running ASP.NET Core applications
#   - Includes all necessary runtime libraries
#   - NOT included: SDK (compiler), build tools - keeps image smaller
#   - Size: ~215 MB (much smaller than SDK image)
# ================================================================================

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# ================================================================================
# SECURITY: Create a non-root user
# ================================================================================
# Why: Running as root is a security vulnerability
#      If the container is compromised, attacker has full system access
#      Using a non-root user limits damage in case of breach
#
# What this does:
#   - Creates a user named "appuser"
#   - UID 1000 (standard non-root user ID)
#   - --disabled-password: no password needed (used only in containers)
#   - --gecos "": no user description
# ================================================================================

RUN adduser -u 1000 --disabled-password --gecos "" appuser

# ================================================================================
# SET WORKING DIRECTORY
# ================================================================================
# Purpose: All subsequent commands run inside /app directory
#          This is where the application files will be placed
# ================================================================================

WORKDIR /app

# ================================================================================
# COPY APPLICATION FILES
# ================================================================================
# Purpose: Copy the pre-published application from your local machine into
#          the container's /app directory
#
# Source: ./publish
#   - This folder contains the compiled, ready-to-run application
#   - Created by: dotnet publish -c Release -o ./publish
#   - Includes: DLLs, configuration files, and dependencies
#
# Destination: . (which means /app, since we set WORKDIR above)
#
# IMPORTANT: This assumes you've already run:
#   1. dotnet restore (download NuGet packages)
#   2. dotnet build -c Release (compile the application)
#   3. dotnet publish -c Release -o ./publish (prepare for deployment)
# ================================================================================

COPY ./publish .

# ================================================================================
# SECURITY: Switch to non-root user
# ================================================================================
# Purpose: All processes in the container will run as "appuser", not root
#          This limits permissions and improves security
#
# Important: This MUST come after COPY command
#   - COPY as root ensures files are copied correctly
#   - Then we switch to non-root for runtime
# ================================================================================

USER appuser

# ================================================================================
# EXPOSE PORT
# ================================================================================
# Purpose: Document which port the application listens on
#          This is for documentation - doesn't actually expose the port
#
# Port 8080: Selected because:
#   - Not a privileged port (don't need root access)
#   - Standard choice for containerized applications
#   - Matches docker-compose.yml configuration
#   - Above port 1024 (non-root can listen here)
#
# Note: To actually expose the port, use port mapping in docker-compose.yml
#       or docker run: -p 8080:8080
# ================================================================================

EXPOSE 8080

# ================================================================================
# ENVIRONMENT VARIABLE: Configure ASP.NET Core
# ================================================================================
# Purpose: Tell ASP.NET Core which URLs to listen on
#
# ASPNETCORE_URLS=http://+:8080
#   - "http://": Use HTTP protocol (not HTTPS - configured at infrastructure level)
#   - "+": Listen on ALL network interfaces (0.0.0.0), not just localhost
#   - ":8080": Listen on port 8080
#
# Why this matters:
#   - Without this, ASP.NET Core might only listen on localhost
#   - In Docker, localhost is the container - external traffic won't reach it
#   - Using "+" allows traffic from docker-compose and other containers
# ================================================================================

ENV ASPNETCORE_URLS=http://+:8080

# ================================================================================
# ENTRY POINT: Application Startup
# ================================================================================
# Purpose: Command that runs when the container starts
#
# ENTRYPOINT ["dotnet", "SecureCleanApiWaf.dll"]
#   - Runs the compiled Blazor application
#   - Format: exec form (preferred over shell form)
#     - Exec form: ["dotnet", "SecureCleanApiWaf.dll"] - runs directly
#     - Shell form: dotnet SecureCleanApiWaf.dll - runs via /bin/sh
#   - Exec form is better because it passes signals correctly
#
# The DLL name must match:
#   - Your .csproj file name: <ProjectName>SecureCleanApiWaf</ProjectName>
#   - Ensure this matches your actual output assembly name
# ================================================================================

ENTRYPOINT ["dotnet", "SecureCleanApiWaf.dll"]

# ================================================================================
# QUICK REFERENCE: DOCKERFILE BEST PRACTICES
# ================================================================================
#
# SECURITY ✅ IMPLEMENTED:
#   ✅ Non-root user (appuser)
#   ✅ Minimal base image (runtime only)
#   ✅ No unnecessary tools
#
# SIZE OPTIMIZATION ✅ IMPLEMENTED:
#   ✅ Uses runtime image instead of SDK
#   ✅ Single stage (published files copied)
#
# IMPROVEMENTS YOU COULD MAKE:
#   □ Add .dockerignore file to exclude unnecessary files
#   □ Add health checks to docker-compose.yml
#   □ Use multi-stage build for fully automated builds
#   □ Add labels for documentation
#
# ================================================================================

# ================================================================================
# ALTERNATIVE: MULTI-STAGE BUILD (Multi-stage Dockerfile)
# ================================================================================
# If you want Docker to handle the entire build process, use this approach:
#
# FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# WORKDIR /app
# COPY . .
# RUN dotnet restore
# RUN dotnet publish -c Release -o /app/publish
#
# FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# RUN adduser -u 1000 --disabled-password --gecos "" appuser
# WORKDIR /app
# COPY --from=build /app/publish .
# USER appuser
# EXPOSE 8080
# ENV ASPNETCORE_URLS=http://+:8080
# ENTRYPOINT ["dotnet", "BlueTreadApp.dll"]
#
# Advantages:
#   ✅ Fully reproducible (no local build needed)
#   ✅ Better for CI/CD pipelines
#   ✅ Single docker build command does everything
#
# Disadvantages:
#   ❌ Slower builds (full compile every time)
#   ❌ Won't work if your network has complex proxy/SSL setup
#   ❌ Larger build context
#
# ================================================================================

# ================================================================================
# USAGE INSTRUCTIONS FOR THIS DOCKERFILE
# ================================================================================
#
# STEP 1: Build the application locally
#   cd C:\DATA\MYSTUFFS\PROFESSIONAL STUFF\TECH CHALLENGE\SecureCleanApiWaf
#   dotnet restore
#   dotnet build -c Release
#   dotnet publish -c Release -o ./publish
#
# STEP 2: Build the Docker image
#   docker build -t securecleanapiwaf:latest .
#
# STEP 3: Run with Docker Compose (recommended)
#   docker-compose up -d
#
# STEP 4: Access the application
#   Open browser: http://localhost:8080
#
# TROUBLESHOOTING:
#   • If DLL name error: Check SecureCleanApiWaf.csproj for <AssemblyName>
#   • If port in use: Change docker-compose.yml port mapping
#   • If build fails: Ensure ./publish folder exists with compiled files
#   • For logs: docker-compose logs -f
#   • If Certificate errors: Use PRE-BUILT approach as shown
#   • If you need HTTPS: Configure at reverse proxy/load balancer level
#   • If you need to download your own certificates: Do it during local build step
#
# ================================================================================