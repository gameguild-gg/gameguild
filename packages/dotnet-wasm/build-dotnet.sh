#!/bin/bash

# Build script for RoslynWrapper
# This script builds the .NET project for browser-wasm and prepares the runtime files

set -e

echo "=== Building RoslynWrapper for Browser WASM ==="

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found. Please install .NET 8 SDK."
    exit 1
fi

# Navigate to dotnet-runtime directory
cd "$(dirname "$0")/dotnet-runtime"

echo "Checking for wasm-tools workload..."
if ! dotnet workload list | grep -q "wasm-tools"; then
    echo "Installing wasm-tools workload..."
    dotnet workload install wasm-tools
    echo "✓ wasm-tools workload installed"
else
    echo "✓ wasm-tools workload already installed"
fi

echo "Restoring packages..."
dotnet restore

echo "Publishing for browser-wasm..."
dotnet publish -c Release -r browser-wasm

echo "Cleaning up npm workspace conflicts..."
find bin -name "package.json" -delete 2>/dev/null || true

echo "Copying all _framework contents recursively..."
mkdir -p ../public/managed
rm -rf ../public/managed/*
cp -rv bin/Release/net8.0/browser-wasm/AppBundle/_framework/* ../public/managed/

# Copy main.js from source
echo "Copying main.js..."
cp -v main.js ../public/managed/

echo "Final cleanup of npm workspace conflicts..."
find ../public/managed -name "package.json" -delete 2>/dev/null || true

echo ""
echo "=== Build Complete ==="
echo ""
echo "✓ All files copied to public/managed/"
echo "✓ Total files: $(ls -1 ../public/managed/ | wc -l)"
echo ""
echo "Next steps:"
echo "1. Run 'npm run compress' to compress assets"
echo "2. Run 'npm run build' to build TypeScript"
echo "3. Run 'npm run integrate' to copy to apps/web"
echo ""
echo "Or simply run 'npm run setup' to do all steps"
echo ""
