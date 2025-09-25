# Testing the Enhanced Context Logging

This guide shows how to test and verify the enhanced context logging functionality.

## Quick Test Setup

1. **Start the API**:
   ```bash
   dotnet run --project apps/api/GameGuild.csproj
   ```

2. **Make authenticated requests** to see the logging in action.

## Expected Log Output

When you make requests to the API, you should see logs similar to these in your terminal:

### For REST API Requests:
```
[14:30:25 INF] 🔍 [CONTEXT] GET /api/projects | User: 12345678-1234-1234-1234-123456789012 (user@example.com) | Tenant: 87654321-4321-4321-4321-210987654321 | Auth: True
[14:30:25 INF] 🔐 [PERMISSIONS] User: 12345678-1234-1234-1234-123456789012 | SystemAdmin: False | TenantAdmin: True | Tenant: 87654321-4321-4321-4321-210987654321
[14:30:25 INF] 📋 [RESOURCE] User: 12345678-1234-1234-1234-123456789012 | No specific resource context
[14:30:25 INF] 🏢 [TENANT] User: 12345678-1234-1234-1234-123456789012 | TenantId: 87654321-4321-4321-4321-210987654321 | TenantName: Example Corp
[14:30:25 INF] 🎯 [REST] GET /api/projects | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | ResourceId:  | Permission: Read
[14:30:25 INF] ✅ [REST-ALLOWED] GET /api/projects | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | ResourceId:  | Permission: Read
```

### For GraphQL Requests:
```
[14:30:30 INF] 🔍 [CONTEXT] POST /graphql | User: 12345678-1234-1234-1234-123456789012 (user@example.com) | Tenant: 87654321-4321-4321-4321-210987654321 | Auth: True
[14:30:30 INF] 🔐 [PERMISSIONS] User: 12345678-1234-1234-1234-123456789012 | SystemAdmin: False | TenantAdmin: True | Tenant: 87654321-4321-4321-4321-210987654321
[14:30:30 INF] 🎯 [GRAPHQL] Operation: Query | Field: projects | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321
[14:30:30 INF] 🔐 [GRAPHQL-PERMISSION] Operation: Query | Field: projects | User: 12345678-1234-1234-1234-123456789012 | Checking: RequireTenantPermissionAttribute
[14:30:30 INF] 🏢 [TENANT-PERMISSION] Field: projects | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | Required: Read
[14:30:30 INF] ✅ [TENANT-ALLOWED] Field: projects | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | Permission: Read
[14:30:30 INF] ✅ [GRAPHQL-ALLOWED] Operation: Query | Field: projects | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | Permission: RequireTenantPermissionAttribute
```

### For Unauthenticated Requests:
```
[14:30:35 INF] 🔍 [CONTEXT] GET /api/projects | User: UNAUTHENTICATED | No user context
[14:30:35 WRN] 🚫 [REST-AUTH] GET /api/projects | User not authenticated or invalid UserId claim
```

### For Permission Denied:
```
[14:30:40 WRN] 🚫 [REST-DENIED] DELETE /api/projects/11111111-1111-1111-1111-111111111111 | User: 12345678-1234-1234-1234-123456789012 | Tenant: 87654321-4321-4321-4321-210987654321 | ResourceId: 11111111-1111-1111-1111-111111111111 | Missing: Delete
```

## Filtering Logs

You can filter the logs in your console or log aggregation system using the bracketed identifiers:

- `[CONTEXT]` - General request context
- `[PERMISSIONS]` - Permission context information  
- `[GRAPHQL]` - GraphQL operations
- `[REST]` - REST API operations
- `[TENANT-PERMISSION]` - Tenant-level permission checks
- `[CONTENT-TYPE-PERMISSION]` - Content-type permission checks
- `[RESOURCE-PERMISSION]` - Resource-level permission checks
- `ALLOWED` - Successful authorizations
- `DENIED` - Failed authorizations

## Using with Log Aggregation

If you're using a log aggregation system like ELK Stack, Splunk, or Azure Application Insights, these structured logs will be automatically parsed and indexed, making it easy to:

1. **Search by User**: Find all actions by a specific user
2. **Search by Tenant**: See all activity within a tenant
3. **Monitor Permissions**: Track permission usage and failures
4. **Debug Issues**: Quickly identify why a request was denied
5. **Performance Analysis**: Monitor which operations are slow

## Production Considerations

- The logs are set to **Information** level, so they will appear in production
- If the logs become too verbose for production, you can:
  - Adjust the log level in `appsettings.Production.json`
  - Use conditional logging based on environment
  - Filter specific log categories in your logging configuration

## Troubleshooting

If you don't see the expected logs:

1. **Check Log Level**: Ensure your logging configuration allows Information level logs
2. **Check Context Registration**: Verify that the context services are properly registered in DI
3. **Check Middleware Order**: Ensure ContextMiddleware is added after UseAuthentication()
4. **Check JWT Claims**: Verify that your JWT tokens contain the required claims (sub, tenantId)

The logging enhancement provides comprehensive visibility into your API's authentication and authorization flow, making debugging much easier!
