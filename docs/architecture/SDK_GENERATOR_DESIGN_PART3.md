# GameGuild TypeScript SDK Generator - Part 3

**Features/Entitlements, Multi-Tenancy, Error Model, and Security Review**

---

## 7. Features/Entitlements Support

### 7.1 Feature Client Interface

```typescript
// src/runtime/features/types.ts

/**
 * Feature evaluation result from the server.
 */
export interface FeatureEvaluation {
  /** Feature key */
  key: string;
  /** Whether the feature is enabled */
  enabled: boolean;
  /** Variant value (for A/B testing) */
  variant?: string;
  /** Additional metadata */
  metadata?: Record<string, unknown>;
}

/**
 * Batch evaluation result.
 */
export interface FeatureEvaluationResult {
  /** Map of feature key to evaluation */
  features: Record<string, FeatureEvaluation>;
  /** Evaluation timestamp */
  evaluatedAt: Date;
  /** Cache status */
  source: 'cache' | 'server' | 'stale';
  /** Time until cache expires */
  expiresIn?: number;
}

/**
 * Feature client configuration.
 */
export interface FeatureClientConfig {
  /** Cache TTL in milliseconds */
  cacheTtl?: number;
  /** Pre-load these feature keys on init */
  preloadKeys?: string[];
  /** Use stale-while-revalidate pattern */
  staleWhileRevalidate?: boolean;
  /** Polling interval for updates (0 = disabled) */
  pollingIntervalMs?: number;
  /** Custom evaluation endpoint */
  evaluateEndpoint?: string;
}

/**
 * Feature client interface.
 */
export interface FeatureClient {
  /**
   * Evaluate multiple feature flags.
   * Results are cached per tenant+user context.
   */
  evaluate(keys: string[]): Promise<FeatureEvaluationResult>;
  
  /**
   * Check if a feature is enabled.
   * Uses cached value when available.
   */
  isEnabled(key: string): Promise<boolean>;
  
  /**
   * Get a feature variant (for A/B tests).
   */
  getVariant(key: string): Promise<string | undefined>;
  
  /**
   * Assert feature is enabled, throw if not.
   */
  requireFeature(key: string): Promise<void>;
  
  /**
   * Get all evaluated features.
   */
  getAll(): Promise<Record<string, boolean>>;
  
  /**
   * Invalidate cache and refresh from server.
   */
  refresh(): Promise<void>;
  
  /**
   * Subscribe to feature changes.
   * Returns unsubscribe function.
   */
  subscribe(callback: FeatureChangeCallback): () => void;
  
  /**
   * Preload features for faster access.
   */
  preload(keys: string[]): Promise<void>;
}

export type FeatureChangeCallback = (
  changes: Array<{
    key: string;
    previousValue: boolean;
    currentValue: boolean;
  }>
) => void;
```

### 7.2 Feature Client Implementation

```typescript
// src/runtime/features/client.ts

import type {
  FeatureClient,
  FeatureClientConfig,
  FeatureEvaluation,
  FeatureEvaluationResult,
  FeatureChangeCallback,
} from './types';
import { FeatureCache } from './cache';
import type { HttpClient } from '../transport/types';

const DEFAULT_CONFIG: Required<FeatureClientConfig> = {
  cacheTtl: 5 * 60 * 1000, // 5 minutes
  preloadKeys: [],
  staleWhileRevalidate: true,
  pollingIntervalMs: 0,
  evaluateEndpoint: '/api/feature-flags/evaluate',
};

export class FeatureClientImpl implements FeatureClient {
  private cache: FeatureCache;
  private subscribers: Set<FeatureChangeCallback> = new Set();
  private pollingTimer?: ReturnType<typeof setInterval>;
  private pendingEvaluation?: Promise<FeatureEvaluationResult>;
  
  constructor(
    private httpClient: HttpClient,
    private tenantId: string | null,
    private userId: string | null,
    private config: FeatureClientConfig = {}
  ) {
    const fullConfig = { ...DEFAULT_CONFIG, ...config };
    this.cache = new FeatureCache(fullConfig.cacheTtl);
    
    // Preload if configured
    if (fullConfig.preloadKeys.length > 0) {
      this.preload(fullConfig.preloadKeys).catch(console.error);
    }
    
    // Start polling if configured
    if (fullConfig.pollingIntervalMs > 0) {
      this.startPolling(fullConfig.pollingIntervalMs);
    }
  }
  
  async evaluate(keys: string[]): Promise<FeatureEvaluationResult> {
    // Check cache first
    const cacheKey = this.getCacheKey(keys);
    const cached = this.cache.get(cacheKey);
    
    if (cached && !cached.isExpired) {
      return {
        features: cached.data,
        evaluatedAt: cached.timestamp,
        source: 'cache',
        expiresIn: cached.expiresIn,
      };
    }
    
    // Return stale while revalidating
    if (cached && this.config.staleWhileRevalidate) {
      this.fetchAndCache(keys).catch(console.error);
      return {
        features: cached.data,
        evaluatedAt: cached.timestamp,
        source: 'stale',
      };
    }
    
    // Fetch fresh
    return this.fetchAndCache(keys);
  }
  
  async isEnabled(key: string): Promise<boolean> {
    const result = await this.evaluate([key]);
    return result.features[key]?.enabled ?? false;
  }
  
  async getVariant(key: string): Promise<string | undefined> {
    const result = await this.evaluate([key]);
    return result.features[key]?.variant;
  }
  
  async requireFeature(key: string): Promise<void> {
    const enabled = await this.isEnabled(key);
    if (!enabled) {
      throw new FeatureNotEnabledError(key);
    }
  }
  
  async getAll(): Promise<Record<string, boolean>> {
    const allCached = this.cache.getAll();
    const result: Record<string, boolean> = {};
    
    for (const [key, evaluation] of Object.entries(allCached)) {
      result[key] = evaluation.enabled;
    }
    
    return result;
  }
  
  async refresh(): Promise<void> {
    const keys = this.cache.getAllKeys();
    if (keys.length > 0) {
      this.cache.clear();
      await this.fetchAndCache(keys);
    }
  }
  
  subscribe(callback: FeatureChangeCallback): () => void {
    this.subscribers.add(callback);
    return () => this.subscribers.delete(callback);
  }
  
  async preload(keys: string[]): Promise<void> {
    await this.fetchAndCache(keys);
  }
  
  dispose(): void {
    if (this.pollingTimer) {
      clearInterval(this.pollingTimer);
    }
    this.subscribers.clear();
  }
  
  private async fetchAndCache(keys: string[]): Promise<FeatureEvaluationResult> {
    // Deduplicate concurrent requests
    if (this.pendingEvaluation) {
      return this.pendingEvaluation;
    }
    
    this.pendingEvaluation = this.doFetch(keys);
    
    try {
      return await this.pendingEvaluation;
    } finally {
      this.pendingEvaluation = undefined;
    }
  }
  
  private async doFetch(keys: string[]): Promise<FeatureEvaluationResult> {
    const endpoint = this.config.evaluateEndpoint ?? DEFAULT_CONFIG.evaluateEndpoint;
    
    const response = await this.httpClient.request<FeatureEvaluation[]>({
      method: 'POST',
      url: endpoint,
      body: { keys },
    });
    
    if (!response.ok) {
      throw new FeatureEvaluationError(response.error?.message ?? 'Failed to evaluate features');
    }
    
    const features: Record<string, FeatureEvaluation> = {};
    const previousValues = this.cache.getAll();
    
    for (const evaluation of response.data) {
      features[evaluation.key] = evaluation;
      this.cache.set(evaluation.key, evaluation);
    }
    
    // Notify subscribers of changes
    this.notifyChanges(previousValues, features);
    
    return {
      features,
      evaluatedAt: new Date(),
      source: 'server',
    };
  }
  
  private notifyChanges(
    previous: Record<string, FeatureEvaluation>,
    current: Record<string, FeatureEvaluation>
  ): void {
    const changes: Array<{
      key: string;
      previousValue: boolean;
      currentValue: boolean;
    }> = [];
    
    for (const [key, evaluation] of Object.entries(current)) {
      const prevEnabled = previous[key]?.enabled ?? false;
      if (prevEnabled !== evaluation.enabled) {
        changes.push({
          key,
          previousValue: prevEnabled,
          currentValue: evaluation.enabled,
        });
      }
    }
    
    if (changes.length > 0) {
      for (const callback of this.subscribers) {
        try {
          callback(changes);
        } catch (error) {
          console.error('Feature change callback error:', error);
        }
      }
    }
  }
  
  private getCacheKey(keys: string[]): string {
    return `${this.tenantId ?? 'global'}:${this.userId ?? 'anon'}:${keys.sort().join(',')}`;
  }
  
  private startPolling(intervalMs: number): void {
    this.pollingTimer = setInterval(async () => {
      try {
        await this.refresh();
      } catch (error) {
        console.error('Feature polling error:', error);
      }
    }, intervalMs);
  }
}

// Custom errors
export class FeatureNotEnabledError extends Error {
  readonly code = 'FEATURE_NOT_ENABLED';
  
  constructor(public readonly featureKey: string) {
    super(`Feature '${featureKey}' is not enabled`);
    this.name = 'FeatureNotEnabledError';
  }
}

export class FeatureEvaluationError extends Error {
  readonly code = 'FEATURE_EVALUATION_ERROR';
  
  constructor(message: string) {
    super(message);
    this.name = 'FeatureEvaluationError';
  }
}
```

### 7.3 Feature Cache

```typescript
// src/runtime/features/cache.ts

import type { FeatureEvaluation } from './types';

interface CacheEntry<T> {
  data: T;
  timestamp: Date;
  expiresAt: number;
}

export class FeatureCache {
  private cache = new Map<string, CacheEntry<FeatureEvaluation>>();
  
  constructor(private ttlMs: number) {}
  
  get(key: string): { data: FeatureEvaluation; timestamp: Date; isExpired: boolean; expiresIn: number } | null {
    const entry = this.cache.get(key);
    if (!entry) return null;
    
    const now = Date.now();
    return {
      data: entry.data,
      timestamp: entry.timestamp,
      isExpired: now >= entry.expiresAt,
      expiresIn: Math.max(0, entry.expiresAt - now),
    };
  }
  
  set(key: string, evaluation: FeatureEvaluation): void {
    this.cache.set(key, {
      data: evaluation,
      timestamp: new Date(),
      expiresAt: Date.now() + this.ttlMs,
    });
  }
  
  getAll(): Record<string, FeatureEvaluation> {
    const result: Record<string, FeatureEvaluation> = {};
    for (const [key, entry] of this.cache) {
      result[key] = entry.data;
    }
    return result;
  }
  
  getAllKeys(): string[] {
    return Array.from(this.cache.keys());
  }
  
  clear(): void {
    this.cache.clear();
  }
  
  delete(key: string): void {
    this.cache.delete(key);
  }
}
```

### 7.4 React Hook for Features

```typescript
// src/integrations/react/hooks/useFeature.ts

import { useState, useEffect, useCallback, useMemo } from 'react';
import { useClient } from '../context';
import type { FeatureClient } from '../../../runtime/features/types';

interface UseFeatureResult {
  /** Whether the feature is enabled */
  isEnabled: boolean;
  /** Loading state */
  isLoading: boolean;
  /** Error if evaluation failed */
  error: Error | null;
  /** Refresh feature value */
  refresh: () => Promise<void>;
}

/**
 * Hook to check if a single feature is enabled.
 */
export function useFeature(featureKey: string): UseFeatureResult {
  const client = useClient();
  const [isEnabled, setIsEnabled] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  
  const evaluate = useCallback(async () => {
    if (!client.features) {
      setError(new Error('Feature client not configured'));
      setIsLoading(false);
      return;
    }
    
    try {
      setIsLoading(true);
      const enabled = await client.features.isEnabled(featureKey);
      setIsEnabled(enabled);
      setError(null);
    } catch (err) {
      setError(err as Error);
    } finally {
      setIsLoading(false);
    }
  }, [client, featureKey]);
  
  useEffect(() => {
    evaluate();
    
    // Subscribe to changes
    const unsubscribe = client.features?.subscribe((changes) => {
      const change = changes.find(c => c.key === featureKey);
      if (change) {
        setIsEnabled(change.currentValue);
      }
    });
    
    return () => unsubscribe?.();
  }, [client, featureKey, evaluate]);
  
  return {
    isEnabled,
    isLoading,
    error,
    refresh: evaluate,
  };
}

interface UseFeaturesResult {
  /** Map of feature key to enabled state */
  features: Record<string, boolean>;
  /** Loading state */
  isLoading: boolean;
  /** Error if evaluation failed */
  error: Error | null;
  /** Check if a specific feature is enabled */
  isEnabled: (key: string) => boolean;
  /** Refresh all features */
  refresh: () => Promise<void>;
}

/**
 * Hook to check multiple features at once.
 */
export function useFeatures(featureKeys: string[]): UseFeaturesResult {
  const client = useClient();
  const [features, setFeatures] = useState<Record<string, boolean>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  
  // Memoize keys to avoid unnecessary re-renders
  const keysString = useMemo(() => featureKeys.sort().join(','), [featureKeys]);
  
  const evaluate = useCallback(async () => {
    if (!client.features) {
      setError(new Error('Feature client not configured'));
      setIsLoading(false);
      return;
    }
    
    try {
      setIsLoading(true);
      const result = await client.features.evaluate(featureKeys);
      const enabledMap: Record<string, boolean> = {};
      for (const [key, evaluation] of Object.entries(result.features)) {
        enabledMap[key] = evaluation.enabled;
      }
      setFeatures(enabledMap);
      setError(null);
    } catch (err) {
      setError(err as Error);
    } finally {
      setIsLoading(false);
    }
  }, [client, keysString]);
  
  useEffect(() => {
    evaluate();
  }, [evaluate]);
  
  const isEnabled = useCallback(
    (key: string): boolean => features[key] ?? false,
    [features]
  );
  
  return {
    features,
    isLoading,
    error,
    isEnabled,
    refresh: evaluate,
  };
}

/**
 * Component that renders children only if feature is enabled.
 */
interface FeatureGateProps {
  feature: string;
  children: React.ReactNode;
  fallback?: React.ReactNode;
  loadingFallback?: React.ReactNode;
}

export function FeatureGate({
  feature,
  children,
  fallback = null,
  loadingFallback = null,
}: FeatureGateProps): React.ReactNode {
  const { isEnabled, isLoading } = useFeature(feature);
  
  if (isLoading) {
    return loadingFallback;
  }
  
  return isEnabled ? children : fallback;
}
```

---

## 8. Multi-Tenancy Support

### 8.1 Tenant Provider Interface

```typescript
// src/runtime/tenant/types.ts

/**
 * Tenant provider interface for multi-tenancy support.
 */
export interface TenantProvider {
  /**
   * Get the current tenant ID.
   * @returns Tenant ID or null for global/system context
   * @throws TenantRequiredError if tenant is required but not available
   */
  getTenantId(): Promise<string | null>;
  
  /**
   * Called when tenant context changes.
   */
  onTenantChange?(tenantId: string | null): void;
  
  /**
   * Called when a request fails due to tenant mismatch.
   */
  onTenantMismatch?(expected: string, actual: string): void;
}

/**
 * Tenant configuration options.
 */
export type TenantConfig =
  | TenantStaticConfig
  | TenantResolverConfig
  | TenantSubdomainConfig
  | TenantRouteConfig;

interface TenantStaticConfig {
  /** Static tenant ID */
  tenantId: string;
}

interface TenantResolverConfig {
  /** Dynamic tenant resolver function */
  resolver: () => string | null | Promise<string | null>;
}

interface TenantSubdomainConfig {
  mode: 'subdomain';
  /** Base domain (e.g., 'gameguild.com') */
  baseDomain: string;
  /** Subdomains to exclude (e.g., ['www', 'api']) */
  excludeSubdomains?: string[];
}

interface TenantRouteConfig {
  mode: 'route';
  /** Route pattern with tenant capture group */
  pattern: RegExp;
  /** Capture group index (default: 1) */
  captureGroup?: number;
}

/**
 * Tenant context for the current request.
 */
export interface TenantContext {
  tenantId: string | null;
  isGlobal: boolean;
  resolvedFrom: 'static' | 'resolver' | 'subdomain' | 'route';
}
```

### 8.2 Tenant Provider Implementation

```typescript
// src/runtime/tenant/provider.ts

import type { TenantProvider, TenantConfig, TenantContext } from './types';

/**
 * Error thrown when tenant is required but not available.
 */
export class TenantRequiredError extends Error {
  readonly code = 'TENANT_REQUIRED';
  
  constructor(message = 'Tenant context is required for this operation') {
    super(message);
    this.name = 'TenantRequiredError';
  }
}

/**
 * Error thrown when there's a tenant mismatch.
 */
export class TenantMismatchError extends Error {
  readonly code = 'TENANT_MISMATCH';
  
  constructor(
    public readonly expected: string,
    public readonly actual: string
  ) {
    super(`Tenant mismatch: expected '${expected}', got '${actual}'`);
    this.name = 'TenantMismatchError';
  }
}

/**
 * Create a tenant provider from configuration.
 */
export function createTenantProvider(config: TenantConfig): TenantProvider {
  if ('tenantId' in config) {
    return new StaticTenantProvider(config.tenantId);
  }
  
  if ('resolver' in config) {
    return new ResolverTenantProvider(config.resolver);
  }
  
  if (config.mode === 'subdomain') {
    return new SubdomainTenantProvider(config.baseDomain, config.excludeSubdomains);
  }
  
  if (config.mode === 'route') {
    return new RouteTenantProvider(config.pattern, config.captureGroup);
  }
  
  throw new Error('Invalid tenant configuration');
}

class StaticTenantProvider implements TenantProvider {
  constructor(private tenantId: string) {}
  
  async getTenantId(): Promise<string> {
    return this.tenantId;
  }
}

class ResolverTenantProvider implements TenantProvider {
  constructor(private resolver: () => string | null | Promise<string | null>) {}
  
  async getTenantId(): Promise<string | null> {
    return this.resolver();
  }
}

class SubdomainTenantProvider implements TenantProvider {
  constructor(
    private baseDomain: string,
    private excludeSubdomains: string[] = ['www', 'api', 'app']
  ) {}
  
  async getTenantId(): Promise<string | null> {
    if (typeof window === 'undefined') {
      throw new Error('SubdomainTenantProvider requires browser environment');
    }
    
    const hostname = window.location.hostname;
    const subdomain = this.extractSubdomain(hostname);
    
    if (!subdomain || this.excludeSubdomains.includes(subdomain)) {
      return null;
    }
    
    return subdomain;
  }
  
  private extractSubdomain(hostname: string): string | null {
    // Handle localhost
    if (hostname === 'localhost' || hostname.startsWith('127.')) {
      return null;
    }
    
    const parts = hostname.split('.');
    const baseParts = this.baseDomain.split('.');
    
    // If hostname matches base domain exactly, no subdomain
    if (parts.length <= baseParts.length) {
      return null;
    }
    
    // Extract subdomain (all parts before base domain)
    const subdomainParts = parts.slice(0, parts.length - baseParts.length);
    return subdomainParts.join('.');
  }
}

class RouteTenantProvider implements TenantProvider {
  constructor(
    private pattern: RegExp,
    private captureGroup: number = 1
  ) {}
  
  async getTenantId(): Promise<string | null> {
    if (typeof window === 'undefined') {
      throw new Error('RouteTenantProvider requires browser environment');
    }
    
    const pathname = window.location.pathname;
    const match = pathname.match(this.pattern);
    
    if (!match || !match[this.captureGroup]) {
      return null;
    }
    
    return match[this.captureGroup];
  }
}

/**
 * Tenant context manager for tracking and validating tenant.
 */
export class TenantContextManager {
  private currentContext: TenantContext | null = null;
  
  constructor(private provider: TenantProvider) {}
  
  async getContext(): Promise<TenantContext> {
    const tenantId = await this.provider.getTenantId();
    
    this.currentContext = {
      tenantId,
      isGlobal: tenantId === null,
      resolvedFrom: this.detectResolveMethod(),
    };
    
    return this.currentContext;
  }
  
  async requireTenant(): Promise<string> {
    const context = await this.getContext();
    
    if (!context.tenantId) {
      throw new TenantRequiredError();
    }
    
    return context.tenantId;
  }
  
  validateTenant(expectedTenantId: string): void {
    if (this.currentContext?.tenantId !== expectedTenantId) {
      this.provider.onTenantMismatch?.(expectedTenantId, this.currentContext?.tenantId ?? 'null');
      throw new TenantMismatchError(expectedTenantId, this.currentContext?.tenantId ?? 'null');
    }
  }
  
  private detectResolveMethod(): TenantContext['resolvedFrom'] {
    // Implementation depends on provider type
    return 'resolver';
  }
}
```

### 8.3 Tenant Header Injection

```typescript
// src/runtime/tenant/interceptor.ts

import type { RequestInterceptor } from '../transport/types';
import type { TenantProvider } from './types';
import { TenantRequiredError } from './provider';

const TENANT_HEADER = 'X-Tenant-Id';

interface TenantInterceptorOptions {
  /** Header name for tenant ID */
  headerName?: string;
  /** Require tenant for all requests */
  required?: boolean;
  /** Paths that don't require tenant */
  excludePaths?: RegExp[];
}

/**
 * Create a request interceptor that injects tenant header.
 */
export function createTenantInterceptor(
  provider: TenantProvider,
  options: TenantInterceptorOptions = {}
): RequestInterceptor {
  const {
    headerName = TENANT_HEADER,
    required = false,
    excludePaths = [/^\/api\/auth\//, /^\/health/],
  } = options;
  
  return async (request, context) => {
    // Check if path is excluded
    const url = new URL(request.url);
    const isExcluded = excludePaths.some(pattern => pattern.test(url.pathname));
    
    if (isExcluded) {
      return request;
    }
    
    const tenantId = await provider.getTenantId();
    
    if (!tenantId && required) {
      throw new TenantRequiredError();
    }
    
    if (tenantId) {
      const headers = new Headers(request.headers);
      headers.set(headerName, tenantId);
      
      return new Request(request.url, {
        ...request,
        headers,
      });
    }
    
    return request;
  };
}

/**
 * Create cache key factory that includes tenant.
 */
export function createTenantAwareCacheKey(
  tenantProvider: TenantProvider
): (request: Request) => Promise<string> {
  return async (request: Request) => {
    const tenantId = await tenantProvider.getTenantId();
    const url = new URL(request.url);
    
    // Include tenant in cache key to prevent cross-tenant caching
    return `${tenantId ?? 'global'}:${request.method}:${url.pathname}${url.search}`;
  };
}
```

---

## 9. Error Model

### 9.1 Unified API Error Type

```typescript
// src/runtime/errors/types.ts

/**
 * Base API error interface.
 * All API errors conform to this shape.
 */
export interface ApiError {
  /** HTTP status code */
  status: number;
  /** Error code for programmatic handling */
  code: string;
  /** Human-readable error message */
  message: string;
  /** Request correlation ID for debugging */
  correlationId?: string;
  /** Trace ID for distributed tracing */
  traceId?: string;
  /** Additional error details */
  details?: ErrorDetail[];
  /** Server timestamp */
  timestamp?: string;
  /** Instance URI (identifies the specific error occurrence) */
  instance?: string;
}

/**
 * Field-level error detail.
 */
export interface ErrorDetail {
  /** Field name or path (e.g., 'email' or 'address.city') */
  field?: string;
  /** Error code for this field */
  code: string;
  /** Human-readable message */
  message: string;
  /** Suggested value or fix */
  suggestion?: string;
}

/**
 * Validation error with field-level details.
 * Extends ApiError with typed validation details.
 */
export interface ValidationError extends ApiError {
  code: 'VALIDATION_ERROR';
  status: 400;
  details: ValidationErrorDetail[];
}

export interface ValidationErrorDetail extends ErrorDetail {
  field: string;
  /** Value that failed validation */
  rejectedValue?: unknown;
  /** Validation constraint that failed */
  constraint?: string;
}

/**
 * Error codes enum for type-safe handling.
 */
export enum ErrorCode {
  // Authentication (401)
  UNAUTHORIZED = 'UNAUTHORIZED',
  TOKEN_EXPIRED = 'TOKEN_EXPIRED',
  INVALID_TOKEN = 'INVALID_TOKEN',
  SESSION_EXPIRED = 'SESSION_EXPIRED',
  
  // Authorization (403)
  FORBIDDEN = 'FORBIDDEN',
  INSUFFICIENT_PERMISSIONS = 'INSUFFICIENT_PERMISSIONS',
  RESOURCE_ACCESS_DENIED = 'RESOURCE_ACCESS_DENIED',
  
  // Client Errors (4xx)
  VALIDATION_ERROR = 'VALIDATION_ERROR',
  NOT_FOUND = 'NOT_FOUND',
  CONFLICT = 'CONFLICT',
  RATE_LIMITED = 'RATE_LIMITED',
  PAYLOAD_TOO_LARGE = 'PAYLOAD_TOO_LARGE',
  
  // Feature/Entitlement
  FEATURE_NOT_AVAILABLE = 'FEATURE_NOT_AVAILABLE',
  PLAN_UPGRADE_REQUIRED = 'PLAN_UPGRADE_REQUIRED',
  QUOTA_EXCEEDED = 'QUOTA_EXCEEDED',
  
  // Tenant
  TENANT_REQUIRED = 'TENANT_REQUIRED',
  TENANT_MISMATCH = 'TENANT_MISMATCH',
  TENANT_NOT_FOUND = 'TENANT_NOT_FOUND',
  
  // Server Errors (5xx)
  INTERNAL_ERROR = 'INTERNAL_ERROR',
  SERVICE_UNAVAILABLE = 'SERVICE_UNAVAILABLE',
  GATEWAY_TIMEOUT = 'GATEWAY_TIMEOUT',
  
  // Client-side
  NETWORK_ERROR = 'NETWORK_ERROR',
  TIMEOUT = 'TIMEOUT',
  ABORTED = 'ABORTED',
}

/**
 * Map HTTP status codes to error codes.
 */
export const STATUS_TO_CODE: Record<number, ErrorCode> = {
  400: ErrorCode.VALIDATION_ERROR,
  401: ErrorCode.UNAUTHORIZED,
  403: ErrorCode.FORBIDDEN,
  404: ErrorCode.NOT_FOUND,
  409: ErrorCode.CONFLICT,
  429: ErrorCode.RATE_LIMITED,
  500: ErrorCode.INTERNAL_ERROR,
  502: ErrorCode.SERVICE_UNAVAILABLE,
  503: ErrorCode.SERVICE_UNAVAILABLE,
  504: ErrorCode.GATEWAY_TIMEOUT,
};
```

### 9.2 Error Transformation

```typescript
// src/runtime/errors/transform.ts

import type { ApiError, ValidationError, ErrorDetail } from './types';
import { ErrorCode, STATUS_TO_CODE } from './types';

/**
 * ProblemDetails format from .NET API.
 */
interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

/**
 * Transform HTTP response to typed ApiError.
 */
export async function transformErrorResponse(
  response: Response,
  correlationId?: string
): Promise<ApiError> {
  const status = response.status;
  
  let body: ProblemDetails | null = null;
  try {
    const contentType = response.headers.get('content-type');
    if (contentType?.includes('application/json') || contentType?.includes('application/problem+json')) {
      body = await response.json();
    }
  } catch {
    // Ignore parse errors
  }
  
  // Extract trace ID from header or body
  const traceId = response.headers.get('x-trace-id') ?? body?.traceId;
  
  // Determine error code
  let code = STATUS_TO_CODE[status] ?? ErrorCode.INTERNAL_ERROR;
  
  // Check for specific error types in body
  if (body?.type) {
    code = mapProblemTypeToCode(body.type) ?? code;
  }
  
  // Build error details from validation errors
  const details: ErrorDetail[] = [];
  if (body?.errors) {
    for (const [field, messages] of Object.entries(body.errors)) {
      for (const message of messages) {
        details.push({
          field: field === '' ? undefined : field,
          code: 'VALIDATION_FAILED',
          message,
        });
      }
    }
  }
  
  const error: ApiError = {
    status,
    code,
    message: body?.detail ?? body?.title ?? response.statusText ?? 'An error occurred',
    correlationId,
    traceId,
    details: details.length > 0 ? details : undefined,
    timestamp: new Date().toISOString(),
    instance: body?.instance,
  };
  
  return error;
}

/**
 * Map ProblemDetails type URI to error code.
 */
function mapProblemTypeToCode(type: string): ErrorCode | null {
  const mapping: Record<string, ErrorCode> = {
    'https://tools.ietf.org/html/rfc7231#section-6.5.1': ErrorCode.VALIDATION_ERROR,
    'https://tools.ietf.org/html/rfc7231#section-6.5.3': ErrorCode.FORBIDDEN,
    'https://tools.ietf.org/html/rfc7231#section-6.5.4': ErrorCode.NOT_FOUND,
    'https://tools.ietf.org/html/rfc7231#section-6.5.8': ErrorCode.CONFLICT,
    'https://tools.ietf.org/html/rfc6585#section-4': ErrorCode.RATE_LIMITED,
  };
  
  return mapping[type] ?? null;
}

/**
 * Transform network/fetch errors to ApiError.
 */
export function transformNetworkError(error: Error, correlationId?: string): ApiError {
  if (error.name === 'AbortError') {
    return {
      status: 0,
      code: ErrorCode.ABORTED,
      message: 'Request was aborted',
      correlationId,
    };
  }
  
  if (error.name === 'TimeoutError' || error.message.includes('timeout')) {
    return {
      status: 0,
      code: ErrorCode.TIMEOUT,
      message: 'Request timed out',
      correlationId,
    };
  }
  
  return {
    status: 0,
    code: ErrorCode.NETWORK_ERROR,
    message: error.message || 'Network error occurred',
    correlationId,
  };
}
```

### 9.3 Result Type

```typescript
// src/runtime/result/types.ts

/**
 * Result type for API operations.
 * Provides type-safe success/error handling.
 */
export type Result<T, E = Error> = 
  | { ok: true; data: T; error?: never }
  | { ok: false; data?: never; error: E };

/**
 * Create a success result.
 */
export function ok<T>(data: T): Result<T, never> {
  return { ok: true, data };
}

/**
 * Create an error result.
 */
export function err<E>(error: E): Result<never, E> {
  return { ok: false, error };
}

/**
 * Unwrap a result, throwing if error.
 */
export function unwrap<T, E>(result: Result<T, E>): T {
  if (result.ok) {
    return result.data;
  }
  throw result.error;
}

/**
 * Unwrap with default value.
 */
export function unwrapOr<T, E>(result: Result<T, E>, defaultValue: T): T {
  return result.ok ? result.data : defaultValue;
}

/**
 * Map success value.
 */
export function map<T, U, E>(result: Result<T, E>, fn: (value: T) => U): Result<U, E> {
  if (result.ok) {
    return ok(fn(result.data));
  }
  return result;
}

/**
 * Map error value.
 */
export function mapErr<T, E, F>(result: Result<T, E>, fn: (error: E) => F): Result<T, F> {
  if (!result.ok) {
    return err(fn(result.error));
  }
  return result;
}

/**
 * Chain results.
 */
export async function andThen<T, U, E>(
  result: Result<T, E>,
  fn: (value: T) => Promise<Result<U, E>>
): Promise<Result<U, E>> {
  if (result.ok) {
    return fn(result.data);
  }
  return result;
}

/**
 * Combine multiple results.
 */
export function all<T extends readonly Result<unknown, unknown>[]>(
  ...results: T
): Result<
  { [K in keyof T]: T[K] extends Result<infer U, unknown> ? U : never },
  T[number] extends Result<unknown, infer E> ? E : never
> {
  const data: unknown[] = [];
  
  for (const result of results) {
    if (!result.ok) {
      return result as Result<never, T[number] extends Result<unknown, infer E> ? E : never>;
    }
    data.push(result.data);
  }
  
  return ok(data as { [K in keyof T]: T[K] extends Result<infer U, unknown> ? U : never });
}
```

---

## 10. Security Review

### 10.1 Identified Risks and Mitigations

| Risk | Severity | Category | Mitigation | Status |
|------|----------|----------|------------|--------|
| Token leakage in logs | HIGH | Auth | Safe logging that redacts Authorization headers | ✅ Designed |
| SSR token exposure in HTML | CRITICAL | Auth | Server-only token access patterns, no client hydration | ✅ Designed |
| Cross-tenant caching | CRITICAL | Multi-Tenant | Tenant ID in cache keys, fail-closed tenant validation | ✅ Designed |
| Token refresh storms | HIGH | Auth | Mutex pattern for refresh, single in-flight request | ✅ Designed |
| CSRF attacks | HIGH | Auth | CSRF token support for cookie-based auth | ✅ Designed |
| localStorage token storage | MEDIUM | Auth | Recommend HTTP-only cookies, warn on localStorage | ✅ Designed |
| Credential reuse across tenants | CRITICAL | Multi-Tenant | Separate client instances per tenant | ✅ Designed |
| Stale feature flags | MEDIUM | Features | Stale-while-revalidate, configurable TTL | ✅ Designed |
| Retry amplification | HIGH | Network | Exponential backoff, max retries, jitter | ✅ Designed |
| Correlation ID injection | LOW | Network | Validate correlation ID format | ✅ Designed |

### 10.2 Safe Logging Implementation

```typescript
// src/plugins/logging.ts

import type { Plugin, RequestContext, ResponseContext } from '../runtime/transport/types';

interface LoggingOptions {
  level: 'debug' | 'info' | 'warn' | 'error';
  /** Custom logger implementation */
  logger?: Logger;
  /** Additional headers to redact */
  redactHeaders?: string[];
  /** Log request bodies (DANGER: may contain sensitive data) */
  logRequestBody?: boolean;
  /** Log response bodies (DANGER: may contain sensitive data) */
  logResponseBody?: boolean;
  /** Maximum body length to log */
  maxBodyLength?: number;
}

interface Logger {
  debug(message: string, data?: Record<string, unknown>): void;
  info(message: string, data?: Record<string, unknown>): void;
  warn(message: string, data?: Record<string, unknown>): void;
  error(message: string, data?: Record<string, unknown>): void;
}

/**
 * Headers that are ALWAYS redacted.
 * These contain authentication credentials.
 */
const ALWAYS_REDACT_HEADERS = [
  'authorization',
  'x-api-key',
  'cookie',
  'set-cookie',
  'x-csrf-token',
  'x-auth-token',
  'proxy-authorization',
];

/**
 * Patterns that indicate sensitive data in any header.
 */
const SENSITIVE_PATTERNS = [
  /token/i,
  /secret/i,
  /password/i,
  /credential/i,
  /key/i,
  /auth/i,
];

const REDACTED = '[REDACTED]';

export function createLoggingPlugin(options: LoggingOptions = { level: 'info' }): Plugin {
  const logger = options.logger ?? console;
  const redactHeaders = new Set([
    ...ALWAYS_REDACT_HEADERS,
    ...(options.redactHeaders ?? []).map(h => h.toLowerCase()),
  ]);
  
  return {
    name: 'logging',
    
    onRequest(request: Request, context: RequestContext): Request {
      const safeHeaders = redactHeaders(request.headers, redactHeaders);
      
      const logData: Record<string, unknown> = {
        method: request.method,
        url: request.url,
        headers: safeHeaders,
        correlationId: context.correlationId,
      };
      
      if (options.logRequestBody && context.body) {
        logData.body = truncateBody(context.body, options.maxBodyLength ?? 1000);
      }
      
      logger[options.level]('API Request', logData);
      
      return request;
    },
    
    onResponse(response: Response, context: ResponseContext): Response {
      const safeHeaders = redactHeaders(response.headers, redactHeaders);
      
      const logData: Record<string, unknown> = {
        status: response.status,
        statusText: response.statusText,
        headers: safeHeaders,
        correlationId: context.correlationId,
        duration: context.duration,
      };
      
      logger[options.level]('API Response', logData);
      
      return response;
    },
    
    onError(error: Error, context: RequestContext): Error {
      logger.error('API Error', {
        message: error.message,
        name: error.name,
        correlationId: context.correlationId,
        // NEVER log stack traces in production (may contain sensitive data)
        ...(process.env.NODE_ENV !== 'production' && { stack: error.stack }),
      });
      
      return error;
    },
  };
}

function redactHeaders(
  headers: Headers,
  redactSet: Set<string>
): Record<string, string> {
  const result: Record<string, string> = {};
  
  headers.forEach((value, key) => {
    const lowerKey = key.toLowerCase();
    
    // Check explicit redact list
    if (redactSet.has(lowerKey)) {
      result[key] = REDACTED;
      return;
    }
    
    // Check sensitive patterns
    if (SENSITIVE_PATTERNS.some(pattern => pattern.test(key))) {
      result[key] = REDACTED;
      return;
    }
    
    result[key] = value;
  });
  
  return result;
}

function truncateBody(body: unknown, maxLength: number): string {
  const str = typeof body === 'string' ? body : JSON.stringify(body);
  if (str.length <= maxLength) {
    return str;
  }
  return str.slice(0, maxLength) + `... [truncated, ${str.length} total chars]`;
}
```

### 10.3 SSR Safety Guidelines

```typescript
// src/integrations/next/ssr-safety.ts

/**
 * SSR Safety Guidelines for GameGuild SDK
 * 
 * CRITICAL: Follow these patterns to prevent token leakage in SSR.
 */

/**
 * Rule 1: NEVER pass tokens through component props.
 * 
 * ❌ WRONG:
 * ```tsx
 * // Server Component
 * export default async function Page() {
 *   const session = await auth();
 *   return <ClientComponent token={session.api.accessToken} />;
 * }
 * ```
 * 
 * ✅ CORRECT:
 * ```tsx
 * // Server Component
 * export default async function Page() {
 *   const data = await fetchData(); // Fetch on server
 *   return <ClientComponent data={data} />; // Pass data, not token
 * }
 * ```
 */

/**
 * Rule 2: Use Server Actions for authenticated API calls from Client Components.
 * 
 * ✅ CORRECT:
 * ```tsx
 * // actions.ts (Server Action)
 * 'use server';
 * export async function fetchUserData() {
 *   const client = await createServerClient({ ... });
 *   return client.users.getMe();
 * }
 * 
 * // ClientComponent.tsx
 * 'use client';
 * function ClientComponent() {
 *   const { data } = useAction(fetchUserData);
 *   return <div>{data?.name}</div>;
 * }
 * ```
 */

/**
 * Runtime check for SSR safety.
 * Use in development to catch accidental token exposure.
 */
export function assertNoTokenInProps(props: Record<string, unknown>): void {
  if (process.env.NODE_ENV === 'production') return;
  
  const sensitivePatterns = [
    /token/i,
    /secret/i,
    /password/i,
    /credential/i,
    /authorization/i,
  ];
  
  function checkValue(key: string, value: unknown, path: string): void {
    if (value === null || value === undefined) return;
    
    // Check key name
    if (sensitivePatterns.some(p => p.test(key))) {
      console.error(
        `⚠️ SSR Safety Warning: Potentially sensitive prop detected at '${path}'.\n` +
        `This may expose secrets in server-rendered HTML.\n` +
        `Consider using Server Actions instead.`
      );
    }
    
    // Check string values that look like tokens
    if (typeof value === 'string' && value.length > 20) {
      if (value.startsWith('eyJ') || value.match(/^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\./)) {
        console.error(
          `⚠️ SSR Safety Warning: JWT-like value detected at '${path}'.\n` +
          `This will expose the token in server-rendered HTML.\n` +
          `NEVER pass tokens through props.`
        );
      }
    }
    
    // Recurse into objects
    if (typeof value === 'object' && !Array.isArray(value)) {
      for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
        checkValue(k, v, `${path}.${k}`);
      }
    }
  }
  
  for (const [key, value] of Object.entries(props)) {
    checkValue(key, value, key);
  }
}

/**
 * Provider that validates SSR safety in development.
 */
export function SSRSafetyProvider({ children }: { children: React.ReactNode }) {
  if (process.env.NODE_ENV !== 'production' && typeof window === 'undefined') {
    // Server-side: check for token exposure in global state
    if ((globalThis as Record<string, unknown>).__NEXT_DATA__) {
      const data = (globalThis as Record<string, unknown>).__NEXT_DATA__ as Record<string, unknown>;
      assertNoTokenInProps(data.props as Record<string, unknown>);
    }
  }
  
  return children;
}
```

### 10.4 Rate Limiting and Retry Plugin

```typescript
// src/plugins/retry.ts

import type { Plugin, RequestContext } from '../runtime/transport/types';

interface RetryOptions {
  /** Maximum retry attempts */
  maxAttempts?: number;
  /** Base delay in ms */
  baseDelayMs?: number;
  /** Maximum delay in ms */
  maxDelayMs?: number;
  /** Backoff strategy */
  backoff?: 'linear' | 'exponential';
  /** Add jitter to prevent thundering herd */
  jitter?: boolean;
  /** Status codes that trigger retry */
  retryableStatuses?: number[];
  /** Retry on network errors */
  retryNetworkErrors?: boolean;
  /** Called before each retry */
  onRetry?: (attempt: number, delay: number, error: Error) => void;
}

const DEFAULT_OPTIONS: Required<RetryOptions> = {
  maxAttempts: 3,
  baseDelayMs: 1000,
  maxDelayMs: 30000,
  backoff: 'exponential',
  jitter: true,
  retryableStatuses: [408, 429, 500, 502, 503, 504],
  retryNetworkErrors: true,
  onRetry: () => {},
};

export function createRetryPlugin(options: RetryOptions = {}): Plugin {
  const config = { ...DEFAULT_OPTIONS, ...options };
  const retryableStatuses = new Set(config.retryableStatuses);
  
  return {
    name: 'retry',
    
    async onRequest(request: Request, context: RequestContext): Promise<Request> {
      // Store retry count in context
      (context as Record<string, unknown>).__retryCount = 0;
      return request;
    },
    
    async onError(error: Error, context: RequestContext, retry: () => Promise<Response>): Promise<Response | Error> {
      const retryCount = ((context as Record<string, unknown>).__retryCount as number) ?? 0;
      
      // Check if we should retry
      if (retryCount >= config.maxAttempts) {
        return error; // Max retries reached
      }
      
      // Check if error is retryable
      if (!isRetryableError(error, config.retryNetworkErrors)) {
        return error;
      }
      
      // Calculate delay
      const delay = calculateDelay(retryCount, config);
      
      // Notify callback
      config.onRetry(retryCount + 1, delay, error);
      
      // Wait and retry
      await sleep(delay);
      
      // Increment retry count
      (context as Record<string, unknown>).__retryCount = retryCount + 1;
      
      return retry();
    },
    
    async onResponse(response: Response, context: RequestContext, retry: () => Promise<Response>): Promise<Response> {
      const retryCount = ((context as Record<string, unknown>).__retryCount as number) ?? 0;
      
      // Check if status is retryable
      if (!retryableStatuses.has(response.status)) {
        return response;
      }
      
      // Check retry count
      if (retryCount >= config.maxAttempts) {
        return response;
      }
      
      // Handle rate limiting
      let delay: number;
      if (response.status === 429) {
        // Respect Retry-After header
        const retryAfter = response.headers.get('Retry-After');
        if (retryAfter) {
          delay = parseRetryAfter(retryAfter);
        } else {
          delay = calculateDelay(retryCount, config);
        }
      } else {
        delay = calculateDelay(retryCount, config);
      }
      
      // Notify callback
      config.onRetry(retryCount + 1, delay, new Error(`HTTP ${response.status}`));
      
      // Wait and retry
      await sleep(delay);
      
      // Increment retry count
      (context as Record<string, unknown>).__retryCount = retryCount + 1;
      
      return retry();
    },
  };
}

function calculateDelay(attempt: number, config: Required<RetryOptions>): number {
  let delay: number;
  
  if (config.backoff === 'exponential') {
    delay = config.baseDelayMs * Math.pow(2, attempt);
  } else {
    delay = config.baseDelayMs * (attempt + 1);
  }
  
  // Apply max delay cap
  delay = Math.min(delay, config.maxDelayMs);
  
  // Add jitter (±25%)
  if (config.jitter) {
    const jitterRange = delay * 0.25;
    delay += (Math.random() * 2 - 1) * jitterRange;
  }
  
  return Math.round(delay);
}

function isRetryableError(error: Error, retryNetworkErrors: boolean): boolean {
  if (!retryNetworkErrors) return false;
  
  // Network errors
  if (error.name === 'TypeError' && error.message.includes('fetch')) {
    return true;
  }
  
  // Timeout errors
  if (error.name === 'TimeoutError') {
    return true;
  }
  
  return false;
}

function parseRetryAfter(value: string): number {
  // Try parsing as seconds
  const seconds = parseInt(value, 10);
  if (!isNaN(seconds)) {
    return seconds * 1000;
  }
  
  // Try parsing as HTTP date
  const date = new Date(value);
  if (!isNaN(date.getTime())) {
    return Math.max(0, date.getTime() - Date.now());
  }
  
  // Default: 1 second
  return 1000;
}

function sleep(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}
```

### 10.5 Idempotency Support

```typescript
// src/plugins/idempotency.ts

import type { Plugin, RequestContext } from '../runtime/transport/types';
import { randomUUID } from 'crypto';

interface IdempotencyOptions {
  /** Header name for idempotency key */
  headerName?: string;
  /** Methods that require idempotency key */
  methods?: string[];
  /** Custom key generator */
  keyGenerator?: () => string;
}

const DEFAULT_OPTIONS: Required<IdempotencyOptions> = {
  headerName: 'Idempotency-Key',
  methods: ['POST', 'PUT', 'PATCH'],
  keyGenerator: () => randomUUID(),
};

/**
 * Plugin that adds idempotency keys to mutation requests.
 * Prevents duplicate submissions when retrying.
 */
export function createIdempotencyPlugin(options: IdempotencyOptions = {}): Plugin {
  const config = { ...DEFAULT_OPTIONS, ...options };
  const methodsSet = new Set(config.methods.map(m => m.toUpperCase()));
  
  // Store keys for retry consistency
  const keyStore = new WeakMap<Request, string>();
  
  return {
    name: 'idempotency',
    
    onRequest(request: Request, context: RequestContext): Request {
      // Only add to mutation methods
      if (!methodsSet.has(request.method.toUpperCase())) {
        return request;
      }
      
      // Check if key already exists (from previous attempt)
      let key = keyStore.get(request);
      
      if (!key) {
        // Check if user provided a key in headers
        key = request.headers.get(config.headerName) ?? undefined;
        
        // Generate new key if not provided
        if (!key) {
          key = config.keyGenerator();
        }
        
        keyStore.set(request, key);
      }
      
      // Add header
      const headers = new Headers(request.headers);
      headers.set(config.headerName, key);
      
      return new Request(request.url, {
        ...request,
        headers,
      });
    },
  };
}
```

---

*This completes Part 3. Continue to Part 4 for CI/CD Automation, Test Plan, Implementation Roadmap, and Final Report.*
