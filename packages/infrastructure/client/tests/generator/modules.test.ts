import { describe, expect, it } from 'vitest';

import { generateModules } from '../../scripts/codegen/modules.js';
import type { OpenApiSpec } from '../../scripts/fetch-spec.js';

describe('Module Generator', () => {
  it('preserves word boundaries from PascalCase OpenAPI tags in public module names', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test API', version: '1.0.0' },
      paths: {
        '/v1/subscriptions': {
          get: {
            operationId: 'getSubscriptions',
            tags: ['CommerceSubscriptions'],
            responses: { '200': { description: 'OK' } },
          },
        },
      },
    } as OpenApiSpec;

    const modules = generateModules(spec);

    expect(modules['commerce-subscriptions']).toContain('export class CommerceSubscriptionsModule');
  });

  it('emits required body parameters before optional query parameters', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test API', version: '1.0.0' },
      paths: {
        '/v1/courses/{courseId}/interactions/progress': {
          put: {
            operationId: 'putCourseInteractionsProgress',
            tags: ['Learning/courses/contentInteraction'],
            parameters: [
              {
                name: 'courseId',
                in: 'path',
                required: true,
                schema: { type: 'string' },
              },
              {
                name: 'includeHistory',
                in: 'query',
                required: false,
                schema: { type: 'boolean' },
              },
            ],
            requestBody: {
              required: true,
              content: {
                'application/json': {
                  schema: {
                    $ref: '#/components/schemas/UpdateProgressInput',
                  },
                },
              },
            },
            responses: {
              '200': {
                description: 'Updated',
                content: {
                  'application/json': {
                    schema: {
                      $ref: '#/components/schemas/ProgressResult',
                    },
                  },
                },
              },
            },
          },
        },
      },
      components: {
        schemas: {
          UpdateProgressInput: {
            type: 'object',
            properties: {
              progress: { type: 'number' },
            },
          },
          ProgressResult: {
            type: 'object',
            properties: {
              success: { type: 'boolean' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const modules = generateModules(spec);
    const output = modules['learning-courses-content-interaction'];

    expect(output).toBeDefined();
    expect(output).toContain(
      'async putCourseInteractionsProgress(courseId: string, body: Types.UpdateProgressInput, query?: { includeHistory?: boolean })',
    );
  });
});
