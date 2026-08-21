/**
 * Copy the freshly-built CPython WASM module into the sysroot.
 */

import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { defineBuildScript } from './lib/build-script.ts';
import { loadToolchainStateSync, lockedVersion } from './toolchain/config.ts';
import { toolchainPaths } from './toolchain/paths.ts';

defineBuildScript({
    label: 'deploy-cpython',
    requireEmsdk: true,
    run: async ({ step, log }) => {
        const P = toolchainPaths();
        const pythonVersion = lockedVersion(loadToolchainStateSync().lock, 'python');
        const buildWasmDir = path.join(P.builds, 'cpython', 'wasm');

        await step(`deploy cpython.wasm (python ${pythonVersion})`, () => {
            const src = path.join(buildWasmDir, 'python.wasm');
            if (!fs.existsSync(src)) {
                throw new Error(`Source not found: ${src}`);
            }
            const dest = path.join(P.sysroot, 'usr', 'lib', 'cpython.wasm');
            shell.mkdir('-p', path.dirname(dest));
            shell.cp(src, dest);
            log(`copied ${src} → ${dest}`);
        });
    },
});
