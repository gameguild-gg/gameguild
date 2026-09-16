import assert from 'node:assert/strict';
import { chmod, mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const repositoryRoot = fileURLToPath(new URL('../../../', import.meta.url));
const script = join(repositoryRoot, 'scripts/deploy/rollout-devtron.sh');
const releaseSha = 'a'.repeat(40);
const treeSha = 'b'.repeat(40);
const previousRelease = 'c'.repeat(40);
const previousTree = 'd'.repeat(40);
const linuxShellOnly = process.platform === 'win32' ? { skip: 'requires the Linux release shell and jq' } : {};

async function run(command, args, options) {
  const child = spawn(command, args, { ...options, stdio: ['ignore', 'pipe', 'pipe'] });
  let stdout = '';
  let stderr = '';
  child.stdout.setEncoding('utf8');
  child.stderr.setEncoding('utf8');
  child.stdout.on('data', (chunk) => { stdout += chunk; });
  child.stderr.on('data', (chunk) => { stderr += chunk; });
  const [exitCode] = await once(child, 'exit');
  return { exitCode, stdout, stderr };
}

async function createFixture(t, failCurrentWeb) {
  const root = await mkdtemp(join(tmpdir(), 'gameguild-rollout-'));
  t.after(() => rm(root, { recursive: true, force: true }));
  const fakeBin = join(root, 'bin');
  const releaseDir = join(root, 'release');
  const log = join(root, 'rollout.log');
  await mkdir(fakeBin);
  await mkdir(releaseDir);

  const currentDigest = `sha256:${'e'.repeat(64)}`;
  const previousDigest = `sha256:${'f'.repeat(64)}`;
  const services = ['api', 'web'].map((service) => ({
    service,
    image: `registry.example/gameguild-${service}`,
    imageDigest: currentDigest,
    sourceSha: '1'.repeat(40),
    releaseSha,
    treeSha,
  }));
  const previousServices = ['api', 'web'].map((service) => ({
    service,
    image: `registry.example/gameguild-${service}`,
    imageDigest: previousDigest,
    sourceSha: '2'.repeat(40),
    releaseSha: previousRelease,
    treeSha: previousTree,
  }));
  await writeFile(join(releaseDir, 'promoted-services.json'), JSON.stringify(services));
  await writeFile(join(releaseDir, 'previous-release-manifest.json'), JSON.stringify({ services: previousServices }));

  const fakeNode = join(fakeBin, 'node');
  await writeFile(
    fakeNode,
    `#!/usr/bin/env bash\nset -e\nscript="$1"; shift\nvalue() { local wanted="$1"; shift; while (($#)); do if [[ "$1" == "$wanted" ]]; then printf '%s' "$2"; return; fi; shift 2; done; }\nif [[ "$script" == *devtron-payload.mjs ]]; then\n  image=$(value --image "$@"); tag=$(value --tag "$@"); digest=$(value --digest "$@"); output=$(value --output "$@")\n  printf '{"dockerImage":"%s:%s","digest":"%s"}\\n' "$image" "$tag" "$digest" > "$output"\nelif [[ "$script" == *verify-release.mjs ]]; then\n  service=$(value --service "$@"); release=$(value --release-sha "$@")\n  printf 'verify:%s:%s\\n' "$service" "$release" >> "$ROLLOUT_LOG"\n  if [[ "$FAIL_CURRENT_WEB" == true && "$service" == web && "$release" == "$CURRENT_RELEASE" ]]; then exit 1; fi\nelif [[ "$script" == *production-smoke.mjs ]]; then\n  printf 'smoke\\n' >> "$ROLLOUT_LOG"\nelse\n  echo "unexpected node script $script" >&2; exit 1\nfi\n`,
  );
  await chmod(fakeNode, 0o755);

  const fakeCurl = join(fakeBin, 'curl');
  await writeFile(
    fakeCurl,
    `#!/usr/bin/env bash\nset -e\npayload=''; url=''\nwhile (($#)); do\n  if [[ "$1" == --data-binary ]]; then payload="\${2#@}"; shift 2; continue; fi\n  url="$1"; shift\ndone\nimage=$(jq -r '.dockerImage' "$payload")\nprintf 'trigger:%s:%s\\n' "$image" "$url" >> "$ROLLOUT_LOG"\nprintf '{"ok":true}\\n'\n`,
  );
  await chmod(fakeCurl, 0o755);

  return {
    log,
    options: {
      cwd: repositoryRoot,
      env: {
        ...process.env,
        PATH: `${fakeBin}:${process.env.PATH}`,
        RELEASE_DIR: releaseDir,
        ROLLOUT_LOG: log,
        FAIL_CURRENT_WEB: String(failCurrentWeb),
        CURRENT_RELEASE: releaseSha,
        RELEASE_SHA: releaseSha,
        TREE_SHA: treeSha,
        RELEASED_AT: '2026-09-01T12:05:00Z',
        MIGRATION_REQUIRED: 'false',
        DEVTRON_BASE_URL: 'https://devtron.example',
        DEVTRON_API_TOKEN: 'token',
        DEVTRON_EXTERNAL_CI_ID_API: '11',
        DEVTRON_EXTERNAL_CI_ID_WEB: '22',
        GAMEGUILD_API_URL: 'https://api.example',
        GAMEGUILD_WEB_URL: 'https://web.example',
        GITHUB_REPOSITORY: 'gameguild-gg/gameguild',
      },
    },
  };
}

test('rolls API out before Web and runs smoke only after both are verified', linuxShellOnly, async (t) => {
  const fixture = await createFixture(t, false);
  const result = await run('bash', [script], fixture.options);
  assert.equal(result.exitCode, 0, result.stderr || result.stdout);
  const log = await readFile(fixture.log, 'utf8');
  assert.ok(log.indexOf(`trigger:registry.example/gameguild-api:release-${releaseSha}`) < log.indexOf(`trigger:registry.example/gameguild-web:release-${releaseSha}`));
  assert.ok(log.indexOf('smoke') > log.indexOf(`verify:web:${releaseSha}`));
});

test('restores Web and API in reverse order when the Web release identity fails', linuxShellOnly, async (t) => {
  const fixture = await createFixture(t, true);
  const result = await run('bash', [script], fixture.options);
  assert.notEqual(result.exitCode, 0);
  const log = await readFile(fixture.log, 'utf8');
  const rollbackWeb = log.indexOf(`trigger:registry.example/gameguild-web:release-${previousRelease}`);
  const rollbackApi = log.indexOf(`trigger:registry.example/gameguild-api:release-${previousRelease}`);
  assert.ok(rollbackWeb > log.indexOf(`verify:web:${releaseSha}`), log);
  assert.ok(rollbackApi > rollbackWeb, log);
  assert.doesNotMatch(log, /^smoke$/mu);
});
