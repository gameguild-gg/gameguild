// @ts-check
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { AUTHORING_HOST_CASE_IDS, assertAuthoringHostEvidence, resolveAuthoringHostEnvironment } from './coding-authoring-browser-contracts.mts';

/** @type {import('./coding-authoring-browser-contracts.mts').AuthoringHostEvidence} */
const complete = {
  kind: 'actual-host-coding-authoring',
  nextHost: true,
  realApi: true,
  persona: 'non-admin-course-owner',
  officialGrading: false,
  cases: AUTHORING_HOST_CASE_IDS.map((id) => ({ id, passed: true })),
  compilerRequests: 0,
  academicBrowserRequests: 0,
  cleanupErrors: [],
  errors: [],
};

test('C3 contract: localhost defaults require explicit E2E consent', () => {
  assert.deepEqual(resolveAuthoringHostEnvironment({ E2E_RUN: '1' }), {
    webBaseUrl: 'http://localhost:3000',
    apiBaseUrl: 'http://localhost:8080',
  });
  assert.throws(() => resolveAuthoringHostEnvironment({}), /E2E_RUN/);
});

test('C3 contract: loopback URLs roundtrip without permitting remote writes', () => {
  assert.deepEqual(
    resolveAuthoringHostEnvironment({ E2E_RUN: '1', PLAYWRIGHT_WEB_BASE_URL: 'http://gameguild.localhost:3011/', API_BASE_URL: 'http://127.0.0.1:8081/' }),
    { webBaseUrl: 'http://gameguild.localhost:3011', apiBaseUrl: 'http://127.0.0.1:8081' },
  );
  for (const value of [
    'https://example.com',
    'http://localhost.example.com',
    'http://user:pass@localhost:3000',
    'http://localhost:3000/path',
    'http://localhost:3000?token=x',
  ]) {
    assert.throws(() => resolveAuthoringHostEnvironment({ E2E_RUN: '1', PLAYWRIGHT_WEB_BASE_URL: value }), /loopback|origin/);
  }
});

test('C3 contract: acceptance requires every named real-host case exactly once', () => {
  assert.doesNotThrow(() => assertAuthoringHostEvidence(complete));
  for (const id of AUTHORING_HOST_CASE_IDS) {
    assert.throws(() => assertAuthoringHostEvidence({ ...complete, cases: complete.cases.filter((entry) => entry.id !== id) }), /case/);
    assert.throws(() => assertAuthoringHostEvidence({ ...complete, cases: [...complete.cases, { id, passed: true }] }), /case/);
    assert.throws(() => assertAuthoringHostEvidence({ ...complete, cases: complete.cases.map((entry) => ({ ...entry, passed: entry.id !== id })) }), /case/);
  }
});

test('C3 contract: failures, compiler boot, academic calls and cleanup failures cannot pass', () => {
  assert.throws(() => assertAuthoringHostEvidence({ ...complete, compilerRequests: 1 }), /compiler/);
  assert.throws(() => assertAuthoringHostEvidence({ ...complete, academicBrowserRequests: 1 }), /academic/);
  assert.throws(() => assertAuthoringHostEvidence({ ...complete, errors: ['Observed failure'] }), /error/);
  assert.throws(() => assertAuthoringHostEvidence({ ...complete, cleanupErrors: ['Owned fixture cleanup failed'] }), /cleanup/);
});
