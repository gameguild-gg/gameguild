# @game-guild/client

Type-safe API client for the GameGuild platform with automatic code generation from OpenAPI/Swagger specifications.

## Features

- 🔄 **Automatic Code Generation** - Generate typed clients from OpenAPI spec
- ✅ **Runtime Validation** - Zod schemas for request/response validation
- 🔐 **Pluggable Authentication** - HTTP-only cookies, memory tokens, or hybrid
- 🏢 **Multi-Tenancy Support** - Automatic `X-Tenant-Id` header injection
- 🎯 **Type-Safe Errors** - Discriminated union errors with type guards
- 📦 **Tree-Shakeable** - Only import what you use
- ⚡ **Minimal Runtime Dependencies** - Only Zod required; React, Next.js optional

## Installation

```bash
pnpm add @game-guild/client
```

## Quick Start

### Basic Usage

```typescript
import { createClient } from '@game-guild/client';

const client = createClient({
  baseUrl: 'https://api.gameguild.gg',
});

// Use generated endpoints
const result = await client.users.getProfile('user-123');

if (result.ok) {
  console.log(result.data);
} else {
  console.error(result.error);
}
```

### With Authentication

```typescript
import { createClient } from '@game-guild/client';

const client = createClient({
  baseUrl: 'https://api.gameguild.gg',
  auth: {
    getAccessToken: async () => {
      // Return token from your auth provider
      return localStorage.getItem('accessToken');
    },
    onTokenRefresh: async (tokens) => {
      localStorage.setItem('accessToken', tokens.accessToken);
    },
    onAuthenticationRequired: async () => {
      // Redirect to login
      window.location.href = '/login';
    },
  },
});
```

### With Next.js & NextAuth

```typescript
import { createServerClient } from '@game-guild/client';
import { createNextAuthTokenProvider } from '@game-guild/client/next';
import { auth } from '@/auth';

const client = createServerClient({
  baseUrl: process.env.API_URL!,
  auth: createNextAuthTokenProvider({ auth }),
});

// Use in Server Components or Server Actions
export async function getProfile() {
  'use server';
  return client.users.getProfile('me');
}
```

## Code Generation

Generate typed client from your API's OpenAPI specification:

```bash
# Generate from running API
pnpm generate

# Or specify a custom spec URL
OPENAPI_URL=https://api.example.com/swagger/v1/swagger.json pnpm generate

# Force regeneration even if spec hasn't changed
pnpm generate -- --force
```

The generator creates:

- **TypeScript interfaces** - Compile-time type safety
- **Zod schemas** - Runtime validation
- **Endpoint modules** - Organized API methods
- **Error types** - Type-safe error handling

## Runtime Validation with Zod

All generated types include Zod schemas for runtime validation:

```typescript
import { Commerce_Payments_TaxJurisdictionDto, Commerce_Payments_TaxJurisdictionDtoSchema } from '@game-guild/client';

// Validate API response
const response = await fetch('/api/tax-jurisdictions/123');
const data = await response.json();

// Parse and validate (throws on invalid data)
const validated = Commerce_Payments_TaxJurisdictionDtoSchema.parse(data);

// Safe parse (returns result object)
const result = Commerce_Payments_TaxJurisdictionDtoSchema.safeParse(data);
if (result.success) {
  console.log('Valid:', result.data);
} else {
  console.error('Validation errors:', result.error.errors);
}

// Use in forms, API handlers, etc.
export async function createTaxJurisdiction(formData: FormData) {
  const data = {
    code: formData.get('code'),
    name: formData.get('name'),
    defaultRate: parseFloat(formData.get('defaultRate') as string),
  };

  // Validate before sending to API
  const validated = Commerce_Payments_TaxJurisdictionDtoSchema.parse(data);
  return client.taxJurisdictions.create(validated);
}
```

### Custom Validation Rules

Extend generated schemas with custom validation:

```typescript
import { z } from 'zod';
import { Commerce_Payments_TaxJurisdictionDtoSchema } from '@game-guild/client';

const CustomTaxJurisdictionSchema = Commerce_Payments_TaxJurisdictionDtoSchema.refine(
  (data) => {
    // Custom rule: active jurisdictions must have a name
    if (data.isActive && !data.name) {
      return false;
    }
    return true;
  },
  {
    message: 'Active tax jurisdictions must have a name',
    path: ['name'],
  },
).refine(
  (data) => {
    // Custom rule: rate must be between 0 and 100
    if (data.defaultRate !== undefined && (data.defaultRate < 0 || data.defaultRate > 100)) {
      return false;
    }
    return true;
  },
  {
    message: 'Default rate must be between 0 and 100',
    path: ['defaultRate'],
  },
);
```

For more examples, see [examples/zod-validation.ts](./examples/zod-validation.ts).

## Error Handling

The client uses a `Result<T, E>` pattern with type guards:

```typescript
import { isUnauthorized, isForbidden, isApiError } from '@game-guild/client';

const result = await client.users.getProfile('user-123');

if (!result.ok) {
  if (isUnauthorized(result.error)) {
    // Handle 401 - redirect to login
  } else if (isForbidden(result.error)) {
    // Handle 403 - show permission denied
    console.log('Missing permissions:', result.error.requiredPermissions);
  } else if (isApiError(result.error)) {
    // Handle other API errors
    console.error(result.error.message);
  }
}
```

## Multi-Tenancy

Configure tenant context for all requests:

```typescript
const client = createClient({
  baseUrl: 'https://api.gameguild.gg',
  tenant: {
    getTenantId: async () => {
      return getCurrentTenantId();
    },
  },
});
```

## Plugins

Extend functionality with plugins:

```typescript
import { createClient } from '@game-guild/client';
import { retry, logging, cache } from '@game-guild/client/plugins';

const client = createClient({
  baseUrl: 'https://api.gameguild.gg',
  plugins: [retry({ maxRetries: 3, backoff: 'exponential' }), logging({ level: 'debug' }), cache({ ttl: 60_000 })],
});
```

## License

UNLICENSED - Proprietary
