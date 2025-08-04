#!/bin/bash

echo "Testing CMS API endpoints..."
echo "=========================="

echo -e "\n1. Testing root endpoint:"
curl -v http://localhost:5000/ 2>&1 | head -10

echo -e "\n2. Testing /projects endpoint:"
curl -v http://localhost:5000/projects 2>&1 | head -10

echo -e "\n3. Testing /graphql endpoint:"
curl -v http://localhost:5000/graphql 2>&1 | head -10

echo -e "\n4. Testing Swagger endpoint:"
curl -v http://localhost:5000/swagger 2>&1 | head -10
