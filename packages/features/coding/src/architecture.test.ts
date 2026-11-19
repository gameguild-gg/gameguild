import { readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import * as publicApi from './index';

const packageRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

describe('coding package boundaries', () => {
  it('is framework-independent and does not depend on grading or Emception', () => {
    const packageJson = readFileSync(join(packageRoot, 'package.json'), 'utf8');
    const source = readSources(join(packageRoot, 'src'));
    for (const forbidden of ['@game-guild/grading', 'emception', '@gameguild/emception-ide', '@game-guild/emception-ui', 'react', 'next']) {
      expect(packageJson, forbidden).not.toContain(`"${forbidden}"`);
      expect(source, forbidden).not.toMatch(new RegExp(`from ["']${escapeRegExp(forbidden)}`));
    }
  });

  it('exports the parsers and learner-safe projector from its public entrypoint', () => {
    expect(publicApi.parseCodingAuthoringDefinition).toBeTypeOf('function');
    expect(publicApi.parseCodingSubmission).toBeTypeOf('function');
    expect(publicApi.parseCodingExecutionReport).toBeTypeOf('function');
    expect(publicApi.projectCodingDefinitionForLearner).toBeTypeOf('function');
  });
});

function readSources(directory: string): string {
  return readdirSync(directory)
    .map((entry) => join(directory, entry))
    .flatMap((entry) =>
      statSync(entry).isDirectory() ? [readSources(entry)] : entry.endsWith('.ts') && !entry.endsWith('.test.ts') ? [readFileSync(entry, 'utf8')] : [],
    )
    .join('\n');
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
