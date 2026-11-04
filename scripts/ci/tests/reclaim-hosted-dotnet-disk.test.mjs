import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

import { resolveBashExecutable } from '../../../apps/web/scripts/run-testing-lab-browser-e2e.mjs';

const helper = fileURLToPath(new URL('../reclaim-hosted-dotnet-disk.sh', import.meta.url)).replaceAll('\\', '/');
const bashHelper = /^[A-Za-z]:\//.test(helper) ? '/' + helper[0].toLowerCase() + helper.slice(2) : helper;
const harness = `
set -euo pipefail
# Only the command boundary is replaced: no sudo, deletion or disk change.
sudo() {
  printf 'REMOVE'; printf ' <%s>' "$@"; printf '\\n'
  return "\${CLEANUP_TEST_SUDO_STATUS:-0}"
}
df() { printf 'DISK_USAGE\\n'; }
realpath() {
  [[ "$1" == -m && "$2" == -- && "$#" == 3 ]] || return 97
  if [[ "$3" == /opt/ghc && "\${CLEANUP_TEST_REDIRECT:-}" == true ]]; then
    printf '/workspace/protected\\n'
  elif [[ "\${CLEANUP_TEST_REALPATH_FAILURE:-}" == true ]]; then
    return 1
  else
    printf '%s\\n' "$3"
  fi
}
source "$1"
`;

function runCleanup(overrides = {}) {
  const result = spawnSync(resolveBashExecutable(), ['-c', harness, 'test-reclaim', bashHelper], {
    env: {
      ...process.env,
      GITHUB_ACTIONS: 'true',
      RUNNER_ENVIRONMENT: 'github-hosted',
      RUNNER_OS: 'Linux',
      CLEANUP_TEST_SUDO_STATUS: '0',
      CLEANUP_TEST_REDIRECT: '',
      CLEANUP_TEST_REALPATH_FAILURE: '',
      ...overrides,
    },
    encoding: 'utf8',
    timeout: 5000,
    windowsHide: true,
  });
  assert.equal(result.error, undefined);
  return result;
}

for (const [name, environment] of [
  ['local Linux', { GITHUB_ACTIONS: '' }],
  ['self-hosted Linux', { RUNNER_ENVIRONMENT: 'self-hosted' }],
  ['unspecified runner', { RUNNER_ENVIRONMENT: '' }],
  ['hosted Windows', { RUNNER_OS: 'Windows' }],
  ['hosted macOS', { RUNNER_OS: 'macOS' }],
]) {
  test(`${name} never attempts privileged cleanup`, () => {
    const result = runCleanup(environment);
    assert.equal(result.status, 0, result.stderr);
    assert.doesNotMatch(result.stdout, /REMOVE|DISK_USAGE/);
    assert.match(result.stdout, /Skipping/);
  });
}

test('hosted Linux removes only unused fixed toolchains and records disk usage before and after', () => {
  const result = runCleanup();
  assert.equal(result.status, 0, result.stderr);
  assert.deepEqual(
    result.stdout.split('\n').filter((line) => /^(REMOVE|DISK_USAGE)/.test(line)),
    [
      'DISK_USAGE',
      'REMOVE <rm> <-rf> <--one-file-system> <--> </usr/local/lib/android>',
      'REMOVE <rm> <-rf> <--one-file-system> <--> </usr/share/swift>',
      'REMOVE <rm> <-rf> <--one-file-system> <--> </usr/local/.ghcup>',
      'REMOVE <rm> <-rf> <--one-file-system> <--> </opt/ghc>',
      'DISK_USAGE',
    ],
  );
});

test('a redirected final target prevents every deletion, including earlier targets', () => {
  const result = runCleanup({ CLEANUP_TEST_REDIRECT: 'true' });
  assert.equal(result.status, 1);
  assert.match(result.stderr, /Refusing unexpected toolchain path/);
  assert.doesNotMatch(result.stdout, /REMOVE/);
});

test('a failed path resolution prevents deletion', () => {
  const result = runCleanup({ CLEANUP_TEST_REALPATH_FAILURE: 'true' });
  assert.equal(result.status, 1);
  assert.doesNotMatch(result.stdout, /REMOVE/);
});

test('a cleanup failure stays failed without reporting successful post-cleanup evidence', () => {
  const result = runCleanup({ CLEANUP_TEST_SUDO_STATUS: '42' });
  assert.equal(result.status, 42);
  assert.equal(result.stdout.match(/^REMOVE/gm)?.length, 1);
  assert.equal(result.stdout.match(/^DISK_USAGE/gm)?.length, 1);
  assert.doesNotMatch(result.stdout, /disk after/);
});

test('coverage prepares hosted disk before dependency installation and retains its complete gate', async () => {
  const workflow = await readFile(new URL('../../../.github/workflows/pr-verify.yml', import.meta.url), 'utf8');
  const coverage = workflow.split('\n  economy-coverage:')[1].split('\n  economy-regression:')[0];
  assert.match(coverage, /if: runner\.environment == 'github-hosted' && runner\.os == 'Linux'/);
  assert.ok(coverage.indexOf('bash scripts/ci/reclaim-hosted-dotnet-disk.sh') < coverage.indexOf('uses: ./.github/actions/setup-node-workspace'));
  assert.match(coverage, /ECONOMY_GATE_PROFILE: coverage\s+run: bash scripts\/ci\/verify-economy\.sh/);
  assert.match(workflow, /node --test scripts\/ci\/tests\/dotnet-cache-policy\.test\.mjs scripts\/ci\/tests\/reclaim-hosted-dotnet-disk\.test\.mjs/);
});

test('regression shards prepare hosted disk before dependencies and retain all required tests', async () => {
  const workflow = await readFile(new URL('../../../.github/workflows/pr-verify.yml', import.meta.url), 'utf8');
  const regression = workflow.split('\n  economy-regression:')[1].split('\n  required-gate:')[0];
  assert.match(regression, /if: runner\.environment == 'github-hosted' && runner\.os == 'Linux'/);
  const prepare = regression.indexOf('run: bash scripts/ci/reclaim-hosted-dotnet-disk.sh');
  assert.ok(prepare >= 0 && prepare < regression.indexOf('uses: ./.github/actions/setup-node-workspace'));
  assert.ok(prepare < regression.indexOf('uses: ./.github/actions/setup-dotnet'));
  assert.match(regression, /fail-fast: false/);
  assert.match(regression, /shard: \[api-integration-1, api-integration-2, api-integration-3, api-integration-4, api-unit, remaining-1, remaining-2\]/);
  assert.match(regression, /ECONOMY_GATE_PROFILE: regression/);
  assert.match(regression, /run: bash scripts\/ci\/verify-economy\.sh/);
  assert.doesNotMatch(regression, /continue-on-error: true|SKIP_DOCKER_TESTS/);
});
