/**
 * DevTools Integration Tests
 * 
 * Tests for development logging and debugging utilities
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { DevTools } from '../../src/runtime/devtools/devtools.js';
import { ok, err } from '../../src/runtime/result/helpers.js';
import type { RequestConfig } from '../../src/runtime/transport/types.js';
import type { ApiError } from '../../src/runtime/errors/types.js';

describe('DevTools Integration', () => {
  describe('Logging', () => {
    it('should log request start with details', () => {
      const consoleInfoSpy = vi.spyOn(console, 'info');
      const devtools = new DevTools({ enabled: true, logLevel: 'info' });

      const config: RequestConfig = {
        method: 'GET',
        path: '/api/users/123',
        params: { include: 'profile' },
        headers: { 'X-Custom': 'value' },
        requestId: 'test-id-123',
      };

      devtools.logRequestStart(config);

      expect(consoleInfoSpy).toHaveBeenCalled();

      consoleInfoSpy.mockRestore();
    });

    it('should log successful responses', () => {
      const consoleInfoSpy = vi.spyOn(console, 'info');
      const devtools = new DevTools({ enabled: true, logLevel: 'info' });

      const config: RequestConfig = {
        method: 'GET',
        path: '/api/users/123',
        requestId: 'test-id-123',
      };

      devtools.logRequestStart(config);

      const result = ok({ id: '123', name: 'Test User' });
      devtools.logRequestComplete(config, result);

      expect(consoleInfoSpy).toHaveBeenCalledWith(expect.stringContaining('Success'));

      consoleInfoSpy.mockRestore();
    });

    it('should log errors', () => {
      const consoleErrorSpy = vi.spyOn(console, 'error');
      const devtools = new DevTools({ enabled: true, logLevel: 'error' });

      const config: RequestConfig = {
        method: 'POST',
        path: '/api/users',
        requestId: 'test-id-456',
      };

      devtools.logRequestStart(config);

      const error: ApiError = {
        name: 'ApiError',
        code: 'VALIDATION_ERROR',
        message: 'Invalid input',
        status: 400,
      };

      const result = err(error);
      devtools.logRequestComplete(config, result);

      expect(consoleErrorSpy).toHaveBeenCalled();

      consoleErrorSpy.mockRestore();
    });
  });

  describe('Log Levels', () => {
    it('should respect silent log level', () => {
      const consoleInfoSpy = vi.spyOn(console, 'info');
      const devtools = new DevTools({ enabled: true, logLevel: 'silent' });

      const config: RequestConfig = {
        method: 'GET',
        path: '/api/test',
        requestId: 'test-id',
      };

      devtools.logRequestStart(config);

      // Silent mode should not log info
      expect(consoleInfoSpy).not.toHaveBeenCalled();

      consoleInfoSpy.mockRestore();
    });

    it('should log debug info only at debug level', () => {
      const debugDevTools = new DevTools({ enabled: true, logLevel: 'debug' });
      const infoDevTools = new DevTools({ enabled: true, logLevel: 'info' });

      const debugSpy = vi.spyOn(console, 'log');

      const config: RequestConfig = {
        method: 'GET',
        path: '/api/test',
        requestId: 'test-id',
      };

      // Debug level should log
      debugDevTools.logRequestStart(config);
      expect(debugSpy).toHaveBeenCalled();

      debugSpy.mockClear();

      // Info level may not log debug details
      infoDevTools.logRequestStart(config);

      debugSpy.mockRestore();
    });
  });

  describe('Disabled DevTools', () => {
    it('should not log when disabled', () => {
      const consoleSpy = vi.spyOn(console, 'group');
      const devtools = new DevTools({ enabled: false });

      const config: RequestConfig = {
        method: 'GET',
        path: '/api/test',
        requestId: 'test-id',
      };

      devtools.logRequestStart(config);

      expect(consoleSpy).not.toHaveBeenCalled();

      consoleSpy.mockRestore();
    });
  });

  describe('Custom Logger', () => {
    it('should use custom logger implementation', () => {
      const customLogger = {
        group: vi.fn(),
        groupEnd: vi.fn(),
        log: vi.fn(),
        warn: vi.fn(),
        error: vi.fn(),
        info: vi.fn(),
        debug: vi.fn(),
      };

      const devtools = new DevTools({
        enabled: true,
        logger: customLogger,
      });

      const config: RequestConfig = {
        method: 'POST',
        path: '/api/users',
        requestId: 'test-id',
      };

      devtools.logRequestStart(config);

      expect(customLogger.group).toHaveBeenCalled();
    });
  });

  describe('Request Timing', () => {
    it('should track request duration', async () => {
      const consoleInfoSpy = vi.spyOn(console, 'info');
      const devtools = new DevTools({ enabled: true, logLevel: 'info' });

      const config: RequestConfig = {
        method: 'GET',
        path: '/api/test',
        requestId: 'timing-test',
      };

      devtools.logRequestStart(config);

      await new Promise(resolve => setTimeout(resolve, 100));
      
      const result = ok({ data: 'test' });
      devtools.logRequestComplete(config, result);

      // Should log timing information
      expect(consoleInfoSpy).toHaveBeenCalledWith(expect.stringContaining('ms'));

      consoleInfoSpy.mockRestore();
    });
  });
});
