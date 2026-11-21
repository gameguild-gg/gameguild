import assert from 'node:assert/strict';
import test from 'node:test';
import { executeBudgetedCode, validateCodeExecutionBudget } from './execution-budget.ts';

const budget = { timeLimitMs: 50, outputLimitBytes: 4 };
const result = (stdout = '', stderr = '') => ({ stdout, stderr, timedOut: false, exitCode: 0, durationMs: 1 });

test('contract: bounded positive safe integers only', () => {
  assert.doesNotThrow(() => validateCodeExecutionBudget(budget));
  for (const field of ['timeLimitMs', 'outputLimitBytes']) {
    for (const value of [0, -1, 0.5, Number.NaN, Number.POSITIVE_INFINITY, Number.MAX_SAFE_INTEGER + 1]) {
      assert.throws(() => validateCodeExecutionBudget({ ...budget, [field]: value }), /Invalid Code execution budget/);
    }
  }
  assert.throws(() => validateCodeExecutionBudget({ ...budget, timeLimitMs: 30_001 }));
  assert.throws(() => validateCodeExecutionBudget({ ...budget, outputLimitBytes: 2_000_001 }));
});

test('deadline: an unresolved runtime is terminated and the request rejects', async () => {
  let disposed = 0;
  let started = false;
  const pending = executeBudgetedCode(budget, {
    execute: () => { started = true; return new Promise(() => undefined); },
    dispose: () => { disposed++; },
  });
  const observation = await Promise.race([
    pending.then(() => 'resolved', error => error.message),
    new Promise(resolve => { setTimeout(() => resolve('test deadline'), 250); }),
  ]);
  assert.equal(started, true);
  assert.equal(observation, 'Code execution exceeded its runtime budget.');
  assert.equal(disposed, 1);
});

test('output: combined streamed bytes abort before a pending runtime returns', async () => {
  let disposed = 0;
  const pending = executeBudgetedCode(budget, {
    execute: observe => {
      observe(new TextEncoder().encode('abc'));
      observe(new TextEncoder().encode('de'));
      return new Promise(() => undefined);
    },
    dispose: () => { disposed++; },
  });
  const observation = await Promise.race([
    pending.then(() => 'resolved', error => error.message),
    new Promise(resolve => { setTimeout(() => resolve('test deadline'), 250); }),
  ]);
  assert.equal(observation, 'Code execution exceeded its output budget.');
  assert.equal(disposed, 1);
});

test('output: UTF-8 bytes, not JavaScript string length', async () => {
  let disposed = 0;
  await assert.rejects(executeBudgetedCode(budget, {
    execute: async () => result(String.fromCodePoint(0x1f642) + 'a'),
    dispose: () => { disposed++; },
  }), /output budget/);
  assert.equal(disposed, 1);
});

test('output: exact boundary succeeds without double-counting capture', async () => {
  let disposed = 0;
  const expected = result('ab', 'cd');
  const actual = await executeBudgetedCode(budget, {
    execute: async observe => {
      observe(new TextEncoder().encode('ab'));
      observe(new TextEncoder().encode('cd'));
      return expected;
    },
    dispose: () => { disposed++; },
  });
  assert.equal(actual, expected);
  assert.equal(disposed, 0);
});

test('output: settled capture still enforces budget when no stream was emitted', async () => {
  let disposed = 0;
  await assert.rejects(executeBudgetedCode(budget, {
    execute: async () => result('abcd', 'e'),
    dispose: () => { disposed++; },
  }), /output budget/);
  assert.equal(disposed, 1);
});

test('deadline: timed-out runtime result is not a completed student test', async () => {
  let disposed = 0;
  await assert.rejects(executeBudgetedCode(budget, {
    execute: async () => ({ ...result(), timedOut: true }),
    dispose: () => { disposed++; },
  }), /runtime budget/);
  assert.equal(disposed, 1);
});

test('settlement: fast success clears its timer and keeps the runtime alive', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  let disposed = 0;
  await executeBudgetedCode({ ...budget, timeLimitMs: 5 }, {
    execute: async () => result('ok'),
    dispose: () => { disposed++; },
  });
  context.mock.timers.tick(20);
  assert.equal(disposed, 0);
});

test('failure: cancellation error cannot conceal a budget failure or echo private output', async () => {
  const canary = 'PRIVATE_REFERENCE_CANARY';
  await assert.rejects(executeBudgetedCode(budget, {
    execute: async () => result(canary),
    dispose: () => { throw new Error(canary); },
  }), error => error.message === 'Code execution exceeded its output budget.' && !error.message.includes(canary));
});
