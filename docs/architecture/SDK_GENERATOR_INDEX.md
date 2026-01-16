# GameGuild TypeScript SDK Generator - Design Index

**Version:** 1.0.0  
**Date:** January 15, 2026  
**Status:** Design Complete - Ready for Implementation

---

## Document Overview

This design specification is split into 4 parts for manageability:

| Part | Document | Contents |
|------|----------|----------|
| 1 | [SDK_GENERATOR_DESIGN.md](./SDK_GENERATOR_DESIGN.md) | Executive Summary, SDK Product Design, Package Structure |
| 2 | [SDK_GENERATOR_DESIGN_PART2.md](./SDK_GENERATOR_DESIGN_PART2.md) | Generation Pipeline, Authentication Support, Authorization Support |
| 3 | [SDK_GENERATOR_DESIGN_PART3.md](./SDK_GENERATOR_DESIGN_PART3.md) | Features/Entitlements, Multi-Tenancy, Error Model, Security Review |
| 4 | [SDK_GENERATOR_DESIGN_PART4.md](./SDK_GENERATOR_DESIGN_PART4.md) | CI/CD Automation, Test Plan, Implementation Roadmap, Final Report |

---

## Quick Navigation

### Part 1: Foundation
1. [Executive Summary](./SDK_GENERATOR_DESIGN.md#1-executive-summary)
2. [SDK Product Design](./SDK_GENERATOR_DESIGN.md#2-sdk-product-design)
   - Client Instantiation
   - Using Generated Endpoints
   - Authentication Configuration
   - Tenant Configuration
   - Feature Flags Integration
3. [Package Structure](./SDK_GENERATOR_DESIGN.md#3-package-structure)
   - Directory Layout
   - Generated vs Handwritten Separation
   - Package.json Configuration
   - Build Configuration (tsup)

### Part 2: Core Systems
4. [Generation Pipeline](./SDK_GENERATOR_DESIGN_PART2.md#4-generation-pipeline)
   - Fetch OpenAPI Specification
   - Normalize Specification
   - Code Generation (Types, Endpoints)
   - Post-Processing
   - Breaking Change Detection
5. [Authentication Support](./SDK_GENERATOR_DESIGN_PART2.md#5-authentication-support)
   - Token Provider Interface
   - Token Refresh Implementation
   - NextAuth Integration
   - SSR-Safe Token Handling
6. [Authorization Support](./SDK_GENERATOR_DESIGN_PART2.md#6-authorization-support)
   - Authorization Error Types
   - Type Guards
   - Authorization Helpers

### Part 3: Advanced Features
7. [Features/Entitlements Support](./SDK_GENERATOR_DESIGN_PART3.md#7-featuresentitlements-support)
   - Feature Client Interface
   - Feature Client Implementation
   - Feature Cache
   - React Hook for Features
8. [Multi-Tenancy Support](./SDK_GENERATOR_DESIGN_PART3.md#8-multi-tenancy-support)
   - Tenant Provider Interface
   - Tenant Provider Implementation
   - Tenant Header Injection
9. [Error Model](./SDK_GENERATOR_DESIGN_PART3.md#9-error-model)
   - Unified API Error Type
   - Error Transformation
   - Result Type
10. [Security Review](./SDK_GENERATOR_DESIGN_PART3.md#10-security-review)
    - Identified Risks and Mitigations
    - Safe Logging Implementation
    - SSR Safety Guidelines
    - Rate Limiting and Retry Plugin
    - Idempotency Support

### Part 4: Delivery
11. [CI/CD Automation](./SDK_GENERATOR_DESIGN_PART4.md#11-cicd-automation)
    - GitHub Actions Workflow
    - Versioning Strategy
    - Snapshot Testing
12. [Test Plan](./SDK_GENERATOR_DESIGN_PART4.md#12-test-plan)
    - Unit Tests
    - Integration Tests
    - E2E Tests with Next.js
    - Type Tests
13. [Implementation Roadmap](./SDK_GENERATOR_DESIGN_PART4.md#13-implementation-roadmap)
    - Phase 1: Foundation (Weeks 1-2)
    - Phase 2: Core Features (Weeks 3-4)
    - Phase 3: Advanced Features (Weeks 5-6)
    - Phase 4: Polish & Release (Weeks 7-8)
14. [Definition of Done](./SDK_GENERATOR_DESIGN_PART4.md#14-definition-of-done)
15. [Final Report](./SDK_GENERATOR_DESIGN_PART4.md#15-final-report)

---

## Key Interfaces Summary

### Client Creation

```typescript
import { createClient, createServerClient } from '@gameguild/api-client';

// Browser client
const client = createClient({
  baseUrl: 'https://api.gameguild.com',
  auth: { mode: 'bearer', tokenProvider: myTokenProvider },
  tenant: { tenantId: 'acme-corp' },
});

// Server client (Next.js)
const client = await createServerClient({
  baseUrl: process.env.API_URL!,
  auth: { tokenProvider: createNextAuthTokenProvider({ auth }) },
  tenant: { resolver: async () => (await auth())?.currentTenant?.id },
});
```

### Token Provider

```typescript
interface TokenProvider {
  getAccessToken(): Promise<string | null>;
  getRefreshToken?(): Promise<string | null>;
  onTokenRefresh?(tokens: TokenPair): Promise<void>;
  onAuthenticationRequired?(): Promise<void>;
}
```

### Tenant Provider

```typescript
interface TenantProvider {
  getTenantId(): Promise<string | null>;
  onTenantChange?(tenantId: string | null): void;
  onTenantMismatch?(expected: string, actual: string): void;
}
```

### Feature Client

```typescript
interface FeatureClient {
  evaluate(keys: string[]): Promise<FeatureEvaluationResult>;
  isEnabled(key: string): Promise<boolean>;
  requireFeature(key: string): Promise<void>;
  refresh(): Promise<void>;
  subscribe(callback: FeatureChangeCallback): () => void;
}
```

### Result Type

```typescript
type Result<T, E = Error> = 
  | { ok: true; data: T }
  | { ok: false; error: E };
```

### API Error

```typescript
interface ApiError {
  status: number;
  code: string;
  message: string;
  correlationId?: string;
  traceId?: string;
  details?: ErrorDetail[];
}
```

---

## Package Exports

```typescript
// Main entry
export { createClient, createServerClient } from '@gameguild/api-client';

// Next.js integration
export { createNextAuthTokenProvider } from '@gameguild/api-client/next';

// React hooks
export { useClient, useFeature, useFeatures, FeatureGate } from '@gameguild/api-client/react';

// Plugins
export { 
  createRetryPlugin, 
  createLoggingPlugin, 
  createIdempotencyPlugin 
} from '@gameguild/api-client/plugins';

// Error utilities
export {
  isApiError,
  isUnauthorized,
  isForbidden,
  getRequiredPermissions,
} from '@gameguild/api-client';

// Result utilities
export { ok, err, unwrap, unwrapOr, map, mapErr } from '@gameguild/api-client';
```

---

## Generated Code Structure

```
src/generated/
├── index.ts              # Re-exports
├── types.gen.ts          # All DTOs/models
├── errors.gen.ts         # Error type definitions
├── endpoints.gen.ts      # Endpoint definitions
└── modules/              # Module-grouped endpoints
    ├── auth.gen.ts
    ├── users.gen.ts
    ├── programs.gen.ts
    ├── achievements.gen.ts
    ├── feature-flags.gen.ts
    ├── tenants.gen.ts
    └── ...
```

---

## Implementation Priority

### P0 - Must Have (Weeks 1-4)
- [x] Generator pipeline
- [x] Type generation
- [x] Endpoint generation
- [x] Token provider + refresh
- [x] Tenant provider + header injection
- [x] Error transformation
- [x] Result type
- [x] createClient / createServerClient

### P1 - Should Have (Weeks 5-6)
- [x] Feature client
- [x] Retry plugin
- [x] Safe logging plugin
- [x] React hooks
- [x] NextAuth integration
- [x] Breaking change detection

### P2 - Nice to Have (Weeks 7-8)
- [x] Idempotency plugin
- [x] Metrics plugin
- [x] E2E test suite
- [x] Performance benchmarks
- [x] Comprehensive documentation

---

## Security Checklist

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| No token logging | ✅ | Redact sensitive headers in logging plugin |
| SSR token safety | ✅ | Server-only token access, no prop passing |
| Cross-tenant protection | ✅ | Tenant in cache keys, fail-closed validation |
| Refresh storm prevention | ✅ | Mutex pattern in TokenRefreshManager |
| CSRF protection | ✅ | CSRF token support for cookie auth |
| Rate limit handling | ✅ | Retry-After header respect, backoff |
| Idempotency support | ✅ | Idempotency-Key header for mutations |

---

## Getting Started (After Implementation)

### Installation

```bash
npm install @gameguild/api-client
```

### Basic Usage

```typescript
import { createClient } from '@gameguild/api-client';

const client = createClient({
  baseUrl: 'https://api.gameguild.com',
  auth: {
    mode: 'bearer',
    tokenProvider: {
      getAccessToken: async () => localStorage.getItem('access_token'),
    },
  },
  tenant: {
    tenantId: 'my-tenant',
  },
});

// Make API calls
const result = await client.users.list();

if (result.ok) {
  console.log(result.data);
} else {
  console.error(result.error.message);
}
```

### Next.js Server Action

```typescript
'use server';

import { createServerClient } from '@gameguild/api-client';
import { createNextAuthTokenProvider } from '@gameguild/api-client/next';
import { auth } from '@/auth';

export async function getUsers() {
  const client = await createServerClient({
    baseUrl: process.env.API_URL!,
    auth: {
      tokenProvider: createNextAuthTokenProvider({ auth }),
    },
    tenant: {
      resolver: async () => {
        const session = await auth();
        return session?.currentTenant?.id ?? null;
      },
    },
  });

  return client.users.list();
}
```

---

## Related Documents

- [copilot-instructions.md](../../.github/copilot-instructions.md) - Repository conventions
- [ASSETS_MODULE_ARCHITECTURE.md](./ASSETS_MODULE_ARCHITECTURE.md) - Module patterns
- [permissions-dac.md](./permissions-dac.md) - Authorization model
- [auth-module.md](../modules/auth-module.md) - Authentication module docs

---

*Last updated: January 15, 2026*
