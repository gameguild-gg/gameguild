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
 *    With Asyncify, __emscripten_system is listed in ASYNCIFY_IMPORTS,
 *    so Emscripten handles the async suspension/resume automatically.
 *
 * These patches are applied to the build output in `build/` before
 * `deploy:cdn` copies them to `web/public/cdn/`.
 *
 * Additionally, patches are applied to any `.mjs` files already present in
 * `sysroot/usr/lib/` so the manifest generation picks up the correct hashes.
 *
 * NOTE: JSPI patches (resolveGlobalSymbol, WebAssembly.promising, _jspi_wrap)
 * have been removed. All async suspension is now handled by Emscripten's
 * Asyncify runtime (-sASYNCIFY + -sASYNCIFY_IMPORTS in build scripts).
 */

import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { enableBuildKeepalive } from './lib/keepalive.ts';

enableBuildKeepalive('patch-glue');

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
    // Build tools (linked against libcurl-lite, need systemCallback for __dispatch_curl)
    'ninja', 'cmake', 'curl',
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
 * We patch it to first check Module.systemCallbackSync (synchronous fast-path
 * for version probes etc.) then fall through to Module.systemCallback via
 * Asyncify.handleAsync for full subprocess dispatch.
 *
 * The sync fast-path avoids Asyncify issues where the cmake WASM binary may not
 * have the system() call path properly instrumented for stack unwinding.
 * The ToolRunner provides systemCallbackSync for commands it can answer
 * immediately (like ninja --version), and systemCallback for the rest.
 *
 *   Before: if(!command)return 0;return-52
 *   After:  if(!command)return 0;if(Module["systemCallbackSync"]){var sr=Module["systemCallbackSync"](UTF8ToString(command));if(sr!==undefined)return sr}if(Module["systemCallback"]){return Asyncify.handleAsync(function(){return Module["systemCallback"](UTF8ToString(command))})}return-52
 */
function patchSystemCallback(content: string, filename: string): string {
    let patched = content;

    // Check that this file actually has __emscripten_system
    if (!patched.includes('__emscripten_system')) {
        return patched;
    }

    // Hook systemCallbackSync (synchronous fast-path) + systemCallback (async via Asyncify)
    // into the __emscripten_system browser fallback.
    const needle = 'if(!command)return 0;return-52';
    const replacement = 'if(!command)return 0;if(Module["systemCallbackSync"]){var sr=Module["systemCallbackSync"](UTF8ToString(command));if(sr!==undefined)return sr}if(Module["systemCallback"]){return Asyncify.handleAsync(function(){return Module["systemCallback"](UTF8ToString(command))})}return-52';

    // Previous patch variants to upgrade
    const oldPatchAsyncOnly = 'if(!command)return 0;if(Module["systemCallback"]){return Asyncify.handleAsync(function(){return Module["systemCallback"](UTF8ToString(command))})}return-52';
    const oldPatchBare = 'if(!command)return 0;if(Module["systemCallback"]){return Module["systemCallback"](UTF8ToString(command))}return-52';

    // Check for the SPECIFIC combined pattern that includes systemCallbackSync
    if (patched.includes('Module["systemCallbackSync"]')) {
        console.log(`  [${filename}] systemCallback already patched with systemCallbackSync — skipping`);
    } else if (patched.includes(oldPatchAsyncOnly)) {
        // Upgrade: add sync fast-path before the existing Asyncify path
        patched = patched.replace(oldPatchAsyncOnly, replacement);
        patchCount++;
        console.log(`  [${filename}] Patched 2: upgraded systemCallback to include sync fast-path`);
    } else if (patched.includes(oldPatchBare)) {
        // Upgrade old bare patch
        patched = patched.replace(oldPatchBare, replacement);
        patchCount++;
        console.log(`  [${filename}] Patched 2: upgraded bare systemCallback to sync+async`);
    } else if (patched.includes(needle)) {
        patched = patched.replace(needle, replacement);
        patchCount++;
        console.log(`  [${filename}] Patched 2: systemCallback hook with sync fast-path + Asyncify`);
    } else {
        console.warn(`  [${filename}] Warning: __emscripten_system exists but browser fallback pattern not found — skipped`);
    }

    return patched;
}

/**
 * Patch 7: Fix broken callMain that ignores arguments.
 *
 * Some Emscripten builds generate a simplified callMain() that hardcodes
 * argc=0 and argv=0, ignoring the args parameter entirely. This happens
 * when the linker produces a reduced callMain stub (e.g. ninja.mjs).
 *
 * We detect the pattern `var argc=0;var argv=0;` and replace callMain
 * with a full implementation that properly marshals arguments into WASM
 * memory. Since these builds typically lack stackAlloc/stringToUTF8OnStack,
 * we grow WASM memory by 1 page (64KB) to get safe scratch space.
 */
function patchCallMainArgs(content: string, filename: string): string {
    // Upgrade pass: if an older version of this patch is already present
    // (lacking the Asyncify.currData Promise return), rewrite it in-place.
    // The old body ended with: try{var ret=entryFunction(argc,argv);exitJS(ret,true);return ret}catch(e){return handleException(e)}
    const oldTail = 'try{var ret=entryFunction(argc,argv);exitJS(ret,true);return ret}catch(e){return handleException(e)}';
    const newTail = 'try{var ret=entryFunction(argc,argv);if(typeof Asyncify!=="undefined"&&Asyncify.currData){return Asyncify.whenDone().then(function(r){try{exitJS(r,true);return r}catch(e){return handleException(e)}},function(e){return handleException(e)});}exitJS(ret,true);return ret}catch(e){return handleException(e)}';
    if (content.includes('args.unshift(thisProgram)') && content.includes(oldTail)) {
        content = content.replace(oldTail, newTail);
        patchCount++;
        console.log(`  [${filename}] Upgraded callMain: await Asyncify.whenDone() when unwinding`);
        return content;
    }

    const brokenPattern = 'var argc=0;var argv=0;';

    if (!content.includes(brokenPattern)) {
        return content;
    }

    if (content.includes('args.unshift(thisProgram)')) {
        console.log(`  [${filename}] callMain args already patched — skipping`);
        return content;
    }

    // Replace the entire broken callMain function.
    // The broken pattern: function callMain(){...var argc=0;var argv=0;...}
    // We replace just the body between the opening { and the matching }.
    const fnStart = content.indexOf('function callMain()');
    if (fnStart === -1) {
        console.warn(`  [${filename}] Warning: broken argc=0 pattern found but callMain() not found — skipping`);
        return content;
    }

    // Find the start of the function body
    const bodyStart = content.indexOf('{', fnStart);
    if (bodyStart === -1) return content;

    // Find the matching closing brace by counting braces
    let depth = 0;
    let bodyEnd = -1;
    for (let i = bodyStart; i < content.length; i++) {
        if (content[i] === '{') depth++;
        else if (content[i] === '}') {
            depth--;
            if (depth === 0) { bodyEnd = i; break; }
        }
    }
    if (bodyEnd === -1) return content;

    // Build the replacement callMain with proper arg handling.
    // Uses wasmMemory.grow(1) for scratch space since stackAlloc may be unavailable.
    const newCallMain = [
        'function callMain(args=[])',
        '{',
        'var entryFunction=_main;',
        'args.unshift(thisProgram);',
        'var argc=args.length;',
        // Grow memory by 1 page (64KB) to get safe scratch space
        'var oldPages=(wasmMemory.buffer.byteLength/65536)|0;',
        'wasmMemory.grow(1);',
        'updateMemoryViews();',
        'var scratch=oldPages*65536;',
        'var argv=scratch;',
        'var strBase=scratch+(argc+1)*4;',
        'for(var i=0;i<argc;i++){',
        'HEAPU32[(argv>>2)+i]=strBase;',
        'var len=lengthBytesUTF8(args[i])+1;',
        'stringToUTF8Array(args[i],HEAPU8,strBase,len);',
        'strBase+=len;',
        '}',
        'HEAPU32[(argv>>2)+argc]=0;',
        'try{var ret=entryFunction(argc,argv);',
        // If Asyncify unwound during main (e.g. async openat hook), the WASM
        // hasn't actually finished yet — we must return a Promise that resolves
        // once Asyncify rewinds and the program truly exits. Without this, the
        // orchestrator sees exitCode=0 immediately and tears down the instance
        // before any file writes happen, producing silent 15-55ms "success"
        // runs with no output artifact.
        'if(typeof Asyncify!=="undefined"&&Asyncify.currData){',
        'return Asyncify.whenDone().then(function(r){try{exitJS(r,true);return r}catch(e){return handleException(e)}},function(e){return handleException(e)});',
        '}',
        'exitJS(ret,true);',
        'return ret}catch(e){return handleException(e)}',
        '}',
    ].join('');

    content = content.substring(0, fnStart) + newCallMain + content.substring(bodyEnd + 1);
    patchCount++;
    console.log(`  [${filename}] Patched 7: callMain args handling (was broken: argc=0, argv=0)`);
    return content;
}

/**
 * Patch 8+9: Intercept ___syscall_openat for VFS on-demand loading + subprocess dispatch.
 *
 * Two async hooks are injected into the try-block body of ___syscall_openat:
 *
 * Hook A — VFS on-demand file loading (ALL tools):
 *   Checks Module["isCachedSync"](path). If the file is already in the VFSFS
 *   fileData map (pre-warmed or previously fetched), isCachedSync returns true
 *   and we fall through to FS.open() directly (no Asyncify overhead).
 *   If the file is not cached, Module["onPreOpen"](path) is called via
 *   Asyncify.handleAsync. onPreOpen fetches the file from IDB/CDN and places
 *   it in fileData before FS.open() runs synchronously.
 *
 * Hook B — Subprocess dispatch (Python only, via Module["subprocessDispatch"]):
 *   When Python's subprocess shim opens /tmp/__dispatch_subprocess__, dispatch
 *   a child process through the host's tool runner via Asyncify.handleAsync.
 *   Runs before the VFS hook so the magic path is intercepted first.
 *
 * Ordering: subprocess dispatch → VFS hook → sync FS.open() fallback.
 */
function patchOpenat(content: string, filename: string): string {
    const needle = 'path=SYSCALLS.getStr(path);path=SYSCALLS.calculateAt(dirfd,path);var mode=varargs?syscallGetVarargI():0;';

    if (!content.includes(needle)) {
        if (content.includes('onPreOpen')) {
            console.log(`  [${filename}] openat already patched — skipping`);
        }
        return content;
    }

    // Hook B: subprocess dispatch (Python's /tmp/__dispatch_subprocess__ magic path)
    const subprocessBlock =
        'if(path==="/tmp/__dispatch_subprocess__"&&Module["subprocessDispatch"]){' +
        'return Asyncify.handleAsync(function(){' +
        'return Module["subprocessDispatch"]().then(function(){' +
        'return FS.open(path,flags,mode).fd})})' +
        '}';

    // Hook A: VFS on-demand loading — bypass when isCachedSync returns true
    const vfsBlock =
        'if((!Module["isCachedSync"]||!Module["isCachedSync"](path))&&Module["onPreOpen"]){' +
        'return Asyncify.handleAsync(function(){' +
        'return Module["onPreOpen"](path).then(function(){' +
        'return FS.open(path,flags,mode).fd})})' +
        '}';

    const replacement = needle + subprocessBlock + vfsBlock;

    patchCount++;
    console.log(`  [${filename}] Patched 8+9: openat VFS hook + subprocess dispatch via Asyncify`);
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

    // All tools get ENV merge and callMain args fix patches
    content = patchEnvMerge(content, filename);
    content = patchCallMainArgs(content, filename);

    // Apply systemCallback patch to any tool that has __emscripten_system
    // (the function self-guards by checking for that symbol)
    content = patchSystemCallback(content, filename);

    // Apply openat VFS hook + subprocess dispatch (all tools; subprocess block self-guards)
    content = patchOpenat(content, filename);

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

// Patch files in build/cdn/usr/lib/ (used by generate-bundles to create .tar.br)
const CDN_LIB = path.join(BUILD_DIR, 'cdn', 'usr', 'lib');
if (fs.existsSync(CDN_LIB)) {
    console.log('');
    console.log(`Patching files in ${CDN_LIB}/...`);
    for (const tool of ALL_TOOLS) {
        const mjsPath = path.join(CDN_LIB, `${tool}.mjs`);
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
