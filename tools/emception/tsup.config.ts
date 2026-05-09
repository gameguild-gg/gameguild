import { defineConfig } from 'tsup';

// Phase 0.3: meta `emception` is now a thin wrapper over `@gameguild/emception-browser`
// + `@gameguild/emception-xterm`. The full toolchain implementation (worker-entry, vfs,
// emscripten bridge, etc.) lives in the scoped packages, so this build is a
// simple ESM re-export bundle. The .py raw loader and other heavy plumbing
// from earlier versions are no longer needed here — the scoped packages
// pre-compile that source themselves.
export default defineConfig({
    entry: {
        index: 'src/index.ts',
        'worker-entry': 'src/worker-entry.ts',
    },
    format: ['esm'],
    // DTS via separate tsc step (see build:lib script).
    dts: false,
    splitting: true,
    sourcemap: true,
    clean: true,
    outDir: 'dist',
    target: 'es2020',
    platform: 'browser',
    external: [
        '@xterm/xterm',
        '@gameguild/emception-browser',
        '@gameguild/emception-browser/worker',
        '@gameguild/emception-xterm',
        'emception',
    ],
});
