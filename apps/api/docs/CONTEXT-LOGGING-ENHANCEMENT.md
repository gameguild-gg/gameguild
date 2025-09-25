# Context Logging Enhancement

This document describes the comprehensive logging enhancement implemented for the GameGuild API to improve debugging capabilities for authentication, authorization, and permission checks across both REST and GraphQL endpoints.

## Overview

The enhancement adds detailed logging to capture user, tenant, permissions, and target resource information for all API requests, making it much easier to debug authentication and authorization issues.

## Enhanced Components

### 1. ContextMiddleware

**Location**: `Source/Modules/Authorization/Middleware/ContextMiddleware.cs`

**Enhancements**:
- **Information Level Logging**: Changed from Debug to Information level to ensure visibility in production logs
- **Comprehensive Context Logging**: Added structured logging with emojis for easy visual identification
- **Request Path Tracking**: Includes HTTP method and path in all log entries
- **Detailed Context Breakdown**: Separate log entries for different context types

**Log Patterns**:
```
🔍 [CONTEXT] GET /api/projects | User: 12345678-1234-1234-1234-123456789012 (user@example.com) | Tenant: 87654321-4321-4321-4321-210987654321 | Auth: True
🔐 [PERMISSIONS] User: 12345678-1234-1234-1234-123456789012 | SystemAdmin: False | TenantAdmin: True | Tenant: 87654321-4321-4321-4321-210987654321
📋 [RESOURCE] User: 12345678-1234-1234-1234-123456789012 | ResourceId: 11111111-1111-1111-1111-111111111111 | ResourceType: Project | Identifier: project-123
🏢 [TENANT] User: 12345678-1234-1234-1234-123456789012 | TenantId: 87654321-4321-4321-4321-210987654321 | TenantName: Example Corp
```

### 2. DACAuthorizationMiddleware (GraphQL)

**Location**: `Source/Modules/Authorization/DACAuthorizationMiddleware.cs`

**Enhancements**:
- **Operation Tracking**: Logs GraphQL operation type (Query/Mutation) and field name
- **Permission Check Logging**: Detailed logging for each permission check type
- **Success/Failure Logging**: Clear indicators for allowed/denied operations
- **Attribute Type Identification**: Shows which specific permission attribute was checked

**Log Patterns**:
```
🎯 [GRAPHQL] Operation: Query | Field: projects | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321
🔐 [GRAPHQL-PERMISSION] Operation: Query | Field: projects | User: 12345678-1234-1234-1234-123456789012 | Checking: RequireTenantPermissionAttribute
✅ [GRAPHQL-ALLOWED] Operation: Query | Field: projects | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | Permission: RequireTenantPermissionAttribute
🚫 [GRAPHQL-DENIED] Operation: Mutation | Field: deleteProject | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | Missing: RequireResourcePermissionAttribute
```

**Permission Type Specific Logging**:
- **Tenant Permissions**: `🏢 [TENANT-PERMISSION]`, `✅ [TENANT-ALLOWED]`, `🚫 [TENANT-DENIED]`
- **Content-Type Permissions**: `📝 [CONTENT-TYPE-PERMISSION]`, `✅ [CONTENT-TYPE-ALLOWED]`, `🚫 [CONTENT-TYPE-DENIED]`
- **Resource Permissions**: `📋 [RESOURCE-PERMISSION]`, `✅ [RESOURCE-ALLOWED]`, `🚫 [RESOURCE-DENIED]`

### 3. RequireDacPermissionAttribute (REST API)

**Location**: `Source/Modules/Authorization/RequireDacPermissionAttribute.cs`

**Enhancements**:
- **Request Context Logging**: Includes HTTP method and path for REST endpoints
- **User/Tenant/Resource Tracking**: Shows all relevant IDs for each request
- **Permission Resolution Logging**: Detailed logging for permission checks
- **Owner Override Logging**: Special logging when owner override is used

**Log Patterns**:
```
🎯 [REST] GET /api/projects/11111111-1111-1111-1111-111111111111 | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | ResourceId: 11111111-1111-1111-1111-111111111111 | Permission: Read
✅ [REST-ALLOWED] GET /api/projects/11111111-1111-1111-1111-111111111111 | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | ResourceId: 11111111-1111-1111-1111-111111111111 | Permission: Read
🚫 [REST-DENIED] DELETE /api/projects/11111111-1111-1111-1111-111111111111 | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | ResourceId: 11111111-1111-1111-1111-111111111111 | Missing: Delete
```

## Pipeline Integration

The ContextMiddleware has been added to the application pipeline in `ConfigureCommonPipeline()` method:

```csharp
app.UseAuthentication();

// Add context middleware to extract and log user/tenant/permission context
app.UseContextMiddleware();

app.UseAuthorization();
```

This ensures that context logging occurs for all authenticated requests, providing comprehensive visibility into the authentication and authorization flow.

## Log Format Features

### Emoji Indicators
- 🔍 **[CONTEXT]**: General request context information
- 🔐 **[PERMISSIONS]**: Permission-related logging
- 📋 **[RESOURCE]**: Resource context information
- 🏢 **[TENANT]**: Tenant-specific information
- 🎯 **[GRAPHQL/REST]**: Operation-specific logging
- ✅ **[*-ALLOWED]**: Successful authorization
- 🚫 **[*-DENIED]**: Failed authorization
- ❌ **[*-ERROR]**: Error conditions

### Structured Data
All logs include structured data with consistent field names:
- `Method`: HTTP method (GET, POST, etc.)
- `Path`: Request path
- `UserId`: User identifier
- `TenantId`: Tenant identifier  
- `ResourceId`: Resource identifier (when applicable)
- `Permission`: Required permission type
- `Operation`: GraphQL operation type
- `FieldName`: GraphQL field name

## Benefits

1. **Easy Debugging**: Visual indicators and structured logs make it easy to trace authentication flows
2. **Complete Context**: Every log entry includes all relevant context information
3. **Performance Monitoring**: Can identify slow permission checks and authorization bottlenecks
4. **Security Auditing**: Comprehensive audit trail of all authorization decisions
5. **Troubleshooting**: Quick identification of permission misconfigurations

## Usage

The logging is automatically enabled and will appear in your application logs at the Information level. No additional configuration is required beyond the standard Serilog setup already in place.

Filter logs by the bracketed identifiers (e.g., `[CONTEXT]`, `[GRAPHQL]`, `[REST]`) to focus on specific areas of interest during debugging.
