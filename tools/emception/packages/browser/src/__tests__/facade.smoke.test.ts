/**
 * Smoke test: real Worker + WASM end-to-end. Only runs when EMCEPTION_SMOKE=1
 * is set. Exercises compileAndRun via a real worker to prove the façade
 * wiring works beyond mock boundaries. The mocked facade.test.ts gates
 * this task; this file is the full-system sanity check.
 *
 * Run: EMCEPTION_SMOKE=1 node --import tsx --test \
 *   packages/browser/src/__tests__/facade.smoke.test.ts
 *
 * If obtaining a real manifest fixture is impractical in CI, this test
 * stays skipped. Documented in comment above.
 */

import assert from 'node:assert/strict';
import test from 'node:test';
import { ToolchainPreset } from 'emception';
import { createEmception } from '../createEmception.js';

const SMOKE = !!process.env.EMCEPTION_SMOKE;

test('smoke: real worker compileAndRun produces a TestReport-shaped result', { skip: !SMOKE }, async () => {
    const em = await createEmception({ tty: 'none' });
    try {
        const plan = {
            cases: [{ kind: 'stdio' as const, expectedStdout: 'hello\\n' }],
            build: { toolchain: ToolchainPreset.CPP as typeof ToolchainPreset.CPP },
        };
        const report = await em.runTests(plan);
        assert.equal(typeof report.passed, 'number');
        assert.equal(typeof report.failed, 'number');
        assert.ok(Array.isArray(report.cases));
    } finally {
        em.dispose();
    }
});
