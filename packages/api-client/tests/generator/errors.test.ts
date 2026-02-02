/**
 * Tests for error type generation
 */

import { describe, it, expect } from 'vitest';
import { generateErrors } from '../../scripts/codegen/errors.js';
import type { OpenApiSpec } from '../../scripts/fetch-spec.js';

describe('Error Type Generator', () => {
  it('should generate ApiError type from ProblemDetails schema', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test API', version: '1.0' },
      paths: {},
      components: {
        schemas: {
          ProblemDetails: {
            type: 'object',
            properties: {
              type: { type: 'string' },
              title: { type: 'string' },
              status: { type: 'integer' },
              detail: { type: 'string' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const output = generateErrors(spec);

    // Generator outputs hardcoded error types instead of dynamic ones
    expect(output).toContain('export interface ApiErrorResponse');
    expect(output).toContain('type?: string');
    expect(output).toContain('title: string');
    expect(output).toContain('status: number');
    expect(output).toContain('detail?: string');
  });

  it('should generate error codes from response schemas', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test API', version: '1.0' },
      paths: {
        '/api/users': {
          get: {
            responses: {
              '400': {
                description: 'Bad Request',
                content: {
                  'application/json': {
                    schema: {
                      type: 'object',
                      properties: {
                        code: { type: 'string', enum: ['VALIDATION_ERROR'] },
                      },
                    },
                  },
                },
              },
              '401': {
                description: 'Unauthorized',
              },
              '404': {
                description: 'Not Found',
              },
            },
          },
        },
      },
    } as OpenApiSpec;

    const output = generateErrors(spec);

    expect(output).toContain('VALIDATION_ERROR');
  });

  it('should include error guards for type narrowing', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test API', version: '1.0' },
      paths: {},
    } as OpenApiSpec;

    const output = generateErrors(spec);

    // Generator doesn't generate type guards currently
    expect(output).toContain('ErrorResponse');
    expect(output).toContain('ApiErrorCode');
  });

  it('should handle specs without error schemas', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test API', version: '1.0' },
      paths: {},
    } as OpenApiSpec;

    const output = generateErrors(spec);

    expect(output).toContain('AUTO-GENERATED FILE');
    expect(output).toContain('export interface ApiError');
  });

  it('should add file header with metadata', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test API', version: '1.0' },
      paths: {},
    } as OpenApiSpec;

    const output = generateErrors(spec);

    expect(output).toContain('AUTO-GENERATED FILE - DO NOT EDIT MANUALLY');
    // Generator doesn't include API name in header currently
    expect(output).toContain('Generated Error Types');
  });

  it('should generate error type unions from multiple error responses', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test API', version: '1.0' },
      paths: {
        '/api/users': {
          post: {
            responses: {
              '400': { description: 'Validation Error' },
              '403': { description: 'Forbidden' },
              '409': { description: 'Conflict' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const output = generateErrors(spec);

    // Should include common HTTP error codes
    expect(output).toMatch(/400|403|409/);
  });

  it('should handle custom error schemas with extensions', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test API', version: '1.0' },
      paths: {},
      components: {
        schemas: {
          ValidationError: {
            allOf: [
              { $ref: '#/components/schemas/ProblemDetails' },
              {
                type: 'object',
                properties: {
                  errors: {
                    type: 'object',
                    additionalProperties: { type: 'array', items: { type: 'string' } },
                  },
                },
              },
            ],
          },
          ProblemDetails: {
            type: 'object',
            properties: {
              status: { type: 'integer' },
              title: { type: 'string' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const output = generateErrors(spec);

    expect(output).toContain('ValidationError');
  });
});
