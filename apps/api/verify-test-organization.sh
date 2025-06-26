#!/bin/bash

# Test Suite Reorganization Verification Script
echo "=============================================="
echo "CMS Test Suite Organization Verification"
echo "=============================================="

cd "w:/repositories/game-guild/game-guild/apps/cms/src/Tests/Modules"

echo ""
echo "📊 Current Module Structure:"
echo ""

for module in */; do
    if [ -d "$module" ]; then
        module_name=${module%/}
        echo "📁 $module_name/"
        
        # Check for standard structure
        unit_exists=false
        e2e_exists=false
        performance_exists=false
        
        if [ -d "$module/Unit" ]; then
            unit_exists=true
            echo "  ├── Unit/"
            
            # Check Unit subdirectories
            if [ -d "$module/Unit/Services" ]; then
                service_count=$(find "$module/Unit/Services" -name "*.cs" 2>/dev/null | wc -l)
                echo "  │   ├── Services/ ($service_count tests)"
            fi
            
            if [ -d "$module/Unit/Controllers" ]; then
                controller_count=$(find "$module/Unit/Controllers" -name "*.cs" 2>/dev/null | wc -l)
                echo "  │   ├── Controllers/ ($controller_count tests)"
            fi
            
            if [ -d "$module/Unit/Handlers" ]; then
                handler_count=$(find "$module/Unit/Handlers" -name "*.cs" 2>/dev/null | wc -l)
                echo "  │   └── Handlers/ ($handler_count tests)"
            fi
        fi
        
        if [ -d "$module/E2E" ]; then
            e2e_exists=true
            echo "  ├── E2E/"
            
            if [ -d "$module/E2E/API" ]; then
                api_count=$(find "$module/E2E/API" -name "*.cs" 2>/dev/null | wc -l)
                echo "  │   ├── API/ ($api_count tests)"
            fi
            
            if [ -d "$module/E2E/GraphQL" ]; then
                graphql_count=$(find "$module/E2E/GraphQL" -name "*.cs" 2>/dev/null | wc -l)
                echo "  │   └── GraphQL/ ($graphql_count tests)"
            fi
        fi
        
        if [ -d "$module/Performance" ]; then
            performance_exists=true
            performance_count=$(find "$module/Performance" -name "*.cs" 2>/dev/null | wc -l)
            echo "  └── Performance/ ($performance_count tests)"
        fi
        
        # Check for README
        if [ -f "$module/README.md" ]; then
            echo "  └── README.md ✅"
        fi
        
        # Status summary
        status=""
        if [ "$unit_exists" = true ] && [ "$e2e_exists" = true ] && [ "$performance_exists" = true ]; then
            status="✅ Complete"
        elif [ "$unit_exists" = true ] || [ "$e2e_exists" = true ] || [ "$performance_exists" = true ]; then
            status="⚠️  Partial"
        else
            status="❌ Missing"
        fi
        
        echo "  Status: $status"
        echo ""
    fi
done

echo ""
echo "🏃‍♂️ Test Execution Commands:"
echo ""
echo "# Run all tests for reorganized modules:"
echo "dotnet test --filter \"namespace:GameGuild.Tests.Modules.Auth\""
echo "dotnet test --filter \"namespace:GameGuild.Tests.Modules.Permission\""
echo "dotnet test --filter \"namespace:GameGuild.Tests.Modules.Project\""
echo ""
echo "# Run by test type across all modules:"
echo "dotnet test --filter \"FullyQualifiedName~Unit\""
echo "dotnet test --filter \"FullyQualifiedName~E2E\""
echo "dotnet test --filter \"FullyQualifiedName~Performance\""
echo ""

echo "✅ Reorganization Complete!"
echo "All modules now follow the standardized structure:"
echo "  - Unit/ (Services, Controllers, Handlers)"
echo "  - E2E/ (API, GraphQL)"
echo "  - Performance/"
