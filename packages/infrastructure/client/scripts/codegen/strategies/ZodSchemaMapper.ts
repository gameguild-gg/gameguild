/**
 * Zod Schema Mapper - Strategy Pattern for Runtime Validation
 *
 * Generates Zod schema definitions from OpenAPI schemas for runtime validation.
 */

import type { OpenAPIV3 } from 'openapi-types';
import { resolveKnownSchemaTypeName } from '../../normalize.js';

export interface ZodSchemaMapper {
  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean;
  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string;
}

export class ZodReferenceMapper implements ZodSchemaMapper {
  constructor(private readonly knownSchemaNames?: Set<string>) {}

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return '$ref' in schema;
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) {
      const rawTypeName = schema.$ref.replace('#/components/schemas/', '');
      const typeName = resolveKnownSchemaTypeName(rawTypeName, this.knownSchemaNames);
      if (!typeName) {
        return 'z.unknown()';
      }
      // Use z.lazy() to defer evaluation and handle circular/forward references
      return `z.lazy(() => ${typeName}Schema)`;
    }
    throw new Error('Not a reference schema');
  }
}

export class ZodStringMapper implements ZodSchemaMapper {
  private readonly FORMAT_MAP: Record<string, string> = {
    'email': '.email()',
    'uuid': '.uuid()',
    'uri': '.url()',
    'url': '.url()',
    'date-time': '.datetime()',
    'date': '.date()',
  };

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && schema.type === 'string';
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.SchemaObject;
    let zodSchema = 'z.string()';

    // Handle enums
    if (schemaObj.enum && schemaObj.enum.length > 0) {
      const literals = schemaObj.enum.map((v) => `'${v}'`).join(', ');
      zodSchema = `z.enum([${literals}])`;
    } else {
      // Apply format validations
      if (schemaObj.format && this.FORMAT_MAP[schemaObj.format]) {
        zodSchema += this.FORMAT_MAP[schemaObj.format];
      }

      // Apply string constraints
      if (schemaObj.minLength !== undefined) {
        zodSchema += `.min(${schemaObj.minLength})`;
      }
      if (schemaObj.maxLength !== undefined) {
        zodSchema += `.max(${schemaObj.maxLength})`;
      }
      if (schemaObj.pattern) {
        zodSchema += `.regex(/${schemaObj.pattern}/)`;
      }
    }

    // Handle nullable
    if (schemaObj.nullable) {
      zodSchema += '.nullable()';
    }

    return zodSchema;
  }
}

export class ZodNumberMapper implements ZodSchemaMapper {
  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && (schema.type === 'number' || schema.type === 'integer');
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.SchemaObject;
    let zodSchema = schemaObj.type === 'integer' ? 'z.number().int()' : 'z.number()';

    // Apply numeric constraints
    if (schemaObj.minimum !== undefined) {
      zodSchema += `.min(${schemaObj.minimum})`;
    }
    if (schemaObj.maximum !== undefined) {
      zodSchema += `.max(${schemaObj.maximum})`;
    }

    // Handle nullable
    if (schemaObj.nullable) {
      zodSchema += '.nullable()';
    }

    return zodSchema;
  }
}

export class ZodBooleanMapper implements ZodSchemaMapper {
  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && schema.type === 'boolean';
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.SchemaObject;
    let zodSchema = 'z.boolean()';

    if (schemaObj.nullable) {
      zodSchema += '.nullable()';
    }

    return zodSchema;
  }
}

export class ZodArrayMapper implements ZodSchemaMapper {
  constructor(private zodMapperChain: ZodSchemaMapperChain) {}

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && schema.type === 'array';
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.ArraySchemaObject;
    let zodSchema: string;

    if (schemaObj.items) {
      const itemSchema = this.zodMapperChain.map(schemaObj.items as OpenAPIV3.SchemaObject);
      zodSchema = `z.array(${itemSchema})`;

      // Apply array constraints
      if (schemaObj.minItems !== undefined) {
        zodSchema += `.min(${schemaObj.minItems})`;
      }
      if (schemaObj.maxItems !== undefined) {
        zodSchema += `.max(${schemaObj.maxItems})`;
      }
    } else {
      zodSchema = 'z.array(z.unknown())';
    }

    if (schemaObj.nullable) {
      zodSchema += '.nullable()';
    }

    return zodSchema;
  }
}

export class ZodObjectMapper implements ZodSchemaMapper {
  constructor(private zodMapperChain: ZodSchemaMapperChain) {}

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && (schema.type === 'object' || !schema.type);
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.SchemaObject;
    let zodSchema: string;

    if (schemaObj.additionalProperties) {
      if (schemaObj.additionalProperties === true) {
        zodSchema = 'z.record(z.string(), z.unknown())';
      } else {
        const valueSchema = this.zodMapperChain.map(schemaObj.additionalProperties as OpenAPIV3.SchemaObject);
        zodSchema = `z.record(z.string(), ${valueSchema})`;
      }
    } else if (schemaObj.properties) {
      const props = Object.entries(schemaObj.properties)
        .map(([key, val]) => {
          const propSchema = this.zodMapperChain.map(val as OpenAPIV3.SchemaObject);
          const safePropName = /^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(key) ? key : `'${key}'`;
          return `  ${safePropName}: ${propSchema}`;
        })
        .join(',\n');
      zodSchema = `z.object({\n${props}\n})`;
    } else {
      zodSchema = 'z.record(z.string(), z.unknown())';
    }

    if (schemaObj.nullable) {
      zodSchema += '.nullable()';
    }

    return zodSchema;
  }
}

export class ZodUnionMapper implements ZodSchemaMapper {
  constructor(private zodMapperChain: ZodSchemaMapperChain) {}

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && !!(schema.oneOf || schema.anyOf);
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.SchemaObject;
    const variants = schemaObj.oneOf || schemaObj.anyOf || [];

    const schemas = variants.map((v) => this.zodMapperChain.map(v as OpenAPIV3.SchemaObject));
    let zodSchema: string;

    if (schemas.length === 0) {
      zodSchema = 'z.unknown()';
    } else if (schemas.length === 1) {
      zodSchema = schemas[0];
    } else {
      zodSchema = `z.union([${schemas.join(', ')}])`;
    }

    if (schemaObj.nullable) {
      zodSchema += '.nullable()';
    }

    return zodSchema;
  }
}

/**
 * Chain of Responsibility for Zod schema mapping
 */
export class ZodSchemaMapperChain {
  private mappers: ZodSchemaMapper[] = [];

  constructor(knownSchemaNames?: Set<string>) {
    // Order matters - check references first
    this.mappers.push(new ZodReferenceMapper(knownSchemaNames));
    this.mappers.push(new ZodStringMapper());
    this.mappers.push(new ZodNumberMapper());
    this.mappers.push(new ZodBooleanMapper());

    // These need the chain instance
    const arrayMapper = new ZodArrayMapper(this);
    const objectMapper = new ZodObjectMapper(this);
    const unionMapper = new ZodUnionMapper(this);

    this.mappers.push(unionMapper);
    this.mappers.push(arrayMapper);
    this.mappers.push(objectMapper);
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    for (const mapper of this.mappers) {
      if (mapper.canHandle(schema)) {
        return mapper.map(schema);
      }
    }

    // Fallback
    const schemaObj = schema as OpenAPIV3.SchemaObject;
    return `z.unknown()${schemaObj.nullable ? '.nullable()' : ''}`;
  }
}
