/**
 * Build rustc as a WebAssembly binary for browser/WASI execution.
 *
 * This script follows these steps:
 *   1. Download / clone the Rust compiler source
 *   2. Configure with config.toml (selected codegen backend)
 *   3. Build a native stage-1 rustc (for cross-compilation host tools)
 *   4. Copy pre-built rustup rlibs into Emception sysroot
 *   5. Cross-compile rustc itself to wasm32-wasip1 via cargo build
 *   6. Deploy rustc.wasm + sysroot to the Emception sysroot
 *
 * The compiler and user program target are aligned on wasm32-wasip1.
 *
 * Backend selection:
 *   - default: cranelift
 *   - optional: llvm (set RUST_CODEGEN_BACKEND=llvm)
 *
 * Run: tsx scripts/build-rustc.ts
 *
 * Environment variables:
 *   RUST_VERSION    - Rust version tag to checkout (default: '1.84.0')
 *   RUST_CHANNEL    - 'stable' | 'beta' | 'nightly' (default: 'nightly')
 *   RUST_COMMIT     - Exact git commit to checkout (overrides RUST_VERSION)
 *   SKIP_NATIVE     - Set to '1' to skip native stage-1 build (if already done)
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { setupEmsdk } from './lib/emsdk.ts';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = path.resolve(__dirname, '..');
const SYSROOT_RUST = path.join(ROOT, 'sysroot/usr/lib/rust');
const RUST_DIR = path.join(ROOT, 'userland/rust');

// Ensure shell commands fail on error
shell.config.fatal = true;

// ── Rust source configuration ──────────────────────────────────────────────
// Pin a specific version for reproducible builds.
const RUST_GIT_URL = 'https://github.com/rust-lang/rust.git';
const RUST_VERSION = process.env.RUST_VERSION || '1.84.0';
const RUST_CHANNEL = process.env.RUST_CHANNEL || 'stable';
const RUST_COMMIT = process.env.RUST_COMMIT || '';

// Source tarballs from static.rust-lang.org (stable/beta releases)
const RUST_TARBALL = `rustc-${RUST_VERSION}-src.tar.xz`;
const RUST_TARBALL_URL = `https://static.rust-lang.org/dist/rustc-${RUST_VERSION}-src.tar.xz`;
const RUST_SRC_DIR = RUST_COMMIT ? 'rust-git' : `rustc-${RUST_VERSION}-src`;
const RUST_USE_GIT = !!RUST_COMMIT || (RUST_CHANNEL === 'nightly' && (RUST_VERSION === 'nightly' || RUST_VERSION.startsWith('nightly')));
const EMSDK_VERSION = process.env.EMSDK_VERSION || 'latest';
const RUST_CODEGEN_BACKEND = (process.env.RUST_CODEGEN_BACKEND || 'llvm').toLowerCase();

// Runtime target for rustc.wasm itself:
//   'emscripten' — Emscripten standalone module (.mjs + .wasm), like clang/lld.
//                  Required for LLVM backend (reuses existing LLVM wasm libs).
//   'wasi'       — WASI binary (pure .wasm), loaded via custom WASI shim.
//                  Only works with cranelift backend.
const RUST_RUNTIME = (process.env.RUST_RUNTIME || (RUST_CODEGEN_BACKEND === 'llvm' ? 'emscripten' : 'wasi')).toLowerCase() as 'emscripten' | 'wasi';
const RUST_TARGET = RUST_RUNTIME === 'emscripten' ? 'wasm32-unknown-emscripten' : 'wasm32-wasip1';
const USER_PROGRAM_TARGET = 'wasm32-wasip1';

const CONCURRENCY = os.cpus().length;

interface RustToolchainInfo {
    toolchain: string;
    cfgVersion: string;
    release: string;
    channel: string;
    commitHash: string;
    commitDate: string;
}

let selectedRustToolchain: RustToolchainInfo | null = null;

function resolveRustToolchainInfo(): RustToolchainInfo {
    if (selectedRustToolchain) return selectedRustToolchain;

    const exactToolchain = `${RUST_VERSION}-${HOST_TRIPLE}`;
    const listOut = shell.exec('rustup toolchain list', { silent: true }).stdout;
    const installed = listOut
        .split(/\r?\n/)
        .map(line => line.trim())
        .filter(Boolean)
        .map(line => line.split(/\s+/)[0]);

    if (!installed.includes(exactToolchain)) {
        console.log(`Installing Rust toolchain ${RUST_VERSION} for ${HOST_TRIPLE}...`);
        shell.exec(`rustup toolchain install ${RUST_VERSION}`);
    }

    // Ensure wasm target is present for the selected toolchain.
    shell.exec(`rustup target add ${RUST_TARGET} --toolchain ${exactToolchain}`);

    const rustcVer = shell.exec(`rustup run ${exactToolchain} rustc --version`, { silent: true }).stdout.trim();
    // Example: "rustc 1.84.0 (9fc6b4312 2025-01-07)"
    const m = rustcVer.match(/^rustc\s+([^\s]+)\s+\(([^\s]+)\s+([^\)]+)\)$/);
    if (!m) {
        throw new Error(`Unable to parse rustc --version output for ${exactToolchain}: ${rustcVer}`);
    }

    const cfgVersion = rustcVer.replace(/^rustc\s+/, '');
    const release = m[1].replace(/-(nightly|beta|dev).*$/, '');
    const channel = m[1].includes('nightly') ? 'nightly' : m[1].includes('beta') ? 'beta' : 'stable';
    const commitHash = m[2];
    const commitDate = m[3];

    selectedRustToolchain = {
        toolchain: exactToolchain,
        cfgVersion,
        release,
        channel,
        commitHash,
        commitDate,
    };

    console.log(`Using rustup toolchain for sysroot rlibs: ${selectedRustToolchain.toolchain}`);
    console.log(`  rustc --version: rustc ${selectedRustToolchain.cfgVersion}`);

    return selectedRustToolchain;
}

function getRustupToolchainBinDir(): string {
    const tc = resolveRustToolchainInfo();
    const rustupHome = process.env.RUSTUP_HOME || path.join(os.homedir(), '.rustup');
    return path.join(rustupHome, 'toolchains', tc.toolchain, 'bin');
}

// ── Host triple detection ──────────────────────────────────────────────────
function detectHostTriple(): string {
    const arch = os.arch();
    const platform = os.platform();
    if (platform === 'darwin') {
        return arch === 'arm64' ? 'aarch64-apple-darwin' : 'x86_64-apple-darwin';
    } else if (platform === 'linux') {
        return arch === 'arm64' ? 'aarch64-unknown-linux-gnu' : 'x86_64-unknown-linux-gnu';
    }
    throw new Error(`Unsupported platform: ${platform}-${arch}`);
}

const HOST_TRIPLE = detectHostTriple();

function getLibraryPathVarName(): string {
    if (process.platform === 'darwin') return 'DYLD_LIBRARY_PATH';
    if (process.platform === 'win32') return 'PATH';
    return 'LD_LIBRARY_PATH';
}

function findLlvmConfigPath(srcPath: string): string {
    if (process.env.LLVM_CONFIG && fs.existsSync(process.env.LLVM_CONFIG)) {
        return process.env.LLVM_CONFIG;
    }

    const systemLlvm = shell.which('llvm-config');
    if (systemLlvm) {
        return systemLlvm.toString();
    }

    const tc = resolveRustToolchainInfo();
    const rustupHome = process.env.RUSTUP_HOME || path.join(os.homedir(), '.rustup');
    const candidates = [
        path.join(rustupHome, 'toolchains', tc.toolchain, 'bin', 'llvm-config'),
        path.join(srcPath, 'build', HOST_TRIPLE, 'llvm', 'bin', 'llvm-config'),
    ];

    for (const c of candidates) {
        if (fs.existsSync(c)) return c;
    }

    throw new Error(
        `Unable to locate llvm-config. Tried PATH and: ${candidates.join(', ')}. ` +
        'Install llvm-config or run with LLVM_CONFIG=/absolute/path/to/llvm-config.'
    );
}

function findEmsdkClangCxx(): string {
    const candidates = [
        path.join(ROOT, 'tools', 'emsdk', 'upstream', 'bin', 'clang++'),
        path.join(ROOT, 'tools', 'emsdk', 'upstream', 'bin', 'clang++-23'),
    ];

    for (const candidate of candidates) {
        if (fs.existsSync(candidate)) return candidate;
    }

    const systemClangxx = shell.which('clang++');
    if (systemClangxx) {
        return systemClangxx.toString();
    }

    throw new Error('Unable to locate clang++ for wasm32-wasip1 C++ wrapper compilation.');
}

function findWasiCompatibleSysroot(): string {
    const candidates = [
        path.join(ROOT, 'tools', 'emsdk', 'upstream', 'emscripten', 'cache', 'sysroot'),
        process.env.WASI_SYSROOT || '',
    ].filter(Boolean);

    for (const candidate of candidates) {
        if (fs.existsSync(path.join(candidate, 'include', 'c++', 'v1', 'type_traits'))) {
            return candidate;
        }
    }

    throw new Error(
        'Unable to locate a WASI-compatible sysroot with libc++ headers. ' +
        'Expected emsdk cache sysroot or WASI_SYSROOT to contain include/c++/v1/type_traits.'
    );
}

function createWasiSysrootOverlay(baseSysroot: string): string {
    const overlayRoot = path.join(RUST_DIR, '.tmp', 'wasi-sysroot-overlay');
    const overlayInclude = path.join(overlayRoot, 'include');
    const overlayLib = path.join(overlayRoot, 'lib');
    const overlayTargetLib = path.join(overlayLib, 'wasm32-wasip1');
    const sourceInclude = path.join(baseSysroot, 'include');
    const sourceTargetLib = path.join(baseSysroot, 'lib', 'wasm32-emscripten');
    const emsdkRoot = path.join(ROOT, 'tools', 'emsdk', 'upstream');
    const emsdkRootLib = path.join(emsdkRoot, 'lib');

    if (!fs.existsSync(path.join(sourceInclude, 'c++', 'v1', 'type_traits'))) {
        throw new Error(`WASI sysroot include path missing libc++ headers: ${sourceInclude}`);
    }

    if (!fs.existsSync(path.join(sourceTargetLib, 'libc++abi.a'))) {
        throw new Error(`WASI sysroot lib path missing libc++abi.a: ${sourceTargetLib}`);
    }

    shell.rm('-rf', overlayRoot);
    shell.mkdir('-p', overlayTargetLib);

    fs.symlinkSync(sourceInclude, overlayInclude, 'dir');

    // IMPORTANT: do not expose the full emscripten target lib directory here.
    // rust-lld will otherwise pick up libc.a from emscripten and clash with
    // wasm32-wasip1 crt objects (import module mismatch: env vs wasi_snapshot_preview1).
    // Provide only the C++ runtime archives rustc_llvm needs.
    const runtimeLibs = ['libc++.a', 'libc++abi.a', 'libunwind.a', 'libc++experimental.a'];
    for (const lib of runtimeLibs) {
        const candidates = [
            path.join(emsdkRootLib, lib),
            path.join(sourceTargetLib, lib),
        ];
        const src = candidates.find(candidate => fs.existsSync(candidate));
        if (src) {
            const dst = path.join(overlayTargetLib, lib);
            fs.symlinkSync(src, dst, 'file');
        }
    }

    if (!fs.existsSync(path.join(overlayTargetLib, 'libc++abi.a'))) {
        throw new Error(`Overlay lib path missing libc++abi.a at ${overlayTargetLib}`);
    }

    return overlayRoot;
}

function createWasiClangWrapper(cxx: string, sysroot: string): string {
    const wrapperPath = path.join(RUST_DIR, '.tmp', 'clang++-wasip1-wrapper.sh');
    const script = `#!/bin/sh
exec "${cxx}" --target=wasm32-wasip1 --sysroot="${sysroot}" "$@"
`;
    fs.writeFileSync(wrapperPath, script, { mode: 0o755 });
    fs.chmodSync(wrapperPath, 0o755);
    return wrapperPath;
}

function ensureWasiCppToolchainReady(cxx: string, sysroot: string) {
    const probeDir = path.join(RUST_DIR, '.tmp');
    shell.mkdir('-p', probeDir);
    const probeSrc = path.join(probeDir, 'wasi-cxx-probe.cpp');
    const probeObj = path.join(probeDir, 'wasi-cxx-probe.o');
    fs.writeFileSync(probeSrc, '#include <type_traits>\nint main() { return 0; }\n');

    const cmd = `"${cxx}" --target=wasm32-wasip1 --sysroot="${sysroot}" -std=c++17 -stdlib=libc++ -c "${probeSrc}" -o "${probeObj}"`;
    const out = shell.exec(cmd, { silent: true });
    if (out.code !== 0) {
        throw new Error(
            'WASI C++ toolchain probe failed for rustc_llvm wrapper compilation.\n' +
            `Probe command: ${cmd}\n` +
            (out.stderr || out.stdout || 'No compiler diagnostics captured.')
        );
    }
}

// ════════════════════════════════════════════════════════════════════════════
//  Step 1: Download / clone Rust source
// ════════════════════════════════════════════════════════════════════════════

function setupSource() {
    console.log('>>> Setting up Rust source...');
    shell.mkdir('-p', RUST_DIR);
    shell.cd(RUST_DIR);

    const srcPath = path.join(RUST_DIR, RUST_SRC_DIR);
    if (fs.existsSync(srcPath)) {
        const hasGitDir = fs.existsSync(path.join(srcPath, '.git'));
        const sourceModeMismatch = (RUST_USE_GIT && !hasGitDir) || (!RUST_USE_GIT && hasGitDir);
        const sourceLooksIncomplete =
            !fs.existsSync(path.join(srcPath, 'Cargo.toml')) ||
            !fs.existsSync(path.join(srcPath, 'compiler'));

        if (sourceModeMismatch || sourceLooksIncomplete) {
            const expectedMode = RUST_USE_GIT ? 'git checkout' : 'release tarball';
            const actualMode = hasGitDir ? 'git checkout' : 'release tarball';
            const reason = sourceModeMismatch
                ? `mode mismatched (${actualMode} vs expected ${expectedMode})`
                : 'source tree looks incomplete (missing Cargo.toml/compiler)';
            console.log(`Rust source already present at ${RUST_SRC_DIR}, but ${reason}. Recreating source tree...`);
            shell.rm('-rf', srcPath);
        } else {
            console.log(`Rust source already present at ${RUST_SRC_DIR}.`);
            return;
        }
    }

    if (RUST_USE_GIT) {
        // Clone from git — needed for nightly / specific commits
        const commit = RUST_COMMIT || 'master';
        console.log(`Cloning Rust from git (${commit})...`);
        shell.exec(`git clone --depth 1 "${RUST_GIT_URL}" "${RUST_SRC_DIR}"`);
        shell.cd(srcPath);
        if (RUST_COMMIT) {
            shell.exec(`git fetch --depth 1 origin ${RUST_COMMIT}`);
            shell.exec(`git checkout ${RUST_COMMIT}`);
        }
        // Initialize required submodules referenced by compiler/tests.
        console.log('Initializing required submodules...');
        shell.exec('git submodule update --init --depth 1 library/stdarch');
        shell.cd(RUST_DIR);
    } else {
        // Download release tarball (stable/beta)
        const tarball = path.join(RUST_DIR, RUST_TARBALL);
        if (!fs.existsSync(tarball)) {
            console.log(`Downloading Rust source from ${RUST_TARBALL_URL}...`);
            shell.exec(`curl -fSL -o "${tarball}" "${RUST_TARBALL_URL}"`);
        }
        console.log('Extracting Rust source...');
        shell.exec(`tar -xf "${tarball}"`);
    }
}

// ════════════════════════════════════════════════════════════════════════════
//  Step 2: Write config.toml for the Rust build system (x.py)
// ════════════════════════════════════════════════════════════════════════════

function writeConfigToml() {
    console.log('>>> Writing config.toml...');
    const srcPath = path.join(RUST_DIR, RUST_SRC_DIR);
    const configPath = path.join(srcPath, 'config.toml');

    const targetSection = RUST_RUNTIME === 'emscripten'
        ? `[target.wasm32-unknown-emscripten]
# Emscripten linker — emcc is auto-detected via emsdk env.
linker = "emcc"
`
        : `[target.wasm32-wasip1]
# Target-specific linker/sysroot resolution is handled in the runtime toolchain.
`;

    const config = `\
# Auto-generated by build-rustc.ts for Emception (browser WASM target)
# Runtime: ${RUST_RUNTIME}, Backend: ${RUST_CODEGEN_BACKEND}

[rust]
# Use selected codegen backend.
codegen-backends = ["${RUST_CODEGEN_BACKEND}"]
# Faster iteration; set to true for production builds.
optimize = false
debuginfo-level = 0
# Keep rustc single-threaded for reproducibility and to avoid thread-related
# issues in the toolchain bootstrap flow.
parallel-compiler = false
# Skip codegen tests that require running compiled output
codegen-tests = false

[build]
# Native host does the cross-compilation.
host = ["${HOST_TRIPLE}"]
# rustc itself will be cross-compiled to ${RUST_TARGET} in step 5.
target = ["${HOST_TRIPLE}", "${RUST_TARGET}"]
# Build with host rustup cargo/rustc toolchain.
cargo = "${getCargoPath()}"
rustc = "${getRustcPath()}"
# Extended tools are not needed for the browser IDE
extended = false
# Disable docs to save build time
docs = false

[install]
prefix = "${path.join(RUST_DIR, 'install')}"

${targetSection}
`;

    if (fs.existsSync(configPath)) {
        const existing = fs.readFileSync(configPath, 'utf-8');
        if (existing === config) {
            console.log('config.toml already up to date, skipping.');
            return;
        }

        console.log('config.toml already exists but is stale, rewriting.');
    }

    fs.writeFileSync(configPath, config);
    console.log(`Wrote ${configPath}`);
}

/** Find cargo binary path */
function getCargoPath(): string {
    const cargo = path.join(getRustupToolchainBinDir(), 'cargo');
    if (!fs.existsSync(cargo)) {
        throw new Error(`cargo not found for selected toolchain at ${cargo}`);
    }
    return cargo;
}

/** Find rustc binary path */
function getRustcPath(): string {
    const rustc = path.join(getRustupToolchainBinDir(), 'rustc');
    if (!fs.existsSync(rustc)) {
        throw new Error(`rustc not found for selected toolchain at ${rustc}`);
    }
    return rustc;
}

// ════════════════════════════════════════════════════════════════════════════
//  Step 3: Build native stage-1 rustc (runs on the host machine)
// ════════════════════════════════════════════════════════════════════════════

function buildNativeStage1() {
    // The native stage-1 build (via x.py) is NOT required for the cargo-based
    // wasm32-wasip1 cross-compile in crossCompileRustc().
    // Skip by default; set SKIP_NATIVE=0 to opt into the full bootstrap.
    if (process.env.SKIP_NATIVE !== '0') {
        console.log('>>> Skipping native stage-1 build (set SKIP_NATIVE=0 to enable).');
        return;
    }

    console.log('>>> Building native stage-1 rustc (LLVM backend)...');
    const srcPath = path.join(RUST_DIR, RUST_SRC_DIR);
    shell.cd(srcPath);

    // x.py (or just ./x) is the Rust build system
    const xpy = fs.existsSync(path.join(srcPath, 'x')) ? './x' : 'python3 x.py';
    const tc = resolveRustToolchainInfo();
    const toolchainBin = getRustupToolchainBinDir();

    // Build stage-1 compiler with LLVM backend.
    // --skip-stage0-validation bypasses the bootstrap version check, which can
    // incorrectly detect the global rustc (1.97) instead of the pinned toolchain
    // specified in config.toml. The actual compilation uses the correct 1.84.0
    // toolchain via RUSTUP_TOOLCHAIN / PATH and config.toml [build] rustc.
    console.log('Building stage-1 rustc...');
    shell.exec(`${xpy} build compiler/rustc --stage 1 -j ${CONCURRENCY} --skip-stage0-validation`, {
        env: {
            ...process.env,
            RUSTUP_TOOLCHAIN: tc.toolchain,
            PATH: `${toolchainBin}${path.delimiter}${process.env.PATH || ''}`,
        },
    });

    console.log('Stage-1 rustc build complete.');
    shell.cd(RUST_DIR);
}

// ════════════════════════════════════════════════════════════════════════════
//  Step 4: Copy pre-built rlibs from rustup for required wasm targets
//
//  - wasm32-wasip1: rustc.wasm host runtime target and user-program output target
//  - wasm32-unknown-unknown: generic fallback target used by other presets/tests
// ════════════════════════════════════════════════════════════════════════════

function copyRlibsForTarget(tc: ReturnType<typeof resolveRustToolchainInfo>, target: string) {
    const rustupHome = process.env.RUSTUP_HOME || path.join(os.homedir(), '.rustup');
    const toolchainsDir = path.join(rustupHome, 'toolchains');
    const toolchain = tc.toolchain;

    // Ensure the target is installed for this toolchain
    console.log(`  Installing ${target} target for ${toolchain}...`);
    shell.exec(`rustup target add ${target} --toolchain ${toolchain}`, { silent: true });

    const srcRlibs = path.join(toolchainsDir, toolchain, 'lib', 'rustlib', target, 'lib');
    if (!fs.existsSync(srcRlibs)) {
        console.error(`ERROR: ${target} rlibs not found at ${srcRlibs}`);
        process.exit(1);
    }

    const dstRlibs = path.join(SYSROOT_RUST, 'lib', 'rustlib', target, 'lib');
    // IMPORTANT: remove stale files first. Mixing rlibs from different
    // rustc versions causes E0514 "compiled by an incompatible version of rustc".
    if (fs.existsSync(dstRlibs)) {
        shell.rm('-rf', path.join(dstRlibs, '*'));
    }
    shell.mkdir('-p', dstRlibs);
    shell.cp('-rf', path.join(srcRlibs, '*'), dstRlibs);

    const rlibs = shell.ls(path.join(dstRlibs, '*.rlib'));
    console.log(`  Copied ${rlibs.length} rlibs for ${target}.`);

    // Copy codegen backends if present
    const srcCodegenBackends = path.join(toolchainsDir, toolchain, 'lib', 'rustlib', target, 'codegen-backends');
    const dstCodegenBackends = path.join(SYSROOT_RUST, 'lib', 'rustlib', target, 'codegen-backends');
    if (fs.existsSync(srcCodegenBackends)) {
        if (fs.existsSync(dstCodegenBackends)) {
            shell.rm('-rf', path.join(dstCodegenBackends, '*'));
        }
        shell.mkdir('-p', dstCodegenBackends);
        shell.cp('-rf', path.join(srcCodegenBackends, '*'), dstCodegenBackends);
        const backends = shell.ls(path.join(dstCodegenBackends, '*'));
        console.log(`  Copied ${backends.length} codegen backend file(s) for ${target}.`);
    }
}

function copyWasiRlibs() {
    const tc = resolveRustToolchainInfo();

    const rustupHome = process.env.RUSTUP_HOME || path.join(os.homedir(), '.rustup');
    const toolchainsDir = path.join(rustupHome, 'toolchains');
    if (!fs.existsSync(toolchainsDir)) {
        console.error(`ERROR: rustup toolchains directory not found at ${toolchainsDir}`);
        process.exit(1);
    }

    if (!fs.existsSync(path.join(toolchainsDir, tc.toolchain))) {
        console.error(`ERROR: Toolchain not found after install: ${tc.toolchain}`);
        process.exit(1);
    }

    // Copy rlibs for rustc.wasm host runtime target
    console.log(`>>> Copying pre-built ${RUST_TARGET} rlibs from rustup...`);
    copyRlibsForTarget(tc, RUST_TARGET);

    if (USER_PROGRAM_TARGET !== RUST_TARGET) {
        // Copy rlibs for rust-terminal output target (wasi-run)
        console.log(`>>> Copying pre-built ${USER_PROGRAM_TARGET} rlibs from rustup...`);
        copyRlibsForTarget(tc, USER_PROGRAM_TARGET);
    }

    // Copy rlibs for wasm32-unknown-unknown (used by the rust-terminal preset).
    // These MUST be from the same toolchain version as rustc.wasm to avoid
    // E0514 / require_lang_item panics at compile time.
    console.log('>>> Copying pre-built wasm32-unknown-unknown rlibs from rustup...');
    copyRlibsForTarget(tc, 'wasm32-unknown-unknown');

}

// ════════════════════════════════════════════════════════════════════════════
//  Step 5: Cross-compile rustc to ${RUST_TARGET} via cargo build
// ════════════════════════════════════════════════════════════════════════════

/**
 * Detect the LLVM source directory name under userland/llvm/.
 * Checks for git clone dir first, then release tarball dirs.
 */
function LLVM_SRC_DIR_DETECT(): string {
    const llvmDir = path.join(ROOT, 'userland', 'llvm');
    const gitDir = path.join(llvmDir, 'llvm-project-git');
    if (fs.existsSync(gitDir)) return 'llvm-project-git';
    // Find tarball-based source dirs
    const entries = fs.readdirSync(llvmDir).filter(e =>
        e.startsWith('llvm-project-') && e.endsWith('.src') &&
        fs.statSync(path.join(llvmDir, e)).isDirectory()
    );
    if (entries.length > 0) return entries[0];
    return 'llvm-project-git'; // fallback
}

/**
 * Create a wrapper llvm-config script that returns wasm library paths
 * instead of native paths. Headers/includes come from the native build;
 * only --libdir and --link-static-libs are redirected to the wasm build.
 */
function createEmscriptenLlvmConfigWrapper(nativeLlvmConfig: string, wasmLibDir: string, wasmIncludeDir?: string): string {
    const wrapperPath = path.join(RUST_DIR, '.tmp', 'llvm-config-emscripten-wrapper.sh');
    const includeDir = wasmIncludeDir || path.join(ROOT, 'userland', 'llvm', 'build-wasm', 'include');
    shell.mkdir('-p', path.dirname(wrapperPath));
    const script = `#!/bin/sh
# Wrapper around native llvm-config that redirects library/include paths to wasm build.
# Generated by build-rustc.ts for Emscripten cross-compilation.

WASM_LIB_DIR="${wasmLibDir}"
WASM_INCLUDE_DIR="${includeDir}"

case "$*" in
    *--libdir*)
        echo "$WASM_LIB_DIR"
        ;;
    *--includedir*)
        echo "$WASM_INCLUDE_DIR"
        ;;
    *--targets-built*)
        # Only WebAssembly target was built for wasm
        echo "WebAssembly"
        ;;
    *--components*)
        # All components available in wasm build - must include all REQUIRED_COMPONENTS from rustc_llvm/build.rs:
        # ipo bitreader bitwriter linker asmparser lto coverage instrumentation
        # Plus webassembly target components and other general components
        echo "webassembly webassemblyasmparser webassemblycodegen webassemblydesc webassemblydisassembler webassemblyinfo webassemblyutils core support analysis bitreader bitwriter codegen ipo irreader instcombine instrumentation mc mcparser objcarcopts option passes profiledata scalaropts transformutils vectorize target lto coverage linker asmparser aggressiveinstcombine asmprinter binaryformat cfguard coroutines debuginfocodeview debuginfodwarf debuginfomsf debuginfopdb demangle extensions frontendhlsl frontendopenmp globalisel irprinter irreader jitlink mcjit mcdisassembler mirparser object remarks runtimedyld selectiondag symbolize"
        ;;
    *--has-rtti*)
        echo "YES"
        ;;
    *--cxxflags*)
        # Get native cxxflags but prepend wasm build include dir for generated .inc headers
        native_flags=$("${nativeLlvmConfig}" "$@")
        echo "-I$WASM_INCLUDE_DIR $native_flags"
        ;;
    *--libs*|*--link-static-libs*)
        # Get native lib list and filter to only those that exist in the wasm build
        native_output=$("${nativeLlvmConfig}" "$@")
        result=""
        for flag in $native_output; do
            case "$flag" in
                -l*)
                    libname=$(echo "$flag" | sed 's/^-l//')
                    if [ -f "$WASM_LIB_DIR/lib$libname.a" ]; then
                        result="$result $flag"
                    fi
                    ;;
                *)
                    result="$result $flag"
                    ;;
            esac
        done
        echo "$result"
        ;;
    *--system-libs*)
        # No system libs on Emscripten — return empty
        echo ""
        ;;
    *)
        exec "${nativeLlvmConfig}" "$@"
        ;;
esac
`;
    fs.writeFileSync(wrapperPath, script, { mode: 0o755 });
    fs.chmodSync(wrapperPath, 0o755);
    return wrapperPath;
}

// ════════════════════════════════════════════════════════════════════════════
//  Step 4b: Build LLVM 19 wasm static libraries from rustc's bundled LLVM
//
//  The Emception LLVM (llvm-project-git) is v23, but rustc 1.84.0's
//  llvm-wrapper C++ code targets LLVM 19. We must build matching wasm
//  static libraries from rustc's own src/llvm-project.
// ════════════════════════════════════════════════════════════════════════════

function buildRustcLlvmWasmLibs(): { libDir: string; includeDir: string; srcIncludeDir: string } {
    const srcPath = path.join(RUST_DIR, RUST_SRC_DIR);
    const rustcLlvmSrc = path.join(srcPath, 'src', 'llvm-project');
    const wasmBuildDir = path.join(RUST_DIR, 'build-wasm-rustc-llvm');
    const nativeBuildDir = path.join(RUST_DIR, 'build-native-rustc-llvm');

    const result = {
        libDir: path.join(wasmBuildDir, 'lib'),
        includeDir: path.join(wasmBuildDir, 'include'),
        srcIncludeDir: path.join(rustcLlvmSrc, 'llvm', 'include'),
    };

    // Check if already built
    if (fs.existsSync(result.libDir) && shell.ls(path.join(result.libDir, 'libLLVM*.a')).length > 0) {
        console.log('>>> rustc LLVM 19 wasm libraries already built.');
        return result;
    }

    if (!fs.existsSync(rustcLlvmSrc)) {
        throw new Error(
            `rustc's bundled LLVM source not found at ${rustcLlvmSrc}. ` +
            'The source tarball may not include src/llvm-project.'
        );
    }

    console.log('>>> Building LLVM 19 wasm static libraries from rustc source...');
    setupEmsdk(EMSDK_VERSION);

    // Step 1: Build native llvm-tblgen (needed for cross-compile)
    shell.mkdir('-p', nativeBuildDir);
    if (!fs.existsSync(path.join(nativeBuildDir, 'bin', 'llvm-tblgen'))) {
        console.log('  Building native llvm-tblgen for LLVM 19...');
        if (!fs.existsSync(path.join(nativeBuildDir, 'Makefile'))) {
            shell.exec(
                `cmake -S "${rustcLlvmSrc}/llvm" -B "${nativeBuildDir}" ` +
                '-DCMAKE_BUILD_TYPE=Release ' +
                '-DLLVM_TARGETS_TO_BUILD="WebAssembly" ' +
                '-DLLVM_INCLUDE_TESTS=OFF ' +
                '-DLLVM_INCLUDE_BENCHMARKS=OFF ' +
                '-DLLVM_INCLUDE_EXAMPLES=OFF'
            );
        }
        shell.exec(`make -C "${nativeBuildDir}" -j${CONCURRENCY} llvm-tblgen`);
    } else {
        console.log('  Native llvm-tblgen already built.');
    }

    const llvmTblGen = path.join(nativeBuildDir, 'bin', 'llvm-tblgen');

    // Step 2: Configure wasm build with emcmake
    shell.mkdir('-p', wasmBuildDir);
    if (!fs.existsSync(path.join(wasmBuildDir, 'Makefile'))) {
        console.log('  Configuring LLVM 19 for wasm32-unknown-emscripten...');
        shell.exec(
            `emcmake cmake -S "${rustcLlvmSrc}/llvm" -B "${wasmBuildDir}" ` +
            '-DCMAKE_BUILD_TYPE=Release ' +
            '-DCMAKE_CXX_FLAGS="" ' +
            '-DCMAKE_C_FLAGS="" ' +
            '-DLLVM_TARGETS_TO_BUILD="WebAssembly" ' +
            `-DLLVM_TABLEGEN="${llvmTblGen}" ` +
            '-DCMAKE_CROSSCOMPILING=ON ' +
            '-DLLVM_DEFAULT_TARGET_TRIPLE="wasm32-unknown-emscripten" ' +
            '-DLLVM_ENABLE_THREADS=OFF ' +
            '-DLLVM_ENABLE_PIC=OFF ' +
            '-DLLVM_INCLUDE_TESTS=OFF ' +
            '-DLLVM_INCLUDE_BENCHMARKS=OFF ' +
            '-DLLVM_INCLUDE_EXAMPLES=OFF ' +
            '-DBUILD_SHARED_LIBS=OFF ' +
            '-DLLVM_BUILD_LLVM_DYLIB=OFF ' +
            '-DLLVM_LINK_LLVM_DYLIB=OFF ' +
            '-DLLVM_ENABLE_RTTI=ON ' +
            '-DUNIX=1'
        );
    }

    // Step 3: Build static libraries
    console.log('  Building LLVM 19 static libraries (this may take a while)...');
    shell.exec(`emmake make -C "${wasmBuildDir}" -j${CONCURRENCY} llvm-libraries`);

    const libCount = shell.ls(path.join(result.libDir, 'libLLVM*.a')).length;
    console.log(`  Built ${libCount} LLVM 19 wasm static libraries.`);

    return result;
}

function crossCompileRustc() {
    console.log(`>>> Cross-compiling rustc to ${RUST_TARGET}...`);
    const srcPath = path.join(RUST_DIR, RUST_SRC_DIR);
    shell.cd(srcPath);

    // Ensure toolchain env is initialized before cargo cross-compile.
    setupEmsdk(EMSDK_VERSION);

    // Patches needed for WASI compilation:
    // 1. rustc_driver/Cargo.toml: crate-type = ["rlib"] (dylib not supported on WASI)
    // 2. rustc_metadata/Cargo.toml: libloading moved to cfg(not(wasm)) deps
    // 3. rustc_metadata/src/creader.rs: dylib loading wrapped in cfg(not(wasm))
    //
    // These patches should already be applied by setup. If not, apply them:
    applyWasiPatches(srcPath);
    upgradeWasiCfgForEmscripten(srcPath);

    // Build rustc for the selected target using cargo.
    // Env vars needed because we're building outside x.py bootstrap:
    //   RUSTC_BOOTSTRAP=1     - Allow nightly features in stable cargo
    //   CFG_RELEASE           - Version string for rustc --version
    //   CFG_RELEASE_CHANNEL   - Channel for cfg checks
    //   RUSTC_INSTALL_BINDIR  - Install directory for binaries

    const tc = resolveRustToolchainInfo();

    // ── Emscripten standalone module flags ──────────────────────────
    // These mirror STANDALONE_FLAGS from build-llvm.ts (clang/lld build).
    // Passed as -C link-arg=... in RUSTFLAGS so emcc receives them at link time.
    const EMSCRIPTEN_LINK_FLAGS = [
        '-sALLOW_MEMORY_GROWTH=1',
        '-sINITIAL_MEMORY=134217728',        // 128 MB initial (must be > STACK_SIZE)
        '-sMAXIMUM_MEMORY=2147483648',       // 2 GB max heap
        '-sSTACK_SIZE=67108864',             // 64 MB stack (rustc deep recursion)
        '-sABORTING_MALLOC=0',               // don't abort on malloc failure, grow instead
        '-sFORCE_FILESYSTEM=1',
        '-sMODULARIZE=1',
        '-sEXPORT_ES6=1',
        '-sEXIT_RUNTIME=1',
        '-sINVOKE_RUN=0',
        '-sEXPORTED_FUNCTIONS=_main',
        '-sEXPORTED_RUNTIME_METHODS=FS,callMain',
        '-sUSE_ZLIB=1',                     // LLVM uses zlib for compression
        '-sDISABLE_EXCEPTION_CATCHING=0',    // JS-based exceptions (Asyncify-compatible)
        '-sASYNCIFY',
        '-sASYNCIFY_STACK_SIZE=131072',      // 128 KB
        `-sASYNCIFY_IMPORTS=${JSON.stringify([
            '__syscall_openat', '__syscall_stat64', '__syscall_lstat64',
            '__syscall_faccessat', '__syscall_readlinkat', '__syscall_newfstatat',
            '__emscripten_system',
        ])}`,
        '-mno-reference-types',              // required for Asyncify instrumentation
    ];

    const buildEnv: Record<string, string> = {
        RUSTC_BOOTSTRAP: '1',
        CFG_VERSION: tc.cfgVersion,
        CFG_VER_HASH: tc.commitHash,
        CFG_VER_DATE: tc.commitDate,
        CFG_RELEASE: tc.release,
        CFG_RELEASE_CHANNEL: tc.channel,
        // rustc_session::config::host_tuple() uses option_env!("CFG_COMPILER_HOST_TRIPLE")
        // and panics if missing. Ensure this is baked into rustc-main.wasm at compile time.
        CFG_COMPILER_HOST_TRIPLE: RUST_TARGET,
        RUSTC_INSTALL_BINDIR: 'bin',
        // Tell rustc to default to the selected codegen backend at runtime.
        // option_env!("CFG_DEFAULT_CODEGEN_BACKEND") is checked by get_codegen_backend().
        CFG_DEFAULT_CODEGEN_BACKEND: RUST_CODEGEN_BACKEND,
        // RUSTFLAGS: target-dependent link flags
        RUSTFLAGS: '',
    };

    if (RUST_RUNTIME === 'emscripten') {
        // ── Emscripten runtime: link flags for standalone module ──────
        buildEnv.RUSTFLAGS = EMSCRIPTEN_LINK_FLAGS.map(f => `-C link-arg=${f}`).join(' ');

        // Build LLVM 19 wasm static libraries from rustc's bundled LLVM source.
        // The Emception LLVM (llvm-project-git) is v23 and incompatible with
        // rustc 1.84.0's C++ wrapper code that targets LLVM 19.
        const rustcLlvm = buildRustcLlvmWasmLibs();
        const llvmWasmLibDir = rustcLlvm.libDir;

        // Add LLVM wasm lib dir to the native library search path for linking
        buildEnv.RUSTFLAGS += ` -L native=${llvmWasmLibDir}`;

        // Create a wrapper llvm-config that returns wasm lib/include paths
        const nativeLlvmConfig = findLlvmConfigPath(srcPath);
        const llvmConfigWrapper = createEmscriptenLlvmConfigWrapper(nativeLlvmConfig, llvmWasmLibDir, rustcLlvm.includeDir);
        buildEnv.LLVM_CONFIG = llvmConfigWrapper;

        // C++ compiler for cross-compiling rustc_llvm wrapper code (RustWrapper.cpp)
        const empp = path.join(ROOT, 'tools', 'emsdk', 'upstream', 'emscripten', 'em++');
        if (!fs.existsSync(empp)) {
            console.error(`ERROR: em++ not found at ${empp}. Ensure emsdk is set up.`);
            process.exit(1);
        }
        buildEnv.CXX = empp;
        buildEnv['CXX_wasm32-unknown-emscripten'] = empp;
        buildEnv.CXX_wasm32_unknown_emscripten = empp;
        // Include dirs: rustc's LLVM 19 wasm build (generated .inc tablegen headers) + source
        const cxxflags = [
            `-I${rustcLlvm.includeDir}`,
            `-I${rustcLlvm.srcIncludeDir}`,
            '-DLLVM_BUILD_STATIC',
        ].join(' ');
        buildEnv.CXXFLAGS = cxxflags;
        buildEnv['CXXFLAGS_wasm32-unknown-emscripten'] = cxxflags;
        buildEnv.CXXFLAGS_wasm32_unknown_emscripten = cxxflags;

        // Emscripten tools path for cargo to find emcc linker
        const emsdkBin = path.join(ROOT, 'tools', 'emsdk', 'upstream', 'emscripten');
        buildEnv.PATH = `${emsdkBin}${path.delimiter}${process.env.PATH || ''}`;

        // rustc_llvm build.rs calls restore_library_path() which expects REAL_LIBRARY_PATH_VAR.
        // Set it to prevent the build script from panicking.
        const realLibraryPathVar = getLibraryPathVarName();
        buildEnv.REAL_LIBRARY_PATH_VAR = realLibraryPathVar;
        buildEnv.REAL_LIBRARY_PATH = process.env[realLibraryPathVar] || '';

        console.log(`  LLVM_CONFIG (wrapper): ${llvmConfigWrapper}`);
        console.log(`  LLVM wasm libs: ${llvmWasmLibDir}`);
        console.log(`  CXX: ${empp}`);
        console.log(`  CXXFLAGS: ${cxxflags}`);
    } else {
        // ── WASI runtime: wasm-ld link flags ─────────────────────────
        // WASM shadow stack: default 1MB is far too small for rustc's deep recursion.
        // Set to 64MB so compilation doesn't abort with a double-panic (stack overflow).
        // Also export the function table so JS invoke_* shims can dispatch indirect calls.
        buildEnv.RUSTFLAGS = '-C link-arg=-zstack-size=67108864 -C link-arg=--export-table';

        if (RUST_CODEGEN_BACKEND === 'llvm') {
            const realLibraryPathVar = getLibraryPathVarName();
            const llvmConfig = findLlvmConfigPath(srcPath);
            const wasiCxx = findEmsdkClangCxx();
            const wasiSysroot = createWasiSysrootOverlay(findWasiCompatibleSysroot());
            const wasiCxxWrapper = createWasiClangWrapper(wasiCxx, wasiSysroot);
            const wasiCxxFlags = `--sysroot=${wasiSysroot}`;

            ensureWasiCppToolchainReady(wasiCxx, wasiSysroot);

            buildEnv.REAL_LIBRARY_PATH_VAR = realLibraryPathVar;
            buildEnv.REAL_LIBRARY_PATH = process.env[realLibraryPathVar] || '';
            buildEnv.LLVM_CONFIG = llvmConfig;
            buildEnv.WASI_SYSROOT = wasiSysroot;
            buildEnv.CXX = wasiCxxWrapper;
            buildEnv['CXX_wasm32-wasip1'] = wasiCxxWrapper;
            buildEnv.CXX_wasm32_wasip1 = wasiCxxWrapper;
            buildEnv.CXXFLAGS = wasiCxxFlags;
            buildEnv['CXXFLAGS_wasm32-wasip1'] = wasiCxxFlags;
            buildEnv.CXXFLAGS_wasm32_wasip1 = wasiCxxFlags;

            console.log(`  LLVM_CONFIG: ${llvmConfig}`);
            console.log(`  REAL_LIBRARY_PATH_VAR: ${realLibraryPathVar}`);
            console.log(`  WASI_SYSROOT: ${wasiSysroot}`);
            console.log(`  CXX_wasm32-wasip1: ${wasiCxxWrapper}`);
            console.log(`  CXXFLAGS: ${wasiCxxFlags}`);
        }
    }

    // Set env vars
    for (const [key, value] of Object.entries(buildEnv)) {
        process.env[key] = value;
    }

    console.log(`  Version: ${tc.cfgVersion}`);
    console.log(`  Channel: ${tc.channel}`);
    console.log(`  Runtime: ${RUST_RUNTIME}`);
    console.log(`  Backend: ${RUST_CODEGEN_BACKEND}`);
    console.log(`  Target : ${RUST_TARGET}`);
    const rustcMainManifest = path.join('compiler', 'rustc', 'Cargo.toml');
    const buildCmd = `rustup run ${tc.toolchain} cargo build --manifest-path ${rustcMainManifest} --target ${RUST_TARGET} --features ${RUST_CODEGEN_BACKEND} --release`;
    console.log(`  Building with: ${buildCmd}`);

    shell.exec(buildCmd);

    // Clean up env
    for (const key of Object.keys(buildEnv)) {
        delete process.env[key];
    }

    // Verify the output exists
    // Emscripten produces rustc_main.wasm (underscore) + rustc-main.js (hyphen)
    const wasmCandidates = ['rustc-main.wasm', 'rustc_main.wasm'];
    let wasmOutput: string | null = null;
    for (const c of wasmCandidates) {
        const p = path.join(srcPath, 'target', RUST_TARGET, 'release', c);
        if (fs.existsSync(p)) { wasmOutput = p; break; }
    }
    if (!wasmOutput) {
        console.error(`ERROR: rustc wasm not found at ${path.join(srcPath, 'target', RUST_TARGET, 'release')}/{${wasmCandidates.join(',')}}`);
        // List what IS there to help debug
        const releaseDir = path.join(srcPath, 'target', RUST_TARGET, 'release');
        if (fs.existsSync(releaseDir)) {
            const files = fs.readdirSync(releaseDir).filter(f => !f.startsWith('.'));
            console.error(`  Files in release dir: ${files.join(', ')}`);
        }
        process.exit(1);
    }

    const size = fs.statSync(wasmOutput).size;
    console.log(`Cross-compilation complete: ${(size / (1024 * 1024)).toFixed(1)}MB`);
    shell.cd(RUST_DIR);
}

/**
 * Apply patches to the Rust source tree needed for WASI compilation.
 */
function applyWasiPatches(srcPath: string) {
    const ensureLineInFeatures = (cargoTomlPath: string, featureLine: string, beforeFeatureLine: string, label: string) => {
        if (!fs.existsSync(cargoTomlPath)) return;
        let content = fs.readFileSync(cargoTomlPath, 'utf-8');
        if (content.includes(featureLine)) return;

        if (content.includes(beforeFeatureLine)) {
            content = content.replace(beforeFeatureLine, `${featureLine}\n${beforeFeatureLine}`);
        } else if (content.includes('[features]')) {
            content = content.replace('[features]\n', `[features]\n${featureLine}\n`);
        } else {
            content += `\n[features]\n${featureLine}\n`;
        }

        fs.writeFileSync(cargoTomlPath, content);
        console.log(`  Patched ${label}: added ${featureLine}`);
    };

    const ensureLineInDependencies = (cargoTomlPath: string, dependencyLine: string, beforeDependencyLine: string, label: string) => {
        if (!fs.existsSync(cargoTomlPath)) return;
        let content = fs.readFileSync(cargoTomlPath, 'utf-8');
        if (content.includes(dependencyLine)) return;

        if (content.includes(beforeDependencyLine)) {
            content = content.replace(beforeDependencyLine, `${dependencyLine}\n${beforeDependencyLine}`);
        } else if (content.includes('[dependencies]')) {
            content = content.replace('[dependencies]\n', `[dependencies]\n${dependencyLine}\n`);
        } else {
            content += `\n[dependencies]\n${dependencyLine}\n`;
        }

        fs.writeFileSync(cargoTomlPath, content);
        console.log(`  Patched ${label}: added ${dependencyLine}`);
    };

    // Patch 0a: Ensure rustc-main exposes cranelift feature.
    const rustcMainCargo = path.join(srcPath, 'compiler', 'rustc', 'Cargo.toml');
    ensureLineInFeatures(
        rustcMainCargo,
        "cranelift = ['rustc_driver_impl/cranelift']",
        "llvm = ['rustc_driver_impl/llvm']",
        'compiler/rustc/Cargo.toml',
    );

    // Patch 0b: Ensure rustc_driver_impl forwards cranelift feature.
    const rustcDriverImplCargo = path.join(srcPath, 'compiler', 'rustc_driver_impl', 'Cargo.toml');
    ensureLineInFeatures(
        rustcDriverImplCargo,
        "cranelift = ['rustc_interface/cranelift']",
        "llvm = ['rustc_interface/llvm']",
        'compiler/rustc_driver_impl/Cargo.toml',
    );

    // Patch 0c: Ensure rustc_interface declares + exposes rustc_codegen_cranelift.
    const rustcInterfaceCargo = path.join(srcPath, 'compiler', 'rustc_interface', 'Cargo.toml');
    ensureLineInDependencies(
        rustcInterfaceCargo,
        'rustc_codegen_cranelift = { path = "../rustc_codegen_cranelift", optional = true }',
        'rustc_codegen_llvm = { path = "../rustc_codegen_llvm", optional = true }',
        'compiler/rustc_interface/Cargo.toml',
    );
    ensureLineInFeatures(
        rustcInterfaceCargo,
        "cranelift = ['dep:rustc_codegen_cranelift']",
        "llvm = ['dep:rustc_codegen_llvm']",
        'compiler/rustc_interface/Cargo.toml',
    );

    // Patch 1: rustc_driver crate-type from dylib to rlib
    const driverCargo = path.join(srcPath, 'compiler', 'rustc_driver', 'Cargo.toml');
    if (fs.existsSync(driverCargo)) {
        let content = fs.readFileSync(driverCargo, 'utf-8');
        if (content.includes('"dylib"') && !content.includes('"rlib"')) {
            content = content.replace('"dylib"', '"rlib"');
            fs.writeFileSync(driverCargo, content);
            console.log('  Patched rustc_driver/Cargo.toml: dylib → rlib');
        }
    }

    // Patch 1b: rustc_codegen_cranelift crate-type from dylib to rlib for WASI.
    const cgClifCargo = path.join(srcPath, 'compiler', 'rustc_codegen_cranelift', 'Cargo.toml');
    if (fs.existsSync(cgClifCargo)) {
        let content = fs.readFileSync(cgClifCargo, 'utf-8');
        if (content.includes('crate-type = ["dylib"]')) {
            content = content.replace('crate-type = ["dylib"]', 'crate-type = ["rlib"]');
            fs.writeFileSync(cgClifCargo, content);
            console.log('  Patched rustc_codegen_cranelift/Cargo.toml: dylib → rlib');
        }

        // Ensure in-tree rustc crates are declared as direct dependencies.
        // On stable tarballs, rustc_codegen_cranelift may rely on rustc_private
        // extern crates without Cargo deps, which fails in this cross-compile mode.
        const rustcDeps = [
            'jobserver = "0.1"',
            'rustc_abi = { path = "../rustc_abi" }',
            'rustc_ast = { path = "../rustc_ast" }',
            'rustc_codegen_ssa = { path = "../rustc_codegen_ssa" }',
            'rustc_data_structures = { path = "../rustc_data_structures" }',
            'rustc_errors = { path = "../rustc_errors" }',
            'rustc_fs_util = { path = "../rustc_fs_util" }',
            'rustc_hir = { path = "../rustc_hir" }',
            'rustc_incremental = { path = "../rustc_incremental" }',
            'rustc_index = { path = "../rustc_index" }',
            'rustc_metadata = { path = "../rustc_metadata" }',
            'rustc_middle = { path = "../rustc_middle" }',
            'rustc_session = { path = "../rustc_session" }',
            'rustc_span = { path = "../rustc_span" }',
            'rustc_target = { path = "../rustc_target" }',
        ];
        for (const depLine of rustcDeps) {
            ensureLineInDependencies(
                cgClifCargo,
                depLine,
                'cranelift-codegen = { version = "0.113.0", default-features = false, features = ["std", "unwind", "all-native-arch"] }',
                'compiler/rustc_codegen_cranelift/Cargo.toml',
            );
        }
    }

    // Patch 1c: Remove extern crate rustc_driver from rustc_codegen_cranelift
    // to avoid cyclic dependency through rustc-main -> rustc_driver_impl ->
    // rustc_interface -> rustc_codegen_cranelift -> rustc_driver.
    const cgClifLibRs = path.join(srcPath, 'compiler', 'rustc_codegen_cranelift', 'src', 'lib.rs');
    if (fs.existsSync(cgClifLibRs)) {
        let content = fs.readFileSync(cgClifLibRs, 'utf-8');
        if (content.includes('extern crate rustc_driver;')) {
            content = content.replace(/\nextern crate rustc_driver;\n/, '\n');
            console.log('  Patched rustc_codegen_cranelift/src/lib.rs: removed extern crate rustc_driver');
        }

        // If rustc_driver is removed to break dependency cycles, provide local
        // fallback definitions for rustc diagnostic macros used throughout
        // rustc_codegen_cranelift. This keeps compilation working without
        // reintroducing rustc_driver in the dependency graph.
        if (!content.includes('macro_rules! bug') || !content.includes('macro_rules! span_bug')) {
            const insertionPoint = 'use std::any::Any;';
            if (content.includes(insertionPoint)) {
                const fallbackMacros =
                    '// WASI-PATCH: fallback diagnostic macros when rustc_driver is unavailable\n' +
                    '#[allow(unused_macros)]\n' +
                    'macro_rules! bug {\n' +
                    '    ($($arg:tt)*) => ({\n' +
                    '        panic!($($arg)*);\n' +
                    '    });\n' +
                    '}\n' +
                    '\n' +
                    '#[allow(unused_macros)]\n' +
                    'macro_rules! span_bug {\n' +
                    '    ($span:expr, $($arg:tt)*) => ({\n' +
                    '        let _ = &$span;\n' +
                    '        panic!($($arg)*);\n' +
                    '    });\n' +
                    '}\n\n';
                content = content.replace(insertionPoint, `${fallbackMacros}${insertionPoint}`);
                console.log('  Patched rustc_codegen_cranelift/src/lib.rs: added fallback bug!/span_bug! macros');
            }
        }

        fs.writeFileSync(cgClifLibRs, content);
    }

    // Patch 2: Make libloading conditional on non-wasm targets
    const metadataCargo = path.join(srcPath, 'compiler', 'rustc_metadata', 'Cargo.toml');
    if (fs.existsSync(metadataCargo)) {
        let content = fs.readFileSync(metadataCargo, 'utf-8');
        if (content.includes('libloading') && !content.includes('target_family = "wasm"')) {
            // Move libloading from [dependencies] to conditional dependency
            content = content.replace(
                /^libloading\s*=.*$/m,
                '# libloading moved to non-wasm conditional dependency below'
            );
            if (!content.includes('[target.\'cfg(not(target_family = "wasm"))\'.dependencies]')) {
                content += `\n[target.'cfg(not(target_family = "wasm"))'.dependencies]\nlibloading = "0.8.0"\n`;
            }
            fs.writeFileSync(metadataCargo, content);
            console.log('  Patched rustc_metadata/Cargo.toml: conditional libloading');
        }
    }

    // Patch 3: Wrap dylib loading functions in cfg(not(wasm)) in creader.rs
    // The libloading crate is not available on wasm32 (gated in Cargo.toml by Patch 2).
    // We must also gate the source code that references it.
    const creaderRs = path.join(srcPath, 'compiler', 'rustc_metadata', 'src', 'creader.rs');
    if (fs.existsSync(creaderRs)) {
        let content = fs.readFileSync(creaderRs, 'utf-8');
        if (content.includes('libloading') && !content.includes('#[cfg(target_family = "wasm")]')) {
            // Gate attempt_load_dylib with #[cfg(not(target_family = "wasm"))]
            content = content.replace(
                'fn attempt_load_dylib(path: &Path) -> Result<libloading::Library, libloading::Error> {',
                '#[cfg(not(target_family = "wasm"))]\nfn attempt_load_dylib(path: &Path) -> Result<libloading::Library, libloading::Error> {'
            );

            // Gate load_dylib with #[cfg(not(target_family = "wasm"))]
            content = content.replace(
                'fn load_dylib(path: &Path, max_attempts: usize) -> Result<libloading::Library, String> {',
                '#[cfg(not(target_family = "wasm"))]\nfn load_dylib(path: &Path, max_attempts: usize) -> Result<libloading::Library, String> {'
            );

            // Gate the original load_symbol_from_dylib and add a WASI stub
            content = content.replace(
                'pub unsafe fn load_symbol_from_dylib<T: Copy>(\n    path: &Path,\n    sym_name: &str,\n) -> Result<T, DylibError> {',
                '#[cfg(not(target_family = "wasm"))]\npub unsafe fn load_symbol_from_dylib<T: Copy>(\n    path: &Path,\n    sym_name: &str,\n) -> Result<T, DylibError> {'
            );

            // Add WASI stub for load_symbol_from_dylib after the closing brace of the original
            // Find the end of load_symbol_from_dylib: "    Ok(*sym)\n}"
            const wasiStub =
                '\n\n#[cfg(target_family = "wasm")]\npub unsafe fn load_symbol_from_dylib<T: Copy>(\n' +
                '    path: &Path,\n    _sym_name: &str,\n) -> Result<T, DylibError> {\n' +
                '    Err(DylibError::DlOpen(path.display().to_string(), "dynamic loading is not supported on WASI".to_string()))\n' +
                '}';

            // Insert the stub after the function that ends with Ok(*sym) + newline + }
            // The function ends with "    Ok(*sym)\n}" — find the last occurrence after load_symbol_from_dylib
            const loadSymIdx = content.indexOf('pub unsafe fn load_symbol_from_dylib');
            if (loadSymIdx !== -1) {
                // Find the closing brace of this function
                const afterFn = content.indexOf('\n}\n', loadSymIdx);
                if (afterFn !== -1) {
                    content = content.slice(0, afterFn + 3) + wasiStub + content.slice(afterFn + 3);
                }
            }

            fs.writeFileSync(creaderRs, content);
            console.log('  Patched rustc_metadata/src/creader.rs: cfg-gated dylib loading for WASI');
        } else if (content.includes('#[cfg(target_family = "wasm")]')) {
            console.log('  creader.rs: WASI dylib-loading patch already applied.');
        }
    }

    // Patch 4: Skip thread spawning on WASI in run_in_thread_with_globals
    // WASI doesn't support threads, so rustc panics at spawn_scoped().unwrap().
    // Add #[cfg(target_os = "wasi")] guard to call the closure directly.
    const utilRs = path.join(srcPath, 'compiler', 'rustc_interface', 'src', 'util.rs');
    if (fs.existsSync(utilRs)) {
        let content = fs.readFileSync(utilRs, 'utf-8');
        if (content.includes('spawn_scoped') && !content.includes('#[cfg(target_os = "wasi")]')) {
            // Replace the thread-spawning body with a cfg-guarded version
            const oldBody =
                `    // The "thread pool" is a single spawned thread in the non-parallel\n` +
                `    // compiler. We run on a spawned thread instead of the main thread (a) to\n` +
                `    // provide control over the stack size, and (b) to increase similarity with\n` +
                `    // the parallel compiler, in particular to ensure there is no accidental\n` +
                `    // sharing of data between the main thread and the compilation thread\n` +
                `    // (which might cause problems for the parallel compiler).\n` +
                `    let builder = thread::Builder::new().name("rustc".to_string()).stack_size(thread_stack_size);`;

            const newBody =
                `    // WASI doesn't support threads — run the compiler directly on the main thread.\n` +
                `    // SessionGlobals is !Send but that's fine: we create and use it on the same thread.\n` +
                `    #[cfg(target_os = "wasi")]\n` +
                `    {\n` +
                `        let _ = thread_stack_size;\n` +
                `        return rustc_span::create_session_globals_then(\n` +
                `            edition,\n` +
                `            Some(sm_inputs),\n` +
                `            || f(CurrentGcx::new()),\n` +
                `        );\n` +
                `    }\n` +
                `\n` +
                `    // The "thread pool" is a single spawned thread in the non-parallel\n` +
                `    // compiler. We run on a spawned thread instead of the main thread (a) to\n` +
                `    // provide control over the stack size, and (b) to increase similarity with\n` +
                `    // the parallel compiler, in particular to ensure there is no accidental\n` +
                `    // sharing of data between the main thread and the compilation thread\n` +
                `    // (which might cause problems for the parallel compiler).\n` +
                `    #[cfg(not(target_os = "wasi"))]\n` +
                `    {\n` +
                `    let builder = thread::Builder::new().name("rustc".to_string()).stack_size(thread_stack_size);`;

            if (content.includes(oldBody)) {
                content = content.replace(oldBody, newBody);
                // Also wrap the closing of thread::scope
                content = content.replace(
                    `        }\n    })\n}`,
                    `        }\n    })\n    }\n}`
                );
                fs.writeFileSync(utilRs, content);
                console.log('  Patched rustc_interface/src/util.rs: skip thread spawn on WASI');
            } else {
                console.log('  WARNING: util.rs thread-spawn pattern not found (may already be patched).');
            }
        } else if (content.includes('#[cfg(target_os = "wasi")]')) {
            console.log('  util.rs: WASI thread patch already applied.');
        }
    }

    // Patch 5: Disable jobserver helper thread on WASI
    // rustc_data_structures::jobserver::Proxy::new() spawns a helper thread,
    // which fails on WASI with ENOTSUP.
    const jobserverRs = path.join(srcPath, 'compiler', 'rustc_data_structures', 'src', 'jobserver.rs');
    if (fs.existsSync(jobserverRs)) {
        let content = fs.readFileSync(jobserverRs, 'utf-8');
        if (content.includes('failed to create helper thread') && !content.includes('WASI doesn\'t support native threads. Skip helper thread creation.')) {
            content = content.replace(
                `        let proxy_ = Arc::clone(&proxy);`,
                `        #[cfg(target_os = "wasi")]\n` +
                `        {\n` +
                `            // WASI doesn't support native threads. Skip helper thread creation.\n` +
                `            return proxy;\n` +
                `        }\n` +
                `\n` +
                `        #[cfg(not(target_os = "wasi"))]\n` +
                `        {\n` +
                `        let proxy_ = Arc::clone(&proxy);`
            );

            content = content.replace(
                `        proxy.helper.set(helper).unwrap();\n        proxy`,
                `        proxy.helper.set(helper).unwrap();\n        proxy\n        }`
            );

            content = content.replace(
                `    pub fn acquire_thread(&self) {\n        let mut data = self.data.lock();`,
                `    pub fn acquire_thread(&self) {\n` +
                `        #[cfg(target_os = "wasi")]\n` +
                `        {\n` +
                `            return;\n` +
                `        }\n` +
                `\n` +
                `        let mut data = self.data.lock();`
            );

            content = content.replace(
                `    pub fn release_thread(&self) {\n        let mut data = self.data.lock();`,
                `    pub fn release_thread(&self) {\n` +
                `        #[cfg(target_os = "wasi")]\n` +
                `        {\n` +
                `            return;\n` +
                `        }\n` +
                `\n` +
                `        let mut data = self.data.lock();`
            );

            fs.writeFileSync(jobserverRs, content);
            console.log('  Patched rustc_data_structures/src/jobserver.rs: disable helper thread on WASI');
        } else if (content.includes('WASI doesn\'t support native threads. Skip helper thread creation.')) {
            console.log('  jobserver.rs: WASI helper-thread patch already applied.');
        }
    }

    // Patch 6: WASI sysroot fallback in rustc_session::filesearch
    // On WASI, current_dll_path() is unsupported. For browser execution,
    // use SYSROOT env var (set by tool-runner) and fallback to /usr/lib/rust.
    // In 1.84.0, the function is `get_or_default_sysroot()` with inner functions
    // `default_from_rustc_driver_dll()` and `from_env_args_next()`.
    const filesearchRs = path.join(srcPath, 'compiler', 'rustc_session', 'src', 'filesearch.rs');
    if (fs.existsSync(filesearchRs)) {
        let content = fs.readFileSync(filesearchRs, 'utf-8');
        const alreadyPatched = content.includes('// WASI-PATCH: early return for sysroot');

        if (!alreadyPatched && content.includes('pub fn get_or_default_sysroot()')) {
            // Gate from_env_args_next with cfg(not(wasi)) if not already gated
            if (content.includes('    fn from_env_args_next() -> Option<PathBuf> {') &&
                !content.includes('#[cfg(not(target_os = "wasi"))]\n    fn from_env_args_next')) {
                content = content.replace(
                    '    fn from_env_args_next() -> Option<PathBuf> {',
                    '    #[cfg(not(target_os = "wasi"))]\n    fn from_env_args_next() -> Option<PathBuf> {'
                );
            }

            // Gate default_from_rustc_driver_dll with cfg(not(wasi)) if not already gated
            if (content.includes('    fn default_from_rustc_driver_dll() -> Result<PathBuf, String> {') &&
                !content.includes('#[cfg(not(target_os = "wasi"))]\n    fn default_from_rustc_driver_dll')) {
                content = content.replace(
                    '    fn default_from_rustc_driver_dll() -> Result<PathBuf, String> {',
                    '    #[cfg(not(target_os = "wasi"))]\n    fn default_from_rustc_driver_dll() -> Result<PathBuf, String> {'
                );
            }

            // Replace the final return of get_or_default_sysroot with WASI-guarded version
            const oldReturn = '    Ok(from_env_args_next().unwrap_or(default_from_rustc_driver_dll()?))';
            if (content.includes(oldReturn)) {
                content = content.replace(
                    oldReturn,
                    '    // WASI-PATCH: early return for sysroot\n' +
                    '    #[cfg(target_os = "wasi")]\n' +
                    '    {\n' +
                    '        if let Some(sysroot) = env::var_os("SYSROOT") {\n' +
                    '            return Ok(PathBuf::from(sysroot));\n' +
                    '        }\n' +
                    '        return Ok(PathBuf::from("/usr/lib/rust"));\n' +
                    '    }\n' +
                    '\n' +
                    '    #[cfg(not(target_os = "wasi"))]\n' +
                    '    Ok(from_env_args_next().unwrap_or(default_from_rustc_driver_dll()?))'
                );
            }

            fs.writeFileSync(filesearchRs, content);
            console.log('  Patched rustc_session/src/filesearch.rs: WASI SYSROOT fallback');
        } else if (alreadyPatched) {
            console.log('  filesearch.rs: WASI SYSROOT patch already applied.');
        }

        // Always ensure a WASI stub for current_dll_path exists.
        // The original only has #[cfg(unix)] and #[cfg(windows)] versions.
        // sysroot_candidates() calls current_dll_path() unconditionally.
        if (!content.includes('#[cfg(target_os = "wasi")]\nfn current_dll_path()')) {
            content = content.replace(
                'pub fn sysroot_candidates()',
                '#[cfg(target_os = "wasi")]\nfn current_dll_path() -> Result<PathBuf, String> {\n    Err("current_dll_path is unsupported on WASI".to_string())\n}\n\npub fn sysroot_candidates()'
            );
            fs.writeFileSync(filesearchRs, content);
            console.log('  Patched filesearch.rs: added WASI current_dll_path stub');
        }
    }

    // Patch 6b: Avoid std::env::split_paths on WASI in rustc_target target search.
    // std::env::split_paths is unsupported on WASI and panics. rustc calls this in
    // Target::search while resolving --target values like wasm32-wasi.
    const targetSpecRs = path.join(srcPath, 'compiler', 'rustc_target', 'src', 'spec', 'mod.rs');
    if (fs.existsSync(targetSpecRs)) {
        let content = fs.readFileSync(targetSpecRs, 'utf-8');
        const alreadyPatched = content.includes('// WASI-PATCH: split RUST_TARGET_PATH without std::env::split_paths');
        const needle =
            '                for dir in env::split_paths(&target_path) {\n' +
            '                    let p = dir.join(&path);\n' +
            '                    if p.is_file() {\n' +
            '                        return load_file(&p);\n' +
            '                    }\n' +
            '                }\n';
        const replacement =
            '                // WASI-PATCH: split RUST_TARGET_PATH without std::env::split_paths\n' +
            '                #[cfg(target_os = "wasi")]\n' +
            '                for dir in target_path.to_string_lossy().split(\':\').filter(|d| !d.is_empty()) {\n' +
            '                    let p = Path::new(dir).join(&path);\n' +
            '                    if p.is_file() {\n' +
            '                        return load_file(&p);\n' +
            '                    }\n' +
            '                }\n' +
            '\n' +
            '                #[cfg(not(target_os = "wasi"))]\n' +
            '                for dir in env::split_paths(&target_path) {\n' +
            '                    let p = dir.join(&path);\n' +
            '                    if p.is_file() {\n' +
            '                        return load_file(&p);\n' +
            '                    }\n' +
            '                }\n';
        if (!alreadyPatched && content.includes(needle)) {
            content = content.replace(needle, replacement);
            fs.writeFileSync(targetSpecRs, content);
            console.log('  Patched rustc_target/src/spec/mod.rs: WASI-safe RUST_TARGET_PATH split');
        } else if (alreadyPatched) {
            console.log('  rustc_target/src/spec/mod.rs: WASI split_paths patch already applied.');
        }
    }

    // Patch 7: Avoid std::process::id() on WASI in rustc_driver_impl ICE path setup.
    // WASI has no process IDs; std::process::id() panics with "no pids on this platform".
    const driverImplLibRs = path.join(srcPath, 'compiler', 'rustc_driver_impl', 'src', 'lib.rs');
    if (fs.existsSync(driverImplLibRs)) {
        let content = fs.readFileSync(driverImplLibRs, 'utf-8');
        if (content.includes('let pid = std::process::id();') && !content.includes('#[cfg(target_os = "wasi")]\n        let pid: u32 = 0;')) {
            content = content.replace(
                '        let pid = std::process::id();',
                '        #[cfg(target_os = "wasi")]\n' +
                '        let pid: u32 = 0;\n' +
                '        #[cfg(not(target_os = "wasi"))]\n' +
                '        let pid = std::process::id();'
            );
            fs.writeFileSync(driverImplLibRs, content);
            console.log('  Patched rustc_driver_impl/src/lib.rs: WASI PID fallback');
        } else if (content.includes('#[cfg(target_os = "wasi")]\n        let pid: u32 = 0;')) {
            console.log('  rustc_driver_impl/src/lib.rs: WASI PID patch already applied.');
        }
    }

    // Patch 15: Fix WASI tmpdir handling in rustc_metadata/src/fs.rs
    // rustc uses a temp directory + rename for atomic metadata writes. In WASI/browser
    // this can fail (tmpdir lifecycle / directory removal quirks), causing fatal aborts.
    // We deterministically rewrite encode_and_write_metadata to use direct writes on WASI.
    const metadataFsRs = path.join(srcPath, 'compiler', 'rustc_metadata', 'src', 'fs.rs');
    if (fs.existsSync(metadataFsRs)) {
        let content = fs.readFileSync(metadataFsRs, 'utf-8');
        const fnStart = content.indexOf("pub fn encode_and_write_metadata(tcx: TyCtxt<'_>) -> (EncodedMetadata, bool) {");
        const nextFn = content.indexOf('\n#[cfg(not(target_os = "linux"))]\npub fn non_durable_rename');

        if (fnStart !== -1 && nextFn !== -1 && fnStart < nextFn) {
            const replacement = `// WASI-PATCH: avoid tmpdir-based atomic metadata writes on WASI
pub fn encode_and_write_metadata(tcx: TyCtxt<'_>) -> (EncodedMetadata, bool) {
    let out_filename = filename_for_metadata(tcx.sess, tcx.output_filenames(()));

    #[cfg(target_os = "wasi")]
    let (metadata_filename, metadata_tmpdir) = {
        // WASI/browser: avoid tempdir+rename flow (can fail due to directory handling).
        // Write metadata directly to the final path and keep it in place.
        // Do not use out_filename here (tempdir lifecycle issues) and avoid root path.
        // Use a stable writable location under /tmp in WASI.
        let metadata_filename = PathBuf::from("/tmp/lib.rmeta");

        let metadata_kind = tcx.metadata_kind();
        let need_metadata_file = tcx.sess.opts.output_types.contains_key(&OutputType::Metadata);

        // Fast-path for browser/WASI: when metadata output is not requested,
        // avoid file round-trips entirely.
        if metadata_kind == MetadataKind::None && !need_metadata_file {
            return (EncodedMetadata::empty(), false);
        }
        match metadata_kind {
            MetadataKind::None => {
                std::fs::File::create(&metadata_filename).unwrap_or_else(|err| {
                    tcx.dcx().emit_fatal(FailedCreateFile { filename: &metadata_filename, err });
                });
            }
            MetadataKind::Uncompressed | MetadataKind::Compressed => {
                encode_metadata(tcx, &metadata_filename);
            }
        };

        let _prof_timer = tcx.sess.prof.generic_activity("write_crate_metadata");
        if need_metadata_file {
            match out_filename {
                OutFileName::Real(ref path) => {
                    if let Err(err) = non_durable_rename(&metadata_filename, path) {
                        tcx.dcx().emit_fatal(FailedWriteError { filename: path.to_path_buf(), err });
                    }
                    if tcx.sess.opts.json_artifact_notifications {
                        tcx.dcx().emit_artifact_notification(path, "metadata");
                    }
                }
                OutFileName::Stdout => {
                    if out_filename.is_tty() {
                        tcx.dcx().emit_err(BinaryOutputToTty);
                    } else if let Err(err) = copy_to_stdout(&metadata_filename) {
                        tcx.dcx().emit_err(FailedCopyToStdout {
                            filename: metadata_filename.clone(),
                            err,
                        });
                    }
                }
            }
        }

        return (EncodedMetadata::empty(), false);
    };

    #[cfg(not(target_os = "wasi"))]
    let (metadata_filename, metadata_tmpdir) = {
        // To avoid races with another rustc process scanning the output directory,
        // we need to write the file somewhere else and atomically move it to its
        // final destination, with an fs::rename call. In order for the rename to
        // always succeed, the temporary file needs to be on the same filesystem,
        // which is why we create it inside the output directory specifically.
        let metadata_tmpdir = TempFileBuilder::new()
            .prefix("rmeta")
            .tempdir_in(out_filename.parent().unwrap_or_else(|| Path::new("")))
            .unwrap_or_else(|err| tcx.dcx().emit_fatal(FailedCreateTempdir { err }));
        let metadata_tmpdir = MaybeTempDir::new(metadata_tmpdir, tcx.sess.opts.cg.save_temps);
        let metadata_filename = metadata_tmpdir.as_ref().join(METADATA_FILENAME);

        // Always create a file at metadata_filename, even if we have nothing to write to it.
        // This simplifies the creation of the output out_filename when requested.
        let metadata_kind = tcx.metadata_kind();
        match metadata_kind {
            MetadataKind::None => {
                std::fs::File::create(&metadata_filename).unwrap_or_else(|err| {
                    tcx.dcx().emit_fatal(FailedCreateFile { filename: &metadata_filename, err });
                });
            }
            MetadataKind::Uncompressed | MetadataKind::Compressed => {
                encode_metadata(tcx, &metadata_filename);
            }
        };

        let _prof_timer = tcx.sess.prof.generic_activity("write_crate_metadata");

        // If the user requests metadata as output, rename metadata_filename
        // to the expected output out_filename. The match above should ensure
        // this file always exists.
        let need_metadata_file = tcx.sess.opts.output_types.contains_key(&OutputType::Metadata);
        let (metadata_filename, metadata_tmpdir) = if need_metadata_file {
            let filename = match out_filename {
                OutFileName::Real(ref path) => {
                    if let Err(err) = non_durable_rename(&metadata_filename, path) {
                        tcx.dcx().emit_fatal(FailedWriteError { filename: path.to_path_buf(), err });
                    }
                    path.clone()
                }
                OutFileName::Stdout => {
                    if out_filename.is_tty() {
                        tcx.dcx().emit_err(BinaryOutputToTty);
                    } else if let Err(err) = copy_to_stdout(&metadata_filename) {
                        tcx.dcx().emit_err(FailedCopyToStdout {
                            filename: metadata_filename.clone(),
                            err,
                        });
                    }
                    metadata_filename
                }
            };
            if tcx.sess.opts.json_artifact_notifications {
                tcx.dcx().emit_artifact_notification(out_filename.as_path(), "metadata");
            }
            (filename, None)
        } else {
            (metadata_filename, Some(metadata_tmpdir))
        };

        (metadata_filename, metadata_tmpdir)
    };

    // Load metadata back to memory: codegen may need to include it in object files.
    let metadata =
        EncodedMetadata::from_path(metadata_filename, metadata_tmpdir).unwrap_or_else(|err| {
            tcx.dcx().emit_fatal(FailedCreateEncodedMetadata { err });
        });

    let need_metadata_module = tcx.metadata_kind() == MetadataKind::Compressed;

    (metadata, need_metadata_module)
}
`;

            content = content.slice(0, fnStart) + replacement + content.slice(nextFn + 1);
            fs.writeFileSync(metadataFsRs, content);
            console.log('  Patched rustc_metadata/src/fs.rs: deterministic WASI metadata write path');
        } else {
            console.log('  WARNING: fs.rs encode_and_write_metadata pattern not found; patch 15 skipped.');
        }
    }

    // Patch 16: Add EncodedMetadata::empty() helper to bypass filesystem metadata roundtrip on WASI.
    const metadataEncoderRs = path.join(srcPath, 'compiler', 'rustc_metadata', 'src', 'rmeta', 'encoder.rs');
    if (fs.existsSync(metadataEncoderRs)) {
        let content = fs.readFileSync(metadataEncoderRs, 'utf-8');
        if (!content.includes('pub fn empty() -> Self')) {
            content = content.replace(
                'impl EncodedMetadata {\n    #[inline]\n    pub fn from_path(path: PathBuf, temp_dir: Option<MaybeTempDir>) -> std::io::Result<Self> {',
                'impl EncodedMetadata {\n    #[inline]\n    pub fn empty() -> Self {\n        Self { mmap: None, _temp_dir: None }\n    }\n\n    #[inline]\n    pub fn from_path(path: PathBuf, temp_dir: Option<MaybeTempDir>) -> std::io::Result<Self> {'
            );
            fs.writeFileSync(metadataEncoderRs, content);
            console.log('  Patched rustc_metadata/src/rmeta/encoder.rs: added EncodedMetadata::empty()');
        }
    }

    // Patch 17: Add cranelift arm in rustc_interface::get_codegen_backend.
    // This allows static in-tree backend resolution on WASI without dynamic loading.
    const utilRsCranelift = path.join(srcPath, 'compiler', 'rustc_interface', 'src', 'util.rs');
    if (fs.existsSync(utilRsCranelift)) {
        let content = fs.readFileSync(utilRsCranelift, 'utf-8');
        if (!content.includes('"cranelift" => rustc_codegen_cranelift::__rustc_codegen_backend')) {
            const needle =
                '            #[cfg(feature = "llvm")]\n' +
                '            "llvm" => rustc_codegen_llvm::LlvmCodegenBackend::new,\n';
            const replacement =
                '            #[cfg(feature = "cranelift")]\n' +
                '            "cranelift" => rustc_codegen_cranelift::__rustc_codegen_backend,\n' +
                '            #[cfg(feature = "llvm")]\n' +
                '            "llvm" => rustc_codegen_llvm::LlvmCodegenBackend::new,\n';
            if (content.includes(needle)) {
                content = content.replace(needle, replacement);
                fs.writeFileSync(utilRsCranelift, content);
                console.log('  Patched rustc_interface/src/util.rs: added cranelift backend arm');
            }
        }
    }

    // Patch 18: Disable cranelift helper thread on WASI.
    // WASI lacks native threads; requesting jobserver tokens via helper thread panics.
    const craneliftLimiterRs = path.join(srcPath, 'compiler', 'rustc_codegen_cranelift', 'src', 'concurrency_limiter.rs');
    if (fs.existsSync(craneliftLimiterRs)) {
        let content = fs.readFileSync(craneliftLimiterRs, 'utf-8');
        const hasWasiGuard = content.includes('#[cfg(target_os = "wasi")]\n        let helper_thread = None;');
        if (!hasWasiGuard) {
            const createHelperNeedle =
                '        let state_helper = state.clone();\n' +
                '        let available_token_condvar_helper = available_token_condvar.clone();\n' +
                '        let helper_thread = sess\n' +
                '            .jobserver\n' +
                '            .clone()\n' +
                '            .into_helper_thread(move |token| {\n' +
                '                let mut state = state_helper.lock().unwrap();\n' +
                '                match token {\n' +
                '                    Ok(token) => {\n' +
                '                        state.add_new_token(token);\n' +
                '                        available_token_condvar_helper.notify_one();\n' +
                '                    }\n' +
                '                    Err(err) => {\n' +
                '                        state.poison(format!("failed to acquire jobserver token: {}", err));\n' +
                '                        // Notify all threads waiting for a token to give them a chance to\n' +
                '                        // gracefully exit.\n' +
                '                        available_token_condvar_helper.notify_all();\n' +
                '                    }\n' +
                '                }\n' +
                '            })\n' +
                '            .unwrap();\n';

            const createHelperReplacement =
                '        #[cfg(target_os = "wasi")]\n' +
                '        let helper_thread = None;\n' +
                '\n' +
                '        #[cfg(not(target_os = "wasi"))]\n' +
                '        let helper_thread = {\n' +
                '            let state_helper = state.clone();\n' +
                '            let available_token_condvar_helper = available_token_condvar.clone();\n' +
                '            Some(sess\n' +
                '                .jobserver\n' +
                '                .clone()\n' +
                '                .into_helper_thread(move |token| {\n' +
                '                    let mut state = state_helper.lock().unwrap();\n' +
                '                    match token {\n' +
                '                        Ok(token) => {\n' +
                '                            state.add_new_token(token);\n' +
                '                            available_token_condvar_helper.notify_one();\n' +
                '                        }\n' +
                '                        Err(err) => {\n' +
                '                            state.poison(format!("failed to acquire jobserver token: {}", err));\n' +
                '                            // Notify all threads waiting for a token to give them a chance to\n' +
                '                            // gracefully exit.\n' +
                '                            available_token_condvar_helper.notify_all();\n' +
                '                        }\n' +
                '                    }\n' +
                '                })\n' +
                '                .unwrap())\n' +
                '        };\n';

            if (content.includes(createHelperNeedle)) {
                content = content.replace(createHelperNeedle, createHelperReplacement);
            }

            const requestNeedle = '            self.helper_thread.as_ref().unwrap().lock().unwrap().request_token();\n';
            const requestReplacement =
                '            if let Some(helper_thread) = self.helper_thread.as_ref() {\n' +
                '                helper_thread.lock().unwrap().request_token();\n' +
                '            }\n';
            if (content.includes(requestNeedle)) {
                content = content.replace(requestNeedle, requestReplacement);
            }

            const helperAssignNeedle = '            helper_thread: Some(Mutex::new(helper_thread)),\n';
            const helperAssignReplacement = '            helper_thread: helper_thread.map(Mutex::new),\n';
            if (content.includes(helperAssignNeedle)) {
                content = content.replace(helperAssignNeedle, helperAssignReplacement);
            }

            fs.writeFileSync(craneliftLimiterRs, content);
            console.log('  Patched rustc_codegen_cranelift/src/concurrency_limiter.rs: disabled helper thread on WASI');
        }

        // Ensure helper_thread type assignment always matches Option<Mutex<HelperThread>>,
        // including runs where the WASI guard patch is already present from earlier builds.
        const helperAssignNeedle = '            helper_thread: Some(Mutex::new(helper_thread)),\n';
        const helperAssignReplacement = '            helper_thread: helper_thread.map(Mutex::new),\n';
        if (content.includes(helperAssignNeedle)) {
            content = content.replace(helperAssignNeedle, helperAssignReplacement);
            fs.writeFileSync(craneliftLimiterRs, content);
            console.log('  Patched rustc_codegen_cranelift/src/concurrency_limiter.rs: fixed helper_thread Option type');
        }
    }

    // Patch 19: Map wasm32-wasip1 -> wasm32-wasi for Cranelift ISA lookup.
    // rustc target wasm32-wasip1 exists and has std libs, but target-lexicon/
    // cranelift ISA lookup may only recognize wasm32-wasi in this toolchain.
    // Keep rustc target as wasip1 while using wasi for ISA lookup only.
    const cgClifLibForTargetAlias = path.join(srcPath, 'compiler', 'rustc_codegen_cranelift', 'src', 'lib.rs');
    if (fs.existsSync(cgClifLibForTargetAlias)) {
        let content = fs.readFileSync(cgClifLibForTargetAlias, 'utf-8');
        const alreadyPatched = content.includes('// WASI-PATCH: map wasm32-wasip1 to wasm32-wasi for Cranelift ISA lookup');
        const needle =
            'fn target_triple(sess: &Session) -> target_lexicon::Triple {\n' +
            '    // FIXME(madsmtm): Use `sess.target.llvm_target` once target-lexicon supports unversioned macOS.\n' +
            '    // See <https://github.com/bytecodealliance/target-lexicon/pull/113>\n' +
            '    match versioned_llvm_target(sess).parse() {\n' +
            '        Ok(triple) => triple,\n' +
            '        Err(err) => sess.dcx().fatal(format!("target not recognized: {}", err)),\n' +
            '    }\n' +
            '}\n';
        const replacement =
            'fn target_triple(sess: &Session) -> target_lexicon::Triple {\n' +
            '    // FIXME(madsmtm): Use `sess.target.llvm_target` once target-lexicon supports unversioned macOS.\n' +
            '    // See <https://github.com/bytecodealliance/target-lexicon/pull/113>\n' +
            '    // WASI-PATCH: map wasm32-wasip1 to wasm32-wasi for Cranelift ISA lookup\n' +
            '    // while keeping rustc target/session semantics on wasm32-wasip1.\n' +
            '    let mut llvm_target = versioned_llvm_target(sess);\n' +
            '    if llvm_target.starts_with("wasm32-wasip1") {\n' +
            '        llvm_target = llvm_target.replacen("wasm32-wasip1", "wasm32-wasi", 1).into();\n' +
            '    }\n' +
            '    match llvm_target.parse() {\n' +
            '        Ok(triple) => triple,\n' +
            '        Err(err) => sess.dcx().fatal(format!("target not recognized: {}", err)),\n' +
            '    }\n' +
            '}\n';
        if (!alreadyPatched && content.includes(needle)) {
            content = content.replace(needle, replacement);
            fs.writeFileSync(cgClifLibForTargetAlias, content);
            console.log('  Patched rustc_codegen_cranelift/src/lib.rs: wasm32-wasip1 ISA alias');
        } else if (alreadyPatched) {
            console.log('  rustc_codegen_cranelift/src/lib.rs: wasm32-wasip1 ISA alias patch already applied.');
        }

        // If an older version of this patch was already applied, fix the Cow<str>
        // assignment type mismatch by adding `.into()`.
        const oldAliasLine = '        llvm_target = llvm_target.replacen("wasm32-wasip1", "wasm32-wasi", 1);';
        const newAliasLine = '        llvm_target = llvm_target.replacen("wasm32-wasip1", "wasm32-wasi", 1).into();';
        if (content.includes(oldAliasLine)) {
            content = content.replace(oldAliasLine, newAliasLine);
            fs.writeFileSync(cgClifLibForTargetAlias, content);
            console.log('  Patched rustc_codegen_cranelift/src/lib.rs: fixed llvm_target Cow conversion');
        }
    }
}

/**
 * When building for Emscripten, upgrade `#[cfg(target_os = "wasi")]` guards in
 * the patched Rust source to `#[cfg(target_family = "wasm")]` so they activate
 * on both wasm32-wasip1 and wasm32-unknown-emscripten.
 *
 * The current_dll_path() stub in filesearch.rs is kept as `target_os = "wasi"`
 * because Emscripten already provides a `#[cfg(unix)]` version (never called
 * at runtime since get_or_default_sysroot returns early).
 */
function upgradeWasiCfgForEmscripten(srcPath: string) {
    if (RUST_RUNTIME !== 'emscripten') return;
    console.log('>>> Upgrading cfg guards from target_os="wasi" to target_family="wasm" for Emscripten...');

    // Files where ALL target_os="wasi" guards should be upgraded
    const simpleFiles = [
        'compiler/rustc_interface/src/util.rs',
        'compiler/rustc_data_structures/src/jobserver.rs',
        'compiler/rustc_driver_impl/src/lib.rs',
        'compiler/rustc_target/src/spec/mod.rs',
        'compiler/rustc_metadata/src/fs.rs',
    ];

    for (const relPath of simpleFiles) {
        const file = path.join(srcPath, relPath);
        if (!fs.existsSync(file)) continue;
        let content = fs.readFileSync(file, 'utf-8');
        const original = content;
        content = content.replace(/target_os = "wasi"/g, 'target_family = "wasm"');
        if (content !== original) {
            fs.writeFileSync(file, content);
            console.log(`  Upgraded: ${relPath}`);
        }
    }

    // filesearch.rs: upgrade all EXCEPT the current_dll_path stub.
    // The stub has cfg(target_os = "wasi") on the line before "fn current_dll_path()".
    // On Emscripten the #[cfg(unix)] version compiles instead (but is never called).
    const filesearchRs = path.join(srcPath, 'compiler', 'rustc_session', 'src', 'filesearch.rs');
    if (fs.existsSync(filesearchRs)) {
        let content = fs.readFileSync(filesearchRs, 'utf-8');
        const original = content;
        const lines = content.split('\n');
        for (let i = 0; i < lines.length; i++) {
            // Skip the cfg line that guards current_dll_path
            if (i + 1 < lines.length && lines[i + 1].includes('fn current_dll_path()')) {
                continue;
            }
            lines[i] = lines[i].replace(/target_os = "wasi"/g, 'target_family = "wasm"');
        }
        content = lines.join('\n');
        if (content !== original) {
            fs.writeFileSync(filesearchRs, content);
            console.log('  Upgraded: compiler/rustc_session/src/filesearch.rs (preserved current_dll_path stub)');
        }
    }

    // Patch vendored jobserver crate: Emscripten's libc doesn't expose pthread_kill.
    // The Helper::join() method uses pthread_kill to interrupt a helper thread blocked
    // in acquire(). On Emscripten there are no real threads, so we skip the signal entirely.
    const jobserverUnixRs = path.join(srcPath, 'vendor', 'jobserver-0.1.32', 'src', 'unix.rs');
    if (fs.existsSync(jobserverUnixRs)) {
        let content = fs.readFileSync(jobserverUnixRs, 'utf-8');
        const needle = 'libc::pthread_kill(self.thread.as_pthread_t() as _, libc::SIGUSR1);';
        if (content.includes(needle) && !content.includes('target_os = "emscripten"')) {
            // Replace the unsafe block containing pthread_kill with a cfg-guarded version
            content = content.replace(
                `            unsafe {\n` +
                `                // Ignore the return value here of \`pthread_kill\`,\n` +
                `                // apparently on OSX if you kill a dead thread it will\n` +
                `                // return an error, but on other platforms it may not. In\n` +
                `                // that sense we don't actually know if this will succeed or\n` +
                `                // not!\n` +
                `                libc::pthread_kill(self.thread.as_pthread_t() as _, libc::SIGUSR1);\n` +
                `            }`,
                `            #[cfg(not(target_os = "emscripten"))]\n` +
                `            unsafe {\n` +
                `                // Ignore the return value here of \`pthread_kill\`,\n` +
                `                // apparently on OSX if you kill a dead thread it will\n` +
                `                // return an error, but on other platforms it may not. In\n` +
                `                // that sense we don't actually know if this will succeed or\n` +
                `                // not!\n` +
                `                libc::pthread_kill(self.thread.as_pthread_t() as _, libc::SIGUSR1);\n` +
                `            }`
            );
            fs.writeFileSync(jobserverUnixRs, content);
            // Clear vendored crate checksum so cargo doesn't reject patched files
            const checksumFile = path.join(srcPath, 'vendor', 'jobserver-0.1.32', '.cargo-checksum.json');
            if (fs.existsSync(checksumFile)) {
                const checksumData = JSON.parse(fs.readFileSync(checksumFile, 'utf-8'));
                checksumData.files = {};
                fs.writeFileSync(checksumFile, JSON.stringify(checksumData));
            }
            console.log('  Patched vendor/jobserver/src/unix.rs: guarded pthread_kill for Emscripten');
        }
    }

    // Patch flock module: Emscripten's libc doesn't expose F_WRLCK/F_RDLCK/F_UNLCK.
    // Route Emscripten to the 'unsupported' flock implementation instead of the unix one.
    const flockRs = path.join(srcPath, 'compiler', 'rustc_data_structures', 'src', 'flock.rs');
    if (fs.existsSync(flockRs)) {
        let content = fs.readFileSync(flockRs, 'utf-8');
        if (!content.includes('target_os = "emscripten"')) {
            // Insert an emscripten match arm before the generic unix arm
            content = content.replace(
                '    cfg(unix) => {\n' +
                '        mod unix;\n' +
                '        use unix as imp;\n' +
                '    }',
                '    cfg(target_os = "emscripten") => {\n' +
                '        mod unsupported;\n' +
                '        use unsupported as imp;\n' +
                '    }\n' +
                '    cfg(unix) => {\n' +
                '        mod unix;\n' +
                '        use unix as imp;\n' +
                '    }'
            );
            fs.writeFileSync(flockRs, content);
            console.log('  Patched rustc_data_structures/src/flock.rs: route Emscripten to unsupported');
        }
    }

    // Patch vendored object crate: on wasm32-unknown-emscripten, both cfg(unix) and
    // cfg(target_arch = "wasm32") are true, causing duplicate NativeFile type definitions.
    // Add not(target_arch = "wasm32") to the unix ELF NativeFile definitions.
    const objectReadMod = path.join(srcPath, 'vendor', 'object-0.36.5', 'src', 'read', 'mod.rs');
    if (fs.existsSync(objectReadMod)) {
        let content = fs.readFileSync(objectReadMod, 'utf-8');
        if (!content.includes('not(target_arch = "wasm32")')) {
            // Add not(target_arch = "wasm32") to unix ELF NativeFile definitions
            content = content.replace(
                '#[cfg(all(\n    unix,\n    not(target_os = "macos"),\n    target_pointer_width = "32",\n    feature = "elf"\n))]\npub type NativeFile<\'data, R = &\'data [u8]> = elf::ElfFile32<\'data, crate::endian::Endianness, R>;',
                '#[cfg(all(\n    unix,\n    not(target_os = "macos"),\n    not(target_arch = "wasm32"),\n    target_pointer_width = "32",\n    feature = "elf"\n))]\npub type NativeFile<\'data, R = &\'data [u8]> = elf::ElfFile32<\'data, crate::endian::Endianness, R>;'
            );
            content = content.replace(
                '#[cfg(all(\n    unix,\n    not(target_os = "macos"),\n    target_pointer_width = "64",\n    feature = "elf"\n))]\npub type NativeFile<\'data, R = &\'data [u8]> = elf::ElfFile64<\'data, crate::endian::Endianness, R>;',
                '#[cfg(all(\n    unix,\n    not(target_os = "macos"),\n    not(target_arch = "wasm32"),\n    target_pointer_width = "64",\n    feature = "elf"\n))]\npub type NativeFile<\'data, R = &\'data [u8]> = elf::ElfFile64<\'data, crate::endian::Endianness, R>;'
            );
            fs.writeFileSync(objectReadMod, content);
            // Clear vendored crate checksum
            const checksumFile = path.join(srcPath, 'vendor', 'object-0.36.5', '.cargo-checksum.json');
            if (fs.existsSync(checksumFile)) {
                const checksumData = JSON.parse(fs.readFileSync(checksumFile, 'utf-8'));
                checksumData.files = {};
                fs.writeFileSync(checksumFile, JSON.stringify(checksumData));
            }
            console.log('  Patched vendor/object/src/read/mod.rs: excluded wasm32 from unix NativeFile');
        }
    }
}

// ════════════════════════════════════════════════════════════════════════════
//  Step 6: Deploy artifacts to Emception sysroot
// ════════════════════════════════════════════════════════════════════════════

function moveArtifacts() {
    console.log('>>> Moving artifacts to sysroot...');
    const srcPath = path.join(RUST_DIR, RUST_SRC_DIR);

    shell.mkdir('-p', SYSROOT_RUST);

    if (RUST_RUNTIME === 'emscripten') {
        // ── Emscripten output: .js + .wasm pair ──
        // cargo + emcc produces rustc-main.js (ES6 module) + rustc_main.wasm
        // Note: emcc output filename derives from the Cargo binary name.
        // The .js file may be named rustc-main.js or rustc_main.js depending on cargo/emcc version.
        const targetDir = path.join(srcPath, 'target', RUST_TARGET, 'release');
        const candidates = ['rustc-main.js', 'rustc_main.js'];
        let jsSrc: string | null = null;
        for (const c of candidates) {
            const p = path.join(targetDir, c);
            if (fs.existsSync(p)) { jsSrc = p; break; }
        }
        if (!jsSrc) {
            console.error(`ERROR: Emscripten JS glue not found at ${targetDir}/{${candidates.join(',')}}`);
            process.exit(1);
        }

        // Find corresponding .wasm file
        // Emscripten may use underscores in the wasm filename even when JS uses hyphens
        const jsBase = path.basename(jsSrc, '.js');
        const wasmCandidates = [
            `${jsBase}.wasm`,
            `${jsBase.replace(/-/g, '_')}.wasm`,
        ];
        let wasmSrc: string | null = null;
        for (const wc of wasmCandidates) {
            const p = path.join(targetDir, wc);
            if (fs.existsSync(p)) { wasmSrc = p; break; }
        }
        if (!wasmSrc) {
            console.error(`ERROR: Emscripten WASM binary not found. Tried: ${wasmCandidates.join(', ')} in ${targetDir}`);
            const files = fs.readdirSync(targetDir).filter(f => f.endsWith('.wasm') || f.endsWith('.js'));
            console.error(`  Available: ${files.join(', ')}`);
            process.exit(1);
        }

        // Deploy as rustc.mjs + rustc.wasm (matching clang.mjs + clang.wasm convention)
        const mjsDst = path.join(SYSROOT_RUST, '..', 'rustc.mjs');
        const wasmDst = path.join(SYSROOT_RUST, '..', 'rustc.wasm');

        console.log(`Copying Emscripten artifacts to sysroot...`);
        shell.cp('-f', jsSrc, mjsDst);
        shell.cp('-f', wasmSrc, wasmDst);

        const mjsSize = fs.statSync(mjsDst).size;
        const wasmSize = fs.statSync(wasmDst).size;
        console.log(`  rustc.mjs:  ${(mjsSize / 1024).toFixed(0)}KB`);
        console.log(`  rustc.wasm: ${(wasmSize / (1024 * 1024)).toFixed(1)}MB`);

        // Also keep a copy in the rust sysroot dir for consistency
        shell.cp('-f', wasmSrc, path.join(SYSROOT_RUST, 'rustc.wasm'));
    } else {
        // ── WASI output: single .wasm binary ──
        const wasmSrc = path.join(srcPath, 'target', RUST_TARGET, 'release', 'rustc-main.wasm');
        const wasmDst = path.join(SYSROOT_RUST, 'rustc.wasm');

        if (fs.existsSync(wasmSrc)) {
            console.log('Copying rustc.wasm to sysroot...');
            shell.cp('-f', wasmSrc, wasmDst);
            const size = fs.statSync(wasmDst).size;
            console.log(`  rustc.wasm: ${(size / (1024 * 1024)).toFixed(1)}MB`);
        } else {
            console.error(`ERROR: rustc-main.wasm not found at ${wasmSrc}`);
            process.exit(1);
        }
    }

    // rlibs are already copied by copyWasiRlibs(), verify they exist
    const dstRlibs = path.join(SYSROOT_RUST, 'lib', 'rustlib', USER_PROGRAM_TARGET, 'lib');
    if (fs.existsSync(dstRlibs)) {
        const rlibs = shell.ls(path.join(dstRlibs, '*.rlib'));
        console.log(`  sysroot: ${rlibs.length} rlibs present for ${USER_PROGRAM_TARGET}`);
    } else {
        console.error(`WARNING: ${USER_PROGRAM_TARGET} sysroot rlibs not found.`);
    }

    console.log(`Artifacts deployed to ${SYSROOT_RUST}`);
}

// ════════════════════════════════════════════════════════════════════════════
//  Main
// ════════════════════════════════════════════════════════════════════════════

async function main() {
    console.log(`=== Building rustc.wasm (${RUST_CODEGEN_BACKEND} backend, ${RUST_RUNTIME} runtime) ===`);
    console.log(`Host triple : ${HOST_TRIPLE}`);
    console.log(`Rust version: ${RUST_VERSION} (${RUST_CHANNEL})`);
    console.log(`Rust source : ${RUST_USE_GIT ? 'git clone' : 'tarball'}`);
    console.log(`Backend     : ${RUST_CODEGEN_BACKEND}`);
    console.log(`Runtime     : ${RUST_RUNTIME}`);
    console.log(`Target      : ${RUST_TARGET}`);
    console.log(`User target : ${USER_PROGRAM_TARGET}`);
    console.log(`Concurrency : ${CONCURRENCY}`);
    console.log();

    if (RUST_CODEGEN_BACKEND !== 'cranelift' && RUST_CODEGEN_BACKEND !== 'llvm') {
        console.error(`ERROR: Unsupported RUST_CODEGEN_BACKEND="${RUST_CODEGEN_BACKEND}". Use "cranelift" or "llvm".`);
        process.exit(1);
    }

    if (process.platform === 'win32' && !shell.which('bash')) {
        console.warn('Skipping Rust build on Windows (bash not found).');
        return;
    }

    // Verify Rust toolchain is available
    if (!shell.which('rustc') || !shell.which('cargo')) {
        console.error('ERROR: Rust toolchain not found.');
        console.error('Install Rust: curl --proto "=https" --tlsv1.2 -sSf https://sh.rustup.rs | sh');
        process.exit(1);
    }

    // Verify rustc.wasm host target is installed
    const targets = shell.exec('rustup target list --installed', { silent: true }).stdout;
    if (!targets.includes(RUST_TARGET)) {
        console.log(`Installing ${RUST_TARGET} target...`);
        shell.exec(`rustup target add ${RUST_TARGET}`);
    }

    if (USER_PROGRAM_TARGET !== RUST_TARGET && !targets.includes(USER_PROGRAM_TARGET)) {
        console.log(`Installing ${USER_PROGRAM_TARGET} target...`);
        shell.exec(`rustup target add ${USER_PROGRAM_TARGET}`);
    }

    setupSource();
    writeConfigToml();
    buildNativeStage1();
    copyWasiRlibs();
    crossCompileRustc();
    moveArtifacts();

    console.log();
    console.log(`=== rustc.wasm build complete (${RUST_RUNTIME} runtime) ===`);
    if (RUST_RUNTIME === 'emscripten') {
        console.log(`  rustc.mjs  : ${path.join(SYSROOT_RUST, '..', 'rustc.mjs')}`);
        console.log(`  rustc.wasm : ${path.join(SYSROOT_RUST, '..', 'rustc.wasm')}`);
    } else {
        console.log(`  rustc.wasm : ${path.join(SYSROOT_RUST, 'rustc.wasm')}`);
    }
    console.log(`  host libs  : ${path.join(SYSROOT_RUST, `lib/rustlib/${RUST_TARGET}/lib/`)}`);
    console.log(`  user libs  : ${path.join(SYSROOT_RUST, `lib/rustlib/${USER_PROGRAM_TARGET}/lib/`)}`);
}

main().catch(e => {
    console.error(e);
    process.exit(1);
});
