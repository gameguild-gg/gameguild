import assert from 'node:assert/strict';
import test from 'node:test';

import {
  normalizeVfsPath,
  posixPath,
  rewriteModuleImports,
  resolveSpecifier,
  extractSpecifiers,
  replaceImportSpecifier,
  patchImportMetaUrls,
  isClassicScriptModule,
  wrapUmdModule,
  parseArgsShim,
  collectTopLevelVarNames,
} from '../dist/node-tool.js';

test('normalizeVfsPath resolves dots and collapses slashes', () => {
  assert.equal(normalizeVfsPath('/usr/lib/../lib/emscripten//src/./x.mjs'), '/usr/lib/emscripten/src/x.mjs');
  assert.equal(normalizeVfsPath('/..'), '/');
  assert.equal(normalizeVfsPath('a/../../b'), '../b');
});

test('posixPath matches node:path for tool usage', () => {
  assert.equal(posixPath.dirname('/usr/lib/emscripten/src/utility.mjs'), '/usr/lib/emscripten/src');
  assert.equal(posixPath.basename('/usr/lib/emscripten/tools/compiler.mjs'), 'compiler.mjs');
  assert.equal(posixPath.extname('/a/b/c.mjs'), '.mjs');
  assert.equal(posixPath.join('/usr/lib/emscripten', 'src', './x.mjs'), '/usr/lib/emscripten/src/x.mjs');
  assert.equal(posixPath.relative('/usr/lib/emscripten/src', '/usr/lib/emscripten/tools/x.mjs'), '../tools/x.mjs');
  assert.equal(posixPath.isAbsolute('/x'), true);
  assert.equal(posixPath.isAbsolute('x'), false);
});

test('rewriteModuleImports maps relative, bare, and node: specifiers', () => {
  const src = [
    `import assert from 'node:assert';`,
    `import {readFile} from '../src/utility.mjs';`,
    `const jsifier = await import('../src/jsifier.mjs');`,
    `import * as acorn from 'acorn';`,
  ].join('\n');
  const out = rewriteModuleImports(src, '/usr/lib/emscripten/tools/compiler.mjs', '/usr/lib/emscripten');
  assert.match(out, /from 'node:assert'/);
  assert.match(out, /from '\/usr\/lib\/emscripten\/src\/utility\.mjs'/);
  assert.match(out, /import\('\/usr\/lib\/emscripten\/src\/jsifier\.mjs'\)/);
  assert.match(out, /from '\/usr\/lib\/emscripten\/node_modules\/acorn'/);
});

test('rewriteModuleImports preserves double-quoted specifiers untouched', () => {
  const src = `import x from "./keep.mjs";`;
  assert.equal(rewriteModuleImports(src, '/a/b.mjs', '/a'), src);
});

test('resolveSpecifier passes through node:, absolute, and blob: forms', () => {
  for (const spec of ['node:fs', '/abs/path.mjs', 'blob:1234']) {
    assert.equal(resolveSpecifier(spec, '/m.mjs', '/root'), spec);
  }
});

test('extractSpecifiers finds static and dynamic imports', () => {
  const src = `import a from 'one.mjs';\nexport {b} from 'two.mjs';\nawait import('three.mjs');\nconst s = 'not a spec';`;
  assert.deepEqual(new Set(extractSpecifiers(src)), new Set(['one.mjs', 'two.mjs', 'three.mjs']));
});

test('replaceImportSpecifier swaps every exact quoted occurrence', () => {
  const src = `import 'x.mjs'; import('x.mjs'); const s = 'x.mjs';`;
  const out = replaceImportSpecifier(src, 'x.mjs', 'blob:y');
  assert.ok(out.includes(`import 'blob:y'`));
  assert.ok(out.includes(`import('blob:y')`));
  assert.ok(out.includes(`const s = 'blob:y'`));
  assert.equal(replaceImportSpecifier(src, 'zzz', 'q'), src);
});

test('patchImportMetaUrls rewrites import.meta.url expressions to file URLs', () => {
  const src = `const d = url.fileURLToPath(new URL('.', import.meta.url));\nconst f = new URL("lib/x.js", import.meta.url);`;
  const out = patchImportMetaUrls(src, '/usr/lib/emscripten/src/utility.mjs');
  assert.match(out, /file:\/\/\/usr\/lib\/emscripten\/src\//);
  assert.match(out, /file:\/\/\/usr\/lib\/emscripten\/src\/lib\/x\.js/);
});

test('isClassicScriptModule detects UMD vs ESM', () => {
  assert.equal(isClassicScriptModule(`(function(global, factory){}(this, function(exports){}));`), true);
  assert.equal(isClassicScriptModule(`export function f(){}\nimport x from 'y';`), false);
  assert.equal(isClassicScriptModule(`export default {};`), false);
  assert.equal(isClassicScriptModule(`import('./dyn.mjs');`), false);
});

test('wrapUmdModule exposes a default export', async () => {
  const wrapped = wrapUmdModule(`exports.answer = 42;`);
  const blob = new Blob([wrapped], { type: 'text/javascript' });
  // Static-compile check only (no blob import in Node); verify shape.
  assert.match(wrapped, /export default/);
  assert.match(wrapped, /var exports = \{\}/);
});

test('parseArgsShim handles flags, shorts, inline values, and positionals', () => {
  const config = {
    options: {
      help: { type: 'boolean', short: 'h' },
      'symbols-only': { type: 'boolean' },
      output: { type: 'string', short: 'o' },
    },
    allowPositionals: true,
  };
  const argv = ['node', 'compiler.mjs', '-', '--symbols-only'];
  const r1 = parseArgsShim(argv, config);
  assert.equal(r1.values['symbols-only'], true);
  assert.deepEqual(r1.positionals, ['-']);

  const r2 = parseArgsShim(['node', 't.mjs', '-o', 'out.js', 'in.js'], config);
  assert.equal(r2.values.output, 'out.js');
  assert.deepEqual(r2.positionals, ['in.js']);

  const r3 = parseArgsShim(['node', 't.mjs', '--output=x.js'], config);
  assert.equal(r3.values.output, 'x.js');

  assert.throws(() => parseArgsShim(['node', 't.mjs', '--nope'], config), /unknown option/);
});

test('parseArgsShim passes acorn-optimizer-style argv', () => {
  const config = {
    options: {
      'closure-friendly': { type: 'boolean' },
      'minify-whitespace': { type: 'boolean' },
      outfile: { type: 'string', short: 'o' },
    },
    allowPositionals: true,
  };
  const r = parseArgsShim(['node', 'acorn-optimizer.mjs', 'main.js', 'JSDCE', '--minify-whitespace', '-o', 'main.jso1.js'], config);
  assert.equal(r.values['minify-whitespace'], true);
  assert.equal(r.values.outfile, 'main.jso1.js');
  assert.deepEqual(r.positionals, ['main.js', 'JSDCE']);
});

test('collectTopLevelVarNames harvests top-level declarations', () => {
  const code = `var ASSERTIONS = 1;\nfunction f() { var nested = 2; }\nvar EXPORTED_FUNCTIONS = ['a'];\nlet notVar = 3;`;
  assert.deepEqual(new Set(collectTopLevelVarNames(code)), new Set(['ASSERTIONS', 'EXPORTED_FUNCTIONS']));
});
