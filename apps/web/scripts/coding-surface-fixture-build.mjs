import { spawn } from 'node:child_process';
import { resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const repo = fileURLToPath(new URL('../../../', import.meta.url));
const helper = fileURLToPath(import.meta.url);
const root = resolve(repo, 'apps/web/test-results/coding-surface-refoundation');

export function codingSurfaceFixtureOptions(kind, directory) {
  if (kind !== 'authoring' && kind !== 'exercise') throw new TypeError('Unknown coding surface fixture.');
  const output = resolve(directory);
  if (!output.startsWith(root + sep)) throw new TypeError('Fixture output must remain inside owned test evidence.');
  const entry = kind === 'authoring' ? 'authoring-browser.tsx' : 'exercise-validation-browser.tsx';
  return {
    configFile: false,
    root: resolve(repo, 'apps/web'),
    publicDir: false,
    base: '/bundle/',
    define: { 'process.env.NODE_ENV': JSON.stringify('production') },
    resolve: { dedupe: ['react', 'react-dom'] },
    build: {
      outDir: output,
      emptyOutDir: false,
      target: 'es2022',
      minify: false,
      lib: { entry: resolve(repo, 'packages/features/coding-surface/src/testing', entry), formats: ['es'], fileName: () => `${kind}.js`, cssFileName: kind },
    },
    worker: { format: 'es' },
  };
}

/** The builder must exit before acquiring browser/compiler memory. */
export async function buildCodingSurfaceFixture(kind, directory) {
  codingSurfaceFixtureOptions(kind, directory);
  await new Promise((done, reject) => {
    const child = spawn(process.execPath, [helper, '--internal-build', kind, directory], {
      cwd: resolve(repo, 'apps/web'),
      env: process.env,
      stdio: 'inherit',
    });
    child.once('error', reject);
    child.once('close', (code, signal) => (code === 0 ? done() : reject(new Error(`Coding fixture build failed (${signal ?? code}).`))));
  });
}

if (resolve(process.argv[1] ?? '') === helper) {
  const [mode, kind, directory] = process.argv.slice(2);
  if (mode !== '--internal-build' || process.argv.length !== 5) throw new TypeError('This helper is internal test infrastructure.');
  const options = codingSurfaceFixtureOptions(kind, directory);
  const { build } = await import('vite');
  await build(options);
}
