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

test('preparation contract: positive bounded integer, independent of the exercise limit', async () => {
  for (const timeLimitMs of [0, -1, 0.5, Number.NaN, Number.POSITIVE_INFINITY, 120_001]) {
    let started = false;
    await assert.rejects(executeBudgetedCode(budget, {
      execute: async () => { started = true; return result(); }, dispose: () => undefined,
    }, { timeLimitMs }), /Invalid Code preparation budget/);
    assert.equal(started, false);
  }
});

test('phase budgets: preparation may outlast the student limit without using its allowance', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  let disposed = 0;
  const expected = { ...result('ok'), phase: 'run' };
  const actual = await executeBudgetedCode(budget, {
    execute: async (_output, phase) => {
      phase('compile');
      context.mock.timers.tick(70);
      phase('link');
      context.mock.timers.tick(20);
      phase('run');
      context.mock.timers.tick(49);
      return expected;
    }, dispose: () => { disposed++; },
  }, { timeLimitMs: 100 });
  assert.equal(actual, expected);
  context.mock.timers.tick(200);
  assert.equal(disposed, 0);
});

test('phase budgets: compile and link share one preparation deadline', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  let disposed = 0;
  await assert.rejects(executeBudgetedCode(budget, {
    execute: async (_output, phase) => {
      phase('compile'); context.mock.timers.tick(70);
      phase('link'); context.mock.timers.tick(30);
      return { ...result(), phase: 'link', exitCode: 1 };
    }, dispose: () => { disposed++; },
  }, { timeLimitMs: 100 }), /preparation budget/);
  assert.equal(disposed, 1);
});

test('phase budgets: an unresolved preparation terminates without a run signal', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  let disposed = 0;
  const pending = executeBudgetedCode(budget, {
    execute: (_output, phase) => { phase('compile'); return new Promise(() => undefined); },
    dispose: () => { disposed++; },
  }, { timeLimitMs: 100 });
  const assertion = assert.rejects(pending, /preparation budget/);
  await new Promise(setImmediate);
  context.mock.timers.tick(50);
  const beforePreparationDeadline = disposed;
  context.mock.timers.tick(50);
  await assertion;
  assert.equal(beforePreparationDeadline, 0);
  assert.equal(disposed, 1);
});

test('phase budgets: unresolved student execution gets exactly its own deadline', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  let disposed = 0;
  const pending = executeBudgetedCode(budget, {
    execute: (_output, phase) => {
      phase('compile'); context.mock.timers.tick(70); phase('link'); phase('run');
      return new Promise(() => undefined);
    }, dispose: () => { disposed++; },
  }, { timeLimitMs: 100 });
  const assertion = assert.rejects(pending, /runtime budget/);
  await new Promise(setImmediate);
  context.mock.timers.tick(49);
  const beforeStudentDeadline = disposed;
  context.mock.timers.tick(1);
  await assertion;
  assert.equal(beforeStudentDeadline, 0);
  assert.equal(disposed, 1);
});

for (const phases of [[], ['run'], ['link'], ['compile', 'run'], ['compile', 'compile'], ['compile', 'link', 'compile'],
  ['compile', 'link', 'run', 'link']]) {
  test(`phase budgets: rejects missing/regressing signals ${JSON.stringify(phases)}`, async () => {
    let disposed = 0;
    await assert.rejects(executeBudgetedCode(budget, {
      execute: async (_output, phase) => { phases.forEach(phase); return { ...result(), phase: 'run' }; },
      dispose: () => { disposed++; },
    }, { timeLimitMs: 100 }), /execution phase/);
    assert.equal(disposed, 1);
  });
}

test('phase budgets: mismatched terminal metadata cannot create a completed result', async () => {
  await assert.rejects(executeBudgetedCode(budget, {
    execute: async (_output, phase) => { phase('compile'); phase('link'); return { ...result(), phase: 'run' }; },
    dispose: () => undefined,
  }, { timeLimitMs: 100 }), /execution phase/);
});

test('phase budgets: compile failure remains a student failure, not a preparation timeout', async () => {
  const expected = { ...result(), phase: 'compile', exitCode: 1 };
  assert.equal(await executeBudgetedCode(budget, {
    execute: async (_output, phase) => { phase('compile'); return expected; }, dispose: () => undefined,
  }, { timeLimitMs: 100 }), expected);
});

test('phase budgets: output bytes remain aggregate across compile, link and run', async () => {
  await assert.rejects(executeBudgetedCode(budget, {
    execute: async (output, phase) => {
      phase('compile'); output(new Uint8Array(2)); phase('link'); phase('run'); output(new Uint8Array(3));
      return { ...result(), phase: 'run' };
    }, dispose: () => undefined,
  }, { timeLimitMs: 100 }), /output budget/);
});
