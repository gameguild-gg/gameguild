/**
 * Build LLVM/Clang/LLD tools as standalone Emscripten WASM modules.
 *
 * Each tool (clang, lld, llvm-nm, llvm-ar, etc.) is compiled as a standalone
 * module that statically links LLVM. No SIDE_MODULE, no shared libraries,
 * no libLLVM.so.wasm.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { detectLLVMGitCommit, detectLLVMVersion, resolveAvailableLLVMRelease } from './lib/detect-versions.ts';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-llvm');

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = path.resolve(__dirname, '..');
const BUILD_DIR = path.join(ROOT, 'build');
const SYSROOT_LIB = path.join(ROOT, 'sysroot/usr/lib');
const LLVM_DIR = path.join(ROOT, 'userland/llvm');

// Ensure shell commands fail on error
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;

// Setup EMSDK first so we can detect the bundled LLVM version
setupEmsdk(EMSDK_VERSION);

// Detect LLVM version from emsdk. If the exact version (or same-major
// release) has a published tarball we use that; otherwise we clone from
// the llvm-project git repo at the exact commit the emsdk was built with.
const DETECTED_LLVM = process.env.LLVM_VERSION || detectLLVMVersion();
const LLVM_GIT_COMMIT = detectLLVMGitCommit();
let LLVM_VERSION: string;
let LLVM_USE_GIT = false;
try {
    LLVM_VERSION = process.env.LLVM_VERSION || resolveAvailableLLVMRelease(PINNED.LLVM_VERSION);
} catch {
    // No released tarball available — fall back to git clone
    LLVM_VERSION = DETECTED_LLVM;
    LLVM_USE_GIT = true;
}
// If the resolved version's major doesn't match the detected major, use git
if (parseInt(LLVM_VERSION) !== parseInt(DETECTED_LLVM)) {
    console.log(`    Resolved LLVM ${LLVM_VERSION} does not match detected major ${DETECTED_LLVM.split('.')[0]}; using git clone instead.`);
    LLVM_VERSION = DETECTED_LLVM;
    LLVM_USE_GIT = true;
}
const LLVM_SRC_DIR = LLVM_USE_GIT ? 'llvm-project-git' : `llvm-project-${LLVM_VERSION}.src`;
const LLVM_TARBALL = `llvm-project-${LLVM_VERSION}.src.tar.xz`;
const LLVM_URL = `https://github.com/llvm/llvm-project/releases/download/llvmorg-${LLVM_VERSION}/${LLVM_TARBALL}`;
const LLVM_GIT_URL = 'https://github.com/llvm/llvm-project.git';
const CONCURRENCY = Number(process.env.EMCEPTION_BUILD_CONCURRENCY || os.cpus().length);

function ensureCMakeBuildDirectory(buildDir: string): void {
    const relative = path.relative(LLVM_DIR, buildDir);
    if (!relative || relative.startsWith('..') || path.isAbsolute(relative)) {
        throw new Error(`Refusing to manage LLVM build directory outside ${LLVM_DIR}: ${buildDir}`);
    }

    const cachePath = path.join(buildDir, 'CMakeCache.txt');
    if (fs.existsSync(cachePath)) {
        const hasBuildSystem = fs.existsSync(path.join(buildDir, 'build.ninja'))
            || fs.existsSync(path.join(buildDir, 'Makefile'))
            || fs.readdirSync(buildDir).some(entry => entry.endsWith('.sln') || entry.endsWith('.slnx'));
        if (!hasBuildSystem) {
            console.log(`Resetting ${relative}: cached CMake configuration is incomplete.`);
            fs.rmSync(buildDir, { recursive: true, force: true });
        }
    }
    fs.mkdirSync(buildDir, { recursive: true });
}

function nativeToolPath(buildDir: string, name: string): string {
    const suffix = process.platform === 'win32' ? '.exe' : '';
    const executable = `${name}${suffix}`;
    const candidates = [
        path.join(buildDir, 'bin', executable),
        path.join(buildDir, 'Release', 'bin', executable),
        path.join(buildDir, 'bin', 'Release', executable),
    ];
    const resolved = candidates.find(candidate => fs.existsSync(candidate));
    if (!resolved) {
        throw new Error(`Native LLVM tool was not produced: ${name} (${candidates.join(', ')})`);
    }
    return resolved;
}

function writeResponseFile(responseFile: string, argumentsList: string[]): boolean {
    const content = argumentsList.map(argument => JSON.stringify(argument)).join('\n');
    const previous = fs.existsSync(responseFile) ? fs.readFileSync(responseFile, 'utf8') : undefined;
    if (previous === content) {
        return false;
    }
    fs.writeFileSync(responseFile, content);
    return true;
}

function linkArtifactsAreCurrent(outputs: string[], inputs: string[], responseChanged: boolean): boolean {
    if (responseChanged || outputs.some(output => !fs.existsSync(output) || fs.statSync(output).size === 0)) {
        return false;
    }
    const oldestOutput = Math.min(...outputs.map(output => fs.statSync(output).mtimeMs));
    return inputs.every(input => fs.existsSync(input) && fs.statSync(input).mtimeMs <= oldestOutput);
}

/** Common Emscripten flags for standalone tool modules */
const STANDALONE_FLAGS = [
    '-sALLOW_MEMORY_GROWTH=1',
    '-sMAXIMUM_MEMORY=2147483648',
    '-sSTACK_SIZE=8388608',           // 8 MB stack — clang does deep recursion during parsing
    '-sFORCE_FILESYSTEM=1',
    '-sMODULARIZE=1',
    '-sEXPORT_ES6=1',
    '-sEXIT_RUNTIME=1',
    '-sINVOKE_RUN=0',
    '-sEXPORTED_FUNCTIONS=_main',     // Keep main despite -Os (DCE strips it otherwise)
    '-sEXPORTED_RUNTIME_METHODS=FS,callMain',
    // Emscripten ports needed by LLVM
    '-sUSE_ZLIB=1',    // LLVMSupport uses zlib for compression
    // Enable Emscripten JS-based exception handling (compatible with Asyncify).
    // Native -fwasm-exceptions requires reference-types, which conflicts with
    // -mno-reference-types needed for Asyncify instrumentation.
    '-sDISABLE_EXCEPTION_CATCHING=0',
    // Asyncify: instrument the binary so async JS imports (FS hooks,
    // subprocess dispatch) transparently suspend/resume the WASM stack.
    // Works in ALL browsers (Chrome, Safari, Firefox) — unlike JSPI.
    '-sASYNCIFY',
    '-sASYNCIFY_STACK_SIZE=131072',   // 128 KB — deep IR recursion in clang
    `-sASYNCIFY_IMPORTS=${JSON.stringify([
        '__syscall_openat', '__syscall_stat64', '__syscall_lstat64',
        '__syscall_faccessat', '__syscall_readlinkat', '__syscall_newfstatat',
        '__emscripten_system',
    ])}`,
    // Disable reference-types — incompatible with asyncify instrumentation
    '-mno-reference-types',
].join(' ');

async function main() {
    console.log(`>>> Building LLVM ${LLVM_VERSION} (standalone micro-kernel modules)...`);
    console.log(`Using ${CONCURRENCY} threads.`);

    if (process.platform === 'win32' && !shell.which('bash')) {
        console.warn('Skipping LLVM build on Windows (bash not found).');
        process.exit(0);
    }

    // 1. Setup LLVM Source
    setupSource();

    // 2. Build Native TableGen
    buildNativeTableGen();

    // 3. Configure and build static LLVM libraries
    buildStaticLLVM();

    // 4. Build Clang (standalone)
    buildClang();

    // 5. Build LLD (standalone)
    buildLLD();

    // 6. Move artifacts
    moveArtifacts();

    console.log('>>> LLVM build complete.');
}

function setupSource() {
    shell.mkdir('-p', LLVM_DIR);
    shell.cd(LLVM_DIR);
    if (!fs.existsSync(LLVM_SRC_DIR)) {
        if (LLVM_USE_GIT) {
            const commit = LLVM_GIT_COMMIT;
            if (!commit) {
                throw new Error(
                    'Cannot determine LLVM git commit from emsdk clang. ' +
                    'Set LLVM_VERSION env var to a specific released version.',
                );
            }
            console.log(`Cloning LLVM from git at commit ${commit}...`);
            // Shallow clone to save time/space, then checkout the exact commit
            shell.exec(`git clone --depth 1 "${LLVM_GIT_URL}" "${LLVM_SRC_DIR}"`);
            shell.cd(LLVM_SRC_DIR);
            shell.exec(`git fetch --depth 1 origin ${commit}`);
            shell.exec(`git checkout ${commit}`);
            shell.cd(LLVM_DIR);
        } else {
            if (!fs.existsSync(LLVM_TARBALL)) {
                console.log(`Downloading LLVM source from ${LLVM_URL}...`);
                shell.exec(`curl -fSL -o "${LLVM_TARBALL}" "${LLVM_URL}"`);
            }
            console.log('Extracting LLVM source...');
            shell.exec(`tar -xf "${LLVM_TARBALL}"`);
        }
    } else {
        console.log('LLVM source already present.');
    }
}

function buildNativeTableGen() {
    console.log('>>> Building native llvm-tblgen and clang-tblgen...');
    const nativeBuildDir = path.join(LLVM_DIR, 'build-native');
    ensureCMakeBuildDirectory(nativeBuildDir);

    if (!fs.existsSync(path.join(nativeBuildDir, 'CMakeCache.txt'))) {
        const sourceDir = path.join(LLVM_DIR, LLVM_SRC_DIR, 'llvm');
        const cmd = [
            'cmake',
            `-S "${sourceDir}"`,
            `-B "${nativeBuildDir}"`,
            '-DCMAKE_BUILD_TYPE=Release',
            '-DLLVM_TARGETS_TO_BUILD="WebAssembly"',
            '-DLLVM_ENABLE_PROJECTS="clang;lld"',
            '-DLLVM_INCLUDE_TESTS=OFF',
            '-DLLVM_INCLUDE_BENCHMARKS=OFF',
            '-DLLVM_INCLUDE_EXAMPLES=OFF',
        ].join(' ');
        shell.exec(cmd);
    }

    shell.exec(`cmake --build "${nativeBuildDir}" --config Release --parallel ${CONCURRENCY} --target llvm-tblgen clang-tblgen`);
    shell.cd(LLVM_DIR);
}

function buildStaticLLVM() {
    console.log('>>> Building static LLVM libraries...');
    const wasmBuildDir = path.join(LLVM_DIR, 'build-wasm');
    const nativeBuildDir = path.join(LLVM_DIR, 'build-native');

    const llvmTblGen = nativeToolPath(nativeBuildDir, 'llvm-tblgen');
    const clangTblGen = nativeToolPath(nativeBuildDir, 'clang-tblgen');

    ensureCMakeBuildDirectory(wasmBuildDir);

    // Configure — static build only, no SIDE_MODULE, no shared libs
    if (!fs.existsSync(path.join(wasmBuildDir, 'CMakeCache.txt'))) {
        console.log('Configuring LLVM for WebAssembly (static)...');
        const sourceDir = path.join(LLVM_DIR, LLVM_SRC_DIR, 'llvm');
        const cmd = [
            'emcmake cmake',
            `-S "${sourceDir}"`,
            `-B "${wasmBuildDir}"`,
            '-DCMAKE_BUILD_TYPE=Release',
            '-DCMAKE_CXX_FLAGS=""',
            '-DCMAKE_C_FLAGS=""',
            '-DLLVM_TARGETS_TO_BUILD="WebAssembly"',
            '-DLLVM_ENABLE_PROJECTS="clang;lld"',
            `-DLLVM_TABLEGEN="${llvmTblGen}"`,
            `-DCLANG_TABLEGEN="${clangTblGen}"`,
            '-DCMAKE_CROSSCOMPILING=ON',
            '-DLLVM_HOST_TRIPLE="wasm32-unknown-emscripten"',
            '-DLLVM_DEFAULT_TARGET_TRIPLE="wasm32-unknown-emscripten"',
            '-DLLVM_ENABLE_THREADS=OFF',
            '-DLLVM_ENABLE_PIC=OFF',
            '-DLLVM_INCLUDE_TESTS=OFF',
            '-DLLVM_INCLUDE_BENCHMARKS=OFF',
            '-DLLVM_INCLUDE_EXAMPLES=OFF',
            '-DBUILD_SHARED_LIBS=OFF',
            '-DLLVM_BUILD_LLVM_DYLIB=OFF',
            '-DLLVM_LINK_LLVM_DYLIB=OFF',
            '-DUNIX=1',
        ].join(' ');

        shell.exec(cmd);
    }

    console.log('Building LLVM static libraries...');
    shell.exec(`cmake --build "${wasmBuildDir}" --parallel ${CONCURRENCY} --target llvm-libraries`);
}

function buildClang() {
    console.log('>>> Building clang.wasm (standalone)...');
    const wasmBuildDir = path.join(LLVM_DIR, 'build-wasm');

    // Build clang tablegen targets and static libraries
    console.log('Building clang tablegen targets...');
    shell.exec(`cmake --build "${wasmBuildDir}" --parallel ${CONCURRENCY} --target clang-tablegen-targets`);

    console.log('Building clang static libraries...');
    shell.exec(`cmake --build "${wasmBuildDir}" --parallel ${CONCURRENCY} --target clang-libraries`);

    // Build clang driver object files — the driver consists of several .cpp files:
    //   driver.cpp     → clang_main()
    //   cc1_main.cpp   → cc1_main()     (compiler frontend)
    //   cc1as_main.cpp → cc1as_main()   (assembler frontend)
    //   cc1gen_reproducer_main.cpp       (crash reproducer)
    //   clang-driver.cpp (auto-generated) → main() wrapper calling clang_main()
    console.log('Building clang driver objects...');
    const driverSubdir = path.join(wasmBuildDir, 'tools/clang/tools/driver');
    const driverObjDir = path.join(driverSubdir, 'CMakeFiles/clang.dir');

    const requiredObjs = [
        'driver.cpp.o',
        'cc1_main.cpp.o',
        'cc1as_main.cpp.o',
        'cc1gen_reproducer_main.cpp.o',
        'clang-driver.cpp.o',
    ];
    const driverTargets = requiredObjs.map(obj => `tools/clang/tools/driver/CMakeFiles/clang.dir/${obj}`);
    shell.exec(`cmake --build "${wasmBuildDir}" --parallel ${CONCURRENCY} --target ${driverTargets.join(' ')}`);

    // Verify they all exist
    for (const obj of requiredObjs) {
        if (!fs.existsSync(path.join(driverObjDir, obj))) {
            console.error(`ERROR: Clang driver object not found: ${obj}`);
            process.exit(1);
        }
    }

    // Collect all driver objects for linking
    const allDriverObjects = fs.readdirSync(driverObjDir)
        .filter(entry => entry.endsWith('.o'))
        .sort()
        .map(entry => path.join(driverObjDir, entry));

    console.log('Linking clang.wasm (standalone)...');
    const clangMjs = path.join(BUILD_DIR, 'clang.mjs');
    const clangWasm = path.join(BUILD_DIR, 'clang.wasm');
    const libDir = path.join(wasmBuildDir, 'lib');
    shell.mkdir('-p', BUILD_DIR);

    // Find LLVM static libraries
    const allLLVMLibs = fs.readdirSync(libDir)
        .filter(entry => entry.startsWith('libLLVM') && entry.endsWith('.a'))
        .sort()
        .map(entry => path.join(libDir, entry));
    if (allLLVMLibs.length === 0) {
        console.error('ERROR: No libLLVM*.a found');
        process.exit(1);
    }

    // Exclude libLLVMOptDriver.a — it defines duplicate cl::opt registrations
    // (e.g. "pgo-cold-func-opt") that conflict with clang's BackendUtil.cpp,
    // causing an abort during __wasm_call_ctors.
    // NOTE: Do NOT exclude Windows driver libs — clangDriver references them.
    const excludedLLVMLibs = new Set([
        'libLLVMOptDriver.a',        // conflicts with clang CodeGen (pgo-cold-func-opt)
    ]);
    const llvmLibs = allLLVMLibs.filter(l => !excludedLLVMLibs.has(path.basename(l)));
    console.log(`  Using ${llvmLibs.length}/${allLLVMLibs.length} LLVM libs (excluded: ${[...excludedLLVMLibs].join(', ')})`);

    // Find clang static libraries (exclude combined libs to avoid duplicate symbols)
    const allClangLibs = fs.readdirSync(libDir)
        .filter(entry => entry.startsWith('libclang') && entry.endsWith('.a'))
        .sort()
        .map(entry => path.join(libDir, entry));
    const clangLibs = allClangLibs.filter(l => {
        const name = path.basename(l);
        return name !== 'libclang-cpp.a' && name !== 'libclang.a';
    });
    if (clangLibs.length === 0) {
        console.error('ERROR: No libclang*.a found');
        process.exit(1);
    }

    const linkArguments = [
        ...STANDALONE_FLAGS.split(' '),
        ...allDriverObjects,
        '-Wl,--whole-archive',
        '-Wl,--start-group',
        ...clangLibs,
        ...llvmLibs,
        '-Wl,--end-group',
        '-Wl,--no-whole-archive',
        '-Os',
        '-o',
        clangMjs,
    ];
    const responseFile = path.join(BUILD_DIR, 'clang-link.rsp');
    const responseChanged = writeResponseFile(responseFile, linkArguments);

    if (linkArtifactsAreCurrent([clangMjs, clangWasm], [...allDriverObjects, ...clangLibs, ...llvmLibs, responseFile], responseChanged)) {
        console.log('Clang link artifacts are current.');
        return;
    }
    shell.exec(`em++ @"${responseFile}"`);
    console.log(`Created ${clangWasm} + ${clangMjs}`);
}

function buildLLD() {
    console.log('>>> Building lld.wasm (standalone)...');
    const wasmBuildDir = path.join(LLVM_DIR, 'build-wasm');

    if (!fs.existsSync(path.join(wasmBuildDir, 'CMakeCache.txt'))) {
        console.warn('LLD step detected missing build-wasm directory; reconfiguring LLVM wasm build...');
        buildStaticLLVM();
    }

    // Build lld static libraries
    console.log('Building lld static libraries...');
    shell.exec(`cmake --build "${wasmBuildDir}" --parallel ${CONCURRENCY} --target lld-libraries`);

    console.log('Ensuring lld driver objects are built...');
    const lldSubdir = path.join(wasmBuildDir, 'tools/lld/tools/lld');
    const lldObjDir = path.join(lldSubdir, 'CMakeFiles/lld.dir');
    const requiredObjs = ['lld.cpp.o', 'lld-driver.cpp.o'];
    const lldTargets = requiredObjs.map(obj => `tools/lld/tools/lld/CMakeFiles/lld.dir/${obj}`);
    shell.exec(`cmake --build "${wasmBuildDir}" --parallel ${CONCURRENCY} --target ${lldTargets.join(' ')}`);
    for (const obj of requiredObjs) {
        if (!fs.existsSync(path.join(lldObjDir, obj))) {
            console.error(`ERROR: LLD driver object not found: ${obj}`);
            process.exit(1);
        }
    }
    const objects = fs.readdirSync(lldObjDir)
        .filter(entry => entry.endsWith('.o'))
        .sort()
        .map(entry => path.join(lldObjDir, entry));

    console.log('Linking lld.wasm (standalone)...');
    const lldMjs = path.join(BUILD_DIR, 'lld.mjs');
    const lldWasm = path.join(BUILD_DIR, 'lld.wasm');
    const libDir = path.join(wasmBuildDir, 'lib');

    // Find LLVM static libraries
    const allLLVMLibs = fs.readdirSync(libDir)
        .filter(entry => entry.startsWith('libLLVM') && entry.endsWith('.a'))
        .sort()
        .map(entry => path.join(libDir, entry));

    // Exclude libLLVMOptDriver.a (duplicate cl::opt registrations)
    const excludedLLVMLibs = new Set([
        'libLLVMOptDriver.a',
    ]);
    const llvmLibs = allLLVMLibs.filter(library => !excludedLLVMLibs.has(path.basename(library)));

    // Find lld static libraries
    const lldLibs = fs.readdirSync(libDir)
        .filter(entry => entry.startsWith('liblld') && entry.endsWith('.a'))
        .sort()
        .map(entry => path.join(libDir, entry));
    if (lldLibs.length === 0) {
        console.error('ERROR: No liblld*.a found');
        process.exit(1);
    }

    const linkArguments = [
        ...STANDALONE_FLAGS.split(' '),
        ...objects,
        '-Wl,--whole-archive',
        '-Wl,--start-group',
        ...lldLibs,
        ...llvmLibs,
        '-Wl,--end-group',
        '-Wl,--no-whole-archive',
        '-Os',
        '-o',
        lldMjs,
    ];
    const responseFile = path.join(BUILD_DIR, 'lld-link.rsp');
    const responseChanged = writeResponseFile(responseFile, linkArguments);
    if (linkArtifactsAreCurrent([lldMjs, lldWasm], [...objects, ...lldLibs, ...llvmLibs, responseFile], responseChanged)) {
        console.log('LLD link artifacts are current.');
        return;
    }
    shell.exec(`em++ @"${responseFile}"`);
    console.log(`Created ${lldWasm} + ${lldMjs}`);
}

function moveArtifacts() {
    console.log('Moving artifacts to sysroot...');
    shell.mkdir('-p', SYSROOT_LIB);

    // Each tool produces .wasm + .mjs (no shared library)
    const artifacts = [
        'clang',
        'lld',
    ];

    for (const art of artifacts) {
        for (const ext of ['.wasm', '.mjs']) {
            const src = path.join(BUILD_DIR, `${art}${ext}`);
            const dst = path.join(SYSROOT_LIB, `${art}${ext}`);
            if (fs.existsSync(src)) {
                console.log(`Copying ${art}${ext} to sysroot...`);
                shell.cp('-f', src, dst);
            } else {
                console.error(`ERROR: ${art}${ext} missing in build directory`);
                process.exit(1);
            }
        }
    }
}

main().catch(e => {
    console.error(e);
    process.exit(1);
});
