# GameGuild TypeScript SDK Generator - Part 4

**CI/CD Automation, Test Plan, Implementation Roadmap, and Final Report**

---

## 11. CI/CD Automation

### 11.1 GitHub Actions Workflow

```yaml
# .github/workflows/sdk-generate.yml
name: SDK Generation

on:
  push:
    branches: [main, develop]
    paths:
      - 'apps/api/**'
  pull_request:
    branches: [main, develop]
    paths:
      - 'apps/api/**'
  workflow_dispatch:
    inputs:
      force_regenerate:
        description: 'Force regeneration even if spec unchanged'
        required: false
        default: 'false'
        type: boolean

env:
  NODE_VERSION: '20'
  DOTNET_VERSION: '9.0.x'

jobs:
  generate-spec:
    name: Generate OpenAPI Spec
    runs-on: ubuntu-latest
    outputs:
      spec-changed: ${{ steps.check-changes.outputs.changed }}
      spec-hash: ${{ steps.check-changes.outputs.hash }}
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore API dependencies
        run: dotnet restore apps/api/GameGuild.sln
      
      - name: Build API
        run: dotnet build apps/api/GameGuild.sln --no-restore
      
      - name: Generate OpenAPI Spec
        run: |
          cd apps/api
          dotnet swagger tofile \
            --output ../../packages/api-client/openapi.json \
            Source/GameGuild.API/bin/Debug/net9.0/GameGuild.API.dll v1
      
      - name: Upload OpenAPI Spec
        uses: actions/upload-artifact@v4
        with:
          name: openapi-spec
          path: packages/api-client/openapi.json
      
      - name: Check for changes
        id: check-changes
        run: |
          HASH=$(sha256sum packages/api-client/openapi.json | cut -d ' ' -f 1)
          echo "hash=$HASH" >> $GITHUB_OUTPUT
          
          # Compare with cached hash
          if [ -f packages/api-client/src/generated/.metadata.json ]; then
            CACHED_HASH=$(jq -r '.hash' packages/api-client/src/generated/.metadata.json)
            if [ "$HASH" = "$CACHED_HASH" ] && [ "${{ github.event.inputs.force_regenerate }}" != "true" ]; then
              echo "changed=false" >> $GITHUB_OUTPUT
            else
              echo "changed=true" >> $GITHUB_OUTPUT
            fi
          else
            echo "changed=true" >> $GITHUB_OUTPUT
          fi

  detect-breaking-changes:
    name: Detect Breaking Changes
    needs: generate-spec
    if: needs.generate-spec.outputs.spec-changed == 'true'
    runs-on: ubuntu-latest
    outputs:
      has-breaking-changes: ${{ steps.diff.outputs.has-breaking }}
      changelog: ${{ steps.diff.outputs.changelog }}
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 2
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
      
      - name: Download new spec
        uses: actions/download-artifact@v4
        with:
          name: openapi-spec
          path: ./new-spec
      
      - name: Get previous spec
        run: |
          git show HEAD~1:packages/api-client/openapi.json > ./old-spec.json 2>/dev/null || echo '{}' > ./old-spec.json
      
      - name: Install openapi-diff
        run: npm install -g openapi-diff
      
      - name: Compare specs
        id: diff
        run: |
          set +e
          openapi-diff ./old-spec.json ./new-spec/openapi.json > diff-result.json 2>&1
          RESULT=$?
          set -e
          
          if [ $RESULT -eq 1 ]; then
            echo "has-breaking=true" >> $GITHUB_OUTPUT
            echo "Breaking changes detected!"
            cat diff-result.json
          else
            echo "has-breaking=false" >> $GITHUB_OUTPUT
          fi
          
          # Generate changelog entry
          node packages/api-client/scripts/generate-changelog.js diff-result.json > changelog-entry.md
          echo "changelog<<EOF" >> $GITHUB_OUTPUT
          cat changelog-entry.md >> $GITHUB_OUTPUT
          echo "EOF" >> $GITHUB_OUTPUT
      
      - name: Fail on breaking changes in PR
        if: steps.diff.outputs.has-breaking == 'true' && github.event_name == 'pull_request'
        run: |
          echo "::error::Breaking changes detected in API. Please update SDK version."
          exit 1

  generate-sdk:
    name: Generate SDK
    needs: [generate-spec, detect-breaking-changes]
    if: always() && needs.generate-spec.outputs.spec-changed == 'true'
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: packages/api-client/package-lock.json
      
      - name: Download OpenAPI Spec
        uses: actions/download-artifact@v4
        with:
          name: openapi-spec
          path: packages/api-client
      
      - name: Install dependencies
        run: |
          cd packages/api-client
          npm ci
      
      - name: Generate SDK code
        run: |
          cd packages/api-client
          npm run generate -- --input ./openapi.json
      
      - name: Run type check
        run: |
          cd packages/api-client
          npm run typecheck
      
      - name: Run tests
        run: |
          cd packages/api-client
          npm run test
      
      - name: Build package
        run: |
          cd packages/api-client
          npm run build
      
      - name: Upload generated code
        uses: actions/upload-artifact@v4
        with:
          name: sdk-generated
          path: |
            packages/api-client/src/generated
            packages/api-client/dist
      
      - name: Create PR with changes
        if: github.event_name == 'push' && github.ref == 'refs/heads/main'
        uses: peter-evans/create-pull-request@v5
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          commit-message: 'chore(sdk): regenerate API client from OpenAPI spec'
          title: 'chore(sdk): Update API Client'
          body: |
            ## API Client Update
            
            This PR was auto-generated due to changes in the API.
            
            ### Changelog
            ${{ needs.detect-breaking-changes.outputs.changelog }}
            
            ### Spec Hash
            `${{ needs.generate-spec.outputs.spec-hash }}`
          branch: sdk/auto-update
          base: main
          labels: |
            sdk
            auto-generated

  publish:
    name: Publish to NPM
    needs: generate-sdk
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    environment: npm-publish
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          registry-url: 'https://registry.npmjs.org'
      
      - name: Download built SDK
        uses: actions/download-artifact@v4
        with:
          name: sdk-generated
          path: packages/api-client
      
      - name: Check version
        id: version
        run: |
          cd packages/api-client
          CURRENT=$(npm view @gameguild/api-client version 2>/dev/null || echo "0.0.0")
          PACKAGE=$(node -p "require('./package.json').version")
          echo "current=$CURRENT" >> $GITHUB_OUTPUT
          echo "package=$PACKAGE" >> $GITHUB_OUTPUT
          
          if [ "$CURRENT" = "$PACKAGE" ]; then
            echo "needs-publish=false" >> $GITHUB_OUTPUT
          else
            echo "needs-publish=true" >> $GITHUB_OUTPUT
          fi
      
      - name: Publish to NPM
        if: steps.version.outputs.needs-publish == 'true'
        run: |
          cd packages/api-client
          npm publish --access public
        env:
          NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}
      
      - name: Create GitHub Release
        if: steps.version.outputs.needs-publish == 'true'
        uses: softprops/action-gh-release@v1
        with:
          tag_name: sdk-v${{ steps.version.outputs.package }}
          name: API Client v${{ steps.version.outputs.package }}
          body: ${{ needs.detect-breaking-changes.outputs.changelog }}
          generate_release_notes: true
```

### 11.2 Versioning Strategy

```typescript
// scripts/version.ts

import { execSync } from 'child_process';
import { readFileSync, writeFileSync } from 'fs';
import { join } from 'path';
import type { BreakingChange } from './diff';

interface PackageJson {
  name: string;
  version: string;
  [key: string]: unknown;
}

type VersionBump = 'major' | 'minor' | 'patch';

/**
 * Determine version bump based on breaking changes.
 */
export function determineVersionBump(changes: BreakingChange[]): VersionBump {
  const hasBreaking = changes.some(c => c.severity === 'breaking');
  const hasNew = changes.some(c => c.type === 'endpoint-added');
  
  if (hasBreaking) {
    return 'major';
  }
  
  if (hasNew) {
    return 'minor';
  }
  
  return 'patch';
}

/**
 * Bump package version.
 */
export function bumpVersion(packagePath: string, bump: VersionBump): string {
  const pkgPath = join(packagePath, 'package.json');
  const pkg: PackageJson = JSON.parse(readFileSync(pkgPath, 'utf-8'));
  
  const [major, minor, patch] = pkg.version.split('.').map(Number);
  
  let newVersion: string;
  switch (bump) {
    case 'major':
      newVersion = `${major + 1}.0.0`;
      break;
    case 'minor':
      newVersion = `${major}.${minor + 1}.0`;
      break;
    case 'patch':
      newVersion = `${major}.${minor}.${patch + 1}`;
      break;
  }
  
  pkg.version = newVersion;
  writeFileSync(pkgPath, JSON.stringify(pkg, null, 2) + '\n');
  
  return newVersion;
}

/**
 * Generate changelog entry.
 */
export function generateChangelog(
  changes: BreakingChange[],
  version: string,
  date: Date = new Date()
): string {
  const dateStr = date.toISOString().split('T')[0];
  
  let changelog = `## [${version}] - ${dateStr}\n\n`;
  
  // Breaking changes
  const breaking = changes.filter(c => c.severity === 'breaking');
  if (breaking.length > 0) {
    changelog += `### ⚠️ Breaking Changes\n\n`;
    for (const change of breaking) {
      changelog += `- **${change.type}**: ${change.description} (${change.path})\n`;
    }
    changelog += '\n';
  }
  
  // New features
  const added = changes.filter(c => c.type === 'endpoint-added');
  if (added.length > 0) {
    changelog += `### ✨ New Features\n\n`;
    for (const change of added) {
      changelog += `- ${change.description}\n`;
    }
    changelog += '\n';
  }
  
  // Changes
  const modified = changes.filter(c => 
    c.severity !== 'breaking' && c.type !== 'endpoint-added'
  );
  if (modified.length > 0) {
    changelog += `### 🔧 Changes\n\n`;
    for (const change of modified) {
      changelog += `- ${change.description}\n`;
    }
  }
  
  return changelog;
}
```

### 11.3 Snapshot Testing for Generated Code

```typescript
// scripts/snapshot-test.ts

import { readFileSync, writeFileSync, existsSync } from 'fs';
import { join } from 'path';
import { diffLines } from 'diff';

interface SnapshotResult {
  passed: boolean;
  file: string;
  diff?: string;
}

const SNAPSHOT_DIR = 'tests/snapshots';

/**
 * Compare generated code against snapshots.
 */
export async function runSnapshotTests(generatedDir: string): Promise<SnapshotResult[]> {
  const results: SnapshotResult[] = [];
  const generatedFiles = await getGeneratedFiles(generatedDir);
  
  for (const file of generatedFiles) {
    const result = await compareWithSnapshot(file);
    results.push(result);
  }
  
  return results;
}

async function compareWithSnapshot(generatedPath: string): Promise<SnapshotResult> {
  const relativePath = generatedPath.replace(/.*\/generated\//, '');
  const snapshotPath = join(SNAPSHOT_DIR, `${relativePath}.snap`);
  
  const generated = readFileSync(generatedPath, 'utf-8');
  
  // Create snapshot if it doesn't exist
  if (!existsSync(snapshotPath)) {
    writeFileSync(snapshotPath, generated);
    return {
      passed: true,
      file: relativePath,
    };
  }
  
  const snapshot = readFileSync(snapshotPath, 'utf-8');
  
  if (generated === snapshot) {
    return {
      passed: true,
      file: relativePath,
    };
  }
  
  // Generate diff
  const differences = diffLines(snapshot, generated);
  const diffOutput = differences
    .filter(part => part.added || part.removed)
    .map(part => {
      const prefix = part.added ? '+' : '-';
      return part.value
        .split('\n')
        .filter(line => line.trim())
        .map(line => `${prefix} ${line}`)
        .join('\n');
    })
    .join('\n');
  
  return {
    passed: false,
    file: relativePath,
    diff: diffOutput,
  };
}

/**
 * Update snapshots with current generated code.
 */
export async function updateSnapshots(generatedDir: string): Promise<void> {
  const generatedFiles = await getGeneratedFiles(generatedDir);
  
  for (const file of generatedFiles) {
    const relativePath = file.replace(/.*\/generated\//, '');
    const snapshotPath = join(SNAPSHOT_DIR, `${relativePath}.snap`);
    const content = readFileSync(file, 'utf-8');
    writeFileSync(snapshotPath, content);
  }
}

async function getGeneratedFiles(dir: string): Promise<string[]> {
  const { glob } = await import('glob');
  return glob(`${dir}/**/*.gen.ts`);
}
```

---

## 12. Test Plan

### 12.1 Test Categories

| Category | Scope | Tools | Location |
|----------|-------|-------|----------|
| Unit Tests | Runtime modules | Vitest | `tests/unit/` |
| Integration Tests | Auth flows, API calls | Vitest + MSW | `tests/integration/` |
| Snapshot Tests | Generated code | Custom | `tests/snapshots/` |
| E2E Tests | Next.js SSR/CSR | Playwright | `tests/e2e/` |
| Type Tests | TypeScript types | tsd | `tests/types/` |

### 12.2 Unit Tests

```typescript
// tests/unit/runtime/auth/refresh.test.ts

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TokenRefreshManager, RefreshTokenExpiredError } from '../../../../src/runtime/auth/refresh';
import type { TokenProvider, TokenPair } from '../../../../src/runtime/auth/types';

describe('TokenRefreshManager', () => {
  let mockProvider: TokenProvider;
  let mockFetch: ReturnType<typeof vi.fn>;
  
  beforeEach(() => {
    mockProvider = {
      getAccessToken: vi.fn().mockResolvedValue('access-token'),
      getRefreshToken: vi.fn().mockResolvedValue('refresh-token'),
      onTokenRefresh: vi.fn(),
      onAuthenticationRequired: vi.fn(),
    };
    
    mockFetch = vi.fn();
  });
  
  describe('getValidToken', () => {
    it('returns access token when not expired', async () => {
      const manager = new TokenRefreshManager(mockProvider, undefined, mockFetch);
      
      const token = await manager.getValidToken();
      
      expect(token).toBe('access-token');
      expect(mockFetch).not.toHaveBeenCalled();
    });
  });
  
  describe('refresh', () => {
    it('calls refresh endpoint and updates tokens', async () => {
      const newTokens: TokenPair = {
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        expiresIn: 3600,
        tokenType: 'Bearer',
      };
      
      mockFetch.mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(newTokens),
      });
      
      const manager = new TokenRefreshManager(
        mockProvider,
        { refreshUrl: '/api/auth/refresh', refreshThreshold: 30000, maxRetries: 3, backoffMs: 100 },
        mockFetch
      );
      
      const result = await manager.refresh();
      
      expect(result).toEqual(newTokens);
      expect(mockProvider.onTokenRefresh).toHaveBeenCalledWith(newTokens);
    });
    
    it('throws RefreshTokenExpiredError on 401', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 401,
      });
      
      const manager = new TokenRefreshManager(mockProvider, undefined, mockFetch);
      
      await expect(manager.refresh()).rejects.toThrow(RefreshTokenExpiredError);
    });
    
    it('prevents concurrent refresh calls', async () => {
      mockFetch.mockImplementation(() => 
        new Promise(resolve => 
          setTimeout(() => resolve({
            ok: true,
            json: () => Promise.resolve({ accessToken: 'new', tokenType: 'Bearer' }),
          }), 100)
        )
      );
      
      const manager = new TokenRefreshManager(mockProvider, undefined, mockFetch);
      
      // Start multiple concurrent refreshes
      const promises = [
        manager.refresh(),
        manager.refresh(),
        manager.refresh(),
      ];
      
      await Promise.all(promises);
      
      // Should only call fetch once
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });
  });
});
```

### 12.3 Integration Tests

```typescript
// tests/integration/auth-flow.test.ts

import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { createClient } from '../../src';

const API_URL = 'http://localhost:5000';

// Mock API handlers
const handlers = [
  http.post(`${API_URL}/api/auth/signin`, async ({ request }) => {
    const body = await request.json() as { email: string; password: string };
    
    if (body.email === 'test@example.com' && body.password === 'password') {
      return HttpResponse.json({
        accessToken: 'test-access-token',
        refreshToken: 'test-refresh-token',
        expiresIn: 3600,
      });
    }
    
    return HttpResponse.json(
      { code: 'INVALID_CREDENTIALS', message: 'Invalid email or password' },
      { status: 401 }
    );
  }),
  
  http.get(`${API_URL}/api/users/me`, ({ request }) => {
    const auth = request.headers.get('Authorization');
    
    if (auth !== 'Bearer test-access-token') {
      return HttpResponse.json(
        { code: 'UNAUTHORIZED', message: 'Unauthorized' },
        { status: 401 }
      );
    }
    
    const tenantId = request.headers.get('X-Tenant-Id');
    
    return HttpResponse.json({
      id: 'user-123',
      email: 'test@example.com',
      tenantId,
    });
  }),
  
  http.post(`${API_URL}/api/auth/refresh`, async ({ request }) => {
    const body = await request.json() as { refreshToken: string };
    
    if (body.refreshToken === 'test-refresh-token') {
      return HttpResponse.json({
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        expiresIn: 3600,
      });
    }
    
    return HttpResponse.json(
      { code: 'REFRESH_TOKEN_EXPIRED', message: 'Refresh token expired' },
      { status: 401 }
    );
  }),
];

const server = setupServer(...handlers);

describe('Authentication Flow', () => {
  beforeAll(() => server.listen());
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());
  
  it('authenticates with email and password', async () => {
    const client = createClient({
      baseUrl: API_URL,
    });
    
    const result = await client.auth.signIn({
      body: { email: 'test@example.com', password: 'password' },
    });
    
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.accessToken).toBe('test-access-token');
    }
  });
  
  it('returns error for invalid credentials', async () => {
    const client = createClient({
      baseUrl: API_URL,
    });
    
    const result = await client.auth.signIn({
      body: { email: 'wrong@example.com', password: 'wrong' },
    });
    
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.error.code).toBe('INVALID_CREDENTIALS');
      expect(result.error.status).toBe(401);
    }
  });
  
  it('injects authentication header', async () => {
    let accessToken = 'test-access-token';
    
    const client = createClient({
      baseUrl: API_URL,
      auth: {
        mode: 'bearer',
        tokenProvider: {
          getAccessToken: async () => accessToken,
        },
      },
      tenant: {
        tenantId: 'tenant-123',
      },
    });
    
    const result = await client.users.getMe();
    
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.id).toBe('user-123');
      expect(result.data.tenantId).toBe('tenant-123');
    }
  });
  
  it('handles 401 and triggers re-authentication', async () => {
    const onAuthRequired = vi.fn();
    
    const client = createClient({
      baseUrl: API_URL,
      auth: {
        mode: 'bearer',
        tokenProvider: {
          getAccessToken: async () => 'invalid-token',
          onAuthenticationRequired: onAuthRequired,
        },
      },
    });
    
    const result = await client.users.getMe();
    
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.error.status).toBe(401);
    }
  });
});
```

### 12.4 E2E Tests with Next.js

```typescript
// tests/e2e/next-app/tests/ssr-safety.spec.ts

import { test, expect } from '@playwright/test';

test.describe('SSR Safety', () => {
  test('does not expose access tokens in HTML source', async ({ page }) => {
    // Sign in first
    await page.goto('/auth/signin');
    await page.fill('[name="email"]', 'test@example.com');
    await page.fill('[name="password"]', 'password');
    await page.click('button[type="submit"]');
    
    // Wait for redirect to dashboard
    await page.waitForURL('/dashboard');
    
    // Get page HTML source
    const html = await page.content();
    
    // Check for token patterns
    const jwtPattern = /eyJ[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]*/;
    expect(html).not.toMatch(jwtPattern);
    
    // Check for common token key names
    expect(html.toLowerCase()).not.toContain('accesstoken');
    expect(html.toLowerCase()).not.toContain('access_token');
    expect(html.toLowerCase()).not.toContain('refreshtoken');
    expect(html.toLowerCase()).not.toContain('refresh_token');
  });
  
  test('authenticated API calls work in Server Components', async ({ page }) => {
    await page.goto('/auth/signin');
    await page.fill('[name="email"]', 'test@example.com');
    await page.fill('[name="password"]', 'password');
    await page.click('button[type="submit"]');
    
    await page.waitForURL('/dashboard');
    
    // Check that user data is rendered (fetched server-side)
    await expect(page.locator('[data-testid="user-email"]')).toHaveText('test@example.com');
  });
  
  test('client-side API calls work after hydration', async ({ page }) => {
    await page.goto('/auth/signin');
    await page.fill('[name="email"]', 'test@example.com');
    await page.fill('[name="password"]', 'password');
    await page.click('button[type="submit"]');
    
    await page.waitForURL('/dashboard');
    
    // Trigger client-side fetch
    await page.click('[data-testid="refresh-button"]');
    
    // Wait for client-side update
    await expect(page.locator('[data-testid="last-updated"]')).toContainText(/\d{2}:\d{2}:\d{2}/);
  });
  
  test('tenant context is correctly applied', async ({ page }) => {
    // Navigate to tenant-specific URL
    await page.goto('/t/acme-corp/dashboard');
    
    // Verify tenant context in UI
    await expect(page.locator('[data-testid="current-tenant"]')).toHaveText('acme-corp');
    
    // Verify API calls include tenant header
    const [request] = await Promise.all([
      page.waitForRequest(req => req.url().includes('/api/') && req.method() !== 'OPTIONS'),
      page.click('[data-testid="load-data"]'),
    ]);
    
    expect(request.headers()['x-tenant-id']).toBe('acme-corp');
  });
});
```

### 12.5 Type Tests

```typescript
// tests/types/client.test-d.ts

import { expectType, expectError } from 'tsd';
import { createClient } from '../../src';
import type { Result } from '../../src/runtime/result/types';
import type { ApiError } from '../../src/runtime/errors/types';
import type { UserDto, CreateUserRequest } from '../../src/generated/types.gen';

// Test client creation
const client = createClient({
  baseUrl: 'http://localhost:5000',
});

// Test typed endpoint methods
async function testEndpoints() {
  // GET request returns Result with typed data
  const listResult = await client.users.list();
  expectType<Result<UserDto[], ApiError>>(listResult);
  
  if (listResult.ok) {
    expectType<UserDto[]>(listResult.data);
    expectType<string>(listResult.data[0].id);
    expectType<string | undefined>(listResult.data[0].email);
  } else {
    expectType<ApiError>(listResult.error);
    expectType<number>(listResult.error.status);
    expectType<string>(listResult.error.code);
  }
  
  // POST request with typed body
  const createResult = await client.users.create({
    body: {
      email: 'test@example.com',
      givenName: 'Test',
      familyName: 'User',
    },
  });
  expectType<Result<UserDto, ApiError>>(createResult);
  
  // Error on wrong body type
  expectError(client.users.create({
    body: {
      email: 123, // Should be string
    },
  }));
  
  // Path parameters are required
  const getResult = await client.users.getById({
    userId: 'user-123',
  });
  expectType<Result<UserDto, ApiError>>(getResult);
  
  // Error when path param missing
  expectError(client.users.getById({}));
}

// Test auth configuration types
createClient({
  baseUrl: 'http://localhost:5000',
  auth: {
    mode: 'bearer',
    tokenProvider: {
      getAccessToken: async () => 'token',
      getRefreshToken: async () => 'refresh',
      onTokenRefresh: async (tokens) => {
        expectType<string>(tokens.accessToken);
      },
    },
  },
});

// Error on invalid auth mode
expectError(createClient({
  baseUrl: 'http://localhost:5000',
  auth: {
    mode: 'invalid',
  },
}));

// Test tenant configuration types
createClient({
  baseUrl: 'http://localhost:5000',
  tenant: {
    tenantId: 'tenant-123',
  },
});

createClient({
  baseUrl: 'http://localhost:5000',
  tenant: {
    mode: 'subdomain',
    baseDomain: 'gameguild.com',
  },
});

// Error on invalid tenant mode
expectError(createClient({
  baseUrl: 'http://localhost:5000',
  tenant: {
    mode: 'invalid',
  },
}));
```

---

## 13. Implementation Roadmap

### 13.1 Phase Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        IMPLEMENTATION ROADMAP                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  PHASE 1                PHASE 2                PHASE 3              PHASE 4 │
│  Foundation            Core Features          Advanced              Polish  │
│  (Week 1-2)            (Week 3-4)            (Week 5-6)           (Week 7-8)│
│                                                                             │
│  ┌───────────┐        ┌───────────┐        ┌───────────┐        ┌─────────┐ │
│  │Generator  │        │Auth       │        │Features   │        │E2E Tests│ │
│  │Pipeline   │───────▶│Support    │───────▶│Client     │───────▶│Docs     │ │
│  │Basic Types│        │Tenant     │        │Plugins    │        │Release  │ │
│  └───────────┘        │Errors     │        │React Hooks│        └─────────┘ │
│                       └───────────┘        └───────────┘                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 13.2 Detailed Phase Breakdown

#### Phase 1: Foundation (Weeks 1-2)

| Task | Effort | Owner | Dependencies |
|------|--------|-------|--------------|
| Create package structure | 4h | - | None |
| Implement OpenAPI fetcher | 4h | - | None |
| Implement spec normalizer | 8h | - | OpenAPI fetcher |
| Create Handlebars templates | 8h | - | None |
| Implement type generator | 12h | - | Templates |
| Implement endpoint generator | 12h | - | Type generator |
| Implement error type generator | 4h | - | Type generator |
| Basic transport layer (fetch) | 8h | - | None |
| Result type utilities | 4h | - | None |
| Unit tests for generators | 8h | - | All generators |
| CI pipeline for generation | 4h | - | Unit tests |

**Deliverables:**
- [ ] Working generator that produces types and endpoint stubs
- [ ] Basic fetch transport
- [ ] CI pipeline that generates on API changes

#### Phase 2: Core Features (Weeks 3-4)

| Task | Effort | Owner | Dependencies |
|------|--------|-------|--------------|
| TokenProvider interface | 4h | - | None |
| Token refresh manager | 8h | - | TokenProvider |
| CSRF support | 4h | - | Transport |
| TenantProvider interface | 4h | - | None |
| Tenant header interceptor | 4h | - | TenantProvider |
| ApiError transformation | 8h | - | None |
| Error type guards | 4h | - | ApiError |
| Authorization helpers | 4h | - | Error guards |
| createClient factory | 8h | - | All providers |
| createServerClient factory | 8h | - | createClient |
| NextAuth integration | 8h | - | TokenProvider |
| Integration tests | 12h | - | All core features |

**Deliverables:**
- [ ] Full auth support with refresh
- [ ] Multi-tenancy with fail-closed validation
- [ ] Typed error handling
- [ ] Working client factories

#### Phase 3: Advanced Features (Weeks 5-6)

| Task | Effort | Owner | Dependencies |
|------|--------|-------|--------------|
| Feature client interface | 4h | - | None |
| Feature client implementation | 8h | - | Interface |
| Feature cache | 4h | - | Client |
| Retry plugin | 8h | - | Transport |
| Logging plugin (safe) | 4h | - | Transport |
| Idempotency plugin | 4h | - | Transport |
| Metrics plugin | 4h | - | Transport |
| React hooks (useClient, useFeature) | 8h | - | Client |
| ClientProvider context | 4h | - | Hooks |
| SSR safety utilities | 4h | - | None |
| Breaking change detection | 8h | - | Diff library |
| Changelog generation | 4h | - | Breaking change detection |

**Deliverables:**
- [ ] Complete feature flags client
- [ ] All plugins implemented
- [ ] React integration ready
- [ ] Automated breaking change detection

#### Phase 4: Polish & Release (Weeks 7-8)

| Task | Effort | Owner | Dependencies |
|------|--------|-------|--------------|
| Next.js E2E test app | 12h | - | All features |
| Playwright E2E tests | 12h | - | Test app |
| SSR safety tests | 4h | - | E2E tests |
| Type tests (tsd) | 4h | - | All types |
| Snapshot tests | 4h | - | Generator |
| API documentation | 8h | - | All features |
| Migration guide | 4h | - | Docs |
| README and examples | 4h | - | Docs |
| npm publish workflow | 4h | - | CI |
| GitHub release automation | 4h | - | CI |
| Performance benchmarks | 4h | - | All features |
| Security audit | 4h | - | All features |

**Deliverables:**
- [ ] Full E2E test coverage
- [ ] Complete documentation
- [ ] Published npm package
- [ ] Migration guide from current client

### 13.3 Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| OpenAPI spec inconsistencies | High | Medium | Robust normalization layer |
| Breaking changes in API | Medium | High | Breaking change detection in CI |
| Token refresh race conditions | Medium | High | Mutex pattern, thorough testing |
| SSR token leakage | Low | Critical | Compile-time checks, E2E tests |
| Performance regression | Low | Medium | Benchmarks, lazy loading |
| React 19 compatibility | Low | Medium | Peer dependency flexibility |

---

## 14. Definition of Done

### 14.1 Feature Completion Criteria

Each feature is considered "done" when:

- [ ] Implementation complete and follows patterns in this document
- [ ] Unit tests with >80% coverage
- [ ] Integration tests for critical paths
- [ ] TypeScript types are accurate (tsd tests pass)
- [ ] No ESLint errors or warnings
- [ ] Documentation in code (JSDoc comments)
- [ ] Example usage in README or docs

### 14.2 SDK Release Criteria

The SDK is ready for v1.0.0 release when:

#### Functional Requirements
- [ ] All endpoint types generated correctly from OpenAPI
- [ ] All DTO types generated with accurate nullability
- [ ] createClient and createServerClient factories work
- [ ] Token provider interface implemented with refresh
- [ ] Tenant provider interface implemented with fail-closed validation
- [ ] Feature client implemented with caching
- [ ] All plugins (retry, logging, idempotency) implemented
- [ ] React hooks (useClient, useFeature) implemented
- [ ] Next.js integration tested with App Router

#### Non-Functional Requirements
- [ ] Tree-shakeable (unused modules not bundled)
- [ ] ESM and CJS outputs
- [ ] Total bundle size < 50KB gzipped (core)
- [ ] No runtime dependencies (only peer deps)
- [ ] TypeScript 5.3+ compatibility
- [ ] React 18+ and React 19 RC compatibility
- [ ] Next.js 14+ and Next.js 15 compatibility
- [ ] Node.js 18+ compatibility

#### Security Requirements
- [ ] No token logging (verified by code review)
- [ ] SSR safety (verified by E2E tests)
- [ ] No cross-tenant caching (verified by integration tests)
- [ ] CSRF protection for cookie auth
- [ ] Credential storage warnings documented

#### Quality Requirements
- [ ] Unit test coverage >80%
- [ ] Integration test coverage for critical paths
- [ ] E2E tests for Next.js SSR/CSR
- [ ] Type tests passing
- [ ] Snapshot tests for generated code
- [ ] No P0/P1 bugs open
- [ ] Performance benchmarks baseline established

#### Documentation Requirements
- [ ] README with quick start
- [ ] API reference (auto-generated from JSDoc)
- [ ] Migration guide from @hey-api/openapi-ts
- [ ] Security best practices guide
- [ ] Examples for common use cases:
  - [ ] Basic authentication
  - [ ] Token refresh flow
  - [ ] Multi-tenant setup
  - [ ] Feature flags usage
  - [ ] Server Action patterns
  - [ ] Error handling patterns

#### CI/CD Requirements
- [ ] Automated generation on API changes
- [ ] Breaking change detection
- [ ] Automated changelog generation
- [ ] npm publish workflow
- [ ] GitHub release automation
- [ ] Semantic versioning enforced

---

## 15. Final Report

### 15.1 Executive Summary

This document specifies a production-grade TypeScript SDK generator for the GameGuild platform, replacing the current `@hey-api/openapi-ts` approach with a custom solution tailored to our authentication, authorization, multi-tenancy, and feature flag requirements.

**Key Outcomes:**
- Type-safe API client generated from OpenAPI
- First-class support for JWT auth with automatic refresh
- Fail-closed multi-tenancy with tenant header injection
- Feature flags client with caching
- SSR-safe patterns for Next.js
- Pluggable architecture for retry, logging, and metrics

### 15.2 Architectural Decisions

| Decision | Rationale | Alternatives Considered |
|----------|-----------|------------------------|
| Custom generator vs. extending existing | Need deep integration with auth/tenant/features | Forking @hey-api/openapi-ts |
| Result<T,E> pattern | Explicit error handling, TypeScript-friendly | Throwing exceptions, Option type |
| Handlebars templates | Proven, simple, maintainable | AST manipulation, string concatenation |
| Plugin architecture | Extensibility without core changes | Middleware chains, decorators |
| Separate server/browser clients | Different auth patterns, SSR safety | Single configurable client |
| Token refresh mutex | Prevent refresh storms | Queue-based, debouncing |
| Tenant fail-closed | Security requirement | Fail-open with logging |

### 15.3 Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Token leakage | Safe logging, SSR checks, no prop passing |
| Cross-tenant caching | Tenant in cache keys, fail-closed validation |
| Refresh storms | Mutex pattern, single in-flight request |
| Breaking changes | Automated detection, semantic versioning |
| Bundle size | Tree-shaking, lazy loading, code splitting |
| React 19 incompatibility | Peer dependency flexibility, testing |

### 15.4 Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Type coverage | 100% | TypeScript strict mode |
| Test coverage | >80% | Vitest coverage report |
| Bundle size | <50KB | Bundlephobia |
| Generation time | <30s | CI pipeline metrics |
| Error rate | <0.1% | Runtime monitoring |
| Developer satisfaction | >4/5 | Team survey |

### 15.5 Next Steps

1. **Immediate (Week 1):**
   - Create package structure
   - Set up build tooling (tsup, vitest)
   - Implement basic generator

2. **Short-term (Weeks 2-4):**
   - Complete core features (auth, tenant, errors)
   - Integrate with current frontend

3. **Medium-term (Weeks 5-8):**
   - Add advanced features (plugins, React hooks)
   - Complete E2E testing
   - Documentation and release

4. **Long-term:**
   - React Native support
   - GraphQL integration (if needed)
   - Real-time subscriptions

---

## Appendix A: Quick Reference

### Creating a Client

```typescript
// Browser (SPA)
const client = createClient({
  baseUrl: 'https://api.gameguild.com',
  auth: {
    mode: 'bearer',
    tokenProvider: myTokenProvider,
  },
  tenant: { tenantId: 'acme-corp' },
});

// Server (Next.js)
const client = await createServerClient({
  baseUrl: process.env.API_URL!,
  auth: { tokenProvider: createNextAuthTokenProvider({ auth }) },
  tenant: { resolver: async () => (await auth())?.currentTenant?.id ?? null },
});
```

### Making Requests

```typescript
// With Result pattern
const result = await client.users.list();
if (result.ok) {
  console.log(result.data);
} else {
  console.error(result.error);
}

// With throwOnError
const client = createClient({ throwOnError: true, ... });
try {
  const users = await client.users.list();
} catch (error) {
  if (isForbidden(error)) {
    const permissions = getRequiredPermissions(error);
  }
}
```

### Feature Flags

```typescript
// Check feature
if (await client.features.isEnabled('beta_ui')) {
  renderBetaUI();
}

// React hook
const { isEnabled, isLoading } = useFeature('beta_ui');
```

### Error Handling

```typescript
import { isApiError, isUnauthorized, isForbidden, getRequiredPermissions } from '@gameguild/api-client';

if (!result.ok) {
  if (isUnauthorized(result.error)) {
    redirect('/login');
  }
  if (isForbidden(result.error)) {
    showPermissionError(getRequiredPermissions(result.error));
  }
}
```

---

*End of SDK Generator Design Specification*
