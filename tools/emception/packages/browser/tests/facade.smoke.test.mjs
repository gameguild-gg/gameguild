import assert from 'node:assert/strict';
import test from 'node:test';

import { ToolchainPreset } from 'emception';
import { createEmception } from '../dist/createEmception.js';

const runSmokeTest = process.env.EMCEPTION_SMOKE === '1';

test('smoke: a real worker produces a TestReport-shaped result', { skip: !runSmokeTest }, async () => {
  const emception = await createEmception({ tty: 'none' });
  try {
    const report = await emception.runTests({
      cases: [{ kind: 'stdio', expectedStdout: 'hello\n' }],
      build: { toolchain: ToolchainPreset.CPP },
    });
    assert.equal(typeof report.passed, 'number');
    assert.equal(typeof report.failed, 'number');
    assert.ok(Array.isArray(report.cases));
  } finally {
    emception.dispose();
  }
});
