#!/bin/bash

# Chat API - Replit Setup Script
# Run this to set up the development environment

set -e

echo "🚀 Chat API - Replit Setup"
echo "================================="
echo ""

# Check .NET installation
echo "✓ Checking .NET installation..."
dotnet --version

# Restore dependencies
echo "✓ Restoring NuGet packages..."
cd apiContact
dotnet restore

# Build project
echo "✓ Building project..."
dotnet build

echo ""
echo "================================="
echo "✅ Setup complete!"
echo ""
echo "To start the API, run:"
echo "  cd apiContact && dotnet run"
echo ""
echo "API will be available at:"
echo "  http://localhost:5000"
echo "  Swagger UI: http://localhost:5000/swagger"
echo ""
