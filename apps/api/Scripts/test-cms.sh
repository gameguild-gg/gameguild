#!/bin/bash

echo "Starting CMS application..."
cd /w/repositories/game-guild/game-guild/apps/cms

# Start the application in background
dotnet run --no-build &
CMS_PID=$!

echo "CMS started with PID: $CMS_PID"

# Wait for the application to start
sleep 5

echo "Testing API endpoints..."

# Test the health of the API
echo "Testing /projects endpoint..."
curl -s -w "HTTP Status: %{http_code}\n" http://localhost:5000/projects

echo ""

# Test GraphQL endpoint
echo "Testing /graphql endpoint..."
curl -s -w "HTTP Status: %{http_code}\n" http://localhost:5000/graphql

echo ""

# Test Swagger endpoint
echo "Testing /swagger endpoint..."
curl -s -w "HTTP Status: %{http_code}\n" http://localhost:5000/swagger

echo ""

# Keep the script running for a moment to see logs
sleep 2

# Clean up
echo "Stopping CMS..."
kill $CMS_PID

echo "Done."
