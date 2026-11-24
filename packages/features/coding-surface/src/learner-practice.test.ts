import { MAX_CODING_DEFINITION_BYTES, MAX_CODING_TESTS, type CodingExerciseType, type CodingLearnerDelivery } from '@game-guild/coding';
import { codingContractFixture } from '@game-guild/coding/testing';
import { describe, expect, it } from 'vitest';
import { codingLearnerPractice as practice } from './learner-practice';
import type { CodingLearnerPracticeState, CodingPersonalCase } from './learner-practice-types';

function delivery(family: CodingExerciseType): CodingLearnerDelivery {
  const value = structuredClone(family === 'function' ? codingContractFixture.functionLearnerDelivery : codingContractFixture.learnerDelivery);
  value.publicTests =
    family === 'function'
      ? [
          {
            id: 'sample-sum',
            name: 'Add integers',
            kind: 'function',
            weightUnits: 100,
            function: {
              name: 'sum',
              parameters: [
                { name: 'left', type: 'integer' },
                { name: 'right', type: 'integer' },
              ],
              returnType: 'integer',
            },
            cases: [
              {
                id: 'positive',
                inputs: [
                  { type: 'integer', value: 2 },
                  { type: 'integer', value: 3 },
                ],
                expected: { type: 'integer', value: 5 },
              },
              {
                id: 'negative',
                inputs: [
                  { type: 'integer', value: -2 },
                  { type: 'integer', value: 1 },
                ],
                expected: { type: 'integer', value: -1 },
              },
            ],
          },
        ]
      : [
          {
            id: 'sample-greeting',
            name: 'Greeting',
            kind: 'stdio',
            weightUnits: 100,
            stdin: 'Ada\n',
            expected: { stdout: 'Hello Ada\n', stderr: '', exitCode: 0 },
          },
        ];
  return value;
}

function add(state: CodingLearnerPracticeState, id = 'mine-1'): CodingLearnerPracticeState {
  return practice.reduce(state, { kind: 'add', id });
}

function personal(state: CodingLearnerPracticeState): CodingPersonalCase {
  const value = state.personalCases[0];
  if (!value) throw new Error('The test requires a personal case.');
  return value;
}

describe('UX05-A1 isolated learner practice', () => {
  it.each(['function', 'standard-io'] as const)('clones only the authorized %s case data without files or academic fields', (family) => {
    const source = delivery(family);
    const before = structuredClone(source);
    const state = practice.create('tenant:member:revision:attempt', source);
    expect(state.contextKey).toBe('tenant:member:revision:attempt');
    expect(state.exerciseType).toBe(family);
    expect(state.publicCases).toHaveLength(family === 'function' ? 2 : 1);
    expect(state.personalCases).toEqual([]);
    expect(state.selection).toEqual({ source: 'public', id: state.publicCases[0].id });
    expect(source).toEqual(before);
    for (const key of ['files', 'toolchain', 'validationSolutions', 'score', 'submissionId']) expect(state).not.toHaveProperty(key);
    source.publicTests[0].name = 'Changed elsewhere';
    expect(state.publicCases[0].name).not.toContain('Changed elsewhere');
  });

  it('flattens each public function value case without losing its typed values', () => {
    const state = practice.create('a', delivery('function'));
    expect(state.publicCases).toEqual([
      expect.objectContaining({
        source: 'public',
        kind: 'function',
        inputs: [
          { type: 'integer', text: '2' },
          { type: 'integer', text: '3' },
        ],
        expected: { type: 'integer', text: '5' },
      }),
      expect.objectContaining({
        source: 'public',
        kind: 'function',
        inputs: [
          { type: 'integer', text: '-2' },
          { type: 'integer', text: '1' },
        ],
        expected: { type: 'integer', text: '-1' },
      }),
    ]);
    expect(new Set(state.publicCases.map((value) => value.id)).size).toBe(2);
    expect(state.signature).toEqual({
      name: 'sum',
      parameters: [
        { name: 'left', type: 'integer' },
        { name: 'right', type: 'integer' },
      ],
      returnType: 'integer',
    });
  });

  it('retains stdout, intentional empty stderr and explicit zero exit from a public case', () => {
    const state = practice.create('a', delivery('standard-io'));
    expect(state.publicCases[0]).toMatchObject({ kind: 'stdio', stdin: 'Ada\n', expected: { stdout: 'Hello Ada\n', stderr: '', exitCode: '0' } });
  });

  it('freezes public values at runtime and does not alias caller-owned personal scalar buffers', () => {
    const state = add(practice.create('a', delivery('function')));
    const sample = state.publicCases[0];
    if (sample.kind !== 'function') throw new Error('Expected function case.');
    expect(Object.isFrozen(state.publicCases)).toBe(true);
    expect(Object.isFrozen(sample.inputs)).toBe(true);
    expect(Reflect.set(sample.inputs[0], 'text', 'not public')).toBe(false);
    expect(Object.isFrozen(state.signature?.parameters)).toBe(true);
    const value = personal(state);
    if (value.kind !== 'function') throw new Error('Expected function input.');
    const left = { type: 'integer' as const, text: '2' };
    const changed = practice.reduce(state, { kind: 'replace', value: { ...value, inputs: [left, value.inputs[1]] } });
    left.text = 'outside change';
    expect(personal(changed)).toMatchObject({ inputs: [{ text: '2' }, { text: '' }] });
  });

  it.each(['', ' ', 'x'.repeat(257), 'attempt\n'])('rejects an invalid practice scope %j', (contextKey) => {
    expect(() => practice.create(contextKey, delivery('standard-io'))).toThrow(TypeError);
  });

  it.each(['function', 'standard-io'] as const)('adds %s buffers without inventing an expected answer', (family) => {
    const state = add(practice.create('a', delivery(family)));
    expect(state.view).toBe('personal');
    expect(state.selection).toEqual({ source: 'personal', id: 'mine-1' });
    const value = personal(state);
    expect(value.source).toBe('personal');
    if (value.kind === 'function') {
      expect(value.inputs).toEqual([
        { type: 'integer', text: '' },
        { type: 'integer', text: '' },
      ]);
      expect(value.expected).toBeNull();
    } else expect(value.expected).toEqual({ stdout: null, stderr: null, exitCode: null });
    expect(value).not.toHaveProperty('weightUnits');
  });

  it.each(['function', 'standard-io'] as const)('copies a %s public case into independent personal data', (family) => {
    const state = practice.create('a', delivery(family));
    const before = structuredClone(state);
    const copy = practice.reduce(state, { kind: 'duplicate', id: 'mine-1', selection: { source: 'public', id: state.publicCases[0].id } });
    const value = personal(copy);
    expect(value).toMatchObject({ id: 'mine-1', source: 'personal', kind: state.publicCases[0].kind });
    const changed =
      value.kind === 'stdio' ? { ...value, stdin: 'My own input\n' } : { ...value, inputs: [{ type: 'integer' as const, text: '-' }, value.inputs[1]] };
    const edited = practice.reduce(copy, { kind: 'replace', value: changed });
    expect(edited.publicCases).toEqual(before.publicCases);
    expect(state).toEqual(before);
    expect(personal(edited)).not.toEqual(value);
  });

  it('preserves incomplete scalar text and optional expected values until an execution boundary validates them', () => {
    const state = add(practice.create('a', delivery('function')));
    const value = personal(state);
    if (value.kind !== 'function') throw new Error('Expected function input.');
    const edited = practice.reduce(state, {
      kind: 'replace',
      value: {
        ...value,
        inputs: [
          { type: 'integer', text: '-' },
          { type: 'integer', text: '3e' },
        ],
        expected: { type: 'integer', text: 'not finished' },
      },
    });
    expect(personal(edited)).toMatchObject({ inputs: [{ text: '-' }, { text: '3e' }], expected: { text: 'not finished' } });
    expect(edited).not.toHaveProperty('passed');
  });

  it('does not collapse an empty output expectation into an absent one', () => {
    const state = add(practice.create('a', delivery('standard-io')));
    const value = personal(state);
    if (value.kind !== 'stdio') throw new Error('Expected standard input.');
    const withEmpty = practice.reduce(state, { kind: 'replace', value: { ...value, expected: { stdout: '', stderr: null, exitCode: null } } });
    expect(personal(withEmpty)).toMatchObject({ expected: { stdout: '' } });
    const noOracle = practice.reduce(withEmpty, { kind: 'replace', value: { ...value, expected: { stdout: null, stderr: null, exitCode: null } } });
    expect(personal(noOracle)).toMatchObject({ expected: { stdout: null } });
  });

  it('duplicates a personal buffer without aliasing its previous input', () => {
    const state = add(practice.create('a', delivery('function')));
    const copy = practice.reduce(state, { kind: 'duplicate', id: 'mine-2', selection: { source: 'personal', id: 'mine-1' } });
    expect(copy.personalCases).toHaveLength(2);
    expect(copy.personalCases[1].name).toContain('copy');
    expect(copy.personalCases[1]).not.toBe(state.personalCases[0]);
    expect(state.personalCases).toHaveLength(1);
  });

  it('requires deletion confirmation, preserves cancellation, then selects a surviving case', () => {
    const state = add(add(practice.create('a', delivery('standard-io'))), 'mine-2');
    const pending = practice.reduce(state, { kind: 'request-delete', id: 'mine-2' });
    expect(pending.personalCases).toEqual(state.personalCases);
    expect(pending.pendingDeletion).toBe('mine-2');
    expect(practice.reduce(pending, { kind: 'cancel-delete' })).toEqual(state);
    const removed = practice.reduce(pending, { kind: 'confirm-delete' });
    expect(removed.personalCases.map((value) => value.id)).toEqual(['mine-1']);
    expect(removed.selection).toEqual({ source: 'personal', id: 'mine-1' });
    const empty = practice.reduce(practice.reduce(removed, { kind: 'request-delete', id: 'mine-1' }), { kind: 'confirm-delete' });
    expect(empty.personalCases).toEqual([]);
    expect(empty.selection).toBeNull();
  });

  it('refuses to edit, select or delete a case not owned by the current context', () => {
    const state = add(practice.create('a', delivery('standard-io')));
    const value = personal(state);
    expect(() => practice.reduce(state, { kind: 'replace', value: { ...value, id: 'other-context' } })).toThrow(TypeError);
    expect(() => practice.reduce(state, { kind: 'select', selection: { source: 'personal', id: 'other-context' } })).toThrow(TypeError);
    expect(() => practice.reduce(state, { kind: 'request-delete', id: state.publicCases[0].id })).toThrow(TypeError);
    expect(() => practice.reduce(state, { kind: 'confirm-delete' })).toThrow(TypeError);
    expect(state.personalCases).toHaveLength(1);
  });

  it.each(['', ' ', '../case', '_case', 'case\n', 'x'.repeat(129)])('rejects an invalid personal identity %j', (id) => {
    expect(() => add(practice.create('a', delivery('standard-io')), id)).toThrow(TypeError);
  });

  it('rejects duplicate identities and limits the bounded personal list', () => {
    let state = add(practice.create('a', delivery('standard-io')));
    expect(() => add(state)).toThrow(TypeError);
    expect(() => add(state, state.publicCases[0].id)).toThrow(TypeError);
    for (let index = 1; index < MAX_CODING_TESTS; index++) state = add(state, `mine-${index + 1}`);
    expect(state.personalCases).toHaveLength(MAX_CODING_TESTS);
    expect(() => add(state, 'one-too-many')).toThrow(TypeError);
  });

  it('rejects mixed-family replacement and mismatched scalar shapes without changing the previous state', () => {
    const state = add(practice.create('a', delivery('function')));
    const value = personal(state);
    if (value.kind !== 'function') throw new Error('Expected function input.');
    const before = structuredClone(state);
    expect(() =>
      practice.reduce(state, {
        kind: 'replace',
        value: { id: value.id, source: 'personal', name: 'Wrong family', kind: 'stdio', stdin: '', expected: { stdout: null, stderr: null, exitCode: null } },
      }),
    ).toThrow(TypeError);
    expect(() => practice.reduce(state, { kind: 'replace', value: { ...value, inputs: [] } })).toThrow(TypeError);
    expect(() => practice.reduce(state, { kind: 'replace', value: { ...value, expected: { type: 'boolean', text: 'true' } } })).toThrow(TypeError);
    expect(state).toEqual(before);
  });

  it('rejects oversized personal state rather than silently truncating it', () => {
    const state = add(practice.create('a', delivery('standard-io')));
    const value = personal(state);
    if (value.kind !== 'stdio') throw new Error('Expected standard input.');
    expect(() => practice.reduce(state, { kind: 'replace', value: { ...value, stdin: 'x'.repeat(MAX_CODING_DEFINITION_BYTES + 1) } })).toThrow(TypeError);
    expect(personal(state)).toMatchObject({ stdin: '' });
  });

  it('rejects academic fields smuggled into an otherwise valid personal replacement', () => {
    const state = add(practice.create('a', delivery('standard-io')));
    const value = Object.assign({ ...personal(state) }, { weightUnits: 100, submissionId: 'official-identity' });
    expect(() => practice.reduce(state, { kind: 'replace', value })).toThrow(TypeError);
    expect(personal(state)).not.toHaveProperty('submissionId');
  });

  it('starts a separate personal context and handles an empty public-case list without inventing a signature', () => {
    const source = delivery('function');
    source.publicTests = [];
    const empty = practice.create('new-attempt', source);
    expect(empty.signature).toBeNull();
    expect(empty.selection).toBeNull();
    expect(() => add(empty)).toThrow(TypeError);
    const other = practice.create('other-attempt', delivery('standard-io'));
    expect(other.personalCases).toEqual([]);
    expect(other.contextKey).not.toBe(empty.contextKey);
  });

  it('rejects author-only additions at the learner delivery boundary', () => {
    const source = Object.assign(delivery('standard-io'), { validationSolutions: [{ content: 'PRIVATE-REFERENCE-CANARY' }] });
    expect(() => practice.create('a', source)).toThrow();
  });

  it('refuses public samples from another family or a different function signature', () => {
    const mixed = delivery('standard-io');
    mixed.publicTests.push(delivery('function').publicTests[0]);
    expect(() => practice.create('a', mixed)).toThrow(TypeError);
    const inconsistent = delivery('function');
    const next = structuredClone(inconsistent.publicTests[0]);
    if (next.kind !== 'function') throw new Error('Expected function fixture.');
    next.id = 'another-signature';
    next.function.name = 'differentProblem';
    inconsistent.publicTests.push(next);
    expect(() => practice.create('a', inconsistent)).toThrow(TypeError);
  });
});
