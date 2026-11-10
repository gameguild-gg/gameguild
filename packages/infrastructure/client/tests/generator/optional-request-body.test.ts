import ts from 'typescript';
import { describe, expect, it } from 'vitest';
import { generateModules } from '../../scripts/codegen/modules.js';
import type { OpenApiSpec } from '../../scripts/fetch-spec.js';

function spec(required?: boolean): OpenApiSpec {
  return {
    openapi: '3.0.3', info: { title: 'Optional credential body contract', version: '1.0' },
    paths: {
      '/v1/credentials/{id}': {
        post: {
          operationId: 'postCredentialProof', tags: ['CredentialProof'],
          parameters: [
            { name: 'id', in: 'path', required: true, schema: { type: 'string' } },
            { name: 'trace', in: 'query', required: false, schema: { type: 'string' } },
          ],
          requestBody: { ...(required === undefined ? {} : { required }),
            content: { 'application/json': { schema: { $ref: '#/components/schemas/CredentialProofInput' } } } },
          responses: { '204': { description: 'Accepted' } },
        },
      },
    },
    components: { schemas: { CredentialProofInput: { type: 'object', properties: { stepUpReceipt: { type: 'string' } } } } },
  };
}

function method(required?: boolean) {
  const code = generateModules(spec(required))['credential-proof'];
  const source = ts.createSourceFile('generated.ts', code, ts.ScriptTarget.ES2022, true, ts.ScriptKind.TS);
  const declaration = source.statements.find(ts.isClassDeclaration);
  if (!declaration) throw new Error('Missing generated module');
  const member = declaration.members.find(value => ts.isMethodDeclaration(value) && value.name.getText(source) === 'postCredentialProof');
  if (!member || !ts.isMethodDeclaration(member)) throw new Error('Missing generated operation');
  return { source, code, member };
}

// The public signature follows OpenAPI, including its default optional body.
// Generated credential-module HTTP tests separately prove omitted and supplied bodies at runtime.
describe('OpenAPI optional request-body module signature', () => {
  it.each([false, undefined])('allows an absent body when requestBody.required is %s', required => {
    const { member, source } = method(required);
    expect(member.parameters.map(parameter => parameter.name.getText(source))).toEqual(['id', 'body', 'query']);
    expect(member.parameters[0].questionToken).toBeUndefined();
    expect(member.parameters[1].questionToken).toBeDefined();
    expect(member.parameters[2].questionToken).toBeDefined();
  });

  it('keeps a declared required body required', () => {
    const { member } = method(true);
    expect(member.parameters[1].questionToken).toBeUndefined();
  });
});
