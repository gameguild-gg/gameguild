import { describe, expect, it } from 'vitest';
import { resolveSeed, type SeedFile } from './resolve-seed';

const seedFiles: SeedFile[] = [
  { path: 'main.cpp', content: '// starter', encoding: 'text' },
  { path: 'util.h', content: '#pragma once', encoding: 'text' },
];

const submissionFiles: SeedFile[] = [
  { path: 'main.cpp', content: 'int main(){}', encoding: 'text' },
  { path: 'extra.py', content: 'print("hi")', encoding: 'text' },
];

describe('resolveSeed', () => {
  it('returns seed mode with the seed files when there is no submission (null)', () => {
    expect(resolveSeed({ draftExists: false, submissionFiles: null, seedFiles })).toEqual({
      mode: 'seed',
      files: seedFiles,
    });
  });

  it('treats an empty submission array as no submission (seed mode)', () => {
    expect(resolveSeed({ draftExists: false, submissionFiles: [], seedFiles })).toEqual({
      mode: 'seed',
      files: seedFiles,
    });
  });

  it('submission mode replaces matching paths and adds new ones', () => {
    const { mode, files } = resolveSeed({ draftExists: false, submissionFiles, seedFiles });
    expect(mode).toBe('submission');
    const byPath = new Map(files.map((f) => [f.path, f]));
    expect(byPath.get('main.cpp')?.content).toBe('int main(){}');
    expect(byPath.get('extra.py')?.content).toBe('print("hi")');
  });

  it('seed files absent from the submission survive the overlay', () => {
    const { files } = resolveSeed({ draftExists: false, submissionFiles, seedFiles });
    const byPath = new Map(files.map((f) => [f.path, f]));
    expect(byPath.get('util.h')?.content).toBe('#pragma once');
  });

  it('keeps seed order first, replacements in place, additions appended last', () => {
    const { files } = resolveSeed({ draftExists: false, submissionFiles, seedFiles });
    expect(files.map((f) => f.path)).toEqual(['main.cpp', 'util.h', 'extra.py']);
  });

  it('draft short-circuits to empty files regardless of a submission', () => {
    expect(resolveSeed({ draftExists: true, submissionFiles, seedFiles })).toEqual({
      mode: 'draft',
      files: [],
    });
  });

  it('draft short-circuits with a null submission too', () => {
    expect(resolveSeed({ draftExists: true, submissionFiles: null, seedFiles })).toEqual({
      mode: 'draft',
      files: [],
    });
  });
});
