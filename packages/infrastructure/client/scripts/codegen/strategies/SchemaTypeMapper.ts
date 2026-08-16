/**
 * Schema Type Mapper - Strategy Pattern
 */

import type { OpenAPIV3 } from 'openapi-types';
import { resolveKnownSchemaTypeName } from '../../normalize.js';

export interface SchemaTypeMapper {
  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean;
  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string;
}

export class ReferenceTypeMapper implements SchemaTypeMapper {
  constructor(private readonly knownSchemaNames?: Set<string>) {}

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return '$ref' in schema;
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) {
      const rawTypeName = schema.$ref.replace('#/components/schemas/', '');
      return resolveKnownSchemaTypeName(rawTypeName, this.knownSchemaNames) ?? 'unknown';
    }
    throw new Error('Not a reference schema');
  }
}

export class StringTypeMapper implements SchemaTypeMapper {
  private readonly FORMAT_MAP: Record<string, string> = {
    'date-time': 'string',
    'date': 'string',
    'uuid': 'string',
    'binary': 'Blob',
  };

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && schema.type === 'string';
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.SchemaObject;
    const nullable = schemaObj.nullable ? ' | null' : '';

    if (schemaObj.enum) {
      return schemaObj.enum.map((v) => `'${v}'`).join(' | ') + nullable;
    }

    if (schemaObj.format && this.FORMAT_MAP[schemaObj.format]) {
      return this.FORMAT_MAP[schemaObj.format] + nullable;
    }

    return `string${nullable}`;
  }
}

export class NumberTypeMapper implements SchemaTypeMapper {
  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && (schema.type === 'number' || schema.type === 'integer');
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');
    const schemaObj = schema as OpenAPIV3.SchemaObject;
    return `number${schemaObj.nullable ? ' | null' : ''}`;
  }
}

export class BooleanTypeMapper implements SchemaTypeMapper {
  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && schema.type === 'boolean';
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');
    const schemaObj = schema as OpenAPIV3.SchemaObject;
    return `boolean${schemaObj.nullable ? ' | null' : ''}`;
  }
}

export class ArrayTypeMapper implements SchemaTypeMapper {
  constructor(private typeMapperChain: TypeMapperChain) {}

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && schema.type === 'array';
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.ArraySchemaObject;
    const nullable = schemaObj.nullable ? ' | null' : '';

    if (schemaObj.items) {
      const itemType = this.typeMapperChain.map(schemaObj.items as OpenAPIV3.SchemaObject);
      return `Array<${itemType}>${nullable}`;
    }

    return `unknown[]${nullable}`;
  }
}

export class ObjectTypeMapper implements SchemaTypeMapper {
  constructor(private typeMapperChain: TypeMapperChain) {}

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && (schema.type === 'object' || !schema.type);
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.SchemaObject;
    const nullable = schemaObj.nullable ? ' | null' : '';

    if (schemaObj.additionalProperties) {
      if (schemaObj.additionalProperties === true) {
        return `Record<string, unknown>${nullable}`;
      }
      const valueType = this.typeMapperChain.map(schemaObj.additionalProperties as OpenAPIV3.SchemaObject);
      return `Record<string, ${valueType}>${nullable}`;
    }

    if (schemaObj.properties) {
      const props = Object.entries(schemaObj.properties)
        .map(([key, val]) => {
          const propType = this.typeMapperChain.map(val as OpenAPIV3.SchemaObject);
          const optional = schemaObj.required?.includes(key) ? '' : '?';
          return `${key}${optional}: ${propType}`;
        })
        .join('; ');
      return `{ ${props} }${nullable}`;
    }

    return `Record<string, unknown>${nullable}`;
  }
}

export class UnionTypeMapper implements SchemaTypeMapper {
  constructor(private typeMapperChain: TypeMapperChain) {}

  canHandle(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): boolean {
    return !('$ref' in schema) && !!(schema.oneOf || schema.anyOf);
  }

  map(schema: OpenAPIV3.SchemaObject | OpenAPIV3.ReferenceObject): string {
    if ('$ref' in schema) throw new Error('Reference schema');

    const schemaObj = schema as OpenAPIV3.SchemaObject;
    const variants = schemaObj.oneOf || schemaObj.anyOf || [];
    const nullable = schemaObj.nullable ? ' | null' : '';

    const types = variants.map((v) => this.typeMapperChain.map(v as OpenAPIV3.SchemaObject)).join(' | ');
    return types + nullable;
  }
}

/**
 * Chain of Responsibility for type mapping
 */
export class TypeMapperChain {
  private mappers: SchemaTypeMapper[] = [];

  constructor(knownSchemaNames?: Set<string>) {
    // Order matters - check references first
    this.mappers.push(new ReferenceTypeMapper(knownSchemaNames));
    this.mappers.push(new StringTypeMapper());
    this.mappers.push(new NumberTypeMapper());
    this.mappers.push(new BooleanTypeMapper());

    // These need the chain instance
    const arrayMapper = new ArrayTypeMapper(this);
    const objectMapper = new ObjectTypeMapper(this);
    const unionMapper = new UnionTypeMapper(this);

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
    return `unknown${schemaObj.nullable ? ' | null' : ''}`;
  }
}
