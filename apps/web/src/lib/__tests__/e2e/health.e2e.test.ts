import { describe, it, expect } from 'vitest';
import { createClient } from '@game-guild/client';
import type {
  APIControllersHealthinessOutput,
  APIControllersLivenessOutput,
  APIControllersReadinessOutput,
  APIControllersDependencyHealthOutput,
} from '@game-guild/client';

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';

describe('Health endpoints E2E', () => {
  const client = createClient({
    baseUrl: BASE_URL,
    timeout: 10_000,
    devtools: { enabled: false },
  });

  it('gets health status', async () => {
    const result = await client.request<APIControllersHealthinessOutput>({
      method: 'GET',
      path: '/health',
      requiresAuth: false,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.status).toBeDefined();
      expect(['Healthy', 'Degraded', 'Unhealthy']).toContain(result.data.status);
    }
  });

  it('gets liveness probe', async () => {
    const result = await client.request<APIControllersLivenessOutput>({
      method: 'GET',
      path: '/live',
      requiresAuth: false,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.alive).toBe(true);
      expect(result.data.status).toBeDefined();
    }
  });

  it('gets readiness probe', async () => {
    const result = await client.request<APIControllersReadinessOutput>({
      method: 'GET',
      path: '/ready',
      requiresAuth: false,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.ready).toBeDefined();
      expect(result.data.status).toBeDefined();
    }
  });

  it('gets dependency health', async () => {
    const result = await client.request<APIControllersDependencyHealthOutput>({
      method: 'GET',
      path: '/health/dependencies',
      requiresAuth: false,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.status).toBeDefined();
      expect(result.data.dependencies).toBeDefined();
      expect(Array.isArray(result.data.dependencies)).toBe(true);
    }
  });
});
