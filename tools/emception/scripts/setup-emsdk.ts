/**
 * Set up the Emscripten SDK at the exact version in toolchain.lock.json.
 *
 * Canonical example of the `defineBuildScript` helper: a build entrypoint
 * should be ~10 lines of declarative configuration.
 */

import { defineBuildScript } from './lib/build-script.ts';
import { setupEmsdk } from './lib/emsdk.ts';
import { loadToolchainStateSync, lockedVersion } from './toolchain/config.ts';

const version = lockedVersion(loadToolchainStateSync().lock, 'emsdk');

defineBuildScript({
    label: 'setup-emsdk',
    run: ({ step }) => step(`install emsdk@${version}`, () => {
        setupEmsdk(version);
    }),
});
