import { runInNewContext } from 'node:vm';
import ts from 'typescript';
import { z } from 'zod';
import type { OpenAPIV3 } from 'openapi-types';
import { describe, expect, it } from 'vitest';
import { generateTypes } from '../../scripts/codegen/types.js';
import type { OpenApiSpec } from '../../scripts/fetch-spec.js';

function runtimeSchema(additionalProperties: boolean | OpenAPIV3.SchemaObject): z.ZodType {
  const spec = {
    openapi: '3.0.1', info: { title: 'Extension contracts', version: '1' }, paths: {},
    components: { schemas: { ExtensibleDto: {
      type: 'object', required: ['status'], properties: { status: { type: 'integer' } }, additionalProperties,
    } } },
  } as OpenApiSpec;
  const compiled = ts.transpileModule(generateTypes(spec), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const exports: Record<string, unknown> = {};
  runInNewContext(compiled, {
    exports,
    require: (id: string) => {
      if (id !== 'zod') throw new Error(`Unexpected generated dependency: ${id}`);
      return { z };
    },
  }, { timeout: 1000 });
  return exports.ExtensibleDtoSchema as z.ZodType;
}

describe('generated additional-property value contracts', () => {
  it.each([true, {}])('accepts every JSON extension value for unrestricted schema %j', (additional) => {
    const schema = runtimeSchema(additional);
    for (const extension of ['synthetic-trace', 7, true, null, ['synthetic'], { source: 'synthetic' }]) {
      expect(schema.parse({ status: 403, traceId: extension })).toEqual({ status: 403, traceId: extension });
    }
  });

  it('retains explicit extension value restrictions and the declared property type', () => {
    const schema = runtimeSchema({ type: 'string' });
    expect(schema.parse({ status: 403, detail: 'Synthetic detail' })).toEqual({ status: 403, detail: 'Synthetic detail' });
    expect(schema.safeParse({ status: 403, detail: 7 }).success).toBe(false);
    expect(schema.safeParse({ status: '403', detail: 'Synthetic detail' }).success).toBe(false);
  });
});
