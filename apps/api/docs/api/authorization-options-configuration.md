# Authorization options configuration

The API host reads static policy definitions from the `Authorization` section. Database-backed policies remain owned by the Identity Authorization module and must not be redefined as static policies.

```json
{
  "Authorization": {
    "DefaultPolicy": "SignedIn",
    "RequireAuthenticatedUser": true,
    "Policies": {
      "ManageProjects": {
        "RequireAuthenticatedUser": true,
        "Roles": ["ProjectManager"],
        "Claims": [
          { "Type": "tenant_id", "AllowedValues": ["current-tenant"] }
        ],
        "AuthenticationSchemes": ["Bearer", "EnterpriseOidc"]
      }
    },
    "RoleHierarchy": {
      "PlatformAdmin": ["ProjectManager"],
      "ProjectManager": ["ProjectEditor"]
    },
    "ClaimTransformations": [
      {
        "SourceClaimType": "groups",
        "TargetClaimType": "role",
        "ValueMappings": {
          "gameguild-project-admins": "ProjectManager"
        }
      }
    ]
  }
}
```

Each configured policy combines its requirements: it can require an authenticated principal, one of its configured roles, every configured claim, and one or more authentication schemes. Role hierarchies expand accepted roles transitively. The options validator rejects empty policy requirements, invalid claim definitions, role hierarchy cycles, and incomplete claim transformations during host setup.

Claim transformations run after authentication and before authorization. They only add claims when the source value has an explicit mapping; unmapped values are ignored and repeated transformation calls do not duplicate claims. Treat mappings as trusted security configuration: a mapping into a role or permission claim can grant access to matching policies.

Dynamic ABAC and resource checks, policy caching and invalidation, and authorization decision audit events remain implemented by the Identity Authorization module. Conditional policies support location conditions, while environment requirements enforce configured IP ranges, time windows, device types, and HTTPS. Tenant context and conditional policy inputs are resolved there at evaluation time. Configure cache behavior through `Authorization:Cache` and tenant behavior through `Tenancy`; the API host does not create a second cache or policy store.

Authentication provider registration and credential settings are owned by the Authentication module. A policy can name any scheme registered by the host in `AuthenticationSchemes`, including multiple schemes. OAuth, OIDC, and SAML provider setup, callbacks, secrets, and identity provisioning remain configured in the Authentication module rather than in authorization policy definitions.

## Migration

Bind and validate the `Authorization` section with `AuthorizationOptionsBuilder.Build(configuration)` or pass it to `SetupAuthorization`. Existing built-in policies and database-backed tenant policies keep their current names and sources. Move only host-owned static policies into this section; continue to declare resource checks with the Identity Authorization permission attributes/services so resource identifiers are resolved from the request and checked against the permission store.

When adding an external provider, register its authentication scheme through the host's authentication configuration, then list that scheme in the policy's `AuthenticationSchemes`. Keep provider credentials in the application's secret provider, not in the `Authorization` section. Claim mappings are opt-in and allowlisted; review any mapping to a role or permission claim as an access grant.
