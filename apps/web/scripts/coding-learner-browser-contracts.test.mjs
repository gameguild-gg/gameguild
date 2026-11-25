// @ts-check
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { LEARNER_HOST_CASE_IDS, assertLearnerHostEvidence, resolveLearnerHostEnvironment } from './coding-learner-browser-contracts.mts';
import { createLearnerHostDefinition } from './coding-learner-browser-proof.mts';

/** @type {import('./coding-learner-browser-contracts.mts').LearnerHostEvidence} */
const complete = {
  kind: 'actual-host-coding-solving',
  nextHost: true,
  realApi: true,
  authentication: 'real-browser-credentials-route',
  personas: ['non-admin-course-owner', 'enrolled-member', 'independent-enrolled-member'],
  officialGrading: false,
  cases: LEARNER_HOST_CASE_IDS.map((id) => ({ id, passed: true })),
  academicMutationsDuringPractice: 0,
  authoringMutationsDuringPractice: 0,
  privateLeaks: 0,
  activeRuntimeWorkers: 0,
  errors: [],
  cleanupErrors: [],
};

test('A3 fixture: current wire cases put the polymorphic discriminator first', () => {
  for (const family of ['function', 'standard-io']) {
    if (family !== 'function' && family !== 'standard-io') throw new Error('Unknown fixture family.');
    const value = createLearnerHostDefinition(family, 'synthetic-private-marker');
    assert.equal(value.ExerciseType, family);
    assert.equal(value.Execution?.Limits.TimeLimitMs, 5000);
    assert.equal(value.Execution?.EntryPoint.Symbol, family === 'function' ? 'twice' : undefined);
    assert.ok(value.Problem?.Statement);
    assert.equal(value.Tests.Public.length, 1);
    assert.equal(value.Tests.Private.length, 1);
    for (const entry of [...value.Tests.Public, ...value.Tests.Private]) {
      assert.equal(Object.keys(entry)[0], 'kind');
      assert.equal(entry.kind, family === 'function' ? 'functional' : 'standard');
    }
    assert.equal(value.Data.Files['/home/user/main.cpp']?.Visibility, 'Public');
    assert.equal(value.Data.Files['/home/user/teacher-notes.txt']?.Visibility, 'Private');
    assert.equal(value.ValidationSolutions?.[0]?.Role, 'reference');
  }
});

test('A3 contract: consent and loopback-only environments precede fixture writes', () => {
  assert.deepEqual(resolveLearnerHostEnvironment({ E2E_RUN: '1' }), {
    webBaseUrl: 'http://localhost:3000',
    apiBaseUrl: 'http://localhost:8080',
  });
  assert.throws(() => resolveLearnerHostEnvironment({}), /E2E_RUN/);
  for (const url of [
    'https://example.com',
    'http://localhost.example.com',
    'http://user:pass@localhost',
    'http://localhost/path',
    'http://localhost?secret=value',
    'http://localhost/#fragment',
    'ftp://localhost',
  ]) {
    for (const key of ['PLAYWRIGHT_WEB_BASE_URL', 'API_BASE_URL']) {
      assert.throws(() => resolveLearnerHostEnvironment({ E2E_RUN: '1', [key]: url }), /loopback|origin/);
    }
  }
  for (const origin of ['http://127.0.0.1:3000', 'http://[::1]:3000', 'https://coding.localhost:3000']) {
    assert.equal(resolveLearnerHostEnvironment({ E2E_RUN: '1', PLAYWRIGHT_WEB_BASE_URL: origin }).webBaseUrl, origin);
  }
});

test('A3 contract: every required named host case passes exactly once', () => {
  assert.doesNotThrow(() => assertLearnerHostEvidence(complete));
  for (const id of LEARNER_HOST_CASE_IDS) {
    assert.throws(() => assertLearnerHostEvidence({ ...complete, cases: complete.cases.filter((entry) => entry.id !== id) }), /case/);
    assert.throws(() => assertLearnerHostEvidence({ ...complete, cases: [...complete.cases, { id, passed: true }] }), /case/);
    assert.throws(() => assertLearnerHostEvidence({ ...complete, cases: complete.cases.map((entry) => ({ ...entry, passed: entry.id !== id })) }), /case/);
  }
});

test('A3 contract: local effects, private leaks, surviving workers and errors fail closed', () => {
  for (const field of ['academicMutationsDuringPractice', 'authoringMutationsDuringPractice', 'privateLeaks', 'activeRuntimeWorkers']) {
    for (const value of [1, -1, NaN, Infinity]) assert.throws(() => assertLearnerHostEvidence({ ...complete, [field]: value }), /effect|leak|worker|mutation/);
  }
  assert.throws(() => assertLearnerHostEvidence({ ...complete, errors: ['Actual browser failure'] }), /error/);
  assert.throws(() => assertLearnerHostEvidence({ ...complete, cleanupErrors: ['Owned cleanup failed'] }), /cleanup/);
});

test('A3 contract: a mock/admin proof cannot become real learner or official acceptance', () => {
  for (const defect of [
    { nextHost: false },
    { realApi: false },
    { authentication: 'mock-session' },
    { officialGrading: true },
    { kind: 'isolated-component' },
    { personas: ['SystemAdmin', 'enrolled-member', 'independent-enrolled-member'] },
  ])
    assert.throws(() => assertLearnerHostEvidence(Object.assign({}, complete, defect)), /scope|persona/);
});
