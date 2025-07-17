# Infrastructure Tests Organization

## 📁 Directory Structure

```
Infrastructure/
├── Pure/                           # Pure infrastructure tests (no business logic)
│   ├── PureGraphQLInfrastructureTests.cs      # GraphQL service registration tests
│   ├── PureGraphQLCQRSIntegrationTests.cs     # GraphQL + CQRS integration tests
│   ├── DatabaseInfrastructureTests.cs         # Database infrastructure tests
│   ├── MiddlewareInfrastructureTests.cs       # ASP.NET Core middleware tests
│   ├── BootstrapComponentTests.cs             # Step-by-step bootstrap tests
│   ├── InfrastructureArchitectureTests.cs     # Architecture validation tests
│   ├── PureTestModels.cs                      # Test models for pure tests
│   ├── PureTestQuery.cs                       # Test GraphQL query types
│   └── PureTestGraphQL.cs                     # Test GraphQL resolvers
├── Integration/                    # Integration tests with business modules
│   ├── GraphQLCQRSInfrastructureTests.cs      # CQRS integration with real modules
│   ├── CoreInfrastructureTests.cs             # Core infrastructure integration
│   └── MockModuleInfrastructureTests.cs       # Tests with mock modules
├── Connectivity/                   # Basic connectivity and validation tests
│   ├── GraphQLBasicConnectivityTests.cs       # Basic GraphQL endpoint tests
│   ├── SchemaIntrospectionTest.cs            # Schema validation tests
│   └── GraphQLServerValidationTest.cs        # Server configuration validation
└── Archive/                        # Deprecated or duplicate tests (to be removed)
    ├── GraphQLSchemaDebugTests.cs             # DUPLICATE - functionality covered in Pure/
    ├── GraphQLFieldDiscoveryTest.cs           # DUPLICATE - functionality covered in Pure/
    ├── GraphQLInfrastructureOnlyTests.cs      # DUPLICATE - replaced by Pure/
    └── [other duplicates]
```

## 🎯 Test Categories

### Pure Tests (`Pure/`)
- **Purpose**: Test infrastructure components in complete isolation
- **Characteristics**: No business modules, minimal dependencies, fast execution
- **Status**: ✅ All 60 tests passing

### Integration Tests (`Integration/`)
- **Purpose**: Test infrastructure with real business modules
- **Characteristics**: Uses real modules, tests full pipeline
- **Status**: ⚠️ Some failures due to missing services

### Connectivity Tests (`Connectivity/`)
- **Purpose**: Basic endpoint and connectivity validation
- **Characteristics**: Simple GET/POST tests, schema validation
- **Status**: ⚠️ Some configuration issues

## 🔧 Common Issues & Fixes Applied

### 1. GraphQL Query Type Registration
**Issue**: "The root type `Query` has already been registered"
**Fix**: Use single Query type registration per test

### 2. Missing Dependencies
**Issue**: `IAuthService` not registered
**Fix**: Add mock implementations or exclude auth modules in pure tests

### 3. ErrorMessage Handling Expectations
**Issue**: Tests expect 200 OK for GraphQL errors, but HotChocolate returns 400
**Fix**: Accept both 200 and 400 status codes for invalid queries

### 4. Namespace Consistency
**Issue**: Mixed namespaces between `GameGuild.Tests` and `GameGuild.API.Tests`
**Fix**: Standardized to `GameGuild.API.Tests.Infrastructure`

## 📊 Test Results Summary
- **Pure Tests**: 60/60 passing (100%)
- **Integration Tests**: ~20 failing (dependency issues)
- **Connectivity Tests**: ~10 failing (configuration issues)
- **Total Infrastructure Tests**: 155 total, 38 failing (75% success rate)

## 🗂️ Files to Remove (Duplicates)
The following files contain duplicate functionality and should be archived:
- `GraphQLSchemaDebugTests.cs` - covered by Pure tests
- `GraphQLFieldDiscoveryTest.cs` - covered by Pure tests  
- `GraphQLInfrastructureOnlyTests.cs` - replaced by Pure tests
- `GraphQLPureInfrastructureSchemaTests.cs` - merged into Pure tests
- `GraphQLInfrastructureArchitectureTests.cs` - merged into Pure tests
- `CoreGraphQLInfrastructureTests.cs` - duplicate functionality
- `GraphQLCQRSMockInfrastructureTests.cs` - overlaps with Integration tests
