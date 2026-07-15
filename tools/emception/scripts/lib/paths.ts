/**
 * Shared path constants used by the build scripts.
 *
 * Every build/deploy script used to compute the same set of paths (`userland/`,
 * `build/`, `sysroot/usr/lib`, `build/cdn`) by hand from `process.cwd()`. That
 * meant a typo in any one script silently created a parallel directory tree.
 *
 * `paths(root?)` returns a frozen record of the canonical locations rooted at
 * `process.cwd()` (or the explicit `root` argument). Use it like:
 *
 *     const P = paths();
 *     shell.mkdir('-p', P.build);
 *     shell.cp(file, P.sysrootLib);
 *
 * If you need a per-tool subdirectory under `userland/`, use `paths().userland`
 * + `path.join` rather than recomputing it.
 */

import path from 'path';

export interface EmceptionPaths {
    /** Repository root (= the directory the script was invoked from). */
    readonly root: string;
    /** Sources downloaded/extracted by build scripts: `<root>/userland`. */
    readonly userland: string;
    /** Build outputs (.wasm, .mjs, .a): `<root>/build`. */
    readonly build: string;
    /** Final CDN payload that gets shipped to consumers: `<root>/build/cdn`. */
    readonly buildCdn: string;
    /** Cross-compile sysroot consumed by clang/lld: `<root>/sysroot`. */
    readonly sysroot: string;
    /** `<sysroot>/usr/lib` — emscripten libraries land here. */
    readonly sysrootLib: string;
    /** `<sysroot>/usr/include` — headers land here. */
    readonly sysrootInclude: string;
    /** Public Next.js mount point: `<root>/public/cdn`. */
    readonly publicCdn: string;
    /** Generated manifest file: `<root>/build/manifest.json`. */
    readonly manifestFile: string;
    /** libcurl-lite include directory used by ninja/cmake. */
    readonly libcurlInclude: string;
    /** libcurl static archive produced by `build:libcurl-lite`. */
    readonly libcurlArchive: string;
}

export function paths(root: string = process.cwd()): EmceptionPaths {
    const userland = path.join(root, 'userland');
    const build = path.join(root, 'build');
    const sysroot = path.join(root, 'sysroot');
    return Object.freeze({
        root,
        userland,
        build,
        buildCdn: path.join(build, 'cdn'),
        sysroot,
        sysrootLib: path.join(sysroot, 'usr', 'lib'),
        sysrootInclude: path.join(sysroot, 'usr', 'include'),
        publicCdn: path.join(root, 'public', 'cdn'),
        manifestFile: path.join(build, 'manifest.json'),
        libcurlInclude: path.join(userland, 'libcurl-lite', 'include'),
        libcurlArchive: path.join(build, 'libcurl.a'),
    });
}
