import type {
  CodingAuthoringDraft,
  CodingDraftChange,
  CodingDraftChangePlan,
  CodingDraftEnvironment,
  CodingDraftFunctionCase,
  CodingDraftOperations,
  CodingDraftTest,
  CodingFunctionSignature,
} from './authoring-draft-contracts';
import { CODING_DRAFT_TYPE } from './authoring-draft-contracts';
import { parseCodingAuthoringDraft } from './authoring-draft-schema';
import type { CodingAuthoringDefinition, CodingScalarValue } from './contracts';
import { parseCodingAuthoringDefinition } from './schemas';

export const codingDraftOperations: CodingDraftOperations = {
  fromDefinition,
  planChange,
  resolveChange,
  duplicateTest,
};

function fromDefinition(value: CodingAuthoringDefinition, environment: CodingDraftEnvironment): CodingAuthoringDraft {
  const definition = parseCodingAuthoringDefinition(value);
  if (definition.tests.some(test => (test.kind === 'function') !== (definition.exerciseType === 'function'))) {
    throw new TypeError('A mixed test plan requires an explicit authoring decision before importing.');
  }
  let functionSignature: CodingFunctionSignature | null = null;
  const first = definition.tests[0];
  if (first?.kind === 'function') {
    functionSignature = first.function;
    if (definition.tests.some(test => test.kind !== 'function' || !equal(test.function, functionSignature))
      || (definition.entryPoint.symbol !== undefined && definition.entryPoint.symbol !== functionSignature.name)) {
      throw new TypeError('A draft requires one coherent function signature; the definition was not changed.');
    }
  } else if (definition.entryPoint.symbol !== undefined) {
    throw new TypeError('A standard I/O draft cannot silently discard an entry-point symbol.');
  }
  const tests: CodingDraftTest[] = definition.tests.map(test => {
    const base = { id: test.id, name: test.name ?? '', visibility: test.visibility, weightUnits: String(test.weightUnits) };
    return test.kind === 'stdio'
      ? { ...base, kind: 'stdio', stdin: test.stdin, expected: {
        stdout: test.expected.stdout, stderr: test.expected.stderr ?? null,
        exitCode: test.expected.exitCode === undefined ? '' : String(test.expected.exitCode),
      } }
      : { ...base, kind: 'function', cases: test.cases.map(testCase => ({
        id: testCase.id, inputs: testCase.inputs.map(scalarBuffer), expected: scalarBuffer(testCase.expected),
      })) };
  });
  return parseCodingAuthoringDraft({
    type: CODING_DRAFT_TYPE,
    schemaVersion: 1,
    exerciseType: definition.exerciseType,
    problem: { statement: '', examples: [], constraints: [] },
    environment,
    toolchain: definition.toolchain,
    files: definition.files,
    entryPoint: { path: definition.entryPoint.path },
    functionSignature,
    tests,
    validationSolutions: [],
    limits: {
      timeLimitMs: String(definition.limits.timeLimitMs),
      memoryLimitBytes: String(definition.limits.memoryLimitBytes),
      outputLimitBytes: String(definition.limits.outputLimitBytes),
      maxFileCount: String(definition.limits.maxFileCount),
    },
    allowLearnerCreatedFiles: definition.allowLearnerCreatedFiles,
  });
}

function planChange(value: CodingAuthoringDraft, change: CodingDraftChange): CodingDraftChangePlan {
  const draft = parseCodingAuthoringDraft(value);
  const baseline = JSON.stringify(draft);
  const impact: CodingDraftChangePlan['impact'] = { removedTestIds: [], changedCaseIds: [], filesToReview: [] };
  switch (change.kind) {
    case 'family':
      if (change.exerciseType !== draft.exerciseType) {
        impact.removedTestIds = draft.tests.map(test => test.id);
        impact.filesToReview = draft.files.map(file => file.id);
        draft.exerciseType = change.exerciseType;
        draft.tests = [];
        draft.functionSignature = null;
      }
      break;
    case 'signature': {
      if (draft.exerciseType !== 'function') throw new TypeError('Only a function draft has a function signature.');
      const previous = draft.functionSignature;
      if (!equal(previous, change.signature)) {
        draft.tests = draft.tests.map(test => test.kind === 'function' ? {
          ...test,
          cases: test.cases.map(testCase => {
            impact.changedCaseIds.push(testCase.id);
            return remapCase(testCase, previous, change.signature);
          }),
        } : test);
        impact.filesToReview = draft.files.filter(file => file.encoding === 'text').map(file => file.id);
        draft.functionSignature = structuredClone(change.signature);
      }
      break;
    }
    case 'environment':
      if (!equal(draft.environment, change.environment)) {
        draft.environment = structuredClone(change.environment);
        impact.filesToReview = draft.files.map(file => file.id);
      }
      break;
    case 'toolchain':
      if (!equal(draft.toolchain, change.toolchain)) {
        draft.toolchain = structuredClone(change.toolchain);
        impact.filesToReview = draft.files.map(file => file.id);
      }
      break;
    default:
      throw new TypeError('Unknown coding draft change.');
  }
  return { baseline, candidate: parseCodingAuthoringDraft(draft), impact };
}

function resolveChange(value: CodingAuthoringDraft, plan: CodingDraftChangePlan, decision: 'confirm' | 'cancel'): CodingAuthoringDraft {
  const current = parseCodingAuthoringDraft(value);
  if (decision === 'cancel') return current;
  if (decision !== 'confirm') throw new TypeError('A draft change requires an explicit confirm or cancel decision.');
  if (JSON.stringify(current) !== plan.baseline) throw new TypeError('The draft change is stale; review its impact again.');
  return parseCodingAuthoringDraft(plan.candidate);
}

function duplicateTest(value: CodingAuthoringDraft, testId: string, newId: string, newCaseIds: readonly string[]): CodingAuthoringDraft {
  const draft = parseCodingAuthoringDraft(value);
  const original = draft.tests.find(test => test.id === testId);
  if (!original) throw new TypeError('The draft test to duplicate does not exist.');
  if (draft.tests.some(test => test.id === newId)) throw new TypeError('A duplicated test requires a fresh ID.');
  const expectedCount = original.kind === 'function' ? original.cases.length : 0;
  const existingCaseIds = new Set(draft.tests.flatMap(test => test.kind === 'function' ? test.cases.map(testCase => testCase.id) : []));
  if (newCaseIds.length !== expectedCount || new Set(newCaseIds).size !== expectedCount
    || newCaseIds.some(id => existingCaseIds.has(id))) {
    throw new TypeError('A duplicated test requires one fresh ID for each function case.');
  }
  const copy = structuredClone(original);
  copy.id = newId;
  copy.name = original.name ? `${original.name} (copy)` : '';
  if (copy.kind === 'function') {
    copy.cases = copy.cases.map((testCase, index) => {
      const id = newCaseIds[index];
      if (id === undefined) throw new TypeError('Missing duplicate case identity.');
      return { ...testCase, id };
    });
  }
  draft.tests.push(copy);
  return parseCodingAuthoringDraft(draft);
}

function remapCase(testCase: CodingDraftFunctionCase, previous: CodingFunctionSignature | null, next: CodingFunctionSignature): CodingDraftFunctionCase {
  const inputs = next.parameters.map(parameter => {
    const oldMatches = previous?.parameters.filter(old => old.name === parameter.name) ?? [];
    const newMatches = next.parameters.filter(item => item.name === parameter.name);
    const index = previous?.parameters.findIndex(old => old.name === parameter.name && old.type === parameter.type) ?? -1;
    const input = testCase.inputs[index];
    return parameter.name && oldMatches.length === 1 && newMatches.length === 1 && input?.type === parameter.type
      ? { ...input } : { type: parameter.type, text: '' };
  });
  return {
    id: testCase.id,
    inputs,
    expected: testCase.expected?.type === next.returnType ? { ...testCase.expected } : null,
  };
}

function scalarBuffer(value: CodingScalarValue) {
  return { type: value.type, text: String(value.value) };
}

function equal(left: unknown, right: unknown): boolean {
  if (left === right) return true;
  if (Array.isArray(left) && Array.isArray(right)) {
    const leftItems: readonly unknown[] = left;
    const rightItems: readonly unknown[] = right;
    return leftItems.length === rightItems.length && leftItems.every((item, index) => equal(item, rightItems[index]));
  }
  if (isRecord(left) && isRecord(right)) {
    const keys = Object.keys(left);
    return keys.length === Object.keys(right).length
      && keys.every(key => Object.hasOwn(right, key) && equal(left[key], right[key]));
  }
  return false;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}
