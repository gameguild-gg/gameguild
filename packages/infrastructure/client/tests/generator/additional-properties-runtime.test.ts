import { runInNewContext } from 'node:vm';
import ts from 'typescript';
import { describe, expect, it } from 'vitest';
import { z } from 'zod';

import { generateTypes } from '../../scripts/codegen/types.js';
import type { OpenApiSpec } from '../../scripts/fetch-spec.js';

function generatedSchema(additionalProperties: true | Record<string, unknown>) {
  const spec = {
    openapi: '3.0.3',
    info: { title: 'Extension contract', version: '1' },
    paths: {},
    components: {
      schemas: {
        Problem: {
          type: 'object',
          properties: { title: { type: 'string' } },
          additionalProperties,
        },
      },
    },
  } as OpenApiSpec;
  const source = generateTypes(spec);
  const output = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const exposed: Record<string, z.ZodType> = {};
  // Evaluate only this owned generator's output, with one explicit library dependency.
  runInNewContext(output, {
    exports: exposed,
    require: (name: string) => {
      if (name !== 'zod') throw new Error('Unexpected generated dependency');
      return { z };
    },
  }, { timeout: 1_000 });
  return exposed.ProblemSchema!;
}

describe('Generated schemas preserve JSON Schema additional-property values', () => {
  it.each([
    ['string', 'trace-identifier'],
    ['number', 42],
    ['boolean', true],
    ['null', null],
    ['array', ['trace-identifier']],
    ['object', { nested: 'trace-identifier' }],
  ])('an empty value schema accepts and preserves a %s extension', (kind, extension) => {
    const payload = { title: 'Request rejected', extension };
    expect(generatedSchema({}).parse(payload), `Valid ${kind} extension`).toEqual(payload);
  });

  it('the boolean-true form also preserves an extension value', () => {
    const payload = { title: 'Request rejected', traceId: 'trace-identifier' };
    expect(generatedSchema(true).parse(payload)).toEqual(payload);
  });

  it('an unconstrained extension does not weaken a declared property', () => {
    expect(generatedSchema({}).safeParse({ title: 42, traceId: 'trace-identifier' }).success).toBe(false);
  });

  it('a typed additional-property schema still rejects an incompatible value', () => {
    const schema = generatedSchema({ type: 'string' });
    expect(schema.safeParse({ title: 'Request rejected', traceId: 'trace-identifier' }).success).toBe(true);
    expect(schema.safeParse({ title: 'Request rejected', traceId: 42 }).success).toBe(false);
  });
});
