import assert from 'node:assert/strict';
import { chmod, mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const repositoryRoot = fileURLToPath(new URL('../../../', import.meta.url));
const script = join(repositoryRoot, 'scripts/deploy/promote-candidates.sh');
const releaseSha = 'a'.repeat(40);
const treeSha = 'b'.repeat(40);
const candidateDigest = `sha256:${'c'.repeat(64)}`;
const previousDigest = `sha256:${'d'.repeat(64)}`;
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

test('promotes the verified candidate digest and preserves unchanged services', linuxShellOnly, async (t) => {
  const root = await mkdtemp(join(tmpdir(), 'gameguild-promote-'));
  t.after(() => rm(root, { recursive: true, force: true }));
  const fakeBin = join(root, 'bin');
  const evidenceDir = join(root, 'evidence');
  const outputDir = join(root, 'release');
  const dockerLog = join(root, 'docker.log');
  await mkdir(fakeBin);
  await mkdir(evidenceDir);

  const image = 'registry.example/gameguild/gameguild-web';
  await writeFile(
    join(evidenceDir, 'candidate-web.json'),
    JSON.stringify({
      service: 'web', image, tag: `candidate-${treeSha}-web`, imageDigest: candidateDigest,
      treeSha, sourceSha: 'e'.repeat(40), builtAt: '2026-09-01T12:00:00Z',
    }),
  );
  const previousManifest = join(root, 'previous.json');
  await writeFile(
    previousManifest,
    JSON.stringify({
      services: [
        {
          service: 'api', image: 'registry.example/gameguild/gameguild-api', imageDigest: previousDigest,
          sourceSha: 'f'.repeat(40), releaseSha: '1'.repeat(40), treeSha: '2'.repeat(40),
        },
        {
          service: 'web', image, imageDigest: previousDigest,
          sourceSha: '3'.repeat(40), releaseSha: '4'.repeat(40), treeSha: '5'.repeat(40),
        },
      ],
    }),
  );

  const fakeDocker = join(fakeBin, 'docker');
  await writeFile(
    fakeDocker,
    `#!/usr/bin/env bash\nset -e\nprintf '%s\\n' "$*" >> "$DOCKER_LOG"\nif [[ "$*" == *'imagetools inspect'* ]]; then printf 'Name: fake\\nDigest: %s\\n' "$CANDIDATE_DIGEST"; fi\n`,
  );
  await chmod(fakeDocker, 0o755);

  const result = await run('bash', [script], {
    cwd: repositoryRoot,
    env: {
      ...process.env,
      PATH: `${fakeBin}:${process.env.PATH}`,
      DOCKER_LOG: dockerLog,
      CANDIDATE_DIGEST: candidateDigest,
      RELEASE_SHA: releaseSha,
      TREE_SHA: treeSha,
      SERVICES_JSON: '["web"]',
      REGISTRY_HOST: 'registry.example',
      REGISTRY_NAMESPACE: 'gameguild',
      VERIFICATION_RUN_ID: '1234',
      RELEASED_AT: '2026-09-01T12:05:00Z',
      MIGRATION_REQUIRED: 'false',
      EVIDENCE_DIR: evidenceDir,
      OUTPUT_DIR: outputDir,
      PREVIOUS_MANIFEST: previousManifest,
    },
  });

  assert.equal(result.exitCode, 0, result.stderr || result.stdout);
  const manifest = JSON.parse(await readFile(join(outputDir, 'release-manifest.json'), 'utf8'));
  assert.equal(manifest.services.find((entry) => entry.service === 'api').imageDigest, previousDigest);
  assert.deepEqual(manifest.services.find((entry) => entry.service === 'web'), {
    service: 'web', image, imageDigest: candidateDigest, sourceSha: 'e'.repeat(40), releaseSha, treeSha,
  });
  const log = await readFile(dockerLog, 'utf8');
  assert.match(log, new RegExp(`imagetools create --tag ${image}:release-${releaseSha} ${image}@${candidateDigest}`));
});
