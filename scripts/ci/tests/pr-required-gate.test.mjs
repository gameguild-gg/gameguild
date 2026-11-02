import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import test from 'node:test';

const workflow = readFileSync(new URL('../../../.github/workflows/pr-verify.yml', import.meta.url), 'utf8');
const requiredGate = workflow.split('\n  required-gate:')[1];
assert.ok(requiredGate, 'PR Required Gate remains present');
const run = requiredGate.match(/ {8}run: \|\r?\n([\s\S]*)/);
assert.ok(run, 'test the actual required-gate Bash, not a duplicate evaluator');
const script = run[1].replace(/^ {10}/gm, '');

function evaluate(overrides = {}) {
  return spawnSync('bash', ['-e', '-c', script], {
    encoding: 'utf8',
    env: {
      ...process.env,
      CLASSIFY: 'success', SECRET_SCAN: 'success', REPOSITORY_POLICY: 'success', WEB: 'success',
      API_SELECTED: 'true', API: 'success', ECONOMY_SELECTED: 'false',
      ECONOMY: 'skipped', ECONOMY_API: 'skipped', ECONOMY_CLIENT: 'skipped',
      ECONOMY_COVERAGE: 'skipped', ECONOMY_REGRESSION: 'skipped',
      ...overrides,
    },
  });
}

test('workflow passes the classifier API selection into the actual gate', () => {
  assert.match(requiredGate, /API_SELECTED: \$\{\{ needs\.classify\.outputs\.api \}\}/);
  assert.match(workflow, /node --test scripts\/ci\/tests\/pr-required-gate\.test\.mjs/);
});

test('workflow passes the classifier Web selection into the actual gate', () => {
  assert.match(requiredGate, /WEB_SELECTED: \$\{\{ needs\.classify\.outputs\.web \}\}/);
});

for (const selected of ['true', 'false']) {
  test(`API success is accepted when selection=${selected}`, () => {
    assert.equal(evaluate({ API_SELECTED: selected }).status, 0);
  });
  for (const result of ['failure', 'cancelled', '']) {
    test(`API ${result || 'missing result'} blocks when selection=${selected}`, () => {
      const outcome = evaluate({ API_SELECTED: selected, API: result });
      assert.equal(outcome.status, 1);
      assert.match(outcome.stderr, /Required PR gates failed: api=/);
    });
  }
}

test('a legitimately unselected API can be skipped in a Web-only PR', () => {
  assert.equal(evaluate({ API_SELECTED: 'false', API: 'skipped' }).status, 0);
});

for (const selected of ['true', '', 'unknown']) {
  test(`API skip blocks when selection is ${selected || 'missing'}`, () => {
    const outcome = evaluate({ API_SELECTED: selected, API: 'skipped' });
    assert.equal(outcome.status, 1);
    assert.match(outcome.stderr, /api=skipped/);
  });
}

test('an unselected API does not excuse a failed mandatory Web gate', () => {
  const outcome = evaluate({ API_SELECTED: 'false', API: 'skipped', WEB: 'failure' });
  assert.equal(outcome.status, 1);
  assert.match(outcome.stderr, /web=failure/);
});

test('a legitimately unselected Web can be skipped in an Emception-only PR', () => {
  assert.equal(evaluate({ WEB_SELECTED: 'false', WEB: 'skipped' }).status, 0);
});

for (const selected of ['true', '', 'unknown']) {
  test(`Web skip blocks when selection is ${selected || 'missing'}`, () => {
    const outcome = evaluate({ WEB_SELECTED: selected, WEB: 'skipped' });
    assert.equal(outcome.status, 1);
    assert.match(outcome.stderr, /web=skipped/);
  });
}

test('selected Economy gates still require success', () => {
  const outcome = evaluate({ API_SELECTED: 'false', API: 'skipped', ECONOMY_SELECTED: 'true' });
  assert.equal(outcome.status, 1);
  assert.match(outcome.stderr, /economy=skipped/);
});
