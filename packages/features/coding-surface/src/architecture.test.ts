import { readdirSync, readFileSync } from 'node:fs';
import { extname, join } from 'node:path';
import { describe, expect, it } from 'vitest';
import ts from 'typescript';

describe('coding surface architecture', () => {
  it('depends only on public boundaries', () => {
    const source = readProductionSource(import.meta.dirname);
    const specifiers = [...source.matchAll(/(?:\bfrom\s+|\bimport\s*\(\s*)['"]([^'"]+)['"]/g)].map((match) => match[1]);
    for (const specifier of specifiers) {
      expect(specifier).not.toContain('@game-guild/emception-ui');
      expect(specifier).not.toContain('apps/web');
      expect(specifier).not.toMatch(/^next(?:\/|$)/);
      expect(specifier).not.toContain('/src/');
    }
  });

  it('keeps the draft session independent from UI, runtime and browser storage', () => {
    for (const name of ['authoring-persistence.ts', 'authoring-persistence-types.ts']) {
      const source = readFileSync(join(import.meta.dirname, name), 'utf8');
      const file = ts.createSourceFile(name, source, ts.ScriptTarget.Latest, true);
      for (const statement of file.statements) {
        if (!ts.isImportDeclaration(statement) || !ts.isStringLiteral(statement.moduleSpecifier)) continue;
        expect(['@game-guild/coding', './authoring-persistence-types']).toContain(statement.moduleSpecifier.text);
      }
      expect(source).not.toMatch(/\b(?:localStorage|sessionStorage|indexedDB|Worker|window)\b/);
    }
  });

  it('exposes the owned persistence port and session through the public entry point', () => {
    const source = readFileSync(join(import.meta.dirname, 'index.ts'), 'utf8');
    const file = ts.createSourceFile('index.ts', source, ts.ScriptTarget.Latest, true);
    const exported = file.statements.flatMap((statement) =>
      ts.isExportDeclaration(statement) && statement.moduleSpecifier && ts.isStringLiteral(statement.moduleSpecifier) ? [statement.moduleSpecifier.text] : [],
    );
    expect(exported).toContain('./authoring-persistence');
    expect(exported).toContain('./authoring-persistence-types');
    expect(exported).toContain('./runtime-session');
    expect(exported).toContain('./runtime-types');
    expect(exported).toContain('./runtime-capabilities');
    expect(exported).toContain('./runtime-capability-types');
    expect(exported).toContain('./graphics-session');
    expect(exported).toContain('./graphics-session-types');
    expect(exported).toContain('./program-session');
    expect(exported).toContain('./program-session-types');
    expect(exported).toContain('./authoring-types');
    expect(exported).toContain('./authoring-surface');
    expect(exported).toContain('./exercise-validation');
    expect(exported).toContain('./exercise-validation-types');
    expect(exported).toContain('./exercise-preparation');
    expect(exported).toContain('./learner-practice-types');
    expect(exported).toContain('./learner-practice');
    expect(exported).toContain('./learner-cases');
  });

  it('keeps personal case preparation offline and outside academic and author-only state', () => {
    const allowed: Record<string, readonly string[]> = {
      'learner-practice-types.ts': ['@game-guild/coding'],
      'learner-practice.ts': ['@game-guild/coding', './learner-practice-types'],
      'learner-cases.tsx': ['react', 'lucide-react', './learner-practice', './learner-practice-types', './learner-practice.css'],
    };
    for (const [name, imports] of Object.entries(allowed)) {
      const source = readFileSync(join(import.meta.dirname, name), 'utf8');
      const file = ts.createSourceFile(name, source, ts.ScriptTarget.Latest, true);
      for (const statement of file.statements) {
        if (ts.isImportDeclaration(statement) && ts.isStringLiteral(statement.moduleSpecifier)) expect(imports).toContain(statement.moduleSpecifier.text);
      }
      expect(source).not.toMatch(/\b(?:localStorage|sessionStorage|indexedDB|Worker|fetch|createEmception|validationSolutions|CodingAuthoringDefinition)\b/);
      expect(source).not.toMatch(/\b(?:submit|release|grantPermission|runTests|compileAndRun)\s*\(/);
    }
  });

  it('keeps authoring views offline and outside academic persistence/execution', () => {
    const names = readdirSync(import.meta.dirname).filter((name) => /^authoring-(?:model|types|surface|fields|problem|environment|starter|tests)\.(?:ts|tsx)$/.test(name));
    expect(names).toHaveLength(8);
    for (const name of names) {
      const source = readFileSync(join(import.meta.dirname, name), 'utf8');
      const file = ts.createSourceFile(name, source, ts.ScriptTarget.Latest, true);
      for (const statement of file.statements) {
        if (!ts.isImportDeclaration(statement) || !ts.isStringLiteral(statement.moduleSpecifier)) continue;
        const specifier = statement.moduleSpecifier.text;
        expect(specifier === '@game-guild/coding' || specifier === 'react' || specifier === 'lucide-react'
          || specifier === '@monaco-editor/react' || specifier.startsWith('./authoring')).toBe(true);
      }
      expect(source).not.toMatch(/\b(?:localStorage|sessionStorage|indexedDB|createEmception|new\s+Worker|fetch)\b/);
      expect(source).not.toMatch(/\b(?:submit|release|grantPermission|runTests|compileAndRun)\s*\(/);
    }
  });

  it('keeps exercise checks behind the existing runner and preview behind a learner-only port', () => {
    const allowed: Record<string, readonly string[]> = {
      'exercise-preparation.ts': ['@game-guild/coding'],
      'exercise-validation-types.ts': ['@game-guild/coding', './runtime-types', './types'],
      'exercise-validation.ts': ['@game-guild/coding', './exercise-preparation', './preview-runner', './types', './exercise-validation-types'],
      'authoring-learner-preview.tsx': ['@game-guild/coding', './workbench-session-types', './coding-surface'],
    };
    for (const [name, imports] of Object.entries(allowed)) {
      const source = readFileSync(join(import.meta.dirname, name), 'utf8');
      const file = ts.createSourceFile(name, source, ts.ScriptTarget.Latest, true);
      for (const statement of file.statements) {
        if (ts.isImportDeclaration(statement) && ts.isStringLiteral(statement.moduleSpecifier)) expect(imports).toContain(statement.moduleSpecifier.text);
      }
      expect(source).not.toMatch(/\b(?:localStorage|sessionStorage|indexedDB|fetch|createEmception|Worker|compileAndRun)\b/);
      expect(source).not.toMatch(/\b(?:submit|release|grantPermission|runTests)\s*\(/);
    }
    const preview = readFileSync(join(import.meta.dirname, 'authoring-learner-preview.tsx'), 'utf8');
    expect(preview).not.toMatch(/\b(?:CodingAuthoringDraft|CodingAuthoringDefinition|validationSolutions|onSubmit|onReviewDecision|workspaceStorageKey)\b/);
    expect(preview).toContain('enableWorkspace={false}');
    expect(preview).toContain('mode="learner"');
  });

  it('keeps runtime acquisition lazy and outside academic/host commands', () => {
    for (const name of ['runtime-session.ts', 'runtime-types.ts']) {
      const source = readFileSync(join(import.meta.dirname, name), 'utf8');
      const file = ts.createSourceFile(name, source, ts.ScriptTarget.Latest, true);
      for (const statement of file.statements) {
        if (!ts.isImportDeclaration(statement) || !ts.isStringLiteral(statement.moduleSpecifier)) continue;
        const specifier = statement.moduleSpecifier.text;
        expect(['@game-guild/coding', '@gameguild/emception-browser', 'emception', './runtime-types']).toContain(specifier);
        if (specifier !== '@game-guild/coding') expect(statement.importClause?.isTypeOnly).toBe(true);
      }
      expect(source).not.toMatch(/\b(?:localStorage|sessionStorage|indexedDB|new\s+Worker)\b/);
      expect(source).not.toMatch(/\b(?:submit|release|grantPermission|fetchAssessment)\s*\(/);
    }
  });

  it('resolves capabilities without importing a runtime or academic implementation', () => {
    for (const name of ['runtime-capabilities.ts', 'runtime-capability-types.ts']) {
      const source = readFileSync(join(import.meta.dirname, name), 'utf8');
      const file = ts.createSourceFile(name, source, ts.ScriptTarget.Latest, true);
      for (const statement of file.statements) {
        if (!ts.isImportDeclaration(statement) || !ts.isStringLiteral(statement.moduleSpecifier)) continue;
        const specifier = statement.moduleSpecifier.text;
        expect(['@game-guild/coding', './runtime-capability-types', './types']).toContain(specifier);
        if (specifier !== '@game-guild/coding') expect(statement.importClause?.isTypeOnly).toBe(true);
      }
      expect(source).not.toMatch(/\b(?:fetch|import|submit|release|grantPermission|fetchAssessment)\s*\(/);
      expect(source).not.toMatch(/\b(?:localStorage|sessionStorage|indexedDB|new\s+Worker)\b/);
    }
  });

  it('keeps graphical execution within public runtime and owned local ports', () => {
    for (const name of ['graphics-session.ts', 'graphics-session-types.ts']) {
      const source = readFileSync(join(import.meta.dirname, name), 'utf8');
      const file = ts.createSourceFile(name, source, ts.ScriptTarget.Latest, true);
      for (const statement of file.statements) {
        if (!ts.isImportDeclaration(statement) || !ts.isStringLiteral(statement.moduleSpecifier)) continue;
        expect(['@game-guild/coding', '@gameguild/emception-browser', 'emception', './graphics-session-types', './runtime-capabilities', './runtime-session', './runtime-types']).toContain(statement.moduleSpecifier.text);
      }
      expect(source).not.toMatch(/\b(?:fetch|import|submit|release|runTests|compileAndRun)\s*\(/);
      expect(source).not.toMatch(/\b(?:localStorage|sessionStorage|indexedDB|new\s+Worker)\b/);
    }
  });

  it('adapts text input without implementing another compiler or academic runner', () => {
    for (const name of ['program-session.ts', 'program-session-types.ts']) {
      const source = readFileSync(join(import.meta.dirname, name), 'utf8');
      const file = ts.createSourceFile(name, source, ts.ScriptTarget.Latest, true);
      for (const statement of file.statements) {
        if (!ts.isImportDeclaration(statement) || !ts.isStringLiteral(statement.moduleSpecifier)) continue;
        expect(['@game-guild/coding', '@gameguild/emception-browser', 'emception', './program-session-types', './runtime-capabilities', './runtime-session', './runtime-types']).toContain(statement.moduleSpecifier.text);
      }
      expect(source).not.toMatch(/\b(?:submit|release|runTests|new\s+Worker)\s*\(/);
      expect(source).not.toMatch(/\b(?:localStorage|sessionStorage|indexedDB)\b/);
      expect(source).not.toMatch(/(?:wasm-ld|clang|cc1|--allow-undefined|--entry=main)/);
    }
  });
});

function readProductionSource(directory: string): string {
  return readdirSync(directory, { withFileTypes: true })
    .flatMap((entry) => {
      const path = join(directory, entry.name);
      if (entry.isDirectory()) return readProductionSource(path);
      if (entry.name.endsWith('.test.ts') || entry.name.endsWith('.test.tsx') || !['.ts', '.tsx'].includes(extname(entry.name))) return [];
      return [readFileSync(path, 'utf8')];
    })
    .join('\n');
}
