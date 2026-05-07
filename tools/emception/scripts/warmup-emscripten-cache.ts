/**
 * Warm up the Emscripten compiled cache before parallel toolchain builds.
 *
 * The first time any `emcc` invocation runs it compiles a set of system
 * libraries (libc, libc++, libdlmalloc, …) into `~/.emscripten_cache/`.
 * When multiple `emcc` processes start simultaneously on a cold cache they
 * all race to do this work, producing corrupted or missing cache entries.
 *
 * This script runs a single trivial `emcc` compile so the cache is fully
 * populated *before* the parallel toolchain builds begin. Subsequent
 * concurrent `emcc` invocations will find the cache warm and skip the
 * compilation step entirely.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { defineBuildScript } from './lib/build-script.ts';

defineBuildScript({
    label: 'warmup-emscripten-cache',
    requireEmsdk: true,
    run: ({ step }) =>
        step('compile trivial program to warm emscripten cache', () => {
            const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'emception-warmup-'));
            const src = path.join(tmpDir, 'warmup.c');
            const out = path.join(tmpDir, 'warmup.js');

            try {
                fs.writeFileSync(src, 'int main(void) { return 0; }\n');
                // -O0: fastest compile — we only want to trigger cache population.
                const result = shell.exec(`emcc -O0 "${src}" -o "${out}"`);
                if (result.code !== 0) {
                    throw new Error(`emcc warmup exited with code ${result.code}`);
                }
                console.log('✓ Emscripten cache warmed up successfully');
            } finally {
                shell.rm('-rf', tmpDir);
            }
        }),
});
