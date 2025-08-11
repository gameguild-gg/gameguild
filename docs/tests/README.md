# CMS Test Suite Organization Guide

## 🎯 Overview

This document defines the standardized organization pattern for all module tests in the Game Guild CMS. This structure
ensures consistency, maintainability, and comprehensive test coverage across all modules.

## 📁 Standardized Test Structure

Each module should follow this exact directory structure:

```
src/Tests/Modules/{ModuleName}/
├── Unit/                           # Unit tests for isolated components
│   ├── Models/                     # Model/Entity tests
│   ├── Services/                   # Service layer tests
│   ├── Controllers/                # Controller tests (if applicable)
│   ├── Handlers/                   # CQRS handler tests (if applicable)
│   └── Validators/                 # Validation logic tests (if applicable)
├── Integration/                    # Integration tests across components
│   ├── Services/                   # Service integration tests
│   ├── Controllers/                # Controller integration tests
│   ├── GraphQL/                    # GraphQL integration tests
│   └── Database/                   # Database integration tests
├── E2E/                           # End-to-end tests
│   ├── API/                       # REST API end-to-end tests
│   ├── GraphQL/                   # GraphQL end-to-end tests
│   └── Workflows/                 # Complete workflow tests
├── Performance/                    # Performance and load tests
│   ├── Services/                   # Service performance tests
│   ├── Database/                   # Database performance tests
│   └── API/                       # API performance tests
└── README.md                      # Module-specific test documentation
```

## 🏷️ Naming Conventions

### Test File Naming

- **Unit Tests**: `{ComponentName}Tests.cs`
  - Example: `ProjectServiceTests.cs`, `UserModelTests.cs`
- **Integration Tests**: `{ComponentName}IntegrationTests.cs`
  - Example: `ProjectServiceIntegrationTests.cs`
- **E2E Tests**: `{ComponentName}E2ETests.cs`
  - Example: `ProjectAPIE2ETests.cs`
- **Performance Tests**: `{ComponentName}PerformanceTests.cs`
  - Example: `ProjectServicePerformanceTests.cs`

### Test Method Naming

Follow the pattern: `{MethodUnderTest}_{Scenario}_{ExpectedResult}`

- Example: `CreateProject_WithValidData_ReturnsProject()`
- Example: `GetProject_WithInvalidId_ThrowsNotFoundException()`

## 🧪 Test Categories

### Unit Tests

- **Purpose**: Test individual components in isolation
- **Scope**: Single class or method
- **Dependencies**: Mocked/stubbed
- **Database**: In-memory or mocked
- **Focus**: Business logic, validation, edge cases

### Integration Tests

- **Purpose**: Test component interactions
- **Scope**: Multiple related components
- **Dependencies**: Real or test doubles
- **Database**: In-memory database
- **Focus**: Data flow, service integration, API contracts

### End-to-End Tests

- **Purpose**: Test complete user scenarios
- **Scope**: Full application stack
- **Dependencies**: All real (test environment)
- **Database**: Test database
- **Focus**: User workflows, API endpoints, business processes

### Performance Tests

- **Purpose**: Test performance characteristics
- **Scope**: Critical paths and bottlenecks
- **Dependencies**: Representative test data
- **Database**: Performance test database
- **Focus**: Response times, throughput, resource usage

## 📋 Test Coverage Requirements

Each module should aim for:

- **Unit Tests**: >90% code coverage
- **Integration Tests**: >80% of integration points
- **E2E Tests**: >70% of critical user workflows
- **Performance Tests**: 100% of performance-critical operations

## 🔧 Required Test Infrastructure

### Base Test Classes

- `UnitTestBase`: Common setup for unit tests
- `IntegrationTestBase`: Common setup for integration tests
- `E2ETestBase`: Common setup for E2E tests
- `PerformanceTestBase`: Common setup for performance tests

### Test Helpers

- `TestDataBuilder`: Consistent test data creation
- `DatabaseTestHelper`: Database setup and cleanup
- `AuthTestHelper`: Authentication and authorization setup
- `MockServiceProvider`: Service mocking utilities

### Test Fixtures

- `TestWebApplicationFactory`: Web application factory for integration/E2E tests
- `DatabaseFixture`: Database fixture for integration tests
- `PerformanceFixture`: Performance testing setup

## 📊 Module Test Status

| Module     | Unit | E2E | Performance | Status          |
|------------|------|-----|-------------|-----------------|
| Auth       | ✅    | ✅   | ✅           | **Reorganized** |
| Permission | ✅    | ✅   | ✅           | **Reorganized** |
| Project    | ✅    | ✅   | ✅           | **Reorganized** |
| User       | ❌    | ❌   | ❌           | Missing         |
| Tenant     | ❌    | ❌   | ❌           | Missing         |
| Content    | ❌    | ❌   | ❌           | Missing         |
| Resource   | ❌    | ❌   | ❌           | Missing         |
| Program    | ❌    | ❌   | ❌           | Missing         |
| Comment    | ❌    | ❌   | ❌           | Missing         |
| Rating     | ❌    | ❌   | ❌           | Missing         |
| Vote       | ❌    | ❌   | ❌           | Missing         |
| Feedback   | ❌    | ❌   | ❌           | Missing         |

Legend:

- ✅ Complete and follows standards
- **Reorganized** Recently reorganized to follow new standards
- ❌ Missing or incomplete

## 🎯 Implementation Priority

### Phase 1: Core Modules (High Priority)

1. Auth (reorganize existing)
2. User
3. Tenant
4. Permission (already complete)

### Phase 2: Content Modules (Medium Priority)

5. Project (reorganize existing)
6. Content
7. Resource
8. Program

### Phase 3: Feature Modules (Lower Priority)

9. Comment
10. Rating
11. Vote
12. Feedback
13. All remaining modules

## 🚀 Getting Started

### For New Modules

1. Create the standardized directory structure
2. Implement unit tests first
3. Add integration tests for key interactions
4. Add E2E tests for critical workflows
5. Add performance tests for bottlenecks
6. Document module-specific testing details in README.md

### For Existing Modules

1. Assess current test structure
2. Create migration plan to standard structure
3. Move existing tests to appropriate directories
4. Fill gaps in test coverage
5. Update test naming to follow conventions
6. Add missing test categories

## 📝 Test Documentation

Each module should include:

- `README.md` with module-specific test information
- Test coverage reports
- Performance benchmarks
- Known issues and workarounds
- Migration notes (for reorganized modules)

## 🔄 Continuous Improvement

- Regular test coverage analysis
- Performance regression testing
- Test maintenance and refactoring
- Documentation updates
- Best practice sharing across modules

---

This organization pattern ensures consistency, maintainability, and comprehensive coverage across all CMS modules.
