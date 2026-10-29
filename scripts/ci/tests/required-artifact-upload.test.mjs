import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync, rmdirSync, unlinkSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import test from 'node:test';
import { selectUploadReceipt } from '../../../.github/actions/upload-required-artifact/select-upload.mjs';

const name = 'full-application-openapi-source';
const digest = 'ab'.repeat(32);
const skipped = { outcome: 'skipped' };
const failure = { outcome: 'failure' };
const success = (id = '123') => ({
  outcome: 'success', id, digest,
  url: `https://github.com/owner/repo/actions/runs/42/artifacts/${id}`,
});

test('the first successful upload keeps the original artifact identity', () => {
  assert.deepEqual(selectUploadReceipt(name, [success(), skipped, skipped]), {
    id: '123', url: success().url, digest, name, attempt: 1,
  });
});

test('an interrupted first upload can be followed by a retained second attempt', () => {
  const result = selectUploadReceipt(name, [failure, success('456'), skipped]);
  assert.equal(result.id, '456');
  assert.equal(result.name, `${name}-retry-2`);
  assert.equal(result.attempt, 2);
});

test('the final retained attempt preserves a distinct name', () => {
  const result = selectUploadReceipt(name, [failure, failure, success('789')]);
  assert.equal(result.name, `${name}-retry-3`);
  assert.equal(result.id, '789');
});

test('all failed or skipped attempts cannot satisfy required retention', () => {
  assert.throws(() => selectUploadReceipt(name, [failure, failure, failure]));
  assert.throws(() => selectUploadReceipt(name, [skipped, skipped, skipped]));
});

test('an apparent success without complete artifact metadata is rejected', () => {
  for (const changes of [{ id: '' }, { digest: '' }, { digest: 'invalid' }, { url: '' }, { url: success('999').url }]) {
    assert.throws(() => selectUploadReceipt(name, [{ ...success(), ...changes }, skipped, skipped]));
  }
});

test('ambiguous receipts and unsafe output values cannot publish success', () => {
  assert.throws(() => selectUploadReceipt(name, [success(), success('456'), skipped]));
  assert.throws(() => selectUploadReceipt(`${name}\nforged=value`, [success(), skipped, skipped]));
  assert.throws(() => selectUploadReceipt(name, [{ ...success(), url: `${success().url}\nforged=value` }, skipped, skipped]));
  assert.throws(() => selectUploadReceipt(name, [{ ...success(), url: success().url.replace('https:', 'http:') }, skipped, skipped]));
});

test('the CLI propagates complete retention metadata to the runner output', () => {
  const directory = mkdtempSync(join(process.env.ARTIFACT_RECEIPT_TEST_TEMP ?? tmpdir(), 'required-artifact-receipt-'));
  const output = join(directory, 'output');
  try {
    const run = spawnSync(process.execPath, ['--max-old-space-size=128', fileURLToPath(new URL('../../../.github/actions/upload-required-artifact/select-upload.mjs', import.meta.url))], {
      encoding: 'utf8',
      timeout: 10000,
      env: { ...process.env, ARTIFACT_BASE_NAME: name, GITHUB_OUTPUT: output,
        UPLOAD_1_OUTCOME: 'failure', UPLOAD_2_OUTCOME: 'success', UPLOAD_2_ID: '456', UPLOAD_2_URL: success('456').url,
        UPLOAD_2_DIGEST: digest, UPLOAD_3_OUTCOME: 'skipped' },
    });
    assert.equal(run.status, 0, run.stderr);
    const values = Object.fromEntries(readFileSync(output, 'utf8').trim().split('\n').map((line) => line.split(/=(.*)/s).slice(0, 2)));
    assert.equal(values['artifact-id'], '456');
    assert.equal(values['artifact-name'], `${name}-retry-2`);
    assert.equal(values['artifact-digest'], digest);
  } finally {
    try { unlinkSync(output); } catch (error) { if (error.code !== 'ENOENT') throw error; }
    rmdirSync(directory);
  }
});

test('the CLI exits unsuccessfully when every upload fails', () => {
  const run = spawnSync(process.execPath, ['--max-old-space-size=128', fileURLToPath(new URL('../../../.github/actions/upload-required-artifact/select-upload.mjs', import.meta.url))], {
    encoding: 'utf8',
    timeout: 10000,
    env: { ...process.env, ARTIFACT_BASE_NAME: name,
      UPLOAD_1_OUTCOME: 'failure', UPLOAD_2_OUTCOME: 'failure', UPLOAD_3_OUTCOME: 'failure' },
  });
  assert.equal(run.status, 1);
  assert.match(run.stderr, /Required evidence retention failed/);
});
