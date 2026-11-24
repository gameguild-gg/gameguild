import { parseCodingAuthoringDraft } from '@game-guild/coding';
import { describe, expect, it } from 'vitest';
import drafts from '../../coding/fixtures/coding-authoring-drafts.json';
import { environmentChange, starterSource } from './authoring-model';

describe('authoring source preparation', () => {
  it.each(['c', 'cpp'] as const)('produces a bounded %s function stub without reference answers or transport boilerplate', (language) => {
    const draft = parseCodingAuthoringDraft(drafts.function);
    draft.environment.language = language;
    draft.functionSignature = { name: 'echo', parameters: [{ name: 'value', type: 'string' }], returnType: 'string' };
    const source = starterSource(draft);
    expect(source).toContain(language === 'cpp' ? 'std::string echo(std::string value)' : 'const char* echo(const char* value)');
    expect(source).not.toMatch(/SYNTHETIC|extern|main\(|emception|doctest/);
    expect(source).toContain('return "";');
  });

  it('rejects malformed names, duplicate parameters and unsupported graphical scaffolds', () => {
    const draft = parseCodingAuthoringDraft(drafts.function);
    draft.functionSignature = { name: 'bad;name', parameters: [], returnType: 'integer' };
    expect(() => starterSource(draft)).toThrow(/complete function signature/);
    draft.functionSignature = {
      name: 'sum',
      parameters: [
        { name: 'same', type: 'integer' },
        { name: 'same', type: 'integer' },
      ],
      returnType: 'integer',
    };
    expect(() => starterSource(draft)).toThrow(/complete function signature/);
    draft.environment.runtime = 'sdl3';
    expect(() => starterSource(draft)).toThrow(/unavailable/);
  });

  it('plans environment and exact toolchain changes together without replacing authored or private files', () => {
    const draft = parseCodingAuthoringDraft(drafts.function);
    const original = structuredClone(draft);
    const plan = environmentChange(draft, { language: 'c', runtime: 'wasi' });
    expect(plan.baseline).toBe(JSON.stringify(draft));
    expect(plan.candidate.environment).toEqual({ language: 'c', runtime: 'wasi' });
    expect(plan.candidate.toolchain.key).toBe('c-emception');
    expect(plan.candidate.files).toEqual(draft.files);
    expect(plan.candidate.validationSolutions).toEqual(draft.validationSolutions);
    expect(plan.impact.filesToReview).toEqual(draft.files.map((file) => file.id));
    expect(draft).toEqual(original);
  });
});
