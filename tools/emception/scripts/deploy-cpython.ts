/**
 * Copy the freshly-built CPython WASM module into the sysroot.
 */

import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { defineBuildScript } from './lib/build-script.ts';
import { detectPythonVersion } from './lib/detect-versions.ts';
import { paths } from './lib/paths.ts';

defineBuildScript({
    label: 'deploy-cpython',
    requireEmsdk: true, // setupEmsdk needed so detectPythonVersion can read the bundled config
    run: async ({ step, log }) => {
        const P = paths();
        const pythonVersion = process.env.PYTHON_VERSION || detectPythonVersion();
        const buildWasmDir = path.join(P.userland, 'cpython', `cpython-${pythonVersion}`, 'build-wasm');

        await step(`deploy cpython.wasm (python ${pythonVersion})`, () => {
            const src = path.join(buildWasmDir, 'python.wasm');
            if (!fs.existsSync(src)) {
                throw new Error(`Source not found: ${src}`);
            }
            const dest = path.join(P.sysrootLib, 'cpython.wasm');
            shell.cp(src, dest);
            log(`copied ${src} → ${dest}`);
        });
    },
});
