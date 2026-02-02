/**
 * OpenAPI Specification Fetcher
 *
 * Fetches and validates the OpenAPI specification from a URL or file.
 */

import { readFileSync } from 'fs';
import type { OpenAPIV3, OpenAPIV3_1 } from 'openapi-types';

export type OpenApiSpec = OpenAPIV3.Document | OpenAPIV3_1.Document;

/**
 * Fetch OpenAPI specification from URL or file path
 */
export async function fetchOpenApiSpec(source: string): Promise<OpenApiSpec> {
  let spec: OpenApiSpec;

  if (source.startsWith('http://') || source.startsWith('https://')) {
    spec = await fetchFromUrl(source);
  } else {
    spec = fetchFromFile(source);
  }

  validateSpec(spec);
  return spec;
}

/**
 * Fetch spec from HTTP(S) URL
 */
async function fetchFromUrl(url: string): Promise<OpenApiSpec> {
  const response = await fetch(url, {
    headers: {
      Accept: 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch OpenAPI spec: HTTP ${response.status} ${response.statusText}`);
  }

  const spec = (await response.json()) as OpenApiSpec;
  return spec;
}

/**
 * Fetch spec from local file
 */
function fetchFromFile(filepath: string): OpenApiSpec {
  const content = readFileSync(filepath, 'utf-8');

  if (filepath.endsWith('.yaml') || filepath.endsWith('.yml')) {
    // Would need yaml parser - for now only support JSON
    throw new Error('YAML OpenAPI specs not yet supported. Please use JSON format.');
  }

  return JSON.parse(content) as OpenApiSpec;
}

/**
 * Validate that the spec is a valid OpenAPI 3.x document
 */
function validateSpec(spec: OpenApiSpec): void {
  if (!spec.openapi) {
    throw new Error('Invalid OpenAPI spec: missing "openapi" field');
  }

  const version = spec.openapi;
  if (!version.startsWith('3.')) {
    throw new Error(`Unsupported OpenAPI version: ${version}. Only 3.x is supported.`);
  }

  if (!spec.info) {
    throw new Error('Invalid OpenAPI spec: missing "info" field');
  }

  if (!spec.paths || Object.keys(spec.paths).length === 0) {
    throw new Error('Invalid OpenAPI spec: no paths defined');
  }
}

/**
 * Get operation IDs from the spec
 */
export function getOperationIds(spec: OpenApiSpec): string[] {
  const ids: string[] = [];

  for (const pathItem of Object.values(spec.paths || {})) {
    if (!pathItem) continue;

    for (const method of ['get', 'post', 'put', 'delete', 'patch', 'options', 'head'] as const) {
      const operation = pathItem[method];
      if (operation?.operationId) {
        ids.push(operation.operationId);
      }
    }
  }

  return ids;
}

/**
 * Get all tags from the spec
 */
export function getTags(spec: OpenApiSpec): string[] {
  const tagSet = new Set<string>();

  // From top-level tags
  for (const tag of spec.tags || []) {
    tagSet.add(tag.name);
  }

  // From operations
  for (const pathItem of Object.values(spec.paths || {})) {
    if (!pathItem) continue;

    for (const method of ['get', 'post', 'put', 'delete', 'patch', 'options', 'head'] as const) {
      const operation = pathItem[method];
      if (operation?.tags) {
        for (const tag of operation.tags) {
          tagSet.add(tag);
        }
      }
    }
  }

  return Array.from(tagSet).sort();
}
