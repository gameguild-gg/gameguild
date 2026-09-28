# GraphQL authorization

The GraphQL endpoint is disabled by default. Enable it under `PresentationLayer`:

```json
{
  "PresentationLayer": {
    "EnableGraphQL": true,
    "GraphQL": {
      "EnableGraphQL": true,
      "Endpoint": "/graphql"
    }
  }
}
```

The API refuses to register GraphQL when HTTP authentication or authorization is
disabled. The mapped endpoint also requires an authenticated caller. GraphQL stays
disabled unless either the presentation-layer switch or the nested GraphQL option is
enabled. The endpoint must be an absolute application path without a query or fragment.

## Current schema rules

The API registers the Projects query and mutation extensions, the computed project
permission fields, and an explicit `Project` output type. The explicit output type is an
allowlist; persistence-only data such as tenant navigation objects and domain-event
collections is not reflected into the schema. TestingLab GraphQL resolvers are not
registered until their resource-level authorization is reviewed.

Hot Chocolate's `[Authorize]` attribute supports authenticated fields and named ASP.NET
authorization policies. Project DAC checks use `RequireGraphQLProjectPermission`:

```csharp
[RequireGraphQLProjectPermission(
    PermissionType.Edit,
    ResourceIdArgumentName = "input.projectId")]
public Task<Project> UpdateProject(UpdateProjectInput input, ...)
```

The attribute supports `All` or `Any` composition, dotted paths into GraphQL input
objects, resource IDs from a parent object for nested fields, and conditional permission
selection from a boolean argument. The permission service receives the actor and tenant
from the authenticated request context; callers cannot choose either value in GraphQL
input. Repeated checks for the same project and permission share an execution-request
cache. Authorization failures return a generic GraphQL error with a stable error code;
the middleware logs denial and evaluation failures without returning resource details.

Use `[RequireGraphQLProjectPermission]` on resolvers that need DAC enforcement. Root
query authorization does not replace resource checks: query handlers must continue to
apply their normal tenant and project access filters.

## Remaining acceptance work

This is an initial framework slice, not completion of issue #335. The remaining work
includes permission-aware introspection and schema filtering, dynamic field masking,
field permission inheritance for additional entity types, durable authorization-denial
audit events, and broader resource-level integration coverage. Schema versioning by
permission level and testing/debugging utilities also remain open. Keep the issue open
until those criteria have code, tests, and operational evidence.

Focused tests:

```powershell
dotnet test apps/api/tests/GameGuild.API.UnitTests/GameGuild.API.UnitTests.csproj --filter FullyQualifiedName~GraphQL
```
