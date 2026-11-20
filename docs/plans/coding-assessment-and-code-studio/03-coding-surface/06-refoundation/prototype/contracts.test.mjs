import assert from 'node:assert/strict';
import { readdir, readFile } from 'node:fs/promises';
import test from 'node:test';
import { loadContracts, selectCases } from './harness.mjs';

const {
  codingPrototypeSchemas: schemas,
  codingPrototypeFixtures: fixtures,
  codingPrototypeModel: model,
  codingPrototypeSimulation: simulation,
  codingPrototypeCodeView: code,
} = await loadContracts();

test('UXP-01: incomplete drafts and all declared fixtures satisfy the contract', () => {
  for (const family of ['function', 'standard-io']) assert.equal(schemas.isDraft(fixtures.blank(family)), true);
  for (const id of ['maximum', 'echo', 'stress']) {
    const fixture = fixtures.example(id);
    assert.equal(schemas.isDraft(fixture.draft), true, id);
    assert.equal(fixture.outcomes.length, fixture.draft.cases.length);
    assert.equal(new Set(fixture.outcomes.map((entry) => entry.caseId)).size, fixture.outcomes.length);
    assert.notEqual(fixture.correct, fixture.incorrect);
  }
});

test('UXP-01: malformed/recovered drafts fail closed without requiring completeness', () => {
  const base = fixtures.example('maximum').draft;
  const mutations = [
    (value) => {
      value.revision = NaN;
    },
    (value) => {
      value.files.push({ ...value.files[0] });
    },
    (value) => {
      value.signature.parameters[0].type = 'Array';
    },
    (value) => {
      delete value.cases[0].inputs['param-a'];
    },
    (value) => {
      value.cases[0].inputs.unknown = '1';
    },
    (value) => {
      value.cases[0].visibility = 'invisible';
    },
    (value) => {
      value.environment = 'cpp-sdl3';
    },
    (value) => {
      value.graphics = true;
    },
    (value) => {
      value.protected.reference = 5;
    },
  ];
  for (const mutate of mutations) {
    const invalid = structuredClone(base);
    mutate(invalid);
    assert.equal(schemas.isDraft(invalid), false);
    assert.throws(() => schemas.parseDraft(invalid), /Invalid prototype draft/);
  }
  for (const value of [null, [], {}, { family: 'future' }]) assert.equal(schemas.isDraft(value), false);
  const parsed = schemas.parseDraft(base);
  parsed.title = 'Different title';
  assert.notEqual(parsed.title, base.title);
});

test('UXP-01: malformed run/save/event states cannot be accepted', () => {
  assert.equal(schemas.isRun({ status: 'completed', candidate: 'c1', results: [] }), false);
  assert.equal(schemas.isRun({ status: 'error', candidate: 'c1', reason: 'wrong-answer', message: 'bad' }), false);
  assert.equal(schemas.isRun({ status: 'running', token: -1, candidate: 'c1' }), false);
  assert.equal(schemas.isSave({ status: 'saved' }), false);
  assert.equal(schemas.isSave({ status: 'saved', revision: 0 }), true);
  assert.equal(schemas.isEvent({ type: 'select-task', task: 'grading' }), false);
  assert.equal(schemas.isEvent({ type: 'run', target: 'official-grade' }), false);
  assert.equal(schemas.isEvent({ type: 'select-task', task: 'validate' }), true);
});

test('UXP-01: selection rejects zero, missing, unknown and duplicate oracles', () => {
  const cases = [{ id: 'UXP-01-entry-lifecycle', slice: 'UXP-01' }];
  assert.throws(() => selectCases([], 'UXP-01'), /zero cases/);
  assert.throws(() => selectCases(cases, 'UXP-02'), /zero cases/);
  assert.throws(() => selectCases(cases, 'UXP-01,UXP-02'), /Missing required UXP-02/);
  assert.throws(() => selectCases(cases, 'UXP-99'), /known UXP/);
  assert.throws(() => selectCases(cases, 'UXP-01,UXP-01'), /Duplicate UXP/);
  assert.throws(() => selectCases([...cases, ...cases], 'UXP-01'), /Duplicate oracle/);
  assert.equal(selectCases(cases, 'UXP-01').length, 1);
});

test('UXP-01: prototype scripts have no application, runtime or dependency imports', async () => {
  const files = (await readdir(new URL('.', import.meta.url))).filter((name) => name.endsWith('.js') || name.endsWith('.ts'));
  assert.ok(files.length >= 3);
  for (const name of files) {
    const source = await readFile(new URL(name, import.meta.url), 'utf8');
    assert.doesNotMatch(source, /(?:from\s*|import\s*\(|require\s*\()\s*['"](?:@game-guild|emception|\.\.\/)/, name);
    assert.doesNotMatch(source, /@ts-ignore|@ts-nocheck|eslint-disable|\bas\s+any\b/, name);
  }
});

test('UXP-02: scalar kinds retain type-specific errors and signature changes preserve compatible values', () => {
  for (const [kind, raw] of [
    ['integer', '2.5'],
    ['integer', ''],
    ['float', 'Infinity'],
    ['boolean', '1'],
  ])
    assert.ok(model.scalarError(kind, raw));
  for (const [kind, raw] of [
    ['integer', '-2'],
    ['float', '0.25'],
    ['boolean', 'false'],
    ['string', ''],
  ])
    assert.equal(model.scalarError(kind, raw), null);
  const draft = fixtures.example('maximum').draft;
  const signature = structuredClone(draft.signature);
  signature.parameters[0].type = 'boolean';
  model.applySignature(draft, signature);
  assert.equal(draft.cases[0].inputs['param-a'], '');
  assert.equal(draft.cases[0].inputs['param-b'], '7');
  assert.match(model.signature(draft.signature), /bool a, int b/);
  assert.equal(schemas.isDraft(draft), true);
});

test('UXP-03: projection uses current data, removes protected data, and has no shared mutable references', () => {
  const draft = fixtures.example('maximum').draft;
  const delivery = simulation.project(draft);
  assert.equal(delivery.samples.length, 2);
  assert.doesNotMatch(JSON.stringify(delivery), /PRIVATE-CANARY|protected|a > b \? a : b/);
  const preview = simulation.workspace(delivery);
  preview.files[0].content = 'independent edit';
  preview.delivery.samples[0].expected = 'different';
  assert.notEqual(preview.files[0].content, delivery.files[0].content);
  assert.notEqual(preview.delivery.samples[0].expected, draft.cases[0].expected);
  assert.equal(delivery.candidate, 'design-exercise:r0');
});

test('UXP-03: unknown sources/cases cannot invent a successful simulated verdict', () => {
  const fixture = fixtures.example('maximum');
  const delivery = simulation.project(fixture.draft);
  assert.equal(simulation.outcome(delivery, 'unrecorded source', 'normal', delivery.samples).reason, 'unrecorded');
  delivery.samples[0].expected = '100';
  assert.equal(simulation.outcome(delivery, fixture.correct, 'normal', delivery.samples).reason, 'unrecorded');
});

test('UXP-05: in-memory recovery is cloned, scoped and rejects malformed drafts', async () => {
  const { codingPrototypeRecovery } = await loadContracts();
  const recovery = codingPrototypeRecovery.create();
  const scope = { tenant: 'tenant-a', actor: 'teacher-a', exercise: 'design-exercise' };
  const draft = fixtures.example('maximum').draft;
  recovery.put(scope, { draft, signatureEditor: draft.signature });
  for (const changed of [
    { ...scope, tenant: 'tenant-b' },
    { ...scope, actor: 'teacher-b' },
    { ...scope, exercise: 'another-exercise' },
  ])
    assert.equal(recovery.get(changed), null);
  const restored = recovery.get(scope);
  restored.draft.cases[0].expected = 'changed';
  assert.equal(recovery.get(scope).draft.cases[0].expected, '7');
  assert.throws(() => recovery.put(scope, { draft: { family: 'function' }, signatureEditor: null }), /Invalid prototype draft/);
  assert.throws(() => recovery.put({ ...scope, exercise: 'wrong' }, { draft, signatureEditor: null }), /does not match/);
  assert.throws(() => recovery.get({ ...scope, actor: '' }), /full synthetic scope/);
});

test('UXP-06: malformed fixture identities and markup-bearing IDs are rejected', () => {
  assert.throws(() => fixtures.example('unknown'), /Unknown prototype fixture/);
  assert.throws(() => fixtures.blank('unknown'), /Unknown exercise family/);
  for (const modify of [
    (draft) => {
      draft.files[0].id = '" onclick="bad';
    },
    (draft) => {
      draft.cases[0].id = '<script>';
    },
    (draft) => {
      draft.signature.parameters[0].id = 'a b';
    },
  ]) {
    const draft = fixtures.example('maximum').draft;
    modify(draft);
    assert.equal(schemas.isDraft(draft), false);
  }
});

test('UXP-06: a partially deleted slice cannot pass with its remaining oracle', () => {
  assert.throws(() => selectCases([{ id: 'UXP-02-blank-function-typed-cases', slice: 'UXP-02' }], 'UXP-02'), /Missing required oracle/);
});

test('UXP-07: editor commands require stable existing-shape file identities', () => {
  for (const type of ['open-file', 'close-file']) {
    assert.equal(schemas.isEvent({ type, fileId: 'file-1' }), true);
    assert.equal(schemas.isEvent({ type, fileId: '<invalid>' }), false);
    assert.equal(schemas.isEvent({ type }), false);
  }
  assert.equal(schemas.isEvent({ type: 'toggle-explorer' }), true);
  assert.equal(schemas.isEvent({ type: 'request-change', change: { kind: 'delete-file', value: 'file-1' } }), true);
});

test('UXP-07: file paths and session baselines cannot alias or escape the workspace', () => {
  const files = fixtures.example('maximum').draft.files;
  for (const path of ['', '/absolute.cpp', '../parent.cpp', 'include/../../escape.cpp', 'include//bad.cpp', './solution.cpp', 'C:\\bad.cpp', '<script>.cpp'])
    assert.ok(code.pathError(path, files), path);
  assert.match(code.pathError('solution.cpp', files), /already exists/);
  assert.equal(code.pathError('solution.cpp', files, 'file-1'), null);
  assert.equal(code.pathError('include/my-helper.hpp', files), null);
  const editor = code.session(files);
  const baseline = editor.baseline['file-1'];
  files[0].content = 'later edit';
  assert.equal(editor.baseline['file-1'], baseline);
  assert.deepEqual(Array.from(editor.openFiles), ['file-1']);
  assert.equal(editor.explorer, false);
});

test('UXP-07: file-set changes have no recorded verdict for the original exercise', () => {
  const fixture = fixtures.example('maximum');
  const delivery = simulation.project(fixture.draft);
  delivery.files[1].content = 'arbitrary changed support';
  assert.equal(simulation.outcome(delivery, fixture.correct, 'normal', delivery.samples).reason, 'unrecorded');
});
