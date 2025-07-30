#!/bin/bash

# Quick validation script for enhanced E2E tests
echo "🧪 Quick E2E Test Validation"
echo "============================"

# Check if test files exist
echo "📁 Checking test files..."

if [[ -f "e2e/integration/comprehensive-frontend-api.spec.ts" ]]; then
    echo "✅ comprehensive-frontend-api.spec.ts exists"
else
    echo "❌ comprehensive-frontend-api.spec.ts missing"
fi

if [[ -f "e2e/integration/real-world-frontend-api.spec.ts" ]]; then
    echo "✅ real-world-frontend-api.spec.ts exists"
else
    echo "❌ real-world-frontend-api.spec.ts missing"
fi

if [[ -f "e2e/integration/data-flow-validation.spec.ts" ]]; then
    echo "✅ data-flow-validation.spec.ts exists"
else
    echo "❌ data-flow-validation.spec.ts missing"
fi

# Check npm scripts
echo ""
echo "📋 Checking npm scripts..."
if grep -q "test:e2e:comprehensive" package.json; then
    echo "✅ test:e2e:comprehensive script exists"
else
    echo "❌ test:e2e:comprehensive script missing"
fi

if grep -q "test:e2e:data-flow" package.json; then
    echo "✅ test:e2e:data-flow script exists"
else
    echo "❌ test:e2e:data-flow script missing"
fi

# Count test scenarios
echo ""
echo "📊 Test Coverage Summary:"
total_tests=0

if [[ -f "e2e/integration/comprehensive-frontend-api.spec.ts" ]]; then
    comp_tests=$(grep -c "test(" e2e/integration/comprehensive-frontend-api.spec.ts)
    echo "🎯 Comprehensive Frontend-API: $comp_tests tests"
    total_tests=$((total_tests + comp_tests))
fi

if [[ -f "e2e/integration/real-world-frontend-api.spec.ts" ]]; then
    real_tests=$(grep -c "test(" e2e/integration/real-world-frontend-api.spec.ts)
    echo "🌍 Real-World Frontend-API: $real_tests tests"
    total_tests=$((total_tests + real_tests))
fi

if [[ -f "e2e/integration/data-flow-validation.spec.ts" ]]; then
    data_tests=$(grep -c "test(" e2e/integration/data-flow-validation.spec.ts)
    echo "🔄 Data Flow Validation: $data_tests tests"
    total_tests=$((total_tests + data_tests))
fi

echo ""
echo "🚀 Total Enhanced E2E Tests: $total_tests"

# Check documentation
echo ""
echo "📚 Documentation Files:"
if [[ -f "e2e/ENHANCED_TESTING_SUMMARY.md" ]]; then
    echo "✅ ENHANCED_TESTING_SUMMARY.md exists"
else
    echo "❌ ENHANCED_TESTING_SUMMARY.md missing"
fi

if [[ -f "e2e/run-comprehensive-tests.sh" ]]; then
    echo "✅ run-comprehensive-tests.sh exists"
else
    echo "❌ run-comprehensive-tests.sh missing"
fi

echo ""
echo "🎉 Enhanced E2E test setup validation complete!"
echo ""
echo "🔧 Available commands:"
echo "  npm run test:e2e:comprehensive - Run all enhanced tests"
echo "  npm run test:e2e:data-flow - Run data flow validation"
echo "  npm run test:e2e:real-world - Run real-world scenarios"
echo "  npm run test:e2e:integration - Run all integration tests"
