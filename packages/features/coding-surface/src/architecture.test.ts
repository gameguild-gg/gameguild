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
