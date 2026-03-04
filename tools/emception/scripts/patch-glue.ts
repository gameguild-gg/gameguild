/**
 * Post-build patches for Emscripten .mjs glue files.
 *
 * Emscripten generates .mjs glue code for each WASM module. The defaults
 * assume a Node.js or browser environment with no external orchestration.
 * Our micro-kernel architecture needs a few modifications:
 *
 * 1. **ENV merge** — The glue initializes `var ENV={}` but never reads
 *    `moduleArg.ENV`.  We inject code so the kernel can pass PYTHONHOME,
 *    PYTHONPATH, HOME, etc. from the tool-runner.
 *
 * 2. **systemCallback** — Python's `os.system()` maps to `__emscripten_system`,
 *    which returns -52 (ENOSYS) in the browser.  We patch it to call
 *    `Module.systemCallback(cmd)` so the subprocess dispatch shim works.
 *
 * These patches are applied to the build output in `build/` before
 * `deploy:cdn` copies them to `web/public/cdn/`.
 *
 * Additionally, patches are applied to any `.mjs` files already present in
 * `sysroot/usr/lib/` so the manifest generation picks up the correct hashes.
 */

import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = path.resolve(__dirname, '..');

const BUILD_DIR = path.join(ROOT, 'build');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');

// All tool .mjs files that need ENV merge patching
const ALL_TOOLS = [
    'clang', 'lld', 'python',
    'wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce',
    // Additional LLVM tools that may have .mjs files
    'llvm-nm', 'llvm-ar', 'llvm-objcopy', 'llc',
];

// Only python.mjs needs the systemCallback patch
const PYTHON_TOOL = 'python';

let patchCount = 0;
let fileCount = 0;

/**
 * Patch 1: ENV merge from moduleArg.
 *
 * Emscripten generates:
 *   var ENV={};
 *
 * We change it to:
 *   var ENV={};if(moduleArg&&moduleArg["ENV"]){for(var _k in moduleArg["ENV"]){ENV[_k]=moduleArg["ENV"][_k]}}
 *
 * This allows the tool-runner to pass PYTHONHOME, PYTHONPATH, HOME, PATH, etc.
 */
function patchEnvMerge(content: string, filename: string): string {
    const needle = 'var ENV={};';
    const replacement = 'var ENV={};if(moduleArg&&moduleArg["ENV"]){for(var _k in moduleArg["ENV"]){ENV[_k]=moduleArg["ENV"][_k]}}';

    if (!content.includes(needle)) {
        console.log(`  [${filename}] No ENV pattern found — skipping ENV merge patch`);
        return content;
    }

    if (content.includes('moduleArg["ENV"]')) {
        console.log(`  [${filename}] ENV merge already patched — skipping`);
        return content;
    }

    patchCount++;
    console.log(`  [${filename}] Patched: ENV merge from moduleArg.ENV`);
    return content.replace(needle, replacement);
}

/**
 * Patch 2: systemCallback for os.system() interception.
 *
 * Emscripten generates __emscripten_system which, in browser mode, returns -52.
 * We patch it to check for Module.systemCallback first.  The callback is async
 * (it spawns child WASM processes), so the function must also be wrapped with
 * WebAssembly.Suspending for JSPI stack-switching to work.
 *
 * Two sub-patches:
 *
 * 2a.  In the function body, call Module["systemCallback"] before returning -52:
 *
 *   Before: if(!command)return 0;return-52
 *   After:  if(!command)return 0;if(Module["systemCallback"]){return Module["systemCallback"](UTF8ToString(command))}return-52
 *
 * 2b.  Wrap __emscripten_system with WebAssembly.Suspending so JSPI can await
 *      the Promise returned by the async systemCallback:
 *
 *   Inject: if(WebAssembly.Suspending){__emscripten_system=new WebAssembly.Suspending(__emscripten_system)}
 */
function patchSystemCallback(content: string, filename: string): string {
    let patched = content;

    // Check that this file actually has __emscripten_system (only python.mjs does)
    if (!patched.includes('__emscripten_system')) {
        return patched;
    }

    // 2a: Hook systemCallback into the __emscripten_system browser fallback
    const needle = 'if(!command)return 0;return-52';
    const replacement = 'if(!command)return 0;if(Module["systemCallback"]){return Module["systemCallback"](UTF8ToString(command))}return-52';

    if (patched.includes('Module["systemCallback"]')) {
        console.log(`  [${filename}] systemCallback already patched — skipping 2a`);
    } else if (patched.includes(needle)) {
        patched = patched.replace(needle, replacement);
        patchCount++;
        console.log(`  [${filename}] Patched 2a: systemCallback hook in __emscripten_system`);
    } else {
        console.warn(`  [${filename}] Warning: __emscripten_system exists but browser fallback pattern not found — 2a skipped`);
    }

    // 2b: Wrap __emscripten_system with WebAssembly.Suspending for JSPI.
    // We look for the wasmImports binding  _emscripten_system:__emscripten_system
    // and inject the wrapping right before the wasmImports object.
    const suspendingNeedle = 'Suspending(__emscripten_system)';
    if (patched.includes(suspendingNeedle)) {
        console.log(`  [${filename}] __emscripten_system Suspending already patched — skipping 2b`);
    } else {
        // Insert immediately before `var wasmImports=`
        const wasmImportsNeedle = 'var wasmImports=';
        if (patched.includes(wasmImportsNeedle)) {
            const suspendingCode = 'if(WebAssembly.Suspending){__emscripten_system=new WebAssembly.Suspending(__emscripten_system)}';
            patched = patched.replace(
                wasmImportsNeedle,
                suspendingCode + wasmImportsNeedle,
            );
            patchCount++;
            console.log(`  [${filename}] Patched 2b: __emscripten_system wrapped with WebAssembly.Suspending`);
        } else {
            console.warn(`  [${filename}] Warning: Could not find 'var wasmImports=' — skipping Suspending patch`);
        }
    }

    return patched;
}

/**
 * Patch 3: resolveGlobalSymbol stub for JSPI in standalone builds.
 *
 * Emscripten's JSPI code references `resolveGlobalSymbol`, which only exists
 * in dynamic-linking builds. We provide a stub that resolves symbols from
 * wasmExports so JSPI async I/O still works.
 */
function patchResolveGlobalSymbol(content: string, filename: string): string {
    const needle = 'if(!WebAssembly.promising){return}const origResolveGlobalSymbol=resolveGlobalSymbol';
    const replacement = 'if(!WebAssembly.promising){return}if(typeof resolveGlobalSymbol==="undefined"){var resolveGlobalSymbol=function(n){return{sym:wasmExports[n]}}}const origResolveGlobalSymbol=resolveGlobalSymbol';

    if (!content.includes(needle)) {
        // Not an error — not all .mjs files have JSPI code
        return content;
    }

    if (content.includes('typeof resolveGlobalSymbol==="undefined"')) {
        console.log(`  [${filename}] resolveGlobalSymbol already patched — skipping`);
        return content;
    }

    patchCount++;
    console.log(`  [${filename}] Patched: resolveGlobalSymbol stub for JSPI`);
    return content.replace(needle, replacement);
}

/**
 * Patch 5: Wrap _main with WebAssembly.promising in callMain().
 *
 * In standalone builds (no MAIN_MODULE/SIDE_MODULE), Emscripten's built-in
 * JSPI code defines a resolveGlobalSymbol override that would wrap `main`
 * with WebAssembly.promising — but that override is never called because
 * $applySignatureConversions doesn't exist in standalone builds.
 *
 * As a result, callMain() calls _main directly without the promising
 * wrapper, and any Suspending-wrapped import (like __emscripten_system)
 * fails with "trying to suspend without WebAssembly.promising".
 *
 * We patch callMain to wrap entryFunction with WebAssembly.promising
 * if available:
 *
 *   Before: var entryFunction=_main;
 *   After:  var entryFunction=_main;if(WebAssembly.promising){entryFunction=WebAssembly.promising(entryFunction)}
 */
function patchCallMainPromising(content: string, filename: string): string {
    const needle = 'var entryFunction=_main;';
    const replacement = 'var entryFunction=_main;if(WebAssembly.promising){entryFunction=WebAssembly.promising(entryFunction)}';

    if (!content.includes(needle)) {
        console.warn(`  [${filename}] Warning: callMain entryFunction pattern not found — skipping promising patch`);
        return content;
    }

    if (content.includes('promising(entryFunction)')) {
        console.log(`  [${filename}] callMain promising already patched — skipping`);
        return content;
    }

    patchCount++;
    console.log(`  [${filename}] Patched: callMain entryFunction wrapped with WebAssembly.promising`);
    return content.replace(needle, replacement);
}

/**
 * Apply all relevant patches to a single .mjs file.
 */
function patchFile(filePath: string): void {
    if (!fs.existsSync(filePath)) return;

    const filename = path.basename(filePath);
    let content = fs.readFileSync(filePath, 'utf8');
    const originalContent = content;

    // All tools get ENV merge and resolveGlobalSymbol patches
    content = patchEnvMerge(content, filename);
    content = patchResolveGlobalSymbol(content, filename);

    // Only python.mjs gets the systemCallback and callMain promising patches
    if (filename === 'python.mjs') {
        content = patchSystemCallback(content, filename);
        content = patchCallMainPromising(content, filename);
    }

    if (content !== originalContent) {
        fs.writeFileSync(filePath, content, 'utf8');
        fileCount++;
    }
}

// ---------------------------------------------------------------------------
// Patch 4: Emscripten Python sources — make ctypes import optional.
//
// colored_logger.py does `import ctypes` at module level. The _ctypes C
// extension is unavailable in WASM CPython, but ctypes is only used for
// Windows console colour detection. We wrap the import in try/except so
// the module loads without _ctypes.
// ---------------------------------------------------------------------------

/**
 * Directories that contain the emscripten tools Python sources.
 *
 * Only the *source* location is patched here — sysroot/usr/lib/emscripten/tools/.
 * Downstream steps (build:manifest → deploy:cdn) copy the already-patched
 * files to build/cdn/ and web/public/cdn/ respectively, so those git-ignored
 * deploy targets must NOT be patched directly.
 */
const EMSCRIPTEN_TOOLS_DIRS = [
    path.join(ROOT, 'sysroot', 'usr', 'lib', 'emscripten', 'tools'),
];

function patchColoredLogger(filePath: string): void {
    if (!fs.existsSync(filePath)) return;

    const filename = path.basename(filePath);
    let content = fs.readFileSync(filePath, 'utf8');

    // Replace bare `import ctypes` with a try/except that sets ctypes = None
    const needle = 'import ctypes\nimport logging';
    const replacement = `try:\n    import ctypes\nexcept ImportError:\n    ctypes = None\nimport logging`;

    if (!content.includes(needle)) {
        if (content.includes('except ImportError')) {
            console.log(`  [${filename}] ctypes import already patched — skipping`);
        } else {
            console.warn(`  [${filename}] Warning: expected ctypes import pattern not found`);
        }
        return;
    }

    content = content.replace(needle, replacement);

    // Also guard the `ansi_color_available()` function where ctypes.windll is used
    // When ctypes is None, the function should just return False for Windows path
    // (we're in WASM, so not Windows anyway — early return via isatty check suffices)
    const windllNeedle = '  kernel32 = ctypes.windll.kernel32';
    if (content.includes(windllNeedle)) {
        content = content.replace(
            windllNeedle,
            '  if ctypes is None:\n    return False\n  kernel32 = ctypes.windll.kernel32',
        );
    }

    fs.writeFileSync(filePath, content, 'utf8');
    patchCount++;
    fileCount++;
    console.log(`  [${filename}] Patched: ctypes import made optional for WASM`);
}

// ---- Main ----

console.log('=== Patching Emscripten .mjs glue files ===');
console.log('');

// Patch files in build/ directory
console.log(`Patching files in ${BUILD_DIR}/...`);
for (const tool of ALL_TOOLS) {
    const mjsPath = path.join(BUILD_DIR, `${tool}.mjs`);
    patchFile(mjsPath);
}

// Patch files in sysroot/usr/lib/ (if they exist there)
if (fs.existsSync(SYSROOT_LIB)) {
    console.log('');
    console.log(`Patching files in ${SYSROOT_LIB}/...`);
    for (const tool of ALL_TOOLS) {
        const mjsPath = path.join(SYSROOT_LIB, `${tool}.mjs`);
        patchFile(mjsPath);
    }
}

// Patch Emscripten Python sources
console.log('');
console.log('Patching Emscripten Python sources...');
for (const toolsDir of EMSCRIPTEN_TOOLS_DIRS) {
    const clPath = path.join(toolsDir, 'colored_logger.py');
    patchColoredLogger(clPath);
}

console.log('');
console.log(`=== Patching complete: ${patchCount} patches applied to ${fileCount} files ===`);

if (patchCount === 0) {
    console.warn('Warning: No patches were applied. This may indicate the .mjs files have not been built yet,');
    console.warn('or the Emscripten version has changed the code patterns. Please verify manually.');
}
