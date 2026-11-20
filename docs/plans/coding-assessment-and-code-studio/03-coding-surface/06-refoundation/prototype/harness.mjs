import assert from 'node:assert/strict';
import { readFile, mkdir } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';
import { chromium } from 'playwright';

export const entryUrl = new URL('../layout-prototype.html', import.meta.url).href;
export const outputDirectory = fileURLToPath(new URL('../../../../../../apps/web/test-results/coding-surface-refoundation/', import.meta.url));
export const sliceIds = ['UXP-01', 'UXP-02', 'UXP-03', 'UXP-04', 'UXP-05', 'UXP-06', 'UXP-07'];
export const requiredOracles = {
  'UXP-01': ['UXP-01-entry-lifecycle'],
  'UXP-02': ['UXP-02-blank-function-typed-cases', 'UXP-02-io-empty-output-and-environment'],
  'UXP-03': ['UXP-03-reference-wrong-and-stale', 'UXP-03-faithful-redacted-independent-preview'],
  'UXP-04': ['UXP-04-diagnose-correct-submit-frozen', 'UXP-04-custom-compile-cancel-stale'],
  'UXP-05': ['UXP-05-save-conflict-recovery-and-lossless-cancel', 'UXP-05-host-layout-keyboard-and-mobile'],
  'UXP-06': [
    'UXP-06-scalar-types-and-capability-guards',
    'UXP-06-modal-reset-and-save-in-flight',
    'UXP-06-capability-specimens-and-captures',
    'UXP-06-immutable-support-and-command-port',
  ],
  'UXP-07': ['UXP-07-author-file-workbench', 'UXP-07-learner-buffers-and-readonly', 'UXP-07-editor-responsive-and-keyboard'],
};

export function selectCases(cases, selection = process.env.CODING_PROTOTYPE_SLICES ?? sliceIds.join(',')) {
  const requested = selection.split(',').map((id) => id.trim());
  assert.ok(requested.length > 0 && requested.every((id) => sliceIds.includes(id)), 'Select known UXP slices.');
  assert.equal(new Set(requested).size, requested.length, 'Duplicate UXP selection.');
  const selected = cases.filter((entry) => requested.includes(entry.slice));
  assert.ok(selected.length > 0, 'Selected prototype suite has zero cases.');
  for (const id of requested)
    assert.ok(
      selected.some((entry) => entry.slice === id),
      `Missing required ${id} oracle.`,
    );
  assert.equal(new Set(selected.map((entry) => entry.id)).size, selected.length, 'Duplicate oracle ID.');
  for (const slice of requested) {
    for (const id of requiredOracles[slice])
      assert.ok(
        selected.some((entry) => entry.slice === slice && entry.id === id),
        `Missing required oracle ${id}.`,
      );
  }
  return selected;
}

export async function loadContracts() {
  const scope = { window: {}, structuredClone };
  const context = vm.createContext(scope);
  for (const name of ['schemas.js', 'html.js', 'code-view.js', 'fixtures.js', 'model.js', 'simulation.js', 'recovery.js']) {
    vm.runInContext(await readFile(new URL(name, import.meta.url), 'utf8'), context, { filename: name, timeout: 1000 });
  }
  return scope.window;
}

export async function openHarness() {
  await mkdir(outputDirectory, { recursive: true });
  const options = existsSync(chromium.executablePath()) ? {} : { channel: 'chrome' };
  const browser = await chromium.launch({ ...options, headless: true });
  let closed = false;
  return {
    browser,
    async close() {
      if (!closed) {
        closed = true;
        await browser.close();
      }
    },
  };
}
