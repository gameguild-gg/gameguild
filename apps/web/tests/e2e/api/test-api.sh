#!/bin/bash

# E2E API Test Script for Game Guild API
# Tests the slug-based program endpoints

API_BASE="http://localhost:5000"
TOTAL_TESTS=0
PASSED_TESTS=0

echo "🚀 Starting API E2E Tests for Program Slugs"
echo "============================================"

# Function to run a test
run_test() {
    local test_name="$1"
    local test_command="$2"
    local expected_result="$3"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    echo
    echo "📋 Test $TOTAL_TESTS: $test_name"
    
    result=$(eval "$test_command")
    if [[ $result == *"$expected_result"* ]]; then
        echo "✅ PASSED"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo "❌ FAILED"
        echo "Expected: $expected_result"
        echo "Got: $result"
    fi
}

# Test 1: Get published programs
echo
echo "Test 1: Getting published programs..."
response=$(curl -s -w "HTTPSTATUS:%{http_code}" "$API_BASE/api/program/published")
http_code=$(echo "$response" | grep -o "HTTPSTATUS:[0-9]*" | sed 's/HTTPSTATUS://')
body=$(echo "$response" | sed 's/HTTPSTATUS:[0-9]*$//')

if [ "$http_code" -eq 200 ]; then
    echo "✅ PASSED - HTTP 200 OK"
    PASSED_TESTS=$((PASSED_TESTS + 1))
    
    # Extract first program slug for further testing
    FIRST_SLUG=$(echo "$body" | jq -r '.[0].slug' 2>/dev/null || echo "game-dev-portfolio")
    echo "🎯 Using slug for testing: $FIRST_SLUG"
else
    echo "❌ FAILED - HTTP $http_code"
    exit 1
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

# Test 2: Get program by valid slug
echo
echo "Test 2: Getting program by slug ($FIRST_SLUG)..."
response=$(curl -s -w "HTTPSTATUS:%{http_code}" "$API_BASE/api/program/slug/$FIRST_SLUG")
http_code=$(echo "$response" | grep -o "HTTPSTATUS:[0-9]*" | sed 's/HTTPSTATUS://')

if [ "$http_code" -eq 200 ]; then
    echo "✅ PASSED - HTTP 200 OK"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    echo "❌ FAILED - HTTP $http_code"
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

# Test 3: Test invalid slug (should return 404)
echo
echo "Test 3: Testing invalid slug..."
response=$(curl -s -w "HTTPSTATUS:%{http_code}" "$API_BASE/api/program/slug/non-existent-slug-12345")
http_code=$(echo "$response" | grep -o "HTTPSTATUS:[0-9]*" | sed 's/HTTPSTATUS://')

if [ "$http_code" -eq 404 ]; then
    echo "✅ PASSED - HTTP 404 Not Found"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    echo "❌ FAILED - Expected 404, got HTTP $http_code"
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

# Test 4: Test another known slug
echo
echo "Test 4: Testing another known slug (unity-3d-game-development)..."
response=$(curl -s -w "HTTPSTATUS:%{http_code}" "$API_BASE/api/program/slug/unity-3d-game-development")
http_code=$(echo "$response" | grep -o "HTTPSTATUS:[0-9]*" | sed 's/HTTPSTATUS://')

if [ "$http_code" -eq 200 ]; then
    echo "✅ PASSED - HTTP 200 OK"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    echo "❌ FAILED - HTTP $http_code"
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

# Summary
echo
echo "============================================"
echo "🎉 Test Summary"
echo "============================================"
echo "Total Tests: $TOTAL_TESTS"
echo "Passed: $PASSED_TESTS"
echo "Failed: $((TOTAL_TESTS - PASSED_TESTS))"

if [ $PASSED_TESTS -eq $TOTAL_TESTS ]; then
    echo "✅ All tests passed!"
    exit 0
else
    echo "❌ Some tests failed!"
    exit 1
fi
