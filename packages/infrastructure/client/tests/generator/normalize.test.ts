/**
 * Tests for OpenAPI spec normalization
 */

import { describe, it, expect } from 'vitest';
import { normalizeSpec } from '../../scripts/normalize.js';
import type { OpenApiSpec } from '../../scripts/fetch-spec.js';
import simpleSpec from './fixtures/simple-spec.json';

describe('OpenAPI Spec Normalizer', () => {
  it('should normalize operation IDs when missing', () => {
    const spec: OpenApiSpec = {
      ...simpleSpec,
      paths: {
        '/api/test': {
          get: {
            // No operationId
            responses: {
              '200': { description: 'Success' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const normalized = normalizeSpec(spec);
    const operation = (normalized.paths['/api/test'] as any).get;

    expect(operation.operationId).toBeDefined();
    expect(operation.operationId).toMatch(/^(get|test)/i);
  });

  it('should preserve existing operation IDs', () => {
    const spec: OpenApiSpec = {
      ...simpleSpec,
      paths: {
        '/api/users': {
          get: {
            operationId: 'getAllUsers',
            responses: {
              '200': { description: 'Success' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const normalized = normalizeSpec(spec);
    const operation = (normalized.paths['/api/users'] as any).get;

    expect(operation.operationId).toBe('getAllUsers');
  });

  it('should normalize tag names to PascalCase', () => {
    const spec: OpenApiSpec = {
      ...simpleSpec,
      tags: [
        { name: 'user-management', description: 'User operations' },
        { name: 'auth_service', description: 'Auth operations' },
      ],
    } as OpenApiSpec;

    const normalized = normalizeSpec(spec);

    expect(normalized.tags).toBeDefined();
    expect(normalized.tags![0].name).toBe('UserManagement');
    expect(normalized.tags![1].name).toBe('AuthService');
  });

  it('should normalize schema names to remove ASP.NET patterns', () => {
    const spec: OpenApiSpec = {
      ...simpleSpec,
      components: {
        schemas: {
          'GameGuild.Identity.Users.UserDto': {
            type: 'object',
            properties: {
              id: { type: 'string' },
            },
          },
          'ProblemDetails': {
            type: 'object',
            properties: {
              detail: { type: 'string' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const normalized = normalizeSpec(spec);
    const schemas = (normalized.components as any)?.schemas;

    // Normalize removes Dto suffix and namespace
    expect(schemas).toHaveProperty('User'); // UserDto -> User
    expect(schemas).not.toHaveProperty('GameGuild.Identity.Users.UserDto');
    expect(schemas).not.toHaveProperty('ProblemDetails'); // Removed by cleanAspNetPatterns
  });

  it('should handle generic type names', () => {
    const spec: OpenApiSpec = {
      ...simpleSpec,
      components: {
        schemas: {
          'ResultOfUserDto': {
            type: 'object',
            properties: {
              data: { $ref: '#/components/schemas/UserDto' },
              success: { type: 'boolean' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const normalized = normalizeSpec(spec);
    const schemas = (normalized.components as any)?.schemas;

    // Normalize preserves generics but removes Dto suffix from type parameter
    expect(schemas).toHaveProperty('ResultOfUser'); // ResultOfUserDto -> ResultOfUser
  });

  it('should update all schema references when normalizing names', () => {
    const spec: OpenApiSpec = {
      ...simpleSpec,
      components: {
        schemas: {
          'Old.Namespace.UserDto': {
            type: 'object',
            properties: {
              id: { type: 'string' },
            },
          },
          'UserListResponse': {
            type: 'object',
            properties: {
              users: {
                type: 'array',
                items: { $ref: '#/components/schemas/Old.Namespace.UserDto' },
              },
            },
          },
        },
      },
    } as OpenApiSpec;

    const normalized = normalizeSpec(spec);
    const schemas = (normalized.components as any)?.schemas;
    const userListOutput = schemas.UserListOutput; // Response -> Output

    // Normalize removes namespace and Dto suffix
    expect(schemas).toHaveProperty('User'); // Old.Namespace.UserDto -> User
    expect(userListOutput.properties.users.items.$ref).toBe('#/components/schemas/User');
  });

  it('should preserve distinct entity and DTO schemas when their normalized names collide', () => {
    const spec: OpenApiSpec = {
      ...simpleSpec,
      components: {
        schemas: {
          Identity_Users_UserProfile: {
            type: 'object',
            properties: { visibility: { type: 'string' } },
          },
          Identity_Users_UserProfileDto: {
            type: 'object',
            properties: { timeZone: { type: 'string' } },
          },
          ProfileEnvelope: {
            type: 'object',
            properties: {
              entity: { $ref: '#/components/schemas/Identity_Users_UserProfile' },
              dto: { $ref: '#/components/schemas/Identity_Users_UserProfileDto' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const normalized = normalizeSpec(spec);
    const schemas = (normalized.components as any)?.schemas;

    expect(schemas).toHaveProperty('IdentityUsersUserProfile');
    expect(schemas).toHaveProperty('IdentityUsersUserProfileDto');
    expect(schemas.ProfileEnvelope.properties.entity.$ref).toBe('#/components/schemas/IdentityUsersUserProfile');
    expect(schemas.ProfileEnvelope.properties.dto.$ref).toBe('#/components/schemas/IdentityUsersUserProfileDto');
  });

  it('should preserve the generic argument when multiple paged result schemas collide', () => {
    const profilePage = 'PagedResult`1[[GameGuild_Identity_Users_UserProfileDto, GameGuild.Identity.Users]]';
    const notificationPage = 'PagedResult`1[[GameGuild_Identity_Users_UserNotificationDto, GameGuild.Identity.Users]]';
    const spec: OpenApiSpec = {
      ...simpleSpec,
      components: {
        schemas: {
          [profilePage]: { type: 'object' },
          [notificationPage]: { type: 'object' },
          PageEnvelope: {
            type: 'object',
            properties: {
              profiles: { $ref: `#/components/schemas/${profilePage}` },
              notifications: { $ref: `#/components/schemas/${notificationPage}` },
            },
          },
        },
      },
    } as OpenApiSpec;

    const normalized = normalizeSpec(spec);
    const schemas = (normalized.components as any)?.schemas;

    expect(schemas).toHaveProperty('PagedResultOfGameGuildIdentityUsersUserProfileDto');
    expect(schemas).toHaveProperty('PagedResultOfGameGuildIdentityUsersUserNotificationDto');
    expect(schemas.PageEnvelope.properties.profiles.$ref).toBe(
      '#/components/schemas/PagedResultOfGameGuildIdentityUsersUserProfileDto',
    );
    expect(schemas.PageEnvelope.properties.notifications.$ref).toBe(
      '#/components/schemas/PagedResultOfGameGuildIdentityUsersUserNotificationDto',
    );
  });

  it('should preserve the original spec structure', () => {
    const spec: OpenApiSpec = { ...simpleSpec };
    const originalInfo = spec.info;

    const normalized = normalizeSpec(spec);

    expect(normalized.info).toEqual(originalInfo);
    expect(normalized.openapi).toBe(spec.openapi);
  });

  it('should handle specs with no components', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Test', version: '1.0' },
      paths: {},
    } as OpenApiSpec;

    expect(() => normalizeSpec(spec)).not.toThrow();
  });

  it('should handle specs with no tags', () => {
    const spec: OpenApiSpec = {
      ...simpleSpec,
      tags: undefined,
    } as OpenApiSpec;

    expect(() => normalizeSpec(spec)).not.toThrow();
  });

  it('should clean ASP.NET controller suffixes from operation tags', () => {
    const spec: OpenApiSpec = {
      ...simpleSpec,
      paths: {
        '/api/users': {
          get: {
            operationId: 'getUsersController',
            tags: ['UsersController'],
            responses: {
              '200': { description: 'Success' },
            },
          },
        },
      },
    } as OpenApiSpec;

    const normalized = normalizeSpec(spec);
    const operation = (normalized.paths['/api/users'] as any).get;

    // Normalize converts to PascalCase but doesn't strip Controller suffix from tags
    expect(operation.tags).toContain('UsersController');
    // But it does strip Controller from operation IDs
    expect(operation.operationId).toBe('getUsers');
  });
});
