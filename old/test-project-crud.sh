#!/bin/bash

# Test script for Project CRUD operations
CMS_URL="http://localhost:5000"

echo "=== Testing Project CRUD Operations ==="
echo

# Test 1: Get projects (should be empty initially)
echo "1. Testing GET /projects (should return empty array)"
curl -X GET "$CMS_URL/projects" -H "Accept: application/json"
echo -e "\n"

# Test 2: Create a project
echo "2. Testing POST /projects (create project)"
PROJECT_RESPONSE=$(curl -s -X POST "$CMS_URL/projects" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Game Project",
    "description": "This is a test game project created via API",
    "shortDescription": "A test game project",
    "status": "Draft",
    "visibility": "Public",
    "websiteUrl": "https://testgame.example.com",
    "repositoryUrl": "https://github.com/test/game-repo",
    "socialLinks": ""
  }')

echo "$PROJECT_RESPONSE"
echo -e "\n"

# Extract project ID if successful
PROJECT_ID=$(echo "$PROJECT_RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)

if [ -n "$PROJECT_ID" ]; then
    echo "Project created with ID: $PROJECT_ID"
    
    # Test 3: Get projects again (should contain our new project)
    echo "3. Testing GET /projects (should now contain the created project)"
    curl -X GET "$CMS_URL/projects" -H "Accept: application/json"
    echo -e "\n"
    
    # Test 4: Get project by ID
    echo "4. Testing GET /projects/$PROJECT_ID"
    curl -X GET "$CMS_URL/projects/$PROJECT_ID" -H "Accept: application/json"
    echo -e "\n"
else
    echo "Failed to create project or extract project ID"
fi

echo "=== Test Complete ==="
