#!/bin/bash

echo "Testing Project Module Integration"
echo "================================="

# Test 1: GET /api/projects (should return empty array initially)
echo "1. Testing GET /api/projects"
curl -s -X GET http://localhost:5001/api/projects \
  -H "Content-Type: application/json" | jq . || echo "No jq available, raw response:"
curl -s -X GET http://localhost:5001/api/projects \
  -H "Content-Type: application/json"

echo -e "\n"

# Test 2: POST /api/projects (create a new project)
echo "2. Testing POST /api/projects"
NEW_PROJECT=$(curl -s -X POST http://localhost:5001/api/projects \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Game Project",
    "description": "A test project created via API",
    "shortDescription": "Test project",
    "status": "Draft",
    "visibility": "Public",
    "websiteUrl": "https://testgame.com",
    "repositoryUrl": "https://github.com/test/game",
    "socialLinks": "https://twitter.com/testgame"
  }')

echo $NEW_PROJECT | jq . 2>/dev/null || echo $NEW_PROJECT

# Extract project ID for further tests
PROJECT_ID=$(echo $NEW_PROJECT | jq -r '.id' 2>/dev/null || echo "unknown")

echo -e "\n"

# Test 3: GET /api/projects (should now return the created project)
echo "3. Testing GET /api/projects (after creation)"
curl -s -X GET http://localhost:5001/api/projects \
  -H "Content-Type: application/json" | jq . 2>/dev/null || curl -s -X GET http://localhost:5001/api/projects

echo -e "\n"

# Test 4: GraphQL query
echo "4. Testing GraphQL query"
curl -s -X POST http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "{ projects { id title description status visibility createdAt } }"
  }' | jq . 2>/dev/null || curl -s -X POST http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ projects { id title description status visibility createdAt } }"}'

echo -e "\n"
echo "Integration test completed!"
