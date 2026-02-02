/**
 * OpenAPI Specification Normalizer
 *
 * Normalizes the OpenAPI spec to ensure consistent naming and structure
 * for code generation.
 */

import type { OpenApiSpec } from './fetch-spec.js';
import type { OpenAPIV3 } from 'openapi-types';
import { toCamelCase, toPascalCase, capitalize } from './utils/naming.js';
import { HTTP_METHODS, ASP_NET_PATTERNS } from './codegen/constants.js';

/**
 * Normalize the OpenAPI specification
 */
export function normalizeSpec(spec: OpenApiSpec): OpenApiSpec {
  const normalized = structuredClone(spec);

  // Normalize operation IDs
  normalizeOperationIds(normalized);

  // Normalize tag names
  normalizeTagNames(normalized);

  // Normalize schema names
  normalizeSchemaNames(normalized);

  // Clean up Microsoft/ASP.NET specific patterns
  cleanAspNetPatterns(normalized);

  return normalized;
}

/**
 * Ensure all operations have unique, camelCase operation IDs
 */
function normalizeOperationIds(spec: OpenApiSpec): void {
  const usedIds = new Set<string>();

  for (const [path, pathItem] of Object.entries(spec.paths || {})) {
    if (!pathItem) continue;

    for (const method of HTTP_METHODS) {
      const operation = pathItem[method] as OpenAPIV3.OperationObject | undefined;
      if (!operation) continue;

      let operationId = operation.operationId;

      if (!operationId) {
        // Generate operation ID from path and method
        operationId = generateOperationId(path, method, operation.tags?.[0]);
      } else {
        // Normalize existing operation ID
        operationId = normalizeOperationIdName(operationId);
      }

      // Ensure uniqueness
      let uniqueId = operationId;
      let counter = 1;
      while (usedIds.has(uniqueId)) {
        uniqueId = `${operationId}${counter}`;
        counter++;
      }

      usedIds.add(uniqueId);
      operation.operationId = uniqueId;
    }
  }
}

/**
 * Generate operation ID from path and method
 */
function generateOperationId(path: string, method: string, tag?: string): string {
  // Remove path parameters and clean up
  const cleanPath = path
    .replace(/\{[^}]+\}/g, '') // Remove {param}
    .replace(/^\/api\/v\d+\/?/, '') // Remove /api/v1/
    .replace(/^\/v\d+\/?/, '') // Remove /v1/
    .replace(/\//g, '_') // Replace / with _
    .replace(/^_|_$/g, ''); // Remove leading/trailing _

  const parts = cleanPath.split('_').filter(Boolean);

  // Build operation name
  let name: string;
  if (parts.length === 0) {
    name = tag ? `${method}${tag}` : method;
  } else {
    const resource = parts.map(capitalize).join('');
    name = `${method}${resource}`;
  }

  return toCamelCase(name);
}

/**
 * Normalize operation ID name (remove prefixes, ensure camelCase)
 */
function normalizeOperationIdName(operationId: string): string {
  // Remove common prefixes added by code generators
  let normalized = operationId
    .replace(/^api_/i, '')
    .replace(/^v\d+_/i, '')
    .replace(/_controller_/i, '_')
    .replace(/Controller$/i, '');

  return toCamelCase(normalized);
}

/**
 * Normalize tag names to PascalCase
 */
function normalizeTagNames(spec: OpenApiSpec): void {
  const tagMapping = new Map<string, string>();

  // Normalize top-level tags
  if (spec.tags) {
    for (const tag of spec.tags) {
      const normalized = toPascalCase(tag.name);
      tagMapping.set(tag.name, normalized);
      tag.name = normalized;
    }
  }

  // Update tags in operations
  for (const pathItem of Object.values(spec.paths || {})) {
    if (!pathItem) continue;

    for (const method of HTTP_METHODS) {
      const operation = pathItem[method] as OpenAPIV3.OperationObject | undefined;
      if (!operation?.tags) continue;

      operation.tags = operation.tags.map((tag) => tagMapping.get(tag) || toPascalCase(tag));
    }
  }
}

/**
 * Normalize schema names (remove Dto suffix, clean Microsoft namespaces)
 */
function normalizeSchemaNames(spec: OpenApiSpec): void {
  const schemas = (spec.components as OpenAPIV3.ComponentsObject)?.schemas;
  if (!schemas) return;

  const schemaMapping = new Map<string, string>();

  // Build mapping of old names to new names
  for (const name of Object.keys(schemas)) {
    const normalized = normalizeSchemaName(name);
    if (normalized !== name) {
      schemaMapping.set(name, normalized);
    }
  }

  // Rename schemas
  for (const [oldName, newName] of schemaMapping) {
    if (schemas[oldName] && !schemas[newName]) {
      schemas[newName] = schemas[oldName];
      delete schemas[oldName];
    }
  }

  // Update all $ref references
  updateSchemaRefs(spec, schemaMapping);
}

/**
 * Normalize a single schema name
 */
function normalizeSchemaName(name: string): string {
  let normalized = name;

  // Remove any dotted namespace prefix (e.g., GameGuild.Identity.Users.UserDto -> UserDto)
  const lastDotIndex = normalized.lastIndexOf('.');
  if (lastDotIndex !== -1) {
    normalized = normalized.substring(lastDotIndex + 1);
  }

  // Remove nested class separator
  normalized = normalized.replace(/\+/g, '');

  // Remove common suffixes
  normalized = normalized.replace(/Dto$/i, '');
  normalized = normalized.replace(/Request$/i, 'Input');
  normalized = normalized.replace(/Response$/i, 'Output');

  // Ensure PascalCase
  return toPascalCase(normalized);
}

/**
 * Update all $ref references in the spec
 */
function updateSchemaRefs(spec: OpenApiSpec, mapping: Map<string, string>): void {
  const updateRefs = (obj: unknown): void => {
    if (!obj || typeof obj !== 'object') return;

    if (Array.isArray(obj)) {
      for (const item of obj) {
        updateRefs(item);
      }
      return;
    }

    const record = obj as Record<string, unknown>;

    if ('$ref' in record && typeof record.$ref === 'string') {
      const refMatch = record.$ref.match(/^#\/components\/schemas\/(.+)$/);
      if (refMatch) {
        const oldName = refMatch[1];
        const newName = mapping.get(oldName);
        if (newName) {
          record.$ref = `#/components/schemas/${newName}`;
        }
      }
    }

    for (const value of Object.values(record)) {
      updateRefs(value);
    }
  };

  updateRefs(spec);
}

/**
 * Clean up ASP.NET specific patterns
 */
function cleanAspNetPatterns(spec: OpenApiSpec): void {
  const schemas = (spec.components as OpenAPIV3.ComponentsObject)?.schemas;
  if (!schemas) return;

  // Remove ProblemDetails-related schemas if they exist (we'll use our own)
  for (const name of ASP_NET_PATTERNS.PROBLEM_DETAILS_SCHEMAS) {
    delete schemas[name];
  }
}


