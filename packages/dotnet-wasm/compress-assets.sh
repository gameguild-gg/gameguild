#!/bin/bash

# Script to compress all runtime assets for web delivery

set -e

echo "=== Compressing DotNet Runtime Assets ==="

# Check if gzip is available
if ! command -v gzip &> /dev/null; then
    echo "Error: gzip not found"
    exit 1
fi

cd "$(dirname "$0")"

# Compress managed assemblies
if [ -d "public/managed" ]; then
    echo "Compressing managed assemblies..."
    
    # Remove package.json that causes npm workspace conflicts
    rm -f public/managed/package.json
    
    find public/managed -name "*.dll" -exec sh -c 'gzip -9 -c "$1" > "$1.gz"' _ {} \;
    echo "✓ Managed assemblies compressed"
fi

# Compress dotnet runtime files
if [ -f "public/managed/dotnet.native.wasm" ]; then
    echo "Compressing dotnet.native.wasm..."
    gzip -9 -c public/managed/dotnet.native.wasm > public/managed/dotnet.native.wasm.gz
    echo "✓ dotnet.native.wasm compressed"
fi

if [ -f "public/managed/dotnet.js" ]; then
    echo "Compressing dotnet.js..."
    gzip -9 -c public/managed/dotnet.js > public/managed/dotnet.js.gz
    echo "✓ dotnet.js compressed"
fi

if [ -f "public/icudt.dat" ]; then
    echo "Compressing icudt.dat..."
    gzip -9 -c public/icudt.dat > public/icudt.dat.gz
    echo "✓ icudt.dat compressed"
fi

echo ""
echo "=== Compression Complete ==="
echo ""
echo "Compressed files are ready in public/ directory"
echo "Original files are preserved for local development"
