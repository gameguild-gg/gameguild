#!/bin/bash

# Create a minimal Blazor WASM project to get all necessary files
cd dotnet-runtime

echo "Creating Blazor WASM template..."
dotnet new blazorwasm-empty -n BlazorTemplate -o BlazorTemplate --force

cd BlazorTemplate

echo "Building Blazor template..."
dotnet publish -c Release -o ../blazor-output

cd ..

echo "Copying necessary files to public/managed..."
cp -v blazor-output/wwwroot/_framework/*.dll ../public/managed/ 2>/dev/null || true
cp -v blazor-output/wwwroot/_framework/*.wasm ../public/managed/ 2>/dev/null || true
cp -v blazor-output/wwwroot/_framework/*.js ../public/managed/ 2>/dev/null || true
cp -v blazor-output/wwwroot/_framework/blazor.boot.json ../public/managed/ 2>/dev/null || true

echo "Cleaning up..."
rm -rf BlazorTemplate blazor-output

echo "Done! Check public/managed/ for Blazor runtime files"
ls -lh ../public/managed/*.dll | head -20
