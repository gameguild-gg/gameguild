/**
 * Set up the Emscripten SDK at the version specified on the CLI
 * (or `EMSDK_VERSION`, or `'latest'`).
 *
 * Canonical example of the `defineBuildScript` helper: a build entrypoint
 * should be ~10 lines of declarative configuration.
 */

import { defineBuildScript } from './lib/build-script.ts';
import { setupEmsdk } from './lib/emsdk.ts';
import { PINNED } from './lib/pinned-versions.ts';

const version = process.argv[2] || process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;

defineBuildScript({
    label: 'setup-emsdk',
    run: ({ step }) => step(`install emsdk@${version}`, () => {
        setupEmsdk(version);
    }),
});
