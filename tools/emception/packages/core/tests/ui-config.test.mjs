// Tests for `@emception/core`'s view-config helpers.
//
// `normalizeViewConfig` is the single source of truth that both the
// webcomponent and React component funnel attributes through; these
// tests pin down its defaults, validation, and inline-source folding.
// `toAttributes` + `diffViewConfigs` underpin parity tests so they get
// dedicated coverage too.

import assert from 'node:assert/strict';
import { test } from 'node:test';

const {
    normalizeViewConfig,
    toAttributes,
    diffViewConfigs,
    BuildConfigError,
} = await import('../dist/index.js');

test('normalizeViewConfig: defaults applied for empty input', () => {
    const cfg = normalizeViewConfig({});
    assert.equal(cfg.preset, undefined);
    assert.equal(cfg.workspace, undefined);
    assert.equal(cfg.autorun, false);
    assert.equal(cfg.canvas, false);
    assert.equal(cfg.showHidden, false);
    assert.equal(cfg.showSolution, false);
    assert.equal(cfg.stdin, 'auto');
    assert.equal(cfg.stdout, 'auto');
    assert.equal(cfg.stderr, 'auto');
});

test('normalizeViewConfig: unknown preset throws BuildConfigError', () => {
    assert.throws(
        () => normalizeViewConfig({ preset: 'lolcode' }),
        (err) => err instanceof BuildConfigError && /unknown preset/.test(err.message),
    );
});

test('normalizeViewConfig: known preset round-trips', () => {
    const cfg = normalizeViewConfig({ preset: 'cpp' });
    assert.equal(cfg.preset, 'cpp');
});

test('normalizeViewConfig: workspace string shorthand becomes {name}', () => {
    const cfg = normalizeViewConfig({ workspace: 'demo' });
    assert.deepEqual(cfg.workspace, { name: 'demo' });
});

test('normalizeViewConfig: empty workspace name throws', () => {
    assert.throws(() => normalizeViewConfig({ workspace: '' }), BuildConfigError);
});

test('normalizeViewConfig: workspace object missing name throws', () => {
    assert.throws(
        () => normalizeViewConfig({ workspace: {} }),
        BuildConfigError,
    );
});

test('normalizeViewConfig: inline source without workspace invents default', () => {
    const cfg = normalizeViewConfig({ source: 'int main(){}' });
    assert.equal(cfg.workspace?.name, 'default');
    assert.equal(cfg.workspace?.seed?.['main.cpp'], 'int main(){}');
});

test('normalizeViewConfig: inline source merges into existing seed', () => {
    const cfg = normalizeViewConfig({
        workspace: { name: 'demo', seed: { 'extra.h': '// hdr' } },
        source: 'int x;',
    });
    assert.equal(cfg.workspace?.seed?.['main.cpp'], 'int x;');
    assert.equal(cfg.workspace?.seed?.['extra.h'], '// hdr');
});

test('normalizeViewConfig: inline source skipped when seed already has main.*', () => {
    const cfg = normalizeViewConfig({
        workspace: { name: 'demo', seed: { 'main.cpp': 'pre-existing' } },
        source: 'should-be-ignored',
    });
    assert.equal(cfg.workspace?.seed?.['main.cpp'], 'pre-existing');
});

test('normalizeViewConfig: seedPolicy fallback applied', () => {
    const cfg = normalizeViewConfig({
        workspace: { name: 'demo' },
        seedPolicy: 'replace',
    });
    assert.equal(cfg.workspace?.seedPolicy, 'replace');
});

test('toAttributes: emits boolean flags as empty strings', () => {
    const cfg = normalizeViewConfig({ preset: 'cpp', autorun: true, canvas: true });
    const attrs = toAttributes(cfg);
    assert.equal(attrs['preset'], 'cpp');
    assert.equal(attrs['autorun'], '');
    assert.equal(attrs['canvas'], '');
});

test('toAttributes: omits unset values', () => {
    const cfg = normalizeViewConfig({});
    const attrs = toAttributes(cfg);
    assert.equal(attrs['autorun'], undefined);
    assert.equal(attrs['preset'], undefined);
});

test('toAttributes: workspace object emits name + seed-policy', () => {
    const cfg = normalizeViewConfig({
        workspace: { name: 'demo', seedPolicy: 'merge' },
    });
    const attrs = toAttributes(cfg);
    assert.equal(attrs['workspace'], 'demo');
    assert.equal(attrs['seed-policy'], 'merge');
});

test('diffViewConfigs: identical configs return null', () => {
    const a = normalizeViewConfig({ preset: 'cpp', autorun: true });
    const b = normalizeViewConfig({ preset: 'cpp', autorun: true });
    assert.equal(diffViewConfigs(a, b), null);
});

test('diffViewConfigs: different configs return a message', () => {
    const a = normalizeViewConfig({ preset: 'cpp', autorun: true });
    const b = normalizeViewConfig({ preset: 'cpp', autorun: false });
    const diff = diffViewConfigs(a, b);
    assert.ok(diff && diff.includes('mismatch'));
});

test('diffViewConfigs: ignores stdin/stdout/stderr (they may be functions)', () => {
    const a = normalizeViewConfig({ preset: 'cpp' });
    const b = normalizeViewConfig({ preset: 'cpp' });
    a.stdin = () => undefined;
    b.stdin = 'auto';
    assert.equal(diffViewConfigs(a, b), null);
});
