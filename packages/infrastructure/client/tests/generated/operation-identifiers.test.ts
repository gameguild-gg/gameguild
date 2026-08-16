import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const generatedRoot = fileURLToPath(new URL('../../src/generated/', import.meta.url));
const endpointsSource = readFileSync(join(generatedRoot, 'endpoints.gen.ts'), 'utf8');

describe('generated operation identifiers', () => {
  it('does not publish operation IDs or module methods with numeric collision suffixes', () => {
    const operationIds = Array.from(
      endpointsSource.matchAll(/operationId: ['"]([A-Za-z_$][A-Za-z0-9_$]*)['"] as const/g),
      (match) => match[1],
    );

    const moduleMethods = readdirSync(join(generatedRoot, 'modules'))
      .filter((fileName) => fileName.endsWith('.gen.ts'))
      .flatMap((fileName) => {
        const source = readFileSync(join(generatedRoot, 'modules', fileName), 'utf8');
        return Array.from(
          source.matchAll(/\basync ([A-Za-z_$][A-Za-z0-9_$]*)\s*\(/g),
          (match) => match[1],
        );
      });

    expect(operationIds.length).toBeGreaterThan(0);
    expect(moduleMethods.length).toBeGreaterThan(0);
    expect(operationIds.filter((operationId) => /\d+$/.test(operationId))).toEqual([]);
    expect(moduleMethods.filter((methodName) => /\d+$/.test(methodName))).toEqual([]);
  }, 15_000);

  it('does not contain duplicate entries in the generated endpoint registry', () => {
    const registryEntries = Array.from(
      endpointsSource.matchAll(/^\s{2}([A-Za-z_$][A-Za-z0-9_$]*):\s+\1Endpoint,\s*$/gm),
      (match) => match[1],
    );
    const seen = new Set<string>();
    const duplicates = new Set<string>();

    for (const entry of registryEntries) {
      if (seen.has(entry)) duplicates.add(entry);
      seen.add(entry);
    }

    expect(registryEntries.length).toBeGreaterThan(0);
    expect([...duplicates]).toEqual([]);
  });
});
