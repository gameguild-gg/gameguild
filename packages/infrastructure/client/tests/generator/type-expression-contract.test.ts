import path from 'node:path';
import ts from 'typescript';
import { describe, expect, it } from 'vitest';
import { generateTypes } from '../../scripts/codegen/types.js';
import type { OpenApiSpec } from '../../scripts/fetch-spec.js';
import { qualifyType } from '../../scripts/utils/type-qualify.js';

describe('generated type expression contracts', () => {
  it.each([
    ['Record<string, User>', 'Record<string, Types.User>'],
    ['Record<string, User | null>', 'Record<string, Types.User | null>'],
    ['Record<string, User> | null', 'Record<string, Types.User> | null'],
    ['Array<User> | null', 'Array<Types.User> | null'],
    ['Partial<User & Profile>', 'Partial<Types.User & Types.Profile>'],
    ['Map<User, Array<Profile | null>>', 'Map<Types.User, Array<Types.Profile | null>>'],
    ["Record<'a | b', User>", "Record<'a | b', Types.User>"],
    ['ReadonlyArray<Record<string, User[]>>', 'ReadonlyArray<Record<string, Types.User[]>>'],
  ])('qualifies nested references in %s', (input, expected) => {
    expect(qualifyType(input)).toBe(expected);
  });

  it('keeps scalar, enum, array and union DTO aliases valid for TypeScript consumers', () => {
    const spec: OpenApiSpec = {
      openapi: '3.0.1',
      info: { title: 'Alias contracts', version: '1.0' },
      paths: {},
      components: {
        schemas: {
          CountDto: { type: 'number' },
          StatusDto: { type: 'string', enum: ['ready', 'done'] },
          IdsDto: { type: 'array', items: { type: 'string' } },
          ChoiceDto: { oneOf: [{ type: 'string' }, { type: 'number' }] },
          UserDto: { type: 'object', required: ['id'], properties: { id: { type: 'string' } } },
        },
      },
    };
    const generated = ts.createSourceFile('generated.ts', generateTypes(spec), ts.ScriptTarget.Latest, true);
    // Compile the actual generated declarations, excluding runtime Zod schemas.
    const declarations = generated.statements
      .filter(statement => ts.isTypeAliasDeclaration(statement) || ts.isInterfaceDeclaration(statement))
      .map(statement => ts.createPrinter().printNode(ts.EmitHint.Unspecified, statement, generated))
      .join('\n');
    const source = declarations + `
      type Assert<T extends true> = T;
      type Same<A, B> = [A, B] extends [B, A] ? true : false;
      type CountContract = Assert<Same<Count, number>>;
      type StatusContract = Assert<Same<Status, 'ready' | 'done'>>;
      type IdsContract = Assert<Same<Ids, string[]>>;
      type ChoiceContract = Assert<Same<Choice, string | number>>;
      type UserContract = Assert<Same<User, { id: string }>>;
    `;
    const file = path.resolve('alias-consumer-contract.ts');
    const options: ts.CompilerOptions = {
      strict: true, noEmit: true, skipLibCheck: true, types: [], target: ts.ScriptTarget.ES2022,
    };
    const host = ts.createCompilerHost(options);
    const getSourceFile = host.getSourceFile.bind(host);
    host.getSourceFile = (name, languageVersion, onError, shouldCreateNewSourceFile) =>
      path.resolve(name) === file ? ts.createSourceFile(file, source, languageVersion, true)
        : getSourceFile(name, languageVersion, onError, shouldCreateNewSourceFile);
    const diagnostics = ts.getPreEmitDiagnostics(ts.createProgram([file], options, host));
    expect(diagnostics.map(diagnostic => ts.flattenDiagnosticMessageText(diagnostic.messageText, '\n'))).toEqual([]);
  });
});
