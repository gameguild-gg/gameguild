// ESM resolver hook: lets TypeScript source files import other TypeScript
// files via `.js` specifiers (the production convention) AND have Node's
// type-stripping test runner resolve them to `.ts` source on disk.
//
// Used by `node --test` runs that exercise modules with cross-module value
// imports. Loaded via `register(new URL('./_resolve-ts.mjs', import.meta.url))`
// at the top of the test entry, before any dynamic `import('./sut.ts')`.
//
// Mirrors the community resolver pattern (e.g. `node-resolve-ts/register`)
// without adding a runtime dep. Node 24's `--experimental-strip-types` runs
// `.ts` files natively but does not rewrite `.js` specifiers.

const TS_EXTENSIONS = ['.ts', '.mts', '.cts'];

export async function resolve(specifier, context, nextResolve) {
  try {
    return await nextResolve(specifier, context);
  } catch (err) {
    if (err?.code !== 'ERR_MODULE_NOT_FOUND' || !specifier.endsWith('.js')) {
      throw err;
    }
    const stripped = specifier.slice(0, -3); // drop `.js`
    for (const ext of TS_EXTENSIONS) {
      try {
        return await nextResolve(stripped + ext, context);
      } catch {
        // try the next extension
      }
    }
    throw err;
  }
}
