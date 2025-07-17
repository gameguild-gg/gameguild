# TestingLab Module Tests

This directory contains comprehensive tests for the TestingLab module, organized by test type and scope.

## Test Structure

### Unit Tests (`Unit/`)
- **Models/**: Tests for TestingLab model classes, data validation, and business logic
- **Services/**: Tests for TestingLab service layer functionality and business operations

### End-to-End Tests (`E2E/`)
- **API/**: Integration tests for REST API endpoints
- **GraphQL/**: Integration tests for GraphQL queries and mutations

### Performance Tests (`Performance/`)
- Load testing and performance benchmarks for TestingLab operations
- Scalability tests for high-volume scenarios

## Test Categories

### API Tests (`E2E/API/TestingControllerTests.cs`)
Tests HTTP endpoints for:
- Testing Request CRUD operations
- Testing Session management
- Participant registration and management
- Feedback collection
- Location management

### GraphQL Tests (`E2E/GraphQL/TestingLabGraphQLTests.cs`)
Tests GraphQL operations for:
- Query operations (testingRequests, testingSessions)
- Mutation operations (create, update, delete)
- Schema validation
- Complex nested queries

### Performance Tests (`Performance/TestingLabPerformanceTests.cs`)
Performance benchmarks for:
- Testing Request creation and retrieval
- Bulk operations with large datasets
- Search and filtering operations
- Concurrent operations
- Statistics generation

### Unit Tests
#### Models (`Unit/Models/TestingLabModelsTests.cs`)
- Entity property validation
- Enum value testing
- Business rule validation
- Model relationship testing

#### Services (`Unit/Services/TestServiceTests.cs`)
- Business logic validation
- Service method behavior
- Error handling
- Data transformation

## Test Data and Fixtures

### Common Test Utilities
- **TestWebApplicationFactory**: Provides isolated test environment with in-memory database
- **Permission Setup**: Helper methods for granting necessary permissions for tests
- **JWT Token Generation**: Authentication helpers for E2E tests
- **Data Seeding**: Methods for creating consistent test data

### Test Data Patterns
- Each test class uses isolated database instances to prevent cross-test contamination
- Permission setup follows the same pattern as working Project module tests
- Authentication uses proper JWT tokens with tenant context
- Test data cleanup is handled automatically via disposal patterns

## Running Tests

### All TestingLab Tests
```bash
dotnet test --filter "FullyQualifiedName~TestingLab"
```

### Specific Test Categories
```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName~TestingLab.Unit"

# E2E tests only
dotnet test --filter "FullyQualifiedName~TestingLab.E2E"

# Performance tests only
dotnet test --filter "FullyQualifiedName~TestingLab.Performance"

# API tests only
dotnet test --filter "FullyQualifiedName~TestingControllerTests"

# GraphQL tests only
dotnet test --filter "FullyQualifiedName~TestingLabGraphQLTests"
```

### Specific Test Methods
```bash
# Run specific test method
dotnet test --filter "MethodName~GetTestingRequests_ShouldReturnEmptyArray_WhenNoRequests"
```

## Test Environment

### Database
- In-memory SQLite database for isolation
- Unique database per test class to prevent interference
- Automatic cleanup after test completion

### Authentication
- JWT-based authentication using test factory services
- Proper tenant context setup
- Permission system integration

### Permissions
- Uses production permission service for realistic testing
- Tenant-level and content-type-level permissions
- Matches working Project module permission patterns

## Best Practices

### Test Organization
- Follow AAA pattern (Arrange, Act, Assert)
- Use descriptive test method names
- Group related tests in the same class
- Use theory tests for parameterized scenarios

### Data Management
- Create minimal test data required for each test
- Use unique identifiers to avoid conflicts
- Clean up resources in Dispose methods
- Isolate test data to prevent cross-test dependencies

### Assertions
- Use specific assertions over general ones
- Include meaningful error messages
- Test both success and failure scenarios
- Validate response structure and content

### Performance Testing
- Set realistic performance expectations
- Test with varying data sizes
- Include concurrent operation testing
- Monitor memory usage and cleanup

## Dependencies

### Required Services
- ApplicationDbContext (in-memory)
- IPermissionService
- IJwtTokenService
- TestService (TestingLab business logic)

### Test Frameworks
- xUnit for test execution
- Microsoft.AspNetCore.Mvc.Testing for integration testing
- Microsoft.EntityFrameworkCore.InMemory for database testing

## Troubleshooting

### Common Issues
1. **Permission Errors**: Ensure proper permission setup in test data seeding
2. **Authentication Failures**: Verify JWT token generation and headers
3. **Database Conflicts**: Check for proper test isolation and cleanup
4. **Service Dependencies**: Ensure all required services are registered in test factory

### Debugging Tips
- Use ITestOutputHelper for test debugging output
- Check test factory service registration
- Verify database state in failing tests
- Compare with working Project module test patterns
