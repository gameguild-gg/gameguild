/**
 * Build Rust stdlib for wasm32-unknown-emscripten target and deploy to sysroot.
 * 
 * This script:
 * 1. Verifies wasm32-unknown-emscripten target is installed
 * 2. Builds core, alloc, std, panic_abort for WebAssembly
 * 3. Copies rlibs to sysroot
 * 4. Verifies all expected artifacts are present
 * 
 * Usage:
 *   npm run build:stdlib-wasm32
 *   or: tsx scripts/build-stdlib-wasm32.ts
 */

import { execSync, spawn } from 'child_process';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = path.resolve(__dirname, '..');
const SYSROOT = path.join(ROOT, 'sysroot');
const RUST_TARGET = 'wasm32-unknown-emscripten';
const WASM32_LIBDIR = path.join(SYSROOT, `usr/lib/rustlib/${RUST_TARGET}/lib`);

function log(msg: string) {
    console.log(`  ${msg}`);
}

function logHeader(msg: string) {
    console.log(`\n[${msg}]`);
}

function execShell(cmd: string, desc?: string): string {
    if (desc) log(`${desc}...`);
    try {
        return execSync(cmd, { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
    } catch (e: any) {
        console.error(`✗ Failed: ${desc || cmd}`);
        console.error(e.stderr || e.message);
        process.exit(1);
    }
}

async function main() {
    console.log(`\n=== Rust ${RUST_TARGET} Stdlib Build ===\n`);

    // Phase 1: Verify toolchain
    logHeader('1/4: Verifying Rust toolchain');

    const rustcSysroot = execShell('rustc --print sysroot', 'Getting rustc sysroot');
    log(`Rustc sysroot: ${rustcSysroot}`);

    let hasWasm32 = false;
    try {
        const targets = execShell('rustup target list --installed');
        hasWasm32 = targets.includes(RUST_TARGET);
    } catch { }

    if (!hasWasm32) {
        log(`${RUST_TARGET} not installed, adding...`);
        execShell(`rustup target add ${RUST_TARGET}`, `Installing ${RUST_TARGET}`);
    }
    log(`✓ ${RUST_TARGET} available`);

    // Phase 2: Build stdlib
    logHeader(`2/4: Building Rust stdlib for ${RUST_TARGET}`);
    log('This may take 20-30 minutes...\n');

    let rustSrc = path.join(rustcSysroot, 'lib/rustlib/src/rust');

    if (!fs.existsSync(rustSrc)) {
        log('Rust source not found, installing rust-src component...');
        execShell('rustup component add rust-src');
        rustSrc = path.join(rustcSysroot, 'lib/rustlib/src/rust');
    }

    const cargoToml = path.join(rustSrc, 'library/Cargo.toml');
    if (!fs.existsSync(cargoToml)) {
        console.error(`✗ Rust source not found at ${rustSrc}`);
        process.exit(1);
    }

    log(`Using Rust source: ${rustSrc}`);
    log('Building core, alloc, std with panic_abort...');

    // Run cargo build with streaming output
    const buildCmd = [
        'cargo', '+nightly', 'build',
        '--target', RUST_TARGET,
        '--release',
        '-Z', 'build-std=core,alloc,std,panic_abort',
        '-Z', 'build-std-features=panic_immediate_abort',
    ];

    const buildProcess = spawn(buildCmd[0], buildCmd.slice(1), {
        cwd: rustSrc,
        stdio: ['inherit', 'inherit', 'inherit'],
    });

    const buildExitCode = await new Promise<number>((resolve) => {
        buildProcess.on('close', resolve);
    });

    if (buildExitCode !== 0) {
        console.error(`✗ Build failed with exit code ${buildExitCode}`);
        process.exit(1);
    }

    log('✓ Stdlib build complete');

    // Phase 3: Deploy to sysroot
    logHeader('3/4: Deploying stdlib to sysroot');

    fs.mkdirSync(WASM32_LIBDIR, { recursive: true });

    const buildStdlibDir = path.join(rustSrc, `target/${RUST_TARGET}/release/deps`);
    if (!fs.existsSync(buildStdlibDir)) {
        console.error(`✗ Build output not found at ${buildStdlibDir}`);
        process.exit(1);
    }

    const rlibs = fs.readdirSync(buildStdlibDir).filter(f => f.endsWith('.rlib'));

    if (rlibs.length === 0) {
        console.error(`✗ No .rlib files found in ${buildStdlibDir}`);
        process.exit(1);
    }

    for (const rlib of rlibs) {
        const src = path.join(buildStdlibDir, rlib);
        const dst = path.join(WASM32_LIBDIR, rlib);
        fs.copyFileSync(src, dst);
        log(`  Copied ${rlib}`);
    }

    log('✓ Stdlib deployed');

    // Phase 4: Verify
    logHeader('4/4: Verifying deployment');

    const deployedRlibs = fs.readdirSync(WASM32_LIBDIR).filter(f => f.endsWith('.rlib'));

    if (deployedRlibs.length < 4) {
        console.error(`✗ Expected at least 4 rlibs, found: ${deployedRlibs.length}`);
        process.exit(1);
    }

    log(`✓ Found ${deployedRlibs.length} rlibs`);
    log('\nStdlib files:');

    for (const rlib of deployedRlibs) {
        const stat = fs.statSync(path.join(WASM32_LIBDIR, rlib));
        const sizeMB = (stat.size / 1024 / 1024).toFixed(2);
        log(`  ${rlib} (${sizeMB} MB)`);
    }

    const totalSize = deployedRlibs.reduce((sum, rlib) => {
        return sum + fs.statSync(path.join(WASM32_LIBDIR, rlib)).size;
    }, 0);

    log(`\nTotal size: ${(totalSize / 1024 / 1024).toFixed(2)} MB`);

    console.log('\n=== SUCCESS ===');
    console.log(`Rust ${RUST_TARGET} stdlib built and deployed to:\n  ${WASM32_LIBDIR}\n`);
    console.log('Next steps:');
    console.log('  1. npm run build:cdn');
    console.log('  2. npm run e2e:rust\n');
}

main().catch(err => {
    console.error('Build failed:', err.message);
    process.exit(1);
});
