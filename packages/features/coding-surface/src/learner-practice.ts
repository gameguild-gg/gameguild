import { MAX_CODING_DEFINITION_BYTES, MAX_CODING_TESTS, parseCodingLearnerDelivery, type CodingScalarValue } from '@game-guild/coding';
import type {
  CodingLearnerPracticeOperations,
  CodingLearnerPracticeState,
  CodingPersonalCase,
  CodingPracticeCase,
  CodingPracticeSelection,
  CodingPublicPracticeCase,
} from './learner-practice-types';

export const codingLearnerPractice: CodingLearnerPracticeOperations = {
  create(contextKey, delivery) {
    if (
      !contextKey.trim() ||
      contextKey.length > 256 ||
      Array.from(contextKey).some((character) => character.charCodeAt(0) < 32 || character.charCodeAt(0) === 127)
    ) {
      throw new TypeError('A bounded learner practice context is required.');
    }
    const parsed = parseCodingLearnerDelivery(delivery);
    const functionTest = parsed.publicTests.find((test) => test.kind === 'function');
    const signature =
      functionTest?.kind === 'function'
        ? Object.freeze({
            name: functionTest.function.name,
            parameters: Object.freeze(functionTest.function.parameters.map((parameter) => Object.freeze({ ...parameter }))),
            returnType: functionTest.function.returnType,
          })
        : null;
    if (
      parsed.publicTests.some((test) => {
        if (parsed.exerciseType === 'standard-io') return test.kind !== 'stdio';
        return (
          test.kind !== 'function' ||
          !signature ||
          test.function.name !== signature.name ||
          test.function.returnType !== signature.returnType ||
          test.function.parameters.length !== signature.parameters.length ||
          test.function.parameters.some(
            (parameter, index) => parameter.name !== signature.parameters[index].name || parameter.type !== signature.parameters[index].type,
          )
        );
      })
    )
      throw new TypeError('Public samples must match the exercise family and one coherent function signature.');
    const publicCases = Object.freeze(
      parsed.publicTests.flatMap((test, index): CodingPublicPracticeCase[] => {
        const name = test.name ?? `Sample ${index + 1}`;
        if (test.kind === 'stdio')
          return [
            Object.freeze({
              id: `public:${test.id}`,
              source: 'public',
              name,
              kind: 'stdio',
              stdin: test.stdin,
              expected: Object.freeze({
                stdout: test.expected.stdout,
                stderr: test.expected.stderr ?? null,
                exitCode: test.expected.exitCode === undefined ? null : String(test.expected.exitCode),
              }),
            }),
          ];
        return test.cases.map((value, caseIndex) =>
          Object.freeze({
            id: `public:${test.id.length}:${test.id}:${value.id}`,
            source: 'public',
            name: test.cases.length === 1 ? name : `${name} / ${caseIndex + 1}`,
            kind: 'function',
            inputs: Object.freeze(value.inputs.map(scalarBuffer)),
            expected: scalarBuffer(value.expected),
          }),
        );
      }),
    );
    return {
      contextKey,
      exerciseType: parsed.exerciseType,
      signature,
      publicCases,
      personalCases: [],
      view: 'public',
      selection: publicCases[0] ? { source: 'public', id: publicCases[0].id } : null,
      pendingDeletion: null,
    };
  },
  reduce(state, intent) {
    switch (intent.kind) {
      case 'view': {
        const cases = intent.view === 'public' ? state.publicCases : state.personalCases;
        const selection = state.selection?.source === intent.view ? state.selection : cases[0] ? { source: intent.view, id: cases[0].id } : null;
        return { ...state, view: intent.view, selection, pendingDeletion: null };
      }
      case 'select':
        findCase(state, intent.selection);
        return { ...state, view: intent.selection.source, selection: { ...intent.selection }, pendingDeletion: null };
      case 'add': {
        const base = { id: intent.id, source: 'personal' as const, name: `Case ${state.personalCases.length + 1}` };
        if (state.exerciseType === 'standard-io')
          return append(state, { ...base, kind: 'stdio', stdin: '', expected: { stdout: null, stderr: null, exitCode: null } });
        if (!state.signature) throw new TypeError('A public function signature is required to prepare personal inputs.');
        return append(state, {
          ...base,
          kind: 'function',
          inputs: state.signature.parameters.map((parameter) => ({ type: parameter.type, text: '' })),
          expected: null,
        });
      }
      case 'duplicate': {
        const original = findCase(state, intent.selection);
        return append(state, { ...cloneCase(original), id: intent.id, source: 'personal', name: `${original.name} copy` });
      }
      case 'replace': {
        findCase(state, { source: 'personal', id: intent.value.id });
        validatePersonalCase(state, intent.value);
        const personalCases = state.personalCases.map((value) =>
          value.id === intent.value.id ? { ...cloneCase(intent.value), source: 'personal' as const } : value,
        );
        validateSize(personalCases);
        return { ...state, personalCases };
      }
      case 'request-delete':
        findCase(state, { source: 'personal', id: intent.id });
        return { ...state, pendingDeletion: intent.id };
      case 'cancel-delete':
        return { ...state, pendingDeletion: null };
      case 'confirm-delete': {
        if (!state.pendingDeletion) throw new TypeError('Select a personal case and confirm its deletion first.');
        findCase(state, { source: 'personal', id: state.pendingDeletion });
        const personalCases = state.personalCases.filter((value) => value.id !== state.pendingDeletion);
        const selection =
          state.selection?.source === 'personal' && state.selection.id === state.pendingDeletion
            ? personalCases[0]
              ? { source: 'personal' as const, id: personalCases[0].id }
              : null
            : state.selection;
        return { ...state, personalCases, selection, pendingDeletion: null };
      }
    }
  },
};

function scalarBuffer(value: CodingScalarValue) {
  return Object.freeze({ type: value.type, text: String(value.value) });
}

function cloneCase(value: CodingPracticeCase): CodingPracticeCase {
  const base = { id: value.id, name: value.name };
  return value.kind === 'stdio'
    ? { ...base, kind: 'stdio', stdin: value.stdin, expected: { ...value.expected } }
    : { ...base, kind: 'function', inputs: value.inputs.map((input) => ({ ...input })), expected: value.expected === null ? null : { ...value.expected } };
}

function findCase(state: CodingLearnerPracticeState, selection: CodingPracticeSelection): CodingPracticeCase {
  const values = selection.source === 'public' ? state.publicCases : state.personalCases;
  const value = values.find((candidate) => candidate.id === selection.id);
  if (!value) throw new TypeError('The case does not belong to this practice context.');
  return value;
}

function append(state: CodingLearnerPracticeState, value: CodingPersonalCase): CodingLearnerPracticeState {
  if (!/^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/.test(value.id) || /\s/.test(value.id)) throw new TypeError('A valid personal case identity is required.');
  if (state.publicCases.some((candidate) => candidate.id === value.id) || state.personalCases.some((candidate) => candidate.id === value.id)) {
    throw new TypeError('This case identity is already in use.');
  }
  if (state.personalCases.length >= MAX_CODING_TESTS) throw new TypeError(`A practice context supports at most ${MAX_CODING_TESTS} personal cases.`);
  validatePersonalCase(state, value);
  const personalCases = [...state.personalCases, { ...cloneCase(value), source: 'personal' as const }];
  validateSize(personalCases);
  return { ...state, personalCases, view: 'personal', selection: { source: 'personal', id: value.id }, pendingDeletion: null };
}

function validatePersonalCase(state: CodingLearnerPracticeState, value: CodingPersonalCase): void {
  if (value.source !== 'personal' || typeof value.name !== 'string') throw new TypeError('A personal case name and source are required.');
  if (value.kind === 'stdio' && state.exerciseType === 'standard-io') {
    onlyKeys(value, ['id', 'name', 'source', 'kind', 'stdin', 'expected']);
    onlyKeys(value.expected, ['stdout', 'stderr', 'exitCode']);
    if (typeof value.stdin !== 'string' || Object.values(value.expected).some((text) => text !== null && typeof text !== 'string')) {
      throw new TypeError('Personal input and expectations must remain text buffers.');
    }
    return;
  }
  if (value.kind !== 'function' || state.exerciseType !== 'function' || !state.signature)
    throw new TypeError('The personal case must match the exercise family.');
  onlyKeys(value, ['id', 'name', 'source', 'kind', 'inputs', 'expected']);
  if (value.inputs.length !== state.signature.parameters.length) throw new TypeError('Personal inputs must match the public function signature.');
  value.inputs.forEach((input, index) => {
    onlyKeys(input, ['type', 'text']);
    if (input.type !== state.signature?.parameters[index].type || typeof input.text !== 'string')
      throw new TypeError('The input type must match its public parameter.');
  });
  if (value.expected !== null) {
    onlyKeys(value.expected, ['type', 'text']);
    if (value.expected.type !== state.signature.returnType || typeof value.expected.text !== 'string')
      throw new TypeError('The expected type must match the public return type.');
  }
}

function onlyKeys(value: object, allowed: readonly string[]): void {
  const keys = Object.keys(value);
  if (keys.length !== allowed.length || keys.some((key) => !allowed.includes(key)))
    throw new TypeError('Personal cases require their exact buffer fields and cannot carry authoring or academic data.');
}

function validateSize(values: readonly CodingPersonalCase[]): void {
  if (new TextEncoder().encode(JSON.stringify(values)).byteLength > MAX_CODING_DEFINITION_BYTES) {
    throw new TypeError('The personal case buffers exceed their byte budget.');
  }
}
