# GameGuild.Projects.UnitTests

This project contains comprehensive unit tests for the GameGuild Projects module.

## Test Coverage

### Entities
- **Project** - Core project entity tests including validation, property setting, and business logic
- **ProjectCategory** - Project categorization tests
- **ProjectCollaborator** - Project collaboration and permissions tests  
- **ProjectMetadata** - Key-value metadata storage tests
- **ProjectStatistics** - Project analytics and statistics tests

### Commands
- **CreateProjectCommand** - Project creation command tests
- **UpdateProjectCommand** - Project update command tests  
- **DeleteProjectCommand** - Project deletion command tests

### Queries  
- **GetAllProjectsQuery** - Project listing and filtering tests
- **GetProjectByIdQuery** - Single project retrieval tests
- **GetProjectBySlugQuery** - Project retrieval by URL slug tests

### Handlers
- **ProjectCommandHandlers** - Command processing logic tests including:
  - Project creation with slug generation
  - Project updates with permission checks
  - Project deletion (soft/hard delete)
  - Error handling and validation
  - Database interaction testing

- **ProjectQueryHandlers** - Query processing logic tests including:
  - Filtering by type, status, visibility
  - Search functionality
  - Pagination
  - Soft delete filtering

### Controllers
- **ProjectsController** - REST API endpoint tests including:
  - Authentication/authorization requirements
  - Parameter validation
  - HTTP status code verification
  - Error response handling

### Validation
- **Command Validators** - FluentValidation tests for:
  - Required field validation
  - String length constraints
  - Enum value validation
  - Business rule validation

## Test Infrastructure

### TestDataBuilder
Provides factory methods for creating test entities with realistic data:
- `CreateProject()` - Creates test projects with various configurations
- `CreateUser()` - Creates test users for ownership/collaboration
- `CreateProjectCategory()` - Creates test categories

### TestProjectsDbContext  
In-memory database context for isolated testing:
- Uses Entity Framework InMemory provider
- Proper entity configuration
- Automatic cleanup between tests

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ClassName=ProjectTests"

# Run tests in parallel
dotnet test --parallel
```

## Test Patterns

The tests follow these patterns:
- **AAA Pattern** - Arrange, Act, Assert for clear test structure
- **Theory/InlineData** - Parameterized tests for multiple scenarios
- **FluentAssertions** - Readable assertion syntax
- **Moq** - Mocking framework for dependencies
- **AutoFixture** - Automatic test data generation

## Dependencies

- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework  
- **AutoFixture** - Test data generation
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database for testing
- **FluentValidation.TestHelper** - Validation testing helpers

## Notes

- All tests use isolated in-memory databases for consistency
- Tests are designed to run in parallel safely
- Mock objects are used for external dependencies
- Test data is generated using AutoFixture for maintainability