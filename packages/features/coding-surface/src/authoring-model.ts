import {
  CODING_TOOLCHAIN,
  codingDraftOperations,
  compileCodingAuthoringDraft,
  CodingValidationError,
  type CodingAuthoringDraft,
  type CodingDraftChangePlan,
  type CodingDraftFunctionCase,
  type CodingDraftTest,
  type CodingExecutionEnvironment,
  type CodingFunctionSignature,
  type CodingParseIssue,
  type CodingScalarType,
} from '@game-guild/coding';
import type { CodingAuthoringSurfaceProps } from './authoring-types';

type IdentityFactory = CodingAuthoringSurfaceProps['createId'];

export function authoringIssues(draft: CodingAuthoringDraft): readonly CodingParseIssue[] {
  try {
    compileCodingAuthoringDraft(draft);
    return [];
  } catch (error) {
    if (error instanceof CodingValidationError) return error.issues;
    throw error;
  }
}

export function blankFunctionCase(signature: CodingFunctionSignature | null, createId: IdentityFactory): CodingDraftFunctionCase {
  return {
    id: createId('case'),
    inputs: signature?.parameters.map((parameter) => ({ type: parameter.type, text: '' })) ?? [],
    expected: signature ? { type: signature.returnType, text: '' } : null,
  };
}

export function blankDraftTest(draft: CodingAuthoringDraft, createId: IdentityFactory): CodingDraftTest {
  const common = { id: createId('test'), name: '', visibility: 'public' as const, weightUnits: '100' };
  return draft.exerciseType === 'function'
    ? { ...common, kind: 'function', cases: [blankFunctionCase(draft.functionSignature, createId)] }
    : { ...common, kind: 'stdio', stdin: '', expected: { stdout: '', stderr: null, exitCode: '' } };
}

export function environmentChange(draft: CodingAuthoringDraft, environment: CodingExecutionEnvironment): CodingDraftChangePlan {
  const plan = codingDraftOperations.planChange(draft, { kind: 'environment', environment });
  const toolchain = codingDraftOperations.planChange(plan.candidate, {
    kind: 'toolchain',
    toolchain: { key: `${environment.language}-emception`, ...CODING_TOOLCHAIN },
  });
  return {
    ...plan,
    candidate: toolchain.candidate,
    impact: { ...plan.impact, filesToReview: [...new Set([...plan.impact.filesToReview, ...toolchain.impact.filesToReview])] },
  };
}

/** A source scaffold only: no harness, compilation, expected answer or academic effect. */
export function starterSource(draft: CodingAuthoringDraft): string {
  if (draft.environment.runtime !== 'wasi') throw new TypeError('Starter generation is unavailable for this environment.');
  const cpp = draft.environment.language === 'cpp';
  if (draft.exerciseType === 'standard-io')
    return cpp ? '#include <iostream>\n\nint main() {\n  return 0;\n}\n' : '#include <stdio.h>\n\nint main(void) {\n  return 0;\n}\n';
  const signature = draft.functionSignature;
  if (!signature || !validSignatureNames(signature)) throw new TypeError('Apply a complete function signature before generating starter code.');
  const names: Record<CodingScalarType, string> = cpp
    ? { integer: 'int', float: 'double', boolean: 'bool', string: 'std::string' }
    : { integer: 'int', float: 'double', boolean: '_Bool', string: 'const char*' };
  const defaults: Record<CodingScalarType, string> = { integer: '0', float: '0.0', boolean: cpp ? 'false' : '0', string: '""' };
  const includes = cpp && [signature.returnType, ...signature.parameters.map((parameter) => parameter.type)].includes('string') ? '#include <string>\n\n' : '';
  const args = signature.parameters.map((parameter) => `${names[parameter.type]} ${parameter.name}`).join(', ') || (cpp ? '' : 'void');
  return `${includes}${names[signature.returnType]} ${signature.name}(${args}) {\n  return ${defaults[signature.returnType]};\n}\n`;
}

function validSignatureNames(signature: CodingFunctionSignature): boolean {
  const valid = (name: string) => /^[A-Za-z_][A-Za-z0-9_]{0,127}$/.test(name);
  return (
    valid(signature.name) &&
    signature.parameters.every((parameter) => valid(parameter.name)) &&
    new Set(signature.parameters.map((parameter) => parameter.name)).size === signature.parameters.length
  );
}
