# Chat API Docker Setup Script (PowerShell)
# This script helps set up and run the Chat API with Docker

param(
    [Parameter(Position = 0)]
    [ValidateSet('dev', 'prod', 'stop', 'stop-prod', 'logs', 'rebuild', 'health', 'clean', 'help')]
    [string]$Command = 'help'
)

# Configuration
$PROJECT_NAME = "chat-api"
$COMPOSE_FILE = "docker-compose.yml"
$COMPOSE_PROD_FILE = "docker-compose.prod.yml"
$ENV_FILE = ".env"

# Functions
function Write-Header {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Blue
    Write-Host $Message -ForegroundColor Blue
    Write-Host "========================================`n" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

function Test-Docker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Error "Docker is not installed"
        Write-Host "Please install Docker Desktop from: https://www.docker.com/products/docker-desktop"
        exit 1
    }
    Write-Success "Docker is installed"
}

function Test-DockerCompose {
    if (-not (docker compose version 2>&1)) {
        Write-Error "Docker Compose is not available"
        exit 1
    }
    Write-Success "Docker Compose is available"
}

function Setup-Env {
    if (-not (Test-Path $ENV_FILE)) {
        Write-Info "Creating $ENV_FILE from .env.example"
        if (Test-Path ".env.example") {
            Copy-Item ".env.example" $ENV_FILE
            Write-Success "$ENV_FILE created"
            Write-Warning "Please update $ENV_FILE with your configuration"
        }
        else {
            Write-Error ".env.example not found"
            exit 1
        }
    }
    else {
        Write-Success "$ENV_FILE already exists"
    }
}

function Start-Development {
    Write-Header "Starting Development Environment"
    
    Write-Info "Pulling latest images..."
    & docker compose -f $COMPOSE_FILE pull
    
    Write-Info "Building and starting containers..."
    & docker compose -f $COMPOSE_FILE up -d
    
    Write-Info "Waiting for services to be ready..."
    Start-Sleep -Seconds 10
    
    # Check if API is running
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/swagger" -ErrorAction SilentlyContinue
        Write-Success "API is running at http://localhost:5000"
        Write-Success "Swagger UI available at http://localhost:5000/swagger"
    }
    catch {
        Write-Warning "API might still be starting, wait a moment and try accessing http://localhost:5000"
    }
    
    # Check admin UIs
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8081" -ErrorAction SilentlyContinue
        Write-Success "MongoDB Express available at http://localhost:8081"
    }
    catch {}
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8082" -ErrorAction SilentlyContinue
        Write-Success "Redis Commander available at http://localhost:8082"
    }
    catch {}
}

function Start-Production {
    Write-Header "Starting Production Environment"
    
    if (-not (Test-Path ".env")) {
        Write-Error "Please create .env file with production credentials"
        exit 1
    }
    
    Write-Info "Pulling latest images..."
    & docker compose -f $COMPOSE_PROD_FILE pull
    
    Write-Info "Building and starting containers..."
    & docker compose -f $COMPOSE_PROD_FILE up -d
    
    Write-Info "Waiting for services to be ready..."
    Start-Sleep -Seconds 10
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/swagger" -ErrorAction SilentlyContinue
        Write-Success "Production API is running at http://localhost:5000"
    }
    catch {
        Write-Warning "API might still be starting"
    }
}

function Stop-Services {
    Write-Header "Stopping Services"
    
    Write-Info "Stopping containers..."
    & docker compose -f $COMPOSE_FILE down
    
    Write-Success "Containers stopped"
}

function Stop-Production {
    Write-Header "Stopping Production Services"
    
    Write-Info "Stopping containers..."
    & docker compose -f $COMPOSE_PROD_FILE down
    
    Write-Success "Production containers stopped"
}

function View-Logs {
    Write-Header "Viewing Logs"
    Write-Info "Logs for API container (press Ctrl+C to exit):"
    & docker compose -f $COMPOSE_FILE logs -f api
}

function Rebuild-Images {
    Write-Header "Rebuilding Images"
    
    Write-Info "Rebuilding without cache..."
    & docker compose -f $COMPOSE_FILE build --no-cache
    
    Write-Success "Images rebuilt"
}

function Health-Check {
    Write-Header "Health Check"
    
    Write-Info "Checking services..."
    
    # Check API
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/swagger" -ErrorAction SilentlyContinue
        Write-Success "API is healthy"
    }
    catch {
        Write-Error "API is not responding"
    }
    
    # Check MongoDB
    try {
        $result = & docker exec chat-mongo mongosh --eval "db.runCommand('ping').ok" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "MongoDB is healthy"
        }
        else {
            Write-Error "MongoDB is not healthy"
        }
    }
    catch {
        Write-Error "Could not connect to MongoDB"
    }
    
    # Check Redis
    try {
        $result = & docker exec chat-redis redis-cli ping 2>&1
        if ($result -eq "PONG") {
            Write-Success "Redis is healthy"
        }
        else {
            Write-Error "Redis is not healthy"
        }
    }
    catch {
        Write-Error "Could not connect to Redis"
    }
}

function Clean-Volumes {
    $confirmation = Read-Host "This will delete all data! Are you sure? (yes/no)"
    
    if ($confirmation -eq "yes") {
        Write-Header "Cleaning Volumes"
        & docker compose -f $COMPOSE_FILE down -v
        Write-Success "Volumes deleted"
    }
    else {
        Write-Info "Cancelled"
    }
}

function Show-Usage {
    $usage = @"
Chat API Docker Setup

Usage: .\scripts\docker-setup.ps1 [-Command] <command>

Commands:
  dev              Start development environment (with admin UIs)
  prod             Start production environment
  stop             Stop all containers (development)
  stop-prod        Stop all containers (production)
  logs             View API logs
  rebuild          Rebuild Docker images
  health           Check service health
  clean            Delete all volumes and data
  help             Show this help message

Examples:
  .\scripts\docker-setup.ps1 dev
  .\scripts\docker-setup.ps1 prod
  .\scripts\docker-setup.ps1 logs
  .\scripts\docker-setup.ps1 health

For more information, visit: https://github.com/Mostafa-SAID7/Chat-Api
"@
    Write-Host $usage -ForegroundColor Cyan
}

# Main execution
function Main {
    Test-Docker
    Test-DockerCompose
    Setup-Env
    
    switch ($Command) {
        "dev" { Start-Development }
        "prod" { Start-Production }
        "stop" { Stop-Services }
        "stop-prod" { Stop-Production }
        "logs" { View-Logs }
        "rebuild" { Rebuild-Images }
        "health" { Health-Check }
        "clean" { Clean-Volumes }
        "help" { Show-Usage }
        default { Show-Usage }
    }
}

Main
