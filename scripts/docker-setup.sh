#!/bin/bash

# Chat API Docker Setup Script
# This script helps set up and run the Chat API with Docker

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="chat-api"
COMPOSE_FILE="docker-compose.yml"
COMPOSE_PROD_FILE="docker-compose.prod.yml"
ENV_FILE=".env"

# Functions
print_header() {
    echo -e "\n${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}\n"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

check_docker() {
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed"
        echo "Please install Docker from: https://docs.docker.com/install/"
        exit 1
    fi
    print_success "Docker is installed"
}

check_docker_compose() {
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed"
        echo "Please install Docker Compose from: https://docs.docker.com/compose/install/"
        exit 1
    fi
    print_success "Docker Compose is installed"
}

setup_env() {
    if [ ! -f "$ENV_FILE" ]; then
        print_info "Creating $ENV_FILE from .env.example"
        if [ -f ".env.example" ]; then
            cp .env.example "$ENV_FILE"
            print_success "$ENV_FILE created"
            print_warning "Please update $ENV_FILE with your configuration"
        else
            print_error ".env.example not found"
            exit 1
        fi
    else
        print_success "$ENV_FILE already exists"
    fi
}

start_development() {
    print_header "Starting Development Environment"
    
    print_info "Pulling latest images..."
    docker-compose -f "$COMPOSE_FILE" pull
    
    print_info "Building and starting containers..."
    docker-compose -f "$COMPOSE_FILE" up -d
    
    # Wait for services to be ready
    print_info "Waiting for services to be ready..."
    sleep 10
    
    # Check if API is running
    if curl -s http://localhost:5000/swagger > /dev/null; then
        print_success "API is running at http://localhost:5000"
        print_success "Swagger UI available at http://localhost:5000/swagger"
    else
        print_warning "API might still be starting, wait a moment and try accessing http://localhost:5000"
    fi
    
    # Check admin UIs
    if curl -s http://localhost:8081 > /dev/null; then
        print_success "MongoDB Express available at http://localhost:8081"
    fi
    
    if curl -s http://localhost:8082 > /dev/null; then
        print_success "Redis Commander available at http://localhost:8082"
    fi
}

start_production() {
    print_header "Starting Production Environment"
    
    if [ ! -f ".env" ]; then
        print_error "Please create .env file with production credentials"
        exit 1
    fi
    
    print_info "Pulling latest images..."
    docker-compose -f "$COMPOSE_PROD_FILE" pull
    
    print_info "Building and starting containers..."
    docker-compose -f "$COMPOSE_PROD_FILE" up -d
    
    print_info "Waiting for services to be ready..."
    sleep 10
    
    if curl -s http://localhost:5000/swagger > /dev/null; then
        print_success "Production API is running at http://localhost:5000"
    else
        print_warning "API might still be starting"
    fi
}

stop_services() {
    print_header "Stopping Services"
    
    print_info "Stopping containers..."
    docker-compose -f "$COMPOSE_FILE" down
    
    print_success "Containers stopped"
}

stop_production() {
    print_header "Stopping Production Services"
    
    print_info "Stopping containers..."
    docker-compose -f "$COMPOSE_PROD_FILE" down
    
    print_success "Production containers stopped"
}

view_logs() {
    print_header "Viewing Logs"
    print_info "Logs for API container (press Ctrl+C to exit):"
    docker-compose -f "$COMPOSE_FILE" logs -f api
}

rebuild() {
    print_header "Rebuilding Images"
    
    print_info "Rebuilding without cache..."
    docker-compose -f "$COMPOSE_FILE" build --no-cache
    
    print_success "Images rebuilt"
}

health_check() {
    print_header "Health Check"
    
    print_info "Checking services..."
    
    # Check API
    if curl -s http://localhost:5000/swagger > /dev/null; then
        print_success "API is healthy"
    else
        print_error "API is not responding"
    fi
    
    # Check MongoDB
    if docker exec chat-mongo mongosh --eval "db.runCommand('ping').ok" &> /dev/null; then
        print_success "MongoDB is healthy"
    else
        print_error "MongoDB is not healthy"
    fi
    
    # Check Redis
    if docker exec chat-redis redis-cli ping > /dev/null; then
        print_success "Redis is healthy"
    else
        print_error "Redis is not healthy"
    fi
}

clean_volumes() {
    print_warning "This will delete all data!"
    read -p "Are you sure? (yes/no): " confirm
    
    if [ "$confirm" = "yes" ]; then
        print_header "Cleaning Volumes"
        docker-compose -f "$COMPOSE_FILE" down -v
        print_success "Volumes deleted"
    else
        print_info "Cancelled"
    fi
}

show_usage() {
    cat << EOF
${BLUE}Chat API Docker Setup${NC}

Usage: ./scripts/docker-setup.sh [COMMAND]

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
  ./scripts/docker-setup.sh dev
  ./scripts/docker-setup.sh prod
  ./scripts/docker-setup.sh logs
  ./scripts/docker-setup.sh health

For more information, visit: https://github.com/Mostafa-SAID7/Chat-Api/docs
EOF
}

# Main script
main() {
    check_docker
    check_docker_compose
    setup_env
    
    case "${1:-help}" in
        dev)
            start_development
            ;;
        prod)
            start_production
            ;;
        stop)
            stop_services
            ;;
        stop-prod)
            stop_production
            ;;
        logs)
            view_logs
            ;;
        rebuild)
            rebuild
            ;;
        health)
            health_check
            ;;
        clean)
            clean_volumes
            ;;
        help|--help|-h)
            show_usage
            ;;
        *)
            print_error "Unknown command: $1"
            show_usage
            exit 1
            ;;
    esac
}

# Run main function
main "$@"
