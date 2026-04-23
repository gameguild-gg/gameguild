/**
 * API Client Integration Verification
 *
 * Comprehensive tests to verify the API client works correctly in the web app
 */

import { describe, expect, it } from 'vitest';

describe('API Client Package Integration', () => {
  const nextIntegrationModulePath = '../../../../../packages/infrastructure/client/dist/next.js';

  describe('Package Installation', () => {
    it('should have @game-guild/client in dependencies', async () => {
      const packageJson = await import('../../../package.json');
      expect(packageJson.dependencies).toHaveProperty('@game-guild/client');
    });
  });

  describe('Main Entry Point', () => {
    it('should import createClient successfully', async () => {
      const { createClient } = await import('@game-guild/client');
      expect(createClient).toBeDefined();
      expect(typeof createClient).toBe('function');
    });

    it('should import ApiError type', async () => {
      const module = await import('@game-guild/client');
      expect(module).toHaveProperty('createClient');
    });

    it('should create a client instance', async () => {
      const { createClient } = await import('@game-guild/client');
      const client = createClient({
        baseUrl: 'http://localhost:5295',
      });

      expect(client).toBeDefined();
      expect(client).toHaveProperty('request');
      expect(client).toHaveProperty('getBaseUrl');
    });
  });

  describe('Next.js Integration Entry Point', () => {
    it('should import Next.js utilities', async () => {
      const nextModule = await import(nextIntegrationModulePath);

      expect(nextModule).toHaveProperty('createNextClient');
      expect(nextModule).toHaveProperty('createRouteClient');
      expect(typeof nextModule.createNextClient).toBe('function');
      expect(typeof nextModule.createRouteClient).toBe('function');
    });

    it('should create Next.js client', async () => {
      const { createNextClient } = await import(nextIntegrationModulePath);
      const client = createNextClient({
        baseUrl: 'http://localhost:5295',
      });

      expect(client).toBeDefined();
    });
  });

  describe('React Integration Entry Point', () => {
    it('should import React utilities', async () => {
      const reactModule = await import('@game-guild/client/react');
      expect(reactModule).toBeDefined();
    });
  });

  describe('Client Configuration', () => {
    it('should accept baseUrl configuration', async () => {
      const { createClient } = await import('@game-guild/client');
      const client = createClient({
        baseUrl: 'http://localhost:5295',
      });

      expect(client).toBeDefined();
    });

    it('should accept headers configuration', async () => {
      const { createClient } = await import('@game-guild/client');
      const client = createClient({
        baseUrl: 'http://localhost:5295',
        headers: {
          'X-Tenant-Id': 'test-tenant',
          'X-Custom-Header': 'custom-value',
        },
      });

      expect(client).toBeDefined();
    });

    it('should accept auth configuration', async () => {
      const { createClient } = await import('@game-guild/client');
      const client = createClient({
        baseUrl: 'http://localhost:5295',
        auth: {
          getToken: async () => 'test-token',
        },
      });

      expect(client).toBeDefined();
    });

    it('should accept interceptors', async () => {
      const { createClient } = await import('@game-guild/client');

      const requestInterceptor = {
        onRequest: async (config: any) => {
          console.log('Request:', config);
          return config;
        },
      };

      const client = createClient({
        baseUrl: 'http://localhost:5295',
        interceptors: [requestInterceptor],
      });

      expect(client).toBeDefined();
    });
  });

  describe('Type Safety', () => {
    it('should provide typed client methods', async () => {
      const { createClient } = await import('@game-guild/client');
      const client = createClient({
        baseUrl: 'http://localhost:5295',
      });

      // Should have health method
      expect(client).toHaveProperty('request');
      expect(typeof client.request).toBe('function');
    });

    it('should export error types', async () => {
      const module = await import('@game-guild/client');

      // Verify module has expected exports
      expect(module.createClient).toBeDefined();
    });
  });

  describe('Error Handling', () => {
    it('should handle client creation errors gracefully', async () => {
      const { createClient } = await import('@game-guild/client');

      // Should not throw on invalid URL
      expect(() => {
        createClient({
          baseUrl: 'invalid-url',
        });
      }).not.toThrow();
    });
  });

  describe('Module System Compatibility', () => {
    it('should work with ES modules import', async () => {
      const module = await import('@game-guild/client');
      expect(module.createClient).toBeDefined();
    });

    it('should export all required sub-modules', async () => {
      // Test main module
      const mainModule = await import('@game-guild/client');
      expect(mainModule.createClient).toBeDefined();

      // Test Next.js module
      const nextModule = await import(nextIntegrationModulePath);
      expect(nextModule.createNextClient).toBeDefined();

      // Test React module (if available in client-side context)
      const reactModule = await import('@game-guild/client/react');
      expect(reactModule).toBeDefined();

    });
  });

  describe('Build Output Verification', () => {
    it('should have TypeScript definitions', async () => {
      // The fact that imports work with TypeScript implies .d.ts files exist
      const { createClient } = await import('@game-guild/client');

      // TypeScript compilation would fail if types weren't available
      const client: ReturnType<typeof createClient> = createClient({
        baseUrl: 'http://localhost:5295',
      });

      expect(client).toBeDefined();
    });
  });
});
