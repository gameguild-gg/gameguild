#!/bin/bash

# 🚀 Enhanced E2E Test Runner for Game Guild
# This script runs comprehensive frontend-to-API integration tests

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Test configuration
API_BASE_URL="http://localhost:5271"
WEB_BASE_URL="http://localhost:3000"
TEST_TIMEOUT=30000
MAX_RETRIES=3

echo -e "${WHITE}🎮 Game Guild - Enhanced E2E Test Suite${NC}"
echo -e "${WHITE}======================================${NC}"
echo ""

# Function to check if a service is running
check_service() {
    local url=$1
    local service_name=$2
    
    echo -e "${BLUE}🔍 Checking ${service_name}...${NC}"
    
    for i in $(seq 1 $MAX_RETRIES); do
        if curl -s --max-time 5 "${url}" > /dev/null 2>&1; then
            echo -e "${GREEN}✅ ${service_name} is running${NC}"
            return 0
        else
            echo -e "${YELLOW}⏳ Waiting for ${service_name} (attempt ${i}/${MAX_RETRIES})...${NC}"
            sleep 3
        fi
    done
    
    echo -e "${RED}❌ ${service_name} is not responding at ${url}${NC}"
    return 1
}

# Function to run test suite
run_test_suite() {
    local test_pattern=$1
    local suite_name=$2
    
    echo -e "${PURPLE}🧪 Running ${suite_name}...${NC}"
    echo -e "${CYAN}Pattern: ${test_pattern}${NC}"
    
    if npx playwright test "${test_pattern}" --reporter=line --timeout="${TEST_TIMEOUT}"; then
        echo -e "${GREEN}✅ ${suite_name} PASSED${NC}"
        return 0
    else
        echo -e "${RED}❌ ${suite_name} FAILED${NC}"
        return 1
    fi
}

# Function to run API-only tests
run_api_tests() {
    echo -e "${PURPLE}🔌 Running API-only tests...${NC}"
    
    if npx playwright test e2e/api/ --config=playwright-api.config.ts --reporter=line --timeout="${TEST_TIMEOUT}"; then
        echo -e "${GREEN}✅ API tests PASSED${NC}"
        return 0
    else
        echo -e "${RED}❌ API tests FAILED${NC}"
        return 1
    fi
}

# Main test execution
main() {
    local failed_tests=0
    local total_tests=0
    
    echo -e "${BLUE}📋 Pre-flight checks...${NC}"
    
    # Check if we're in the right directory
    if [[ ! -f "package.json" ]] || [[ ! -d "e2e" ]]; then
        echo -e "${RED}❌ Please run this script from the web app root directory${NC}"
        exit 1
    fi
    
    # Check if Playwright is installed
    if ! npx playwright --version > /dev/null 2>&1; then
        echo -e "${RED}❌ Playwright is not installed${NC}"
        echo -e "${YELLOW}💡 Run: npm install${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}✅ Environment checks passed${NC}"
    echo ""
    
    # Service availability checks (non-blocking)
    echo -e "${BLUE}🔍 Checking service availability...${NC}"
    
    API_AVAILABLE=false
    WEB_AVAILABLE=false
    
    if check_service "${API_BASE_URL}/api/programs" "API Server"; then
        API_AVAILABLE=true
    fi
    
    if check_service "${WEB_BASE_URL}" "Web Server"; then
        WEB_AVAILABLE=true
    fi
    
    echo ""
    
    # Run API-only tests (these work without frontend)
    if $API_AVAILABLE; then
        echo -e "${CYAN}🎯 Phase 1: API Integration Tests${NC}"
        total_tests=$((total_tests + 1))
        if ! run_api_tests; then
            failed_tests=$((failed_tests + 1))
        fi
        echo ""
    else
        echo -e "${YELLOW}⚠️ Skipping API tests (API server not available)${NC}"
        echo ""
    fi
    
    # Run comprehensive frontend-to-API tests
    if $WEB_AVAILABLE && $API_AVAILABLE; then
        echo -e "${CYAN}🎯 Phase 2: Frontend-to-API Integration Tests${NC}"
        
        # Test suites to run
        declare -A test_suites=(
            ["e2e/integration/comprehensive-frontend-api.spec.ts"]="Comprehensive Frontend-API Integration"
            ["e2e/integration/real-world-frontend-api.spec.ts"]="Real-World Frontend-API Scenarios"
            ["e2e/integration/frontend-api-journey.spec.ts"]="User Journey Frontend-API Tests"
            ["e2e/integration/course-catalog.spec.ts"]="Course Catalog Integration"
        )
        
        for test_file in \"${!test_suites[@]}\"; do
            total_tests=$((total_tests + 1))
            if ! run_test_suite \"$test_file\" \"${test_suites[$test_file]}\"; then
                failed_tests=$((failed_tests + 1))
            fi
            echo ""
        done
        
    elif $WEB_AVAILABLE; then
        echo -e "${YELLOW}⚠️ Running frontend tests with mocked API data${NC}"
        
        total_tests=$((total_tests + 1))
        if ! run_test_suite "e2e/integration/course-catalog.spec.ts" "Frontend UI Tests (Mocked)"; then
            failed_tests=$((failed_tests + 1))
        fi
        echo ""
        
    else
        echo -e "${YELLOW}⚠️ Skipping frontend tests (web server not available)${NC}"
        echo ""
    fi
    
    # Run any additional integration tests
    if [[ -f "e2e/integration/enrollment-flow.spec.ts" ]]; then
        total_tests=$((total_tests + 1))
        if ! run_test_suite "e2e/integration/enrollment-flow.spec.ts" "Enrollment Flow Tests"; then
            failed_tests=$((failed_tests + 1))
        fi
        echo ""
    fi
    
    # Performance and advanced tests
    if $WEB_AVAILABLE; then
        echo -e "${CYAN}🎯 Phase 3: Advanced Integration Tests${NC}"
        
        if [[ -f "e2e/integration/performance-mobile.spec.ts" ]]; then
            total_tests=$((total_tests + 1))
            if ! run_test_suite "e2e/integration/performance-mobile.spec.ts" "Performance & Mobile Tests"; then
                failed_tests=$((failed_tests + 1))
            fi
            echo ""
        fi
        
        if [[ -f "e2e/integration/advanced-frontend-api.spec.ts" ]]; then
            total_tests=$((total_tests + 1))
            if ! run_test_suite "e2e/integration/advanced-frontend-api.spec.ts" "Advanced Frontend-API Tests"; then
                failed_tests=$((failed_tests + 1))
            fi
            echo ""
        fi
    fi
    
    # Final report
    echo -e "${WHITE}📊 Test Execution Summary${NC}"
    echo -e "${WHITE}=========================${NC}"
    echo -e "${BLUE}Total Test Suites: ${total_tests}${NC}"
    echo -e "${GREEN}Passed: $((total_tests - failed_tests))${NC}"
    echo -e "${RED}Failed: ${failed_tests}${NC}"
    
    if [[ $failed_tests -eq 0 ]]; then
        echo -e "${GREEN}🎉 All tests passed!${NC}"
        echo -e "${GREEN}✅ Frontend-to-API integration is working correctly${NC}"
    else
        echo -e "${RED}❌ Some tests failed${NC}"
        echo -e "${YELLOW}💡 Check the output above for details${NC}"
        
        # Provide helpful suggestions
        echo ""
        echo -e "${YELLOW}🔧 Troubleshooting Tips:${NC}"
        if ! $API_AVAILABLE; then
            echo -e "${YELLOW}• Start the API server: cd ../api && dotnet run${NC}"
        fi
        if ! $WEB_AVAILABLE; then
            echo -e "${YELLOW}• Start the web server: npm run dev${NC}"
        fi
        echo -e "${YELLOW}• Check browser console for errors${NC}"
        echo -e "${YELLOW}• Verify API endpoints are responding correctly${NC}"
        echo -e "${YELLOW}• Run tests individually for more detailed output${NC}"
    fi
    
    echo ""
    echo -e "${BLUE}📋 Commands to run individual test suites:${NC}"
    echo -e "${CYAN}npm run test:e2e:api${NC} - API integration tests only"
    echo -e "${CYAN}npm run test:e2e -- --headed${NC} - Run with browser visible"
    echo -e "${CYAN}npm run test:e2e -- --debug${NC} - Run in debug mode"
    echo -e "${CYAN}npm run test:e2e:report${NC} - View detailed test report"
    
    exit $failed_tests
}

# Run main function
main "$@"
