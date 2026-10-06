import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { test } from 'node:test';

test('Emception CI is Linux-only, lockfile-driven, receipt-aware, and Changesets-based', async () => {
  const repoRoot = path.resolve(import.meta.dirname, '..', '..', '..', '..');
  const workflow = await readFile(path.join(repoRoot, '.github', 'workflows', 'emception.yml'), 'utf8');
  const runners = [...workflow.matchAll(/^\s*runs-on:\s*(.+)$/gm)].map((match) => match[1].trim());

  assert.equal(runners.length > 0, true);
  assert.deepEqual([...new Set(runners)], ['ubuntu-latest']);
  assert.match(workflow, /pnpm install --frozen-lockfile --ignore-scripts/);
  assert.doesNotMatch(workflow, /--no-lockfile|continue-on-error/);
  assert.doesNotMatch(workflow, /tools\/emception\/(?:userland|build|sysroot|tools\/emsdk)/);
  assert.match(workflow, /\.cache\/toolchain\/downloads/);
  assert.match(workflow, /artifacts\/toolchain\/receipts/);
  assert.match(workflow, /pnpm --dir tools\/emception toolchain build all/);
  assert.match(workflow, /pnpm --dir tools\/emception run test:curl-lite/);
  assert.equal(
    workflow.indexOf('- name: Build and validate Toolchain receipts')
      < workflow.indexOf('- name: Verify native and WASM curl adapter security regressions'),
    true,
    'curl regressions require the pinned SDK from the receipt build',
  );
  assert.equal(
    workflow.indexOf('- name: Verify native and WASM curl adapter security regressions')
      < workflow.indexOf('- name: Build all Emception packages'),
    true,
    'curl sanitizer regressions must pass before packaging',
  );
  assert.match(workflow, /pnpm --dir tools\/emception toolchain release/);
  assert.match(workflow, /pnpm --dir tools\/emception run verify:release/);
  assert.match(workflow, /changesets\/action@[a-f0-9]{40}\s+# v2\b/);
  assert.doesNotMatch(workflow, /changesets\/action@v\d/);
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
  assert.match(workflow, /pnpm --filter @game-guild\/client run build/);
  const failureDiagnostics = workflow.slice(workflow.indexOf('- name: Upload browser diagnostics on failure'));
  assert.match(failureDiagnostics, /if: failure\(\)/);
  assert.match(failureDiagnostics, /apps\/web\/test-results\/coding-cycle/);
  assert.equal(
    workflow.indexOf('- name: Build generated API client')
      < workflow.indexOf('- name: Run instructor and learner coding assessment cycle'),
    true,
    'the generated API client must exist before the coding-cycle runner imports it',
  );

  const ignore = await readFile(path.join(repoRoot, '.gitignore'), 'utf8');
  const rootPackage = JSON.parse(await readFile(path.join(repoRoot, 'package.json'), 'utf8'));
  assert.doesNotMatch(ignore, /^pnpm-lock\.yaml$/m);
  assert.doesNotMatch(rootPackage.scripts.clean, /pnpm-lock\.yaml/);
});
