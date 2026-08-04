# GameGuild TypeScript SDK Generator - Part 2

**Generation Pipeline, Authentication, and Authorization Support**

---

## 4. Generation Pipeline

### 4.1 Pipeline Overview

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                        SDK GENERATION PIPELINE                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐   │
│  │   FETCH     │───▶│  NORMALIZE  │───▶│  GENERATE   │───▶│    POST-    │   │
│  │   SPEC      │    │    SPEC     │    │    CODE     │    │   PROCESS   │   │
│  └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘   │
│        │                  │                  │                  │            │
│        ▼                  ▼                  ▼                  ▼            │
│   OpenAPI JSON      Normalized JSON    TypeScript files    Formatted code   │
│   from API          with fixes         (raw generated)     (lint + format)  │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐     │
│  │                         CI/CD INTEGRATION                           │     │
│  │  • Detect changes (hash comparison)                                 │     │
│  │  • Breaking change detection (openapi-diff)                         │     │
│  │  • Semantic versioning automation                                   │     │
│  │  • Changelog generation                                             │     │
│  └─────────────────────────────────────────────────────────────────────┘     │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Step 1: Fetch OpenAPI Specification

```typescript
// scripts/generate.ts

import { createHash } from 'crypto';
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';

interface GeneratorConfig {
  /** OpenAPI spec source */
  input: 
    | { type: 'url'; url: string }
    | { type: 'file'; path: string }
    | { type: 'command'; command: string }; // e.g., 'dotnet swagger tofile'
  
  /** Output directory for generated code */
  outputDir: string;
  
  /** Metadata file for change detection */
  metadataFile: string;
  
  /** Skip generation if spec unchanged */
  skipUnchanged: boolean;
  
  /** Formatting options */
  format: {
    prettier: boolean;
    eslint: boolean;
  };
}

const defaultConfig: GeneratorConfig = {
  input: { 
    type: 'url', 
    url: process.env.OPENAPI_URL || 'http://localhost:8080/swagger/v1/swagger.json' 
  },
  outputDir: 'src/generated',
  metadataFile: 'src/generated/.metadata.json',
  skipUnchanged: true,
  format: {
    prettier: true,
    eslint: true,
  },
};

async function fetchSpec(config: GeneratorConfig): Promise<OpenAPIV3.Document> {
  const { input } = config;
  
  switch (input.type) {
    case 'url': {
      console.log(`📡 Fetching OpenAPI spec from ${input.url}`);
      const response = await fetch(input.url, {
        headers: { Accept: 'application/json' },
        signal: AbortSignal.timeout(30_000),
      });
      
      if (!response.ok) {
        throw new Error(`Failed to fetch spec: ${response.status} ${response.statusText}`);
      }
      
      return response.json() as Promise<OpenAPIV3.Document>;
    }
    
    case 'file': {
      console.log(`📄 Reading OpenAPI spec from ${input.path}`);
      const content = readFileSync(input.path, 'utf-8');
      return JSON.parse(content) as OpenAPIV3.Document;
    }
    
    case 'command': {
      console.log(`🔧 Generating OpenAPI spec via command: ${input.command}`);
      const { execSync } = await import('child_process');
      const output = execSync(input.command, { encoding: 'utf-8' });
      return JSON.parse(output) as OpenAPIV3.Document;
    }
  }
}

function computeSpecHash(spec: OpenAPIV3.Document): string {
  // Normalize before hashing (sort keys, remove metadata)
  const normalized = JSON.stringify(spec, Object.keys(spec).sort(), 0);
  return createHash('sha256').update(normalized).digest('hex');
}

interface GenerationMetadata {
  hash: string;
  timestamp: string;
  apiVersion: string;
  generatorVersion: string;
}

function loadMetadata(path: string): GenerationMetadata | null {
  if (!existsSync(path)) return null;
  try {
    return JSON.parse(readFileSync(path, 'utf-8'));
  } catch {
    return null;
  }
}

function saveMetadata(path: string, metadata: GenerationMetadata): void {
  const dir = join(path, '..');
  if (!existsSync(dir)) mkdirSync(dir, { recursive: true });
  writeFileSync(path, JSON.stringify(metadata, null, 2));
}
```

### 4.3 Step 2: Normalize Specification

Pre-process the OpenAPI spec to fix common issues and apply our conventions:

```typescript
// scripts/normalize.ts

import type { OpenAPIV3 } from 'openapi-types';

interface NormalizationOptions {
  /** Prefix for operation IDs */
  operationIdPrefix?: string;
  /** Remove deprecated endpoints */
  removeDeprecated?: boolean;
  /** Tag mappings (rename tags) */
  tagMappings?: Record<string, string>;
  /** Schema name transformations */
  schemaTransforms?: Record<string, string>;
}

export function normalizeSpec(
  spec: OpenAPIV3.Document, 
  options: NormalizationOptions = {}
): OpenAPIV3.Document {
  const normalized = structuredClone(spec);
  
  // 1. Fix operation IDs (ensure unique, PascalCase)
  normalizeOperationIds(normalized);
  
  // 2. Normalize tag names (consistent casing)
  normalizeTags(normalized, options.tagMappings);
  
  // 3. Fix schema names (remove prefixes like "Microsoft.AspNetCore...")
  normalizeSchemaNames(normalized, options.schemaTransforms);
  
  // 4. Ensure consistent response schemas
  normalizeResponses(normalized);
  
  // 5. Add missing operationIds based on path + method
  generateMissingOperationIds(normalized);
  
  // 6. Remove deprecated if requested
  if (options.removeDeprecated) {
    removeDeprecatedOperations(normalized);
  }
  
  return normalized;
}

function normalizeOperationIds(spec: OpenAPIV3.Document): void {
  const usedIds = new Set<string>();
  
  for (const [path, pathItem] of Object.entries(spec.paths ?? {})) {
    if (!pathItem) continue;
    
    for (const method of ['get', 'post', 'put', 'patch', 'delete'] as const) {
      const operation = pathItem[method];
      if (!operation) continue;
      
      let operationId = operation.operationId;
      
      if (!operationId) {
        // Generate from path + method
        operationId = generateOperationId(path, method);
      }
      
      // Convert to camelCase
      operationId = toCamelCase(operationId);
      
      // Ensure uniqueness
      let uniqueId = operationId;
      let counter = 1;
      while (usedIds.has(uniqueId)) {
        uniqueId = `${operationId}${counter++}`;
      }
      usedIds.add(uniqueId);
      
      operation.operationId = uniqueId;
    }
  }
}

function normalizeSchemaNames(
  spec: OpenAPIV3.Document, 
  transforms?: Record<string, string>
): void {
  const schemas = spec.components?.schemas ?? {};
  const renamedSchemas: Record<string, string> = {};
  
  // Default transforms for .NET naming conventions
  const defaultTransforms: Record<string, string> = {
    // Remove Microsoft namespace prefixes
    'MicrosoftAspNetCoreMvcProblemDetails': 'ProblemDetails',
    'MicrosoftAspNetCoreMvcValidationProblemDetails': 'ValidationProblemDetails',
    // Module-specific cleanups
    'ModulesAuthenticationControllers': '',
    'ModulesAuthenticationModels': '',
    'CoreDomainPermissions': '',
    ...transforms,
  };
  
  for (const [originalName, schema] of Object.entries(schemas)) {
    let newName = originalName;
    
    // Apply transforms
    for (const [prefix, replacement] of Object.entries(defaultTransforms)) {
      if (newName.startsWith(prefix)) {
        newName = replacement + newName.slice(prefix.length);
      }
    }
    
    // Clean up common patterns
    newName = newName
      .replace(/Dto$/, '') // Remove Dto suffix
      .replace(/Response$/, 'Response') // Keep Response suffix
      .replace(/Request$/, 'Request'); // Keep Request suffix
    
    if (newName !== originalName) {
      renamedSchemas[originalName] = newName;
    }
  }
  
  // Apply renames to all $ref occurrences
  const specString = JSON.stringify(spec);
  let updatedString = specString;
  
  for (const [original, renamed] of Object.entries(renamedSchemas)) {
    const refPattern = `#/components/schemas/${original}`;
    const newRef = `#/components/schemas/${renamed}`;
    updatedString = updatedString.replaceAll(refPattern, newRef);
  }
  
  const updatedSpec = JSON.parse(updatedString) as OpenAPIV3.Document;
  
  // Rename schema keys
  if (updatedSpec.components?.schemas) {
    const newSchemas: Record<string, OpenAPIV3.SchemaObject> = {};
    for (const [name, schema] of Object.entries(updatedSpec.components.schemas)) {
      const newName = renamedSchemas[name] ?? name;
      newSchemas[newName] = schema as OpenAPIV3.SchemaObject;
    }
    updatedSpec.components.schemas = newSchemas;
  }
  
  Object.assign(spec, updatedSpec);
}

function normalizeTags(
  spec: OpenAPIV3.Document, 
  mappings?: Record<string, string>
): void {
  const tagMapping: Record<string, string> = {
    // Default mappings for GameGuild
    'Auth': 'auth',
    'Authentication': 'auth',
    'Users': 'users',
    'UserProfiles': 'users',
    'Programs': 'programs',
    'Achievements': 'achievements',
    'FeatureFlags': 'featureFlags',
    'Tenants': 'tenants',
    'Permissions': 'permissions',
    'Subscriptions': 'subscriptions',
    'Billing': 'billing',
    'Projects': 'projects',
    'Posts': 'posts',
    'Courses': 'courses',
    'Assessments': 'assessments',
    ...mappings,
  };
  
  for (const pathItem of Object.values(spec.paths ?? {})) {
    if (!pathItem) continue;
    
    for (const method of ['get', 'post', 'put', 'patch', 'delete'] as const) {
      const operation = pathItem[method];
      if (!operation?.tags) continue;
      
      operation.tags = operation.tags.map(tag => tagMapping[tag] ?? toCamelCase(tag));
    }
  }
}

// Helper functions
function toCamelCase(str: string): string {
  return str
    .replace(/[-_\s]+(.)?/g, (_, c) => c?.toUpperCase() ?? '')
    .replace(/^(.)/, c => c.toLowerCase());
}

function toPascalCase(str: string): string {
  const camel = toCamelCase(str);
  return camel.charAt(0).toUpperCase() + camel.slice(1);
}

function generateOperationId(path: string, method: string): string {
  // /api/users/{userId} + GET -> getUsersById
  const segments = path
    .split('/')
    .filter(s => s && !s.startsWith('{'))
    .map(s => toPascalCase(s));
  
  const hasParam = path.includes('{');
  
  const prefix = {
    get: hasParam ? 'get' : 'list',
    post: 'create',
    put: 'update',
    patch: 'patch',
    delete: 'delete',
  }[method] ?? method;
  
  return prefix + segments.join('');
}
```

### 4.4 Step 3: Code Generation

```typescript
// scripts/codegen/types.ts

import type { OpenAPIV3 } from 'openapi-types';
import Handlebars from 'handlebars';
import { readFileSync } from 'fs';
import { join } from 'path';

interface TypeGeneratorOptions {
  templateDir: string;
  includeJSDoc: boolean;
  enumStyle: 'enum' | 'const' | 'union';
}

export function generateTypes(
  spec: OpenAPIV3.Document, 
  options: TypeGeneratorOptions
): string {
  const template = Handlebars.compile(
    readFileSync(join(options.templateDir, 'types.hbs'), 'utf-8')
  );
  
  const schemas = spec.components?.schemas ?? {};
  const types: TypeDefinition[] = [];
  
  for (const [name, schema] of Object.entries(schemas)) {
    types.push(schemaToTypeDefinition(name, schema as OpenAPIV3.SchemaObject, options));
  }
  
  // Sort for deterministic output
  types.sort((a, b) => a.name.localeCompare(b.name));
  
  return template({
    generatedAt: new Date().toISOString(),
    types,
  });
}

interface TypeDefinition {
  name: string;
  kind: 'interface' | 'enum' | 'type';
  jsdoc?: string;
  properties?: PropertyDefinition[];
  enumValues?: EnumValue[];
  unionTypes?: string[];
}

interface PropertyDefinition {
  name: string;
  type: string;
  required: boolean;
  jsdoc?: string;
  readonly?: boolean;
  nullable?: boolean;
}

interface EnumValue {
  name: string;
  value: string | number;
}

function schemaToTypeDefinition(
  name: string, 
  schema: OpenAPIV3.SchemaObject,
  options: TypeGeneratorOptions
): TypeDefinition {
  // Handle enums
  if (schema.enum) {
    return {
      name,
      kind: 'enum',
      jsdoc: schema.description,
      enumValues: schema.enum.map((value, index) => ({
        name: toEnumKey(value),
        value,
      })),
    };
  }
  
  // Handle object types
  if (schema.type === 'object' || schema.properties) {
    const properties: PropertyDefinition[] = [];
    const required = new Set(schema.required ?? []);
    
    for (const [propName, propSchema] of Object.entries(schema.properties ?? {})) {
      const prop = propSchema as OpenAPIV3.SchemaObject;
      properties.push({
        name: propName,
        type: schemaToTypeString(prop),
        required: required.has(propName),
        jsdoc: prop.description,
        readonly: prop.readOnly,
        nullable: prop.nullable,
      });
    }
    
    return {
      name,
      kind: 'interface',
      jsdoc: schema.description,
      properties,
    };
  }
  
  // Handle union types (oneOf, anyOf)
  if (schema.oneOf || schema.anyOf) {
    const variants = (schema.oneOf ?? schema.anyOf) as OpenAPIV3.SchemaObject[];
    return {
      name,
      kind: 'type',
      jsdoc: schema.description,
      unionTypes: variants.map(v => schemaToTypeString(v)),
    };
  }
  
  // Fallback to type alias
  return {
    name,
    kind: 'type',
    jsdoc: schema.description,
    unionTypes: [schemaToTypeString(schema)],
  };
}

function schemaToTypeString(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
  // Handle $ref
  if ('$ref' in schema) {
    const refName = schema.$ref.split('/').pop()!;
    return refName;
  }
  
  // Handle arrays
  if (schema.type === 'array' && schema.items) {
    const itemType = schemaToTypeString(schema.items as OpenAPIV3.SchemaObject);
    return `${itemType}[]`;
  }
  
  // Handle primitives
  const typeMap: Record<string, string> = {
    string: 'string',
    number: 'number',
    integer: 'number',
    boolean: 'boolean',
    object: 'Record<string, unknown>',
  };
  
  if (schema.type && typeMap[schema.type]) {
    let tsType = typeMap[schema.type];
    
    // Handle string formats
    if (schema.type === 'string' && schema.format) {
      switch (schema.format) {
        case 'date-time':
        case 'date':
          tsType = 'string'; // Keep as string, provide Date helpers separately
          break;
        case 'uuid':
          tsType = 'string';
          break;
        case 'binary':
          tsType = 'Blob';
          break;
      }
    }
    
    return schema.nullable ? `${tsType} | null` : tsType;
  }
  
  return 'unknown';
}

function toEnumKey(value: unknown): string {
  const str = String(value);
  return str
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, '_')
    .replace(/^(\d)/, '_$1');
}
```

### 4.5 Step 4: Endpoint Generation

```typescript
// scripts/codegen/endpoints.ts

import type { OpenAPIV3 } from 'openapi-types';

interface EndpointDefinition {
  operationId: string;
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  path: string;
  tag: string;
  summary?: string;
  description?: string;
  parameters: ParameterDefinition[];
  requestBody?: RequestBodyDefinition;
  responses: ResponseDefinition[];
  security: SecurityRequirement[];
  deprecated?: boolean;
}

interface ParameterDefinition {
  name: string;
  in: 'path' | 'query' | 'header';
  type: string;
  required: boolean;
  description?: string;
}

interface RequestBodyDefinition {
  type: string;
  required: boolean;
  contentType: string;
}

interface ResponseDefinition {
  statusCode: number | 'default';
  type: string;
  description?: string;
  isError: boolean;
}

interface SecurityRequirement {
  scheme: string;
  scopes: string[];
}

export function generateEndpoints(spec: OpenAPIV3.Document): Map<string, EndpointDefinition[]> {
  const endpointsByTag = new Map<string, EndpointDefinition[]>();
  
  for (const [path, pathItem] of Object.entries(spec.paths ?? {})) {
    if (!pathItem) continue;
    
    for (const method of ['get', 'post', 'put', 'patch', 'delete'] as const) {
      const operation = pathItem[method];
      if (!operation) continue;
      
      const endpoint = operationToEndpoint(path, method.toUpperCase() as EndpointDefinition['method'], operation, spec);
      
      const tag = endpoint.tag || 'default';
      if (!endpointsByTag.has(tag)) {
        endpointsByTag.set(tag, []);
      }
      endpointsByTag.get(tag)!.push(endpoint);
    }
  }
  
  return endpointsByTag;
}

function operationToEndpoint(
  path: string,
  method: EndpointDefinition['method'],
  operation: OpenAPIV3.OperationObject,
  spec: OpenAPIV3.Document
): EndpointDefinition {
  const parameters: ParameterDefinition[] = [];
  
  // Process parameters
  for (const param of operation.parameters ?? []) {
    const p = resolveRef(param, spec) as OpenAPIV3.ParameterObject;
    parameters.push({
      name: p.name,
      in: p.in as ParameterDefinition['in'],
      type: schemaToTypeString((p.schema ?? {}) as OpenAPIV3.SchemaObject),
      required: p.required ?? false,
      description: p.description,
    });
  }
  
  // Process request body
  let requestBody: RequestBodyDefinition | undefined;
  if (operation.requestBody) {
    const body = resolveRef(operation.requestBody, spec) as OpenAPIV3.RequestBodyObject;
    const content = body.content?.['application/json'];
    if (content?.schema) {
      requestBody = {
        type: schemaToTypeString(content.schema as OpenAPIV3.SchemaObject),
        required: body.required ?? false,
        contentType: 'application/json',
      };
    }
  }
  
  // Process responses
  const responses: ResponseDefinition[] = [];
  for (const [statusCode, response] of Object.entries(operation.responses ?? {})) {
    const resp = resolveRef(response, spec) as OpenAPIV3.ResponseObject;
    const content = resp.content?.['application/json'];
    const code = statusCode === 'default' ? 'default' : parseInt(statusCode, 10);
    
    responses.push({
      statusCode: code,
      type: content?.schema ? schemaToTypeString(content.schema as OpenAPIV3.SchemaObject) : 'void',
      description: resp.description,
      isError: typeof code === 'number' && code >= 400,
    });
  }
  
  // Process security
  const security: SecurityRequirement[] = [];
  for (const sec of operation.security ?? []) {
    for (const [scheme, scopes] of Object.entries(sec)) {
      security.push({ scheme, scopes });
    }
  }
  
  return {
    operationId: operation.operationId!,
    method,
    path,
    tag: operation.tags?.[0] ?? 'default',
    summary: operation.summary,
    description: operation.description,
    parameters,
    requestBody,
    responses,
    security,
    deprecated: operation.deprecated,
  };
}

function resolveRef<T>(obj: T | OpenAPIV3.ReferenceObject, spec: OpenAPIV3.Document): T {
  if (obj && typeof obj === 'object' && '$ref' in obj) {
    const path = (obj as OpenAPIV3.ReferenceObject).$ref.split('/').slice(1);
    let result: unknown = spec;
    for (const key of path) {
      result = (result as Record<string, unknown>)[key];
    }
    return result as T;
  }
  return obj;
}
```

### 4.6 Step 5: Module Template

```handlebars
{{! templates/module.hbs }}
// This file is auto-generated. DO NOT EDIT.
// Generated at: {{generatedAt}}
// Source: {{sourceUrl}}

import type { Client, RequestOptions } from '../runtime/transport/types';
import type { Result } from '../runtime/result/types';
import type { 
{{#each imports}}
  {{this}},
{{/each}}
} from './types.gen';
import type { ApiError } from './errors.gen';

/**
 * {{moduleName}} API endpoints
 * {{#if description}}
 * {{description}}
 * {{/if}}
 */
export function create{{pascalCase moduleName}}Module(client: Client) {
  return {
{{#each endpoints}}
    /**
     * {{summary}}
     * {{#if description}}
     * @description {{description}}
     * {{/if}}
     * {{#if deprecated}}
     * @deprecated
     * {{/if}}
     */
    async {{operationId}}(
      {{#if hasParams}}
      params: {
        {{#each pathParams}}
        /** {{description}} */
        {{name}}: {{type}};
        {{/each}}
        {{#each queryParams}}
        /** {{description}} */
        {{name}}{{#unless required}}?{{/unless}}: {{type}};
        {{/each}}
        {{#if requestBody}}
        /** Request body */
        body{{#unless requestBody.required}}?{{/unless}}: {{requestBody.type}};
        {{/if}}
      },
      {{/if}}
      options?: RequestOptions
    ): Promise<Result<{{successType}}, {{errorType}}>> {
      return client.request({
        method: '{{method}}',
        path: '{{path}}',
        {{#if pathParams.length}}
        pathParams: {
          {{#each pathParams}}
          {{name}}: params.{{name}},
          {{/each}}
        },
        {{/if}}
        {{#if queryParams.length}}
        query: {
          {{#each queryParams}}
          {{name}}: params.{{name}},
          {{/each}}
        },
        {{/if}}
        {{#if requestBody}}
        body: params.body,
        {{/if}}
        {{#if requiresAuth}}
        auth: true,
        {{/if}}
        ...options,
      });
    },

{{/each}}
  };
}

// Request types
{{#each endpoints}}
export interface {{pascalCase operationId}}Request {
  {{#each pathParams}}
  {{name}}: {{type}};
  {{/each}}
  {{#each queryParams}}
  {{name}}{{#unless required}}?{{/unless}}: {{type}};
  {{/each}}
  {{#if requestBody}}
  body{{#unless requestBody.required}}?{{/unless}}: {{requestBody.type}};
  {{/if}}
}

{{/each}}
```

### 4.7 Step 6: Post-Processing

```typescript
// scripts/generate.ts (continued)

import { execSync } from 'child_process';
import { writeFileSync, mkdirSync, existsSync } from 'fs';
import { join } from 'path';

async function postProcess(outputDir: string, options: GeneratorConfig['format']): Promise<void> {
  // 1. Format with Prettier
  if (options.prettier) {
    console.log('🎨 Formatting with Prettier...');
    try {
      execSync(`npx prettier --write "${outputDir}/**/*.ts"`, {
        stdio: 'inherit',
      });
    } catch (error) {
      console.warn('⚠️ Prettier formatting failed, continuing...');
    }
  }
  
  // 2. Fix with ESLint
  if (options.eslint) {
    console.log('🔧 Running ESLint --fix...');
    try {
      execSync(`npx eslint "${outputDir}/**/*.ts" --fix --quiet`, {
        stdio: 'inherit',
      });
    } catch (error) {
      console.warn('⚠️ ESLint fix failed, continuing...');
    }
  }
  
  // 3. Validate TypeScript
  console.log('✅ Validating TypeScript...');
  try {
    execSync('npx tsc --noEmit --project tsconfig.json', {
      stdio: 'inherit',
    });
  } catch (error) {
    console.error('❌ TypeScript validation failed');
    throw error;
  }
}

// Main generation pipeline
async function generate(config: GeneratorConfig = defaultConfig): Promise<void> {
  console.log('🚀 Starting SDK generation...\n');
  
  // Step 1: Fetch spec
  const spec = await fetchSpec(config);
  const hash = computeSpecHash(spec);
  
  // Check if regeneration needed
  const metadata = loadMetadata(config.metadataFile);
  if (config.skipUnchanged && metadata?.hash === hash) {
    console.log('✨ OpenAPI spec unchanged, skipping generation');
    return;
  }
  
  // Step 2: Normalize spec
  console.log('🔄 Normalizing OpenAPI spec...');
  const normalizedSpec = normalizeSpec(spec);
  
  // Step 3: Generate code
  console.log('⚡ Generating TypeScript code...');
  
  const outputDir = config.outputDir;
  if (!existsSync(outputDir)) {
    mkdirSync(outputDir, { recursive: true });
  }
  
  // Generate types
  const types = generateTypes(normalizedSpec, {
    templateDir: 'scripts/templates',
    includeJSDoc: true,
    enumStyle: 'enum',
  });
  writeFileSync(join(outputDir, 'types.gen.ts'), types);
  
  // Generate endpoints by module
  const endpointsByTag = generateEndpoints(normalizedSpec);
  const modulesDir = join(outputDir, 'modules');
  if (!existsSync(modulesDir)) {
    mkdirSync(modulesDir, { recursive: true });
  }
  
  for (const [tag, endpoints] of endpointsByTag) {
    const moduleCode = generateModuleCode(tag, endpoints, normalizedSpec);
    writeFileSync(join(modulesDir, `${tag}.gen.ts`), moduleCode);
  }
  
  // Generate error types
  const errors = generateErrorTypes(normalizedSpec);
  writeFileSync(join(outputDir, 'errors.gen.ts'), errors);
  
  // Generate index
  const index = generateIndex(endpointsByTag);
  writeFileSync(join(outputDir, 'index.ts'), index);
  
  // Step 4: Post-process
  await postProcess(outputDir, config.format);
  
  // Step 5: Save metadata
  saveMetadata(config.metadataFile, {
    hash,
    timestamp: new Date().toISOString(),
    apiVersion: spec.info?.version ?? 'unknown',
    generatorVersion: '1.0.0',
  });
  
  console.log('\n✅ SDK generation complete!');
}

// Run if executed directly
generate().catch(console.error);
```

### 4.8 Breaking Change Detection

```typescript
// scripts/diff.ts

import { execSync } from 'child_process';

interface BreakingChange {
  type: 'endpoint-removed' | 'parameter-required' | 'type-changed' | 'response-changed';
  path: string;
  description: string;
  severity: 'breaking' | 'warning' | 'info';
}

export async function detectBreakingChanges(
  oldSpecPath: string,
  newSpecPath: string
): Promise<BreakingChange[]> {
  // Use openapi-diff library
  const { diff } = await import('openapi-diff');
  
  const result = await diff({
    sourceSpec: { location: oldSpecPath },
    destinationSpec: { location: newSpecPath },
  });
  
  const changes: BreakingChange[] = [];
  
  for (const breakingChange of result.breakingDifferences ?? []) {
    changes.push({
      type: categorizeChange(breakingChange.code),
      path: breakingChange.sourceSpecEntityDetails?.[0]?.location ?? 'unknown',
      description: breakingChange.message,
      severity: 'breaking',
    });
  }
  
  return changes;
}

function categorizeChange(code: string): BreakingChange['type'] {
  if (code.includes('removed')) return 'endpoint-removed';
  if (code.includes('required')) return 'parameter-required';
  if (code.includes('type')) return 'type-changed';
  return 'response-changed';
}

export function generateChangelogEntry(changes: BreakingChange[]): string {
  const breaking = changes.filter(c => c.severity === 'breaking');
  const warnings = changes.filter(c => c.severity === 'warning');
  
  let changelog = `## [Unreleased]\n\n`;
  
  if (breaking.length > 0) {
    changelog += `### ⚠️ Breaking Changes\n\n`;
    for (const change of breaking) {
      changelog += `- **${change.type}**: ${change.description}\n`;
    }
    changelog += '\n';
  }
  
  if (warnings.length > 0) {
    changelog += `### Changes\n\n`;
    for (const change of warnings) {
      changelog += `- ${change.description}\n`;
    }
  }
  
  return changelog;
}
```

---

## 5. Authentication Support

### 5.1 Token Provider Interface

```typescript
// src/runtime/auth/types.ts

/**
 * Token provider interface for bearer token authentication.
 * Implementations must handle token storage securely.
 */
export interface TokenProvider {
  /**
   * Get the current access token.
   * @returns The access token or null if not authenticated
   */
  getAccessToken(): Promise<string | null>;
  
  /**
   * Get the refresh token for automatic token refresh.
   * @returns The refresh token or null if not available
   */
  getRefreshToken?(): Promise<string | null>;
  
  /**
   * Called when tokens are successfully refreshed.
   * Implementation should persist the new tokens.
   */
  onTokenRefresh?(tokens: TokenPair): Promise<void>;
  
  /**
   * Called when the user must re-authenticate.
   * This happens when refresh fails or tokens are invalid.
   */
  onAuthenticationRequired?(): Promise<void>;
}

export interface TokenPair {
  accessToken: string;
  refreshToken?: string;
  expiresIn?: number;
  tokenType: 'Bearer';
  scope?: string;
}

/**
 * Authentication configuration options.
 */
export type AuthConfig = 
  | { mode: 'bearer'; tokenProvider: TokenProvider }
  | { mode: 'cookie'; credentials?: RequestCredentials; csrfTokenHeader?: string }
  | { mode: 'none' };

/**
 * CSRF configuration for cookie-based auth.
 */
export interface CsrfConfig {
  /** Header name for CSRF token */
  headerName: string;
  /** Cookie name containing CSRF token */
  cookieName: string;
  /** Methods that require CSRF token */
  methods: string[];
}
```

### 5.2 Token Refresh Implementation

```typescript
// src/runtime/auth/refresh.ts

import type { TokenProvider, TokenPair } from './types';

interface RefreshConfig {
  /** Endpoint for token refresh */
  refreshUrl: string;
  /** Time before expiry to trigger refresh (ms) */
  refreshThreshold: number;
  /** Maximum retry attempts */
  maxRetries: number;
  /** Backoff multiplier for retries */
  backoffMs: number;
}

const defaultRefreshConfig: RefreshConfig = {
  refreshUrl: '/api/auth/refresh',
  refreshThreshold: 30_000, // 30 seconds before expiry
  maxRetries: 3,
  backoffMs: 1000,
};

/**
 * Token refresh manager with automatic refresh before expiry.
 */
export class TokenRefreshManager {
  private refreshPromise: Promise<TokenPair> | null = null;
  private tokenExpiry: number | null = null;
  
  constructor(
    private provider: TokenProvider,
    private config: RefreshConfig = defaultRefreshConfig,
    private fetch: typeof globalThis.fetch = globalThis.fetch
  ) {}
  
  /**
   * Get a valid access token, refreshing if necessary.
   */
  async getValidToken(): Promise<string | null> {
    const token = await this.provider.getAccessToken();
    
    if (!token) {
      return null;
    }
    
    // Check if refresh needed
    if (this.shouldRefresh()) {
      try {
        await this.refresh();
        return this.provider.getAccessToken();
      } catch (error) {
        // Refresh failed, notify provider
        await this.provider.onAuthenticationRequired?.();
        throw error;
      }
    }
    
    return token;
  }
  
  /**
   * Refresh tokens.
   * Uses mutex pattern to prevent concurrent refreshes.
   */
  async refresh(): Promise<TokenPair> {
    // Return existing refresh promise if one is in progress
    if (this.refreshPromise) {
      return this.refreshPromise;
    }
    
    this.refreshPromise = this.doRefresh();
    
    try {
      return await this.refreshPromise;
    } finally {
      this.refreshPromise = null;
    }
  }
  
  private async doRefresh(): Promise<TokenPair> {
    const refreshToken = await this.provider.getRefreshToken?.();
    
    if (!refreshToken) {
      throw new AuthenticationRequiredError('No refresh token available');
    }
    
    let lastError: Error | null = null;
    
    for (let attempt = 0; attempt < this.config.maxRetries; attempt++) {
      try {
        const response = await this.fetch(this.config.refreshUrl, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ refreshToken }),
        });
        
        if (!response.ok) {
          if (response.status === 401 || response.status === 403) {
            throw new RefreshTokenExpiredError();
          }
          throw new TokenRefreshError(`Refresh failed: ${response.status}`);
        }
        
        const tokens: TokenPair = await response.json();
        
        // Update token expiry
        if (tokens.expiresIn) {
          this.tokenExpiry = Date.now() + tokens.expiresIn * 1000;
        }
        
        // Notify provider
        await this.provider.onTokenRefresh?.(tokens);
        
        return tokens;
      } catch (error) {
        lastError = error as Error;
        
        if (error instanceof RefreshTokenExpiredError) {
          throw error; // Don't retry on expired refresh token
        }
        
        // Exponential backoff
        await this.delay(this.config.backoffMs * Math.pow(2, attempt));
      }
    }
    
    throw lastError ?? new TokenRefreshError('Refresh failed after retries');
  }
  
  private shouldRefresh(): boolean {
    if (!this.tokenExpiry) return false;
    return Date.now() >= this.tokenExpiry - this.config.refreshThreshold;
  }
  
  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// Custom errors
export class AuthenticationRequiredError extends Error {
  readonly code = 'AUTHENTICATION_REQUIRED';
  constructor(message = 'Authentication required') {
    super(message);
    this.name = 'AuthenticationRequiredError';
  }
}

export class RefreshTokenExpiredError extends Error {
  readonly code = 'REFRESH_TOKEN_EXPIRED';
  constructor(message = 'Refresh token expired') {
    super(message);
    this.name = 'RefreshTokenExpiredError';
  }
}

export class TokenRefreshError extends Error {
  readonly code = 'TOKEN_REFRESH_ERROR';
  constructor(message: string) {
    super(message);
    this.name = 'TokenRefreshError';
  }
}
```

### 5.3 NextAuth Integration

```typescript
// src/integrations/next/nextauth.ts

import type { TokenProvider, TokenPair } from '../../runtime/auth/types';

/**
 * Session type from NextAuth (simplified).
 * Extend based on your actual session shape.
 */
interface NextAuthSession {
  api?: {
    accessToken?: string;
    refreshToken?: string;
    expiresAt?: number;
  };
  currentTenant?: {
    id?: string;
  };
  error?: string;
  user?: {
    id?: string;
    email?: string;
  };
}

type AuthFunction = () => Promise<NextAuthSession | null>;

interface NextAuthTokenProviderOptions {
  /** NextAuth auth() function */
  auth: AuthFunction;
  /** Called when authentication is required */
  onAuthRequired?: () => Promise<void>;
  /** Called when refresh fails */
  onRefreshError?: (error: Error) => Promise<void>;
}

/**
 * Create a TokenProvider that integrates with NextAuth.
 * For use in Next.js Server Components and Server Actions.
 */
export function createNextAuthTokenProvider(options: NextAuthTokenProviderOptions): TokenProvider {
  return {
    async getAccessToken(): Promise<string | null> {
      const session = await options.auth();
      
      if (session?.error) {
        console.warn('NextAuth session error:', session.error);
        await options.onAuthRequired?.();
        return null;
      }
      
      return session?.api?.accessToken ?? null;
    },
    
    async getRefreshToken(): Promise<string | null> {
      const session = await options.auth();
      return session?.api?.refreshToken ?? null;
    },
    
    async onTokenRefresh(tokens: TokenPair): Promise<void> {
      // NextAuth handles token persistence via its callbacks
      // This is a no-op in most cases
      console.debug('Tokens refreshed via NextAuth');
    },
    
    async onAuthenticationRequired(): Promise<void> {
      await options.onAuthRequired?.();
    },
  };
}

/**
 * Helper to get the current tenant from NextAuth session.
 */
export async function getTenantFromSession(auth: AuthFunction): Promise<string | null> {
  const session = await auth();
  return session?.currentTenant?.id ?? null;
}
```

### 5.4 SSR-Safe Token Handling

```typescript
// src/integrations/next/server.ts

import { headers, cookies } from 'next/headers';
import type { TokenProvider } from '../../runtime/auth/types';

/**
 * Server-side token provider that reads from request context.
 * NEVER exposes tokens to client-side code.
 */
export function createServerTokenProvider(): TokenProvider {
  return {
    async getAccessToken(): Promise<string | null> {
      // Read from HTTP-only cookie (preferred) or header
      const cookieStore = await cookies();
      const token = cookieStore.get('access_token')?.value;
      
      if (token) {
        return token;
      }
      
      // Fallback to Authorization header (for API routes)
      const headerStore = await headers();
      const authHeader = headerStore.get('authorization');
      
      if (authHeader?.startsWith('Bearer ')) {
        return authHeader.slice(7);
      }
      
      return null;
    },
    
    async getRefreshToken(): Promise<string | null> {
      const cookieStore = await cookies();
      return cookieStore.get('refresh_token')?.value ?? null;
    },
  };
}

/**
 * Validate that code is running on the server.
 * Throws if called from client code.
 */
export function assertServerOnly(operation: string): void {
  if (typeof window !== 'undefined') {
    throw new Error(
      `${operation} can only be called from Server Components or Server Actions. ` +
      `If you need to call this from a Client Component, use a Server Action.`
    );
  }
}

/**
 * Safe headers that can be passed to API without leaking secrets.
 */
export async function getSafeRequestHeaders(): Promise<Record<string, string>> {
  const headerStore = await headers();
  
  // Only propagate safe headers
  const safeHeaders = [
    'x-correlation-id',
    'x-request-id',
    'accept-language',
    'user-agent',
  ];
  
  const result: Record<string, string> = {};
  
  for (const name of safeHeaders) {
    const value = headerStore.get(name);
    if (value) {
      result[name] = value;
    }
  }
  
  return result;
}
```

---

## 6. Authorization Support

### 6.1 Authorization Error Types

```typescript
// src/runtime/errors/authorization.ts

import type { ApiError } from './types';

/**
 * Permission required by the API but not held by the user.
 */
export interface RequiredPermission {
  /** Permission identifier */
  permission: string;
  /** Resource type this permission applies to */
  resourceType?: string;
  /** Specific resource ID (if resource-level permission) */
  resourceId?: string;
  /** Human-readable description */
  description?: string;
}

/**
 * Authorization error with detailed permission information.
 */
export interface AuthorizationError extends ApiError {
  code: 'FORBIDDEN' | 'INSUFFICIENT_PERMISSIONS';
  /** Permissions required for this operation */
  requiredPermissions?: RequiredPermission[];
  /** User's current permissions (if available) */
  currentPermissions?: string[];
  /** Tenant context of the request */
  tenantId?: string;
}

/**
 * Authentication error (401).
 */
export interface AuthenticationError extends ApiError {
  code: 'UNAUTHORIZED' | 'TOKEN_EXPIRED' | 'INVALID_TOKEN';
  /** Redirect URL for re-authentication */
  loginUrl?: string;
}

/**
 * Feature not available error.
 */
export interface FeatureNotAvailableError extends ApiError {
  code: 'FEATURE_NOT_AVAILABLE' | 'PLAN_UPGRADE_REQUIRED';
  /** Feature key that is not available */
  featureKey: string;
  /** Required plan to access this feature */
  requiredPlan?: string;
  /** Upgrade URL */
  upgradeUrl?: string;
}
```

### 6.2 Type Guards

```typescript
// src/runtime/errors/guards.ts

import type { ApiError } from './types';
import type { 
  AuthorizationError, 
  AuthenticationError,
  FeatureNotAvailableError,
  RequiredPermission
} from './authorization';

/**
 * Check if error is an API error.
 */
export function isApiError(error: unknown): error is ApiError {
  return (
    error !== null &&
    typeof error === 'object' &&
    'code' in error &&
    'message' in error &&
    'status' in error
  );
}

/**
 * Check if error is a 401 Unauthorized.
 */
export function isUnauthorized(error: unknown): error is AuthenticationError {
  return isApiError(error) && error.status === 401;
}

/**
 * Check if error is a 403 Forbidden.
 */
export function isForbidden(error: unknown): error is AuthorizationError {
  return isApiError(error) && error.status === 403;
}

/**
 * Check if error is due to insufficient permissions.
 */
export function isInsufficientPermissions(error: unknown): error is AuthorizationError {
  return (
    isForbidden(error) && 
    (error.code === 'INSUFFICIENT_PERMISSIONS' || 
     Array.isArray((error as AuthorizationError).requiredPermissions))
  );
}

/**
 * Check if error is due to missing feature/entitlement.
 */
export function isFeatureNotAvailable(error: unknown): error is FeatureNotAvailableError {
  return (
    isApiError(error) && 
    (error.code === 'FEATURE_NOT_AVAILABLE' || error.code === 'PLAN_UPGRADE_REQUIRED')
  );
}

/**
 * Extract required permissions from error response.
 * Returns empty array if not available.
 */
export function getRequiredPermissions(error: unknown): RequiredPermission[] {
  if (!isInsufficientPermissions(error)) {
    return [];
  }
  return error.requiredPermissions ?? [];
}

/**
 * Check if user is missing a specific permission.
 */
export function isMissingPermission(error: unknown, permission: string): boolean {
  const required = getRequiredPermissions(error);
  return required.some(p => p.permission === permission);
}

/**
 * Get upgrade URL from feature error.
 */
export function getUpgradeUrl(error: unknown): string | undefined {
  if (isFeatureNotAvailable(error)) {
    return error.upgradeUrl;
  }
  return undefined;
}
```

### 6.3 Authorization Helpers

```typescript
// src/runtime/auth/helpers.ts

import type { Result } from '../result/types';
import type { ApiError } from '../errors/types';
import type { AuthorizationError, RequiredPermission } from '../errors/authorization';
import { isForbidden, isUnauthorized, getRequiredPermissions } from '../errors/guards';

/**
 * Handle authorization errors with callbacks.
 */
export interface AuthorizationHandlers<T> {
  /** Handle successful response */
  onSuccess: (data: T) => void;
  /** Handle 401 Unauthorized */
  onUnauthorized?: (error: ApiError) => void;
  /** Handle 403 Forbidden */
  onForbidden?: (error: AuthorizationError, permissions: RequiredPermission[]) => void;
  /** Handle other errors */
  onError?: (error: ApiError) => void;
}

/**
 * Process a result with authorization-aware handlers.
 */
export function handleAuthorizationResult<T>(
  result: Result<T, ApiError>,
  handlers: AuthorizationHandlers<T>
): void {
  if (result.ok) {
    handlers.onSuccess(result.data);
    return;
  }
  
  const error = result.error;
  
  if (isUnauthorized(error)) {
    handlers.onUnauthorized?.(error);
    return;
  }
  
  if (isForbidden(error)) {
    handlers.onForbidden?.(error as AuthorizationError, getRequiredPermissions(error));
    return;
  }
  
  handlers.onError?.(error);
}

/**
 * Create a permission check function for a specific resource.
 */
export function createPermissionChecker(
  userPermissions: string[],
  tenantId?: string
) {
  return {
    /**
     * Check if user has all required permissions.
     */
    hasAll(...permissions: string[]): boolean {
      return permissions.every(p => userPermissions.includes(p));
    },
    
    /**
     * Check if user has any of the required permissions.
     */
    hasAny(...permissions: string[]): boolean {
      return permissions.some(p => userPermissions.includes(p));
    },
    
    /**
     * Check specific permission.
     */
    has(permission: string): boolean {
      return userPermissions.includes(permission);
    },
    
    /**
     * Get current tenant context.
     */
    getTenantId(): string | undefined {
      return tenantId;
    },
  };
}

/**
 * Wrap an API call with automatic 403 handling.
 */
export async function withPermissionGuard<T>(
  call: () => Promise<Result<T, ApiError>>,
  requiredPermissions: string[],
  onInsufficientPermissions: (missing: RequiredPermission[]) => void
): Promise<Result<T, ApiError>> {
  const result = await call();
  
  if (!result.ok && isForbidden(result.error)) {
    const required = getRequiredPermissions(result.error);
    if (required.length > 0) {
      onInsufficientPermissions(required);
    }
  }
  
  return result;
}
```

---

*This completes Part 2. Continue to Part 3 for Features/Entitlements, Multi-Tenancy, Error Model, and Security Review.*
