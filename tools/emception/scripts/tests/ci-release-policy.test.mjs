import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { test } from 'node:test';

test('Emception CI is Linux-only, lockfile-free, receipt-aware, and Changesets-based', async () => {
  const repoRoot = path.resolve(import.meta.dirname, '..', '..', '..', '..');
  const workflow = await readFile(path.join(repoRoot, '.github', 'workflows', 'emception.yml'), 'utf8');
  const runners = [...workflow.matchAll(/^\s*runs-on:\s*(.+)$/gm)].map((match) => match[1].trim());

  assert.equal(runners.length > 0, true);
  assert.deepEqual([...new Set(runners)], ['ubuntu-latest']);
  const installs = workflow.match(/pnpm install --no-lockfile --no-frozen-lockfile --ignore-scripts/g) ?? [];
  assert.equal(installs.length, 2);
  assert.doesNotMatch(workflow, /pnpm install --frozen-lockfile|cache: pnpm|continue-on-error/);
  assert.doesNotMatch(workflow, /tools\/emception\/(?:userland|build|sysroot|tools\/emsdk)/);
  assert.match(workflow, /\.cache\/toolchain\/downloads/);
  assert.match(workflow, /artifacts\/toolchain\/receipts/);
  assert.match(workflow, /pnpm --dir tools\/emception toolchain build all/);
  assert.match(workflow, /pnpm --dir tools\/emception toolchain release/);
  assert.match(workflow, /pnpm --dir tools\/emception run verify:release/);
  assert.match(workflow, /changesets\/action@v2/);
  assert.match(workflow, /version-script: pnpm run version:emception/);
  assert.doesNotMatch(workflow, /auto-changeset\.mjs --apply/);
  assert.match(workflow, /tools\/emception\/packages\/toolchain\/cdn/);
  assert.match(workflow, /emception-v\$\{VERSION\}/);
  assert.equal(
    workflow.indexOf('- name: Build all Emception packages')
      < workflow.indexOf('- name: Generate clean release staging and packages'),
    true,
    'package clean/build must finish before the canonical CDN is staged',
  );

  const ignore = await readFile(path.join(repoRoot, '.gitignore'), 'utf8');
  const rootPackage = JSON.parse(await readFile(path.join(repoRoot, 'package.json'), 'utf8'));
  assert.doesNotMatch(ignore, /^pnpm-lock\.yaml$/m);
  assert.doesNotMatch(rootPackage.scripts.clean, /pnpm-lock\.yaml/);
});
