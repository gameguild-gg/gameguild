import { readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import * as publicApi from './index';

const packageRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

describe('code grading adapter boundaries', () => {
  it('depends only on public coding and grading entrypoints in production code', () => {
    const source = readSources(join(packageRoot, 'src'), { excludeTesting: true });
    expect(source).not.toMatch(/@game-guild\/(?:coding|grading)\//);
    for (const forbidden of ['emception', '@gameguild/emception-ide', '@game-guild/emception-ui', '@game-guild/quiz', 'react', 'next']) {
      expect(source, forbidden).not.toMatch(new RegExp(`from ["']${escapeRegExp(forbidden)}`));
    }
  });

  it('does not add a coding dependency to the generic grading core', () => {
    const gradingRoot = join(packageRoot, '..', 'grading');
    const gradingPackage = readFileSync(join(gradingRoot, 'package.json'), 'utf8');
    const gradingSource = readSources(join(gradingRoot, 'src'));
    expect(gradingPackage).not.toContain('@game-guild/coding');
    expect(gradingPackage).not.toContain('@game-guild/grading-adapter-code');
    expect(gradingSource).not.toContain('@game-guild/coding');
    expect(gradingSource).not.toContain('@game-guild/grading-adapter-code');
  });

  it('exports projection, delivery, decoding, and report mapping publicly', () => {
    expect(publicApi.projectCodeGradingItems).toBeTypeOf('function');
    expect(publicApi.createCodeExecutionDelivery).toBeTypeOf('function');
    expect(publicApi.decodeCodeSubmissionEnvelope).toBeTypeOf('function');
    expect(publicApi.mapCodeExecutionReportToGrade).toBeTypeOf('function');
  });
});

function readSources(directory: string, options: { excludeTesting?: boolean } = {}): string {
  return readdirSync(directory)
    .map((entry) => join(directory, entry))
    .flatMap((entry) => {
      if (options.excludeTesting && relative(directory, entry) === 'testing') return [];
      return statSync(entry).isDirectory()
        ? [readSources(entry, options)]
        : entry.endsWith('.ts') && !entry.endsWith('.test.ts')
          ? [readFileSync(entry, 'utf8')]
          : [];
    })
    .join('\n');
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
