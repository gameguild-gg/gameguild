/**
 * OpenAPI Specification Normalizer
 *
 * Normalizes the OpenAPI spec to ensure consistent naming and structure
 * for code generation.
 */

import type { OpenApiSpec } from './fetch-spec.js';
import type { OpenAPIV3 } from 'openapi-types';
import { toCamelCase, toPascalCase, capitalize, sanitizeIdentifier } from './utils/naming.js';
import { HTTP_METHODS, ASP_NET_PATTERNS } from './codegen/constants.js';

/**
 * Normalize the OpenAPI specification
 */
export function normalizeSpec(spec: OpenApiSpec): OpenApiSpec {
  const normalized = structuredClone(spec);

  // Keep compatibility aliases in the API, but expose only the canonical route in the SDK.
  removeRedundantApiVersionAliases(normalized);

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
 * Remove exact /api/vN aliases when the same operation is also exposed at /vN.
 *
 * Some controllers retain both routes for backwards compatibility. Generating both into the
 * SDK creates duplicate methods for the same action, so prefer the canonical non-/api route.
 */
function removeRedundantApiVersionAliases(spec: OpenApiSpec): void {
  const paths = spec.paths ?? {};

  for (const [path, pathItem] of Object.entries(paths)) {
    if (!pathItem || !/^\/api\/v\d+(?:\/|$)/i.test(path)) continue;

    const canonicalPath = path.replace(/^\/api(?=\/v\d+(?:\/|$))/i, '');
    const canonicalPathItem = paths[canonicalPath];
    if (!canonicalPathItem) continue;

    if (areEquivalentRouteAliases(pathItem, canonicalPathItem)) {
      delete paths[path];
    }
  }
}

function areEquivalentRouteAliases(
  alias: OpenAPIV3.PathItemObject,
  canonical: OpenAPIV3.PathItemObject,
): boolean {
  const comparable = (pathItem: OpenAPIV3.PathItemObject): string => {
    const clone = structuredClone(pathItem) as OpenAPIV3.PathItemObject;

    for (const method of HTTP_METHODS) {
      const operation = clone[method] as OpenAPIV3.OperationObject | undefined;
      if (operation) delete operation.operationId;
    }

    return JSON.stringify(clone);
  };

  return comparable(alias) === comparable(canonical);
}

interface OperationIdCandidate {
  path: string;
  method: string;
  operation: OpenAPIV3.OperationObject;
  baseId: string;
}

/**
 * Ensure all operations have stable, semantic, camelCase operation IDs.
 *
 * Path parameters are omitted while an ID is unique so existing concise names remain stable.
 * When paths collide, every member of that group is regenerated with its parameter names.
 * Unresolved collisions are rejected instead of silently publishing getThing1/getThing2.
 */
function normalizeOperationIds(spec: OpenApiSpec): void {
  const candidates: OperationIdCandidate[] = [];

  for (const [path, pathItem] of Object.entries(spec.paths || {})) {
    if (!pathItem) continue;

    for (const method of HTTP_METHODS) {
      const operation = pathItem[method] as OpenAPIV3.OperationObject | undefined;
      if (!operation) continue;

      const baseId = operation.operationId
        ? normalizeOperationIdName(operation.operationId)
        : generateOperationId(path, method, operation.tags?.[0], false);

      candidates.push({ path, method, operation, baseId });
    }
  }

  const groups = new Map<string, OperationIdCandidate[]>();
  for (const candidate of candidates) {
    const group = groups.get(candidate.baseId) ?? [];
    group.push(candidate);
    groups.set(candidate.baseId, group);
  }

  for (const group of groups.values()) {
    if (group.length === 1) {
      group[0].operation.operationId = group[0].baseId;
      continue;
    }

    for (const candidate of group) {
      candidate.operation.operationId = generateOperationId(
        candidate.path,
        candidate.method,
        candidate.operation.tags?.[0],
        true,
      );
    }
  }

  const assigned = new Map<string, OperationIdCandidate>();
  for (const candidate of candidates) {
    const operationId = candidate.operation.operationId!;
    const previous = assigned.get(operationId);
    if (previous) {
      throw new Error(
        `Unable to create a unique OpenAPI operation ID without a numeric suffix: ` +
          `${previous.method.toUpperCase()} ${previous.path} and ` +
          `${candidate.method.toUpperCase()} ${candidate.path} both resolve to ${operationId}. ` +
          `Assign explicit unique operationIds or remove the duplicate route.`,
      );
    }

    assigned.set(operationId, candidate);
  }
}

/**
 * Generate operation ID from path and method
 */
function generateOperationId(
  path: string,
  method: string,
  tag?: string,
  includePathParameters = false,
): string {
  if (!includePathParameters) {
    // Preserve the established concise names for operations that do not collide.
    const cleanPath = path
      .replace(/\{[^}]+\}/g, '')
      .replace(/^\/api\/v\d+\/?/, '')
      .replace(/^\/v\d+\/?/, '')
      .replace(/:/g, '_')
      .replace(/\//g, '_')
      .replace(/^_|_$/g, '');
    const parts = sanitizeIdentifier(cleanPath).split('_').filter(Boolean);

    let conciseName: string;
    if (parts.length === 0) {
      conciseName = tag ? `${method}${tag}` : method;
    } else {
      conciseName = `${method}${parts.map(capitalize).join('')}`;
    }

    return toCamelCase(conciseName);
  }

  // Remove API version prefixes and split route segments, preserving custom actions.
  const cleanPath = path
    .replace(/^\/api\/v\d+\/?/, '') // Remove /api/v1/
    .replace(/^\/v\d+\/?/, '') // Remove /v1/
    .replace(/^\/+|\/+$/g, '');

  const parts: string[] = [];
  for (const segment of cleanPath.split('/').filter(Boolean)) {
    for (const token of segment.split(':').filter(Boolean)) {
      const parameterMatch = token.match(/^\{([^}:]+)(?::[^}]+)?\}$/);
      if (parameterMatch) {
        if (includePathParameters) {
          parts.push('By', sanitizeIdentifier(parameterMatch[1]));
        }
        continue;
      }

      parts.push(...sanitizeIdentifier(token).split('_').filter(Boolean));
    }
  }

  // Build operation name
  let name: string;
  if (parts.length === 0) {
    name = tag ? `${method}${tag}` : method;
  } else {
    const resource = parts.map(toPascalCase).join('');
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

  // Sanitize to valid identifier (removes colons, special chars)
  normalized = sanitizeIdentifier(normalized);

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
  const components = spec.components as OpenAPIV3.ComponentsObject | undefined;
  const schemas = components?.schemas;
  if (!schemas) return;

  const schemaMapping = new Map<string, string>();
  const preferredNames = new Map<string, string>();
  const preferredNameCounts = new Map<string, number>();
  const assignedNames = new Set<string>();

  // Determine preferred names first so collisions can be disambiguated without
  // making the result depend on the order of schemas in the OpenAPI document.
  for (const name of Object.keys(schemas)) {
    const preferredName = normalizeSchemaName(name);
    preferredNames.set(name, preferredName);
    preferredNameCounts.set(preferredName, (preferredNameCounts.get(preferredName) ?? 0) + 1);
  }

  const assignName = (oldName: string, candidate: string): void => {
    let uniqueName = candidate;
    let suffix = 2;

    while (assignedNames.has(uniqueName)) {
      uniqueName = `${candidate}${suffix}`;
      suffix += 1;
    }

    assignedNames.add(uniqueName);
    schemaMapping.set(oldName, uniqueName);
  };

  // Preserve the concise name for schemas that do not collide.
  for (const [oldName, preferredName] of preferredNames) {
    if (preferredNameCounts.get(preferredName) === 1) {
      assignName(oldName, preferredName);
    }
  }

  // Entity/DTO pairs and closed generic types need a stable semantic suffix.
  for (const [oldName, preferredName] of preferredNames) {
    if (preferredNameCounts.get(preferredName) !== 1) {
      assignName(oldName, normalizeCollidingSchemaName(oldName));
    }
  }

  const renamedSchemas: Record<string, OpenAPIV3.ReferenceObject | OpenAPIV3.SchemaObject> = {};
  for (const [oldName, schema] of Object.entries(schemas)) {
    renamedSchemas[schemaMapping.get(oldName) ?? oldName] = schema;
  }
  components.schemas = renamedSchemas;

  // Update all $ref references
  updateSchemaRefs(spec, schemaMapping);
}

/**
 * Produce a stable unique name when lossy normalization maps multiple schemas
 * to the same value (for example UserProfile and UserProfileDto).
 */
function normalizeCollidingSchemaName(name: string): string {
  const genericMatch = name.match(/^(.+?)`\d+\[\[([^,\]]+)/);
  if (genericMatch) {
    return `${toPascalCase(genericMatch[1])}Of${toPascalCase(genericMatch[2])}`;
  }

  const lastDotIndex = name.lastIndexOf('.');
  const localName = lastDotIndex === -1 ? name : name.substring(lastDotIndex + 1);
  return toPascalCase(localName.replace(/\+/g, ''));
}

/**
 * Normalize a single schema name
 */
function normalizeSchemaName(name: string): string {
  let normalized = name;

  // Strip .NET generic arity and type parameters
  // e.g. "PagedResult`1[[GameGuild_Identity_Users_UserDto, ..." -> "PagedResult"
  // Note: ASP.NET may not close the brackets, so match from backtick to end
  normalized = normalized.replace(/`\d+.*$/, '');

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

