# GameGuild TypeScript SDK Generator - Production Design Specification

**Version:** 1.0.0  
**Date:** January 15, 2026  
**Author:** Platform Engineering Team  
**Status:** Design Complete - Ready for Implementation

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [SDK Product Design](#2-sdk-product-design)
3. [Package Structure](#3-package-structure)
4. [Generation Pipeline](#4-generation-pipeline)
5. [Authentication Support](#5-authentication-support)
6. [Authorization Support](#6-authorization-support)
7. [Features/Entitlements Support](#7-featuresentitlements-support)
8. [Multi-Tenancy Support](#8-multi-tenancy-support)
9. [Error Model](#9-error-model)
10. [Security Review](#10-security-review)
11. [CI/CD Automation](#11-cicd-automation)
12. [Test Plan](#12-test-plan)
13. [Implementation Roadmap](#13-implementation-roadmap)
14. [Definition of Done](#14-definition-of-done)

---

## 1. Executive Summary

### 1.1 Current State

GameGuild currently uses `@hey-api/openapi-ts` to generate TypeScript clients from the .NET API's OpenAPI specification. While functional, this approach has significant limitations:

- **No first-class auth handling**: Manual token injection via `configureAuthenticatedClient()`
- **No authorization awareness**: Permission errors are generic exceptions
- **No feature flag integration**: No client-side helpers for feature gating
- **Weak multi-tenancy**: Manual `X-Tenant-Id` header management
- **SSR safety concerns**: No built-in protection against token leakage
- **Limited error typing**: ProblemDetails not fully leveraged

### 1.2 Proposed Solution

Build a custom SDK generator and runtime package (`@gameguild/api-client`) that:

1. **Generates typed clients** from OpenAPI with our conventions
2. **Provides first-class AuthN/AuthZ** with pluggable token providers
3. **Integrates feature flags** with typed helpers and caching
4. **Enforces multi-tenancy** with fail-closed tenant context
5. **Guarantees SSR safety** with compile-time and runtime guards
6. **Models all errors** with typed discriminated unions

### 1.3 Key Benefits

| Capability | Current State | Proposed State |
|------------|---------------|----------------|
| Type Safety | Partial (DTOs only) | Full (DTOs + errors + features) |
| Auth Integration | Manual per-request | Automatic with pluggable providers |
| Authorization | Generic 403 handling | Typed permission errors + helpers |
| Feature Flags | None | First-class client with caching |
| Multi-Tenancy | Manual headers | Fail-closed with context providers |
| SSR Safety | Manual discipline | Enforced by architecture |
| Error Handling | Partial ProblemDetails | Full typed error unions |

### 1.4 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          @gameguild/api-client                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────┐  │
│  │   /src/generated/   │    │    /src/runtime/    │    │  /src/plugins/  │  │
│  │                     │    │                     │    │                 │  │
│  │  • types.gen.ts     │    │  • transport/       │    │  • retry.ts     │  │
│  │  • endpoints.gen.ts │    │    ├─ fetch.ts      │    │  • logging.ts   │  │
│  │  • errors.gen.ts    │    │    ├─ undici.ts     │    │  • cache.ts     │  │
│  │  • modules/         │    │    └─ types.ts      │    │  • metrics.ts   │  │
│  │    ├─ auth.gen.ts   │    │  • auth/            │    │                 │  │
│  │    ├─ users.gen.ts  │    │    ├─ provider.ts   │    └─────────────────┘  │
│  │    └─ ...           │    │    ├─ session.ts    │                         │
│  │                     │    │    └─ csrf.ts       │                         │
│  └─────────────────────┘    │  • tenant/          │                         │
│           │                 │    └─ provider.ts   │                         │
│           │                 │  • features/        │                         │
│           ▼                 │    ├─ client.ts     │                         │
│  ┌─────────────────────┐    │    └─ cache.ts      │                         │
│  │   /src/index.ts     │◄───│  • errors/          │                         │
│  │   (Public API)      │    │    ├─ types.ts      │                         │
│  └─────────────────────┘    │    └─ guards.ts     │                         │
│                             └─────────────────────┘                         │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                        /scripts/                                    │    │
│  │  • generate.ts (main generator)                                     │    │
│  │  • normalize.ts (spec pre-processing)                               │    │
│  │  • templates/ (Handlebars templates)                                │    │
│  │  • diff.ts (breaking change detection)                              │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. SDK Product Design

### 2.1 Client Instantiation

The SDK provides a fluent, type-safe API for creating configured clients.

```typescript
import { createClient, createServerClient } from '@gameguild/api-client';
import type { ClientConfig, ServerClientConfig } from '@gameguild/api-client';

// ============================================================================
// BROWSER CLIENT (Client Components, SPAs)
// ============================================================================

const browserClient = createClient({
  // Required
  baseUrl: 'https://api.gameguild.com',
  
  // Auth Configuration
  auth: {
    // Option 1: Token Provider (recommended for SPAs)
    tokenProvider: {
      getAccessToken: async () => sessionStorage.getItem('access_token'),
      getRefreshToken: async () => sessionStorage.getItem('refresh_token'),
      onTokenRefresh: async (tokens) => {
        sessionStorage.setItem('access_token', tokens.accessToken);
        sessionStorage.setItem('refresh_token', tokens.refreshToken);
      },
      onTokenExpired: async () => {
        window.location.href = '/login';
      },
    },
    // Option 2: Cookie-based (for SSR hydration)
    // mode: 'cookie',
    // csrfTokenHeader: 'X-CSRF-Token',
  },
  
  // Tenant Configuration
  tenant: {
    // Option 1: Static tenant
    tenantId: 'tenant-123',
    // Option 2: Dynamic resolver
    // resolver: () => getCurrentTenantFromRoute(),
    // Option 3: Subdomain-based
    // mode: 'subdomain',
  },
  
  // Optional: Transport customization
  transport: {
    // Custom fetch (for testing, proxies, etc.)
    fetch: globalThis.fetch,
    // Request timeout
    timeout: 30_000,
    // Retry configuration
    retry: {
      maxAttempts: 3,
      backoff: 'exponential',
      retryableStatuses: [408, 429, 500, 502, 503, 504],
    },
  },
  
  // Optional: Plugins
  plugins: [
    loggingPlugin({ level: 'debug' }),
    metricsPlugin({ onRequest: (metrics) => analytics.track(metrics) }),
  ],
});

// ============================================================================
// SERVER CLIENT (Next.js Server Actions, API Routes, Edge)
// ============================================================================

// For Next.js App Router server components/actions
import { auth } from '@/auth'; // NextAuth

const serverClient = await createServerClient({
  baseUrl: process.env.API_BASE_URL!,
  
  // Server-side auth: reads from request context
  auth: {
    // Async token provider that reads from session
    tokenProvider: async () => {
      const session = await auth();
      if (!session?.api?.accessToken) {
        throw new AuthenticationRequiredError();
      }
      return {
        accessToken: session.api.accessToken,
        // No refresh on server - let client handle refresh
      };
    },
  },
  
  // Tenant from session
  tenant: {
    resolver: async () => {
      const session = await auth();
      return session?.currentTenant?.id ?? null;
    },
  },
  
  // Server-specific options
  server: {
    // Never cache authenticated responses
    cache: 'no-store',
    // Propagate request headers (correlation ID, etc.)
    propagateHeaders: ['x-correlation-id', 'x-request-id'],
  },
});

// ============================================================================
// PUBLIC CLIENT (Unauthenticated endpoints)
// ============================================================================

const publicClient = createClient({
  baseUrl: 'https://api.gameguild.com',
  // No auth configuration - only public endpoints accessible
});
```

### 2.2 Using Generated Endpoints

Endpoints are grouped by OpenAPI tags (modules) with consistent naming:

```typescript
import { createClient } from '@gameguild/api-client';

const client = createClient({ /* config */ });

// ============================================================================
// Module-based access (recommended)
// ============================================================================

// Users module
const users = await client.users.list({ take: 20, skip: 0 });
const user = await client.users.getById({ userId: 'user-123' });
const created = await client.users.create({ body: { email: 'test@example.com' } });

// Programs module  
const programs = await client.programs.list();
const program = await client.programs.getById({ programId: 'prog-123' });

// Achievements module
const achievements = await client.achievements.list();
const leaderboard = await client.achievements.getLeaderboard();

// Feature Flags module
const flags = await client.featureFlags.evaluate({ 
  body: { keys: ['dark_mode', 'beta_features'] } 
});

// ============================================================================
// Type-safe request/response
// ============================================================================

// Request types are fully typed
const createUserRequest: client.users.CreateRequest = {
  body: {
    email: 'user@example.com',
    givenName: 'John',
    familyName: 'Doe',
  },
};

// Response types include success AND error variants
const result = await client.users.create(createUserRequest);

// Result is a discriminated union
if (result.ok) {
  // result.data is typed as UserDto
  console.log(result.data.id);
} else {
  // result.error is typed as ApiError
  if (result.error.code === 'VALIDATION_ERROR') {
    // result.error.details contains field-level errors
    result.error.details.forEach(d => console.log(d.field, d.message));
  }
}

// ============================================================================
// Alternative: Throw on error (opt-in)
// ============================================================================

const client = createClient({ throwOnError: true, /* ... */ });

try {
  // Throws ApiError on non-2xx response
  const user = await client.users.create({ body: { email: 'test@example.com' } });
  // user is typed as UserDto (no Result wrapper)
} catch (error) {
  if (isApiError(error)) {
    // Typed error handling
  }
}
```

### 2.3 Authentication Configuration

```typescript
import type { 
  TokenProvider, 
  CookieAuthConfig,
  OAuth2Config 
} from '@gameguild/api-client';

// ============================================================================
// Token Provider Interface (Bearer Token / JWT)
// ============================================================================

interface TokenProvider {
  /**
   * Get the current access token.
   * Return null if not authenticated.
   */
  getAccessToken(): Promise<string | null>;
  
  /**
   * Get the refresh token (optional).
   * Used for automatic token refresh.
   */
  getRefreshToken?(): Promise<string | null>;
  
  /**
   * Called when tokens are refreshed.
   * Persist the new tokens.
   */
  onTokenRefresh?(tokens: TokenPair): Promise<void>;
  
  /**
   * Called when refresh fails and user must re-authenticate.
   */
  onAuthenticationRequired?(): Promise<void>;
  
  /**
   * Called when access is denied (403).
   * May include required permissions.
   */
  onAuthorizationDenied?(error: AuthorizationError): Promise<void>;
}

interface TokenPair {
  accessToken: string;
  refreshToken?: string;
  expiresIn?: number;
  tokenType?: 'Bearer';
}

// ============================================================================
// NextAuth Integration (Server-Side)
// ============================================================================

import { auth } from '@/auth';
import { createNextAuthTokenProvider } from '@gameguild/api-client/next';

// Pre-built provider for NextAuth
const tokenProvider = createNextAuthTokenProvider({
  auth, // NextAuth auth() function
  onRefreshError: async () => {
    // Redirect to sign-in
    redirect('/api/auth/signin');
  },
});

// ============================================================================
// Cookie-Based Auth (SSR-Safe)
// ============================================================================

interface CookieAuthConfig {
  mode: 'cookie';
  /**
   * Include credentials in requests.
   * @default 'same-origin'
   */
  credentials?: RequestCredentials;
  /**
   * CSRF token header name.
   * If set, reads CSRF token from cookie and sends in header.
   */
  csrfTokenHeader?: string;
  /**
   * CSRF token cookie name.
   * @default 'csrf-token'
   */
  csrfCookieName?: string;
}

// Cookie-based client (session cookies + CSRF)
const cookieClient = createClient({
  baseUrl: '/api', // Same origin
  auth: {
    mode: 'cookie',
    credentials: 'same-origin',
    csrfTokenHeader: 'X-CSRF-Token',
  },
});
```

### 2.4 Tenant Configuration

```typescript
import type { TenantProvider, TenantConfig } from '@gameguild/api-client';

// ============================================================================
// Tenant Provider Interface
// ============================================================================

interface TenantProvider {
  /**
   * Get the current tenant ID.
   * Return null for global/system context.
   * Throw TenantRequiredError if tenant is required but not available.
   */
  getTenantId(): Promise<string | null>;
  
  /**
   * Called when a request fails due to tenant mismatch.
   */
  onTenantMismatch?(expected: string, actual: string): Promise<void>;
}

// ============================================================================
// Tenant Configuration Options
// ============================================================================

type TenantConfig = 
  // Static tenant ID
  | { tenantId: string }
  // Dynamic resolver function
  | { resolver: () => string | null | Promise<string | null> }
  // Subdomain-based (e.g., tenant.gameguild.com)
  | { mode: 'subdomain'; baseDomain: string }
  // Route-based (e.g., /t/{tenantId}/...)
  | { mode: 'route'; pattern: RegExp };

// ============================================================================
// Usage Examples
// ============================================================================

// Static tenant (simplest)
const client1 = createClient({
  baseUrl: 'https://api.gameguild.com',
  tenant: { tenantId: 'tenant-abc' },
});

// From session (server-side)
const client2 = await createServerClient({
  baseUrl: process.env.API_URL!,
  tenant: {
    resolver: async () => {
      const session = await auth();
      const tenantId = session?.currentTenant?.id;
      if (!tenantId) {
        throw new TenantRequiredError('No tenant in session');
      }
      return tenantId;
    },
  },
});

// From URL subdomain (client-side)
const client3 = createClient({
  baseUrl: 'https://api.gameguild.com',
  tenant: {
    mode: 'subdomain',
    baseDomain: 'gameguild.com',
    // Extracts 'acme' from 'acme.gameguild.com'
  },
});
```

### 2.5 Feature Flags Integration

```typescript
import type { FeatureClient, FeatureConfig } from '@gameguild/api-client';

// ============================================================================
// Feature Client Interface
// ============================================================================

interface FeatureClient {
  /**
   * Evaluate multiple feature flags.
   * Results are cached per tenant+user.
   */
  evaluate(keys: string[]): Promise<FeatureEvaluationResult>;
  
  /**
   * Check if a single feature is enabled.
   * Uses cached values when available.
   */
  isEnabled(key: string): Promise<boolean>;
  
  /**
   * Check feature and throw if not enabled.
   */
  requireFeature(key: string): Promise<void>;
  
  /**
   * Get all evaluated features.
   */
  getAll(): Promise<Record<string, boolean>>;
  
  /**
   * Invalidate cache and re-fetch.
   */
  refresh(): Promise<void>;
  
  /**
   * Subscribe to feature changes (for real-time updates).
   */
  subscribe(callback: (features: Record<string, boolean>) => void): () => void;
}

interface FeatureEvaluationResult {
  features: Record<string, boolean>;
  evaluatedAt: Date;
  source: 'cache' | 'server';
}

// ============================================================================
// Usage Examples
// ============================================================================

const client = createClient({
  baseUrl: 'https://api.gameguild.com',
  auth: { /* ... */ },
  tenant: { tenantId: 'tenant-123' },
  features: {
    // Cache TTL (default: 5 minutes)
    cacheTtl: 5 * 60 * 1000,
    // Pre-fetch these features on client init
    preloadKeys: ['dark_mode', 'beta_features', 'ai_assistant'],
    // Stale-while-revalidate pattern
    staleWhileRevalidate: true,
  },
});

// Check feature
if (await client.features.isEnabled('beta_features')) {
  // Show beta UI
}

// Require feature (throws FeatureNotEnabledError)
await client.features.requireFeature('ai_assistant');
await client.aiAssistant.generate({ prompt: '...' });

// Batch evaluation
const { features } = await client.features.evaluate([
  'dark_mode',
  'beta_features', 
  'ai_assistant',
]);

// React hook (provided by @gameguild/api-client/react)
import { useFeature, useFeatures } from '@gameguild/api-client/react';

function MyComponent() {
  const { isEnabled, isLoading } = useFeature('beta_features');
  const { features, refresh } = useFeatures(['dark_mode', 'ai_assistant']);
  
  if (isLoading) return <Spinner />;
  if (!isEnabled) return <UpgradePrompt />;
  
  return <BetaFeature />;
}
```

---

## 3. Package Structure

### 3.1 Directory Layout

```
packages/api-client/
├── package.json
├── tsconfig.json
├── tsup.config.ts                 # Build configuration
├── vitest.config.ts               # Test configuration
├── README.md
├── CHANGELOG.md
│
├── scripts/                       # Generator scripts
│   ├── generate.ts                # Main generator entry point
│   ├── normalize.ts               # OpenAPI spec normalization
│   ├── codegen/
│   │   ├── types.ts               # DTO/model generation
│   │   ├── endpoints.ts           # Endpoint method generation
│   │   ├── errors.ts              # Error type generation
│   │   └── modules.ts             # Module grouping logic
│   ├── templates/                 # Handlebars templates
│   │   ├── types.hbs
│   │   ├── endpoint.hbs
│   │   ├── module.hbs
│   │   └── error.hbs
│   ├── diff.ts                    # Breaking change detection
│   ├── validate.ts                # Post-generation validation
│   └── utils/
│       ├── naming.ts              # Naming conventions
│       ├── openapi.ts             # OpenAPI parsing utilities
│       └── formatting.ts          # Prettier integration
│
├── src/
│   ├── index.ts                   # Main public entry point
│   ├── client.ts                  # createClient factory
│   ├── server.ts                  # createServerClient factory
│   │
│   ├── generated/                 # AUTO-GENERATED (do not edit)
│   │   ├── index.ts               # Re-exports all generated code
│   │   ├── types.gen.ts           # All DTOs/models
│   │   ├── errors.gen.ts          # Error type definitions
│   │   ├── endpoints.gen.ts       # All endpoint definitions
│   │   └── modules/               # Module-grouped endpoints
│   │       ├── auth.gen.ts
│   │       ├── users.gen.ts
│   │       ├── programs.gen.ts
│   │       ├── achievements.gen.ts
│   │       ├── feature-flags.gen.ts
│   │       ├── tenants.gen.ts
│   │       ├── permissions.gen.ts
│   │       └── ... (one per OpenAPI tag)
│   │
│   ├── runtime/                   # HANDWRITTEN runtime code
│   │   ├── index.ts
│   │   │
│   │   ├── transport/             # HTTP transport layer
│   │   │   ├── types.ts           # Transport interfaces
│   │   │   ├── fetch.ts           # Fetch adapter (default)
│   │   │   ├── undici.ts          # Undici adapter (Node.js)
│   │   │   └── interceptors.ts    # Request/response interceptors
│   │   │
│   │   ├── auth/                  # Authentication
│   │   │   ├── types.ts           # TokenProvider, AuthConfig
│   │   │   ├── provider.ts        # Token provider implementation
│   │   │   ├── refresh.ts         # Token refresh logic
│   │   │   ├── session.ts         # Session management
│   │   │   └── csrf.ts            # CSRF protection
│   │   │
│   │   ├── tenant/                # Multi-tenancy
│   │   │   ├── types.ts           # TenantProvider, TenantConfig
│   │   │   ├── provider.ts        # Tenant provider implementation
│   │   │   └── resolver.ts        # Tenant resolution strategies
│   │   │
│   │   ├── features/              # Feature flags
│   │   │   ├── types.ts           # FeatureClient interface
│   │   │   ├── client.ts          # Feature client implementation
│   │   │   ├── cache.ts           # Feature caching
│   │   │   └── guards.ts          # Feature gate helpers
│   │   │
│   │   ├── errors/                # Error handling
│   │   │   ├── types.ts           # ApiError, specialized errors
│   │   │   ├── guards.ts          # Type guards (isApiError, etc.)
│   │   │   └── transform.ts       # Response error transformation
│   │   │
│   │   ├── result/                # Result type utilities
│   │   │   ├── types.ts           # Result<T, E> type
│   │   │   └── helpers.ts         # ok(), err(), unwrap()
│   │   │
│   │   └── utils/                 # Shared utilities
│   │       ├── headers.ts         # Header manipulation
│   │       ├── url.ts             # URL building
│   │       ├── serialization.ts   # Body serialization
│   │       └── correlation.ts     # Request correlation IDs
│   │
│   ├── plugins/                   # Optional plugins
│   │   ├── types.ts               # Plugin interface
│   │   ├── retry.ts               # Retry with backoff
│   │   ├── logging.ts             # Safe request logging
│   │   ├── cache.ts               # Response caching
│   │   ├── metrics.ts             # Performance metrics
│   │   └── idempotency.ts         # Idempotency key support
│   │
│   └── integrations/              # Framework integrations
│       ├── next/                  # Next.js specific
│       │   ├── index.ts
│       │   ├── server.ts          # Server component helpers
│       │   ├── client.ts          # Client component helpers
│       │   ├── middleware.ts      # Edge middleware support
│       │   └── nextauth.ts        # NextAuth integration
│       │
│       └── react/                 # React hooks
│           ├── index.ts
│           ├── hooks.ts           # useClient, useFeature, etc.
│           ├── context.ts         # ClientProvider
│           └── ssr.ts             # SSR safety utilities
│
├── tests/
│   ├── unit/
│   │   ├── runtime/
│   │   │   ├── auth.test.ts
│   │   │   ├── tenant.test.ts
│   │   │   ├── features.test.ts
│   │   │   └── errors.test.ts
│   │   └── plugins/
│   │       ├── retry.test.ts
│   │       └── logging.test.ts
│   │
│   ├── integration/
│   │   ├── auth-flow.test.ts
│   │   ├── tenant-context.test.ts
│   │   └── feature-flags.test.ts
│   │
│   ├── e2e/
│   │   └── next-app/              # Next.js test app
│   │       ├── app/
│   │       └── tests/
│   │
│   └── snapshots/                 # Generated code snapshots
│       ├── types.gen.ts.snap
│       └── modules/
│
└── dist/                          # Build output
    ├── index.js                   # ESM entry
    ├── index.cjs                  # CJS entry
    ├── index.d.ts                 # Type declarations
    └── ...
```

### 3.2 Generated vs Handwritten Separation

| Category | Location | Ownership | Regeneration |
|----------|----------|-----------|--------------|
| DTOs/Models | `/src/generated/types.gen.ts` | Generator | Full |
| Endpoints | `/src/generated/modules/*.gen.ts` | Generator | Full |
| Error Types | `/src/generated/errors.gen.ts` | Generator | Full |
| Transport | `/src/runtime/transport/` | Human | Never |
| Auth Provider | `/src/runtime/auth/` | Human | Never |
| Tenant Provider | `/src/runtime/tenant/` | Human | Never |
| Feature Client | `/src/runtime/features/` | Human | Never |
| Error Guards | `/src/runtime/errors/` | Human | Never |
| Plugins | `/src/plugins/` | Human | Never |
| React Hooks | `/src/integrations/react/` | Human | Never |
| Next.js Integration | `/src/integrations/next/` | Human | Never |

### 3.3 Package.json Configuration

```json
{
  "name": "@gameguild/api-client",
  "version": "0.1.0",
  "description": "Type-safe API client for GameGuild platform",
  "type": "module",
  "main": "./dist/index.cjs",
  "module": "./dist/index.js",
  "types": "./dist/index.d.ts",
  "exports": {
    ".": {
      "import": {
        "types": "./dist/index.d.ts",
        "default": "./dist/index.js"
      },
      "require": {
        "types": "./dist/index.d.cts",
        "default": "./dist/index.cjs"
      }
    },
    "./next": {
      "import": {
        "types": "./dist/integrations/next/index.d.ts",
        "default": "./dist/integrations/next/index.js"
      }
    },
    "./react": {
      "import": {
        "types": "./dist/integrations/react/index.d.ts",
        "default": "./dist/integrations/react/index.js"
      }
    },
    "./plugins": {
      "import": {
        "types": "./dist/plugins/index.d.ts",
        "default": "./dist/plugins/index.js"
      }
    }
  },
  "sideEffects": false,
  "files": [
    "dist",
    "README.md",
    "CHANGELOG.md"
  ],
  "scripts": {
    "generate": "tsx scripts/generate.ts",
    "generate:watch": "tsx scripts/generate.ts --watch",
    "generate:diff": "tsx scripts/diff.ts",
    "build": "tsup",
    "build:watch": "tsup --watch",
    "test": "vitest",
    "test:coverage": "vitest --coverage",
    "test:e2e": "playwright test",
    "lint": "eslint src --ext .ts,.tsx",
    "typecheck": "tsc --noEmit",
    "clean": "rimraf dist",
    "prepublishOnly": "npm run build"
  },
  "dependencies": {},
  "peerDependencies": {
    "react": "^18.0.0 || ^19.0.0",
    "next": "^14.0.0 || ^15.0.0"
  },
  "peerDependenciesMeta": {
    "react": { "optional": true },
    "next": { "optional": true }
  },
  "devDependencies": {
    "@types/node": "^20.0.0",
    "typescript": "^5.3.0",
    "tsup": "^8.0.0",
    "vitest": "^1.0.0",
    "prettier": "^3.0.0",
    "eslint": "^8.0.0",
    "tsx": "^4.0.0",
    "openapi-types": "^12.0.0",
    "handlebars": "^4.7.0"
  },
  "engines": {
    "node": ">=18.0.0"
  },
  "keywords": [
    "gameguild",
    "api-client",
    "typescript",
    "openapi",
    "sdk"
  ]
}
```

### 3.4 Build Configuration (tsup)

```typescript
// tsup.config.ts
import { defineConfig } from 'tsup';

export default defineConfig({
  entry: {
    index: 'src/index.ts',
    'integrations/next/index': 'src/integrations/next/index.ts',
    'integrations/react/index': 'src/integrations/react/index.ts',
    'plugins/index': 'src/plugins/index.ts',
  },
  format: ['esm', 'cjs'],
  dts: true,
  sourcemap: true,
  clean: true,
  splitting: true,
  treeshake: true,
  target: 'es2022',
  external: ['react', 'next'],
  esbuildOptions(options) {
    // Ensure generated code is not bundled separately
    options.mainFields = ['module', 'main'];
  },
});
```

---

*This completes Part 1 of the SDK design. Continue to Part 2 for Generation Pipeline and Authentication Support.*
