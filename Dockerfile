# Multi-stage build for optimal image size
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files
COPY ["apiContact/apiContact.csproj", "apiContact/"]

# Restore dependencies
RUN dotnet restore "apiContact/apiContact.csproj"

# Copy source code
COPY . .

# Build application
RUN dotnet build "apiContact/apiContact.csproj" -c Release -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish "apiContact/apiContact.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - minimal image
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Set working directory
WORKDIR /app

# Create non-root user for security
RUN useradd -m -u 1001 appuser

# Copy published app from build stage
COPY --from=publish /app/publish .

# Change ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Set environment
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run application
ENTRYPOINT ["dotnet", "apiContact.dll"]
